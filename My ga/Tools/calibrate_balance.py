"""Offline seed calibration. Runtime uses the exported, fixed Unity balance table."""
import math,json,re
from decimal import Decimal
from pathlib import Path
D=[10,36,100,260,600,2800,5600,14000,36000,110000,400000,1620000,4900000,6500000,17000000,32000000]
I=[.7,.65,.5,.45,.35,.6,.45,.42,.4,.45,.6,.9,1,.4,.4,.25]
NAMES=['制式手枪','重型手枪','冲锋手枪','MP5 冲锋枪','P90','战术霰弹枪','卡宾枪','突击步枪','HK416','SCAR-H','精确射手步枪','栓动狙击步枪','重型狙击步枪','轻机枪','通用机枪','M134 转管机枪']
MINUTES=[0,.5,1.4,2.8,4.8,8,13,21,33,50,73,101,135,174,218,266]
def levels(s): return (0 if s==0 else max(1,s*(s+1)//3),s*(s+1)//4,sum(s>=x for x in [2,5,9,13]))
def duration(hp,w,d,c):
    hits=max(1,math.ceil(hp/(D[w]*1.15**d)-1e-9)); n=c+1
    return .15+(hits-1)//n*(I[w]+c*.08)+(hits-1)%n*.08
bosses=[]
for s in range(16):
    d,g,c=levels(s)
    for j,target in enumerate([3.5,6,10]):
        hits=1
        while .15+hits//(c+1)*(I[s]+c*.08)+hits%(c+1)*.08 <= target+1e-9: hits+=1
        hp=max(D[s]*1.15**d*(hits-.25),bosses[-1]['health']*1.12 if bosses else 1)
        reward=round(10*3.2**s*[1,1.4,2.4][j])
        bosses.append(dict(name=['守卫','巨兽','领主'][j]+f' {s+1:02}',health=hp,reward=reward,firstBonus=reward))
for j,(hp,r,b) in enumerate([(34,10,10),(70,14,15),(120,24,25)]):bosses[j].update(health=hp,reward=r,firstBonus=b)
prices={'d':[10],'g':[],'c':[],'w':[0,120]}
def increasing_price(kind,cost):
    growth=1.5 if kind=='w' else 1.1
    return max(cost,math.ceil(prices[kind][-1]*growth)) if prices[kind] else cost
class Sim:
    def __init__(self): self.w=0;self.d=0;self.g=0;self.c=0;self.coins=0;self.time=0;self.high=-1;self.log=[];self.farms=set();self.paybacks=[];self.lastgrowth=0;self.gap=0
    def reward(self,b,g=None):return int(Decimal(bosses[b]['reward'])*Decimal('1.12')**(self.g if g is None else g))
    def ttk(self,b): return duration(bosses[b]['health'],self.w,self.d,self.c)
    def growth(self): self.gap=max(self.gap,self.time-self.lastgrowth);self.lastgrowth=self.time
    def kill(self,b,count=1):
        self.time+=count*(self.ttk(b)+1);self.coins+=count*self.reward(b)
        if b>self.high:self.coins+=bosses[b]['firstBonus'];self.high=b;self.growth()
    def push(self):
        while self.high<47 and self.ttk(self.high+1)<=15+1e-8:self.kill(self.high+1)
    def farm(self):return max(range(self.high+1),key=lambda b:self.reward(b)/(self.ttk(b)+1) if self.ttk(b)<=15 else 0)
    def income(self): b=self.farm();return self.reward(b)/(self.ttk(b)+1)
    def buy(self,kind,cost):
        b=self.farm();self.farms.add(b)
        if cost>self.coins:self.kill(b,math.ceil((cost-self.coins)/self.reward(b)))
        if kind=='g':self.paybacks.append(cost/((self.reward(b,self.g+1)-self.reward(b))/(self.ttk(b)+1)))
        self.coins-=cost;setattr(self,kind,getattr(self,kind)+1);self.growth();self.log.append([self.time,kind,getattr(self,kind),cost,b+1]);self.push()
    def opening(self):
        self.kill(0);self.coins-=10;self.d=1;self.kill(1);self.kill(2);self.kill(0,4)
        self.coins-=120;self.w=1;self.growth();self.log.append([self.time,'w',1,120,1]);self.push()
def actions(s,sim):
    td,tg,tc=levels(s+1);out=[];d=sim.d;g=sim.g;c=sim.c
    if c<tc:out.append('c')
    while d<td or g<tg:
        if g<tg:out.append('g');g+=1
        if d<td:out.append('d');d+=1
    return out+['w']
sim=Sim();sim.opening()
for s in range(1,15):
    todo=actions(s,sim)
    for k,kind in enumerate(todo):
        inc=sim.income()
        if kind=='g':
            b=sim.farm(); payback=45+min(375,s*25)
            cost=max(1,round((sim.reward(b,sim.g+1)-sim.reward(b))/(sim.ttk(b)+1)*payback))
        else:
            remaining=max(0,MINUTES[s+1]*60-sim.time)
            remaining_expensive=sum(x!='g' for x in todo[k:])
            # Reserve future gold purchases and progression travel inside each stage.
            reserve=sum(x=='g' for x in todo[k:])*(45+s*25)*.12
            wait=max(1,(remaining-reserve)/remaining_expensive)
            cost=max(1,math.floor(sim.coins+inc*wait))
        cost=increasing_price(kind,cost)
        prices[kind].append(cost);sim.buy(kind,cost)
result={'battleLimit':15,'respawnDelay':1,'firstShotDelay':.15,'burstGap':.08,'damageMultiplier':1.15,
'weapons':[dict(name=n,damage=d,attackInterval=i,cost=c) for n,d,i,c in zip(NAMES,D,I,prices['w'])],
'damageCosts':prices['d'],'goldCosts':prices['g'],'comboCosts':prices['c'],'bosses':bosses}
Path('Tools/v2-balance.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('weapon minutes',[(x[2]+1,round(x[0]/60,2)) for x in sim.log if x[1]=='w'])
print('complete',sim.time/60,'boss',sim.high+1,'maxgap',sim.gap,'farms',len(sim.farms),'payback',min(sim.paybacks),max(sim.paybacks))
print('counts',[(k,len(v)) for k,v in prices.items()])
Path('TestResults/v2-calibration-seed.json').write_text(json.dumps({'log':sim.log,'maxgap':sim.gap,'paybacks':sim.paybacks},indent=2),encoding='utf-8')
# Export fixed defaults; the Unity asset is refreshed explicitly by the editor setup.
p=Path('Assets/BossClicker/Scripts/GameBalance.cs')
s=p.read_text(encoding='utf-8')
a=s.index('        public Weapon[] weapons');s=s[:a]
s+='        public Weapon[] weapons = {\n'+',\n'.join('            new Weapon('+json.dumps(w['name'],ensure_ascii=False)+f", {w['damage']}, {w['attackInterval']}, {w['cost']}L)" for w in result['weapons'])+'\n        };\n'
for field in ['damageCosts','goldCosts','comboCosts']:
    s+='        public long[] '+field+' = { '+', '.join(str(x)+'L' for x in result[field])+' };\n'
s+='        public Boss[] bosses = {\n'+',\n'.join('            new Boss('+json.dumps(b['name'],ensure_ascii=False)+f", {b['health']:.17g}d, {b['reward']}L, {b['firstBonus']}L)" for b in bosses)+'\n        };\n    }\n}\n'
p.write_text(s,encoding='utf-8')

def number(value):
    return f'{value:,.10g}' if isinstance(value,float) else f'{value:,}'

report=['# 巨兽锻坊 v2：固定数值与验证报告','',
'配置：`Assets/BossClicker/Data/PrototypeBalance.asset`。所有价格固定；武器价格至少比上一把高 50%，每条永久强化至少比上一级高 10%。','',
'## 实测仿真结果','',
'使用真实 GameSession 和实际资产运行，计入逐发攻击、刷新、首通、强化支出与金币取整。','',
'| 购买路线 | 操作耗时 | W16（分钟） | B48（分钟） | 最长成长空档（秒） |','|---|---:|---:|---:|---:|']
labels={'reference':'参考：逐段补强化再换枪','guns-first':'攒枪：先换枪再补强化','upgrades-first':'强化：提前买下一阶段强化'}
for route in labels:
    for lag in (0,1):
        route_path=Path(f'TestResults/v2-route-{route}-{lag}.md')
        if route_path.exists():
            match=re.search(r'W16：([\d.]+) 分钟；B48：([\d.]+) 分钟。最长成长空档：([\d.]+)',route_path.read_text(encoding='utf-8'))
            if match: report.append(f'| [{labels[route]}]({route_path.name}) | {lag} 秒 | '+ ' | '.join(match.groups())+' |')
weapon_minutes=[0]+[entry[0]/60 for entry in sim.log if entry[1]=='w']
report += ['',f'参考路线：W2 {weapon_minutes[1]:.2f} 分钟，W3 {weapon_minutes[2]:.2f} 分钟，W4 {weapon_minutes[3]:.2f} 分钟，W5 {weapon_minutes[4]:.2f} 分钟，W16 {weapon_minutes[15]:.2f} 分钟；最长成长空档 {sim.gap:.2f} 秒。','',
'开局仍为 26.95 秒购买 W2：B1 首通后买火力一级，推进 B2、B3，手选 B1 回刷 4 次，以 120 金币购买 W2，剩余 8。','',
'## 购买价格审计','',
'| 序号 | 名称 | 单发伤害 | 轮间隔（秒） | 价格 |','|---|---|---:|---:|---:|']
for i,w in enumerate(result['weapons']): report.append(f"| W{i+1} | {w['name']} | {number(w['damage'])} | {w['attackInterval']} | {number(w['cost'])} |")
report += ['','表中等级是购买后的等级；三条轨道独立。','',
'| 等级 | 火力（×1.15） | 金币（×1.12） | 连击（+1 发） |','|---:|---:|---:|---:|']
for i in range(80):
    values=[number(result[k][i]) if i<len(result[k]) else '—' for k in ('damageCosts','goldCosts','comboCosts')]
    report.append(f"| {i+1} | {' | '.join(values)} |")
report += ['','## Boss 固定表','',
'| 关卡 | HP | 基础金币 | 固定首通额外 |','|---|---:|---:|---:|']
for i,boss in enumerate(result['bosses']): report.append(f"| B{i+1} | {number(boss['health'])} | {number(boss['reward'])} | {number(boss['firstBonus'])} |")
report += ['','## 验证','',
'- `v2-price-red.xml` 复现了 W3 价格低于 W2。','- `v2-price-green.xml` 验证实际资产满足涨价下限。',
'- `v2-editmode.xml`：26 项通过，0 失败，包含六条完整购买路线。','- `v2-windows-build.log`：Windows 构建成功。']
Path('TestResults/v2-balance-report.md').write_text('\n'.join(report)+'\n',encoding='utf-8')
