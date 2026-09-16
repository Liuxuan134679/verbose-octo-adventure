# LOOTSHOT · Unity WebGL v4.1 数值测试原型

项目位置：`C:\Users\root\Documents\GitHub\verbose-octo-adventure\My ga`

Unity：`2022.3.51f1c1`

当前范围：8 把现代远程武器、24 个 Boss（12 组普通/强化形态）。本版集中验证按住连射、逐段强化门槛和后期回刷节奏。

## 当前玩法

- 主菜单负责选枪、选 Boss、购买武器、升级当前武器火力与弹量、升级全局金币收益。
- 火力和弹量分别按自身等级计价，使用现有价格表，互不抬高对方的升级价格。
- 武器按顺序逐把购买，金币足够即可购买下一把，无须先通关指定 Boss；购买后自动装备，Boss 关卡仍按通关进度开放。
- 点击“开始战斗”进入独立战斗界面；战斗期间不能购买、换枪或选关。
- 按下鼠标左键立即发射一颗，继续按住会按当前武器射速持续发射，松开后停止。八把枪从每秒 1.00 发逐渐提升到 2.00 发。
- 快速重复点击不能绕过射速；每次真实发射分别扣除弹药、生成子弹、造成命中并结算金币。
- 每次有效命中立即掉落金币；击败后再发放击败奖励，首次通关奖励固定且不受金币倍率影响。
- 弹药清空且所有在途子弹结算后，Boss 仍有生命则失败。已经获得的命中金币保留。
- 结算只有“返回主菜单”。失败后当前 Boss 不变；玩家强化后再次点击“开始战斗”。
- 首次击败最前沿 Boss 才推进并默认选择下一关。已通关 Boss 由玩家手动选择回刷。

## 开始试玩

在 Unity Hub 中打开本项目，载入 `Assets/Scenes/SampleScene.unity` 后点击 Play。

WebGL 成品位于 `Builds/WebGL/index.html`，不能直接双击它。双击项目根目录的 `Preview-WebGL.cmd` 会启动仅限本机的 HTTP 服务并打开浏览器；预览期间保持命令窗口开启，结束时按 `Ctrl+C`。

也可以在 Unity 中选择 `Boss Clicker > Build And Run WebGL Prototype`。出现 “Unable to parse ... Loading pre-compressed content via a file:// URL” 表示仍在直接打开文件，应使用上述两个入口。

## Inspector 手动调参

在 Project 窗口选择 `Assets/BossClicker/Data/PrototypeBalance.asset`。全局成长、8 把武器、金币强化价格和 24 个 Boss 的运行参数都可以直接修改，进入 Play 后读取当前资产值。

- 修改“火力/弹量单项等级上限”时，每把武器的“强化价格（按单项等级）”元素数量至少达到该上限。原 16 档价格完整保留，当前两条 8 级强化分别使用前 8 档。
- 修改“每把武器对应 Boss 数”时，Boss 列表元素数量应等于武器数量 × 该数值。
- 修改配置导致旧存档超出新上限时，使用主菜单重置功能创建符合新配置的测试存档。
- `Boss Clicker > Apply V4.1 Balance` 会将该资产恢复为默认数值，手动调整后不要执行这个命令，除非确实需要重置数值表。

## v4.1 数值

```text
总强化等级 = 当前枪火力等级 + 当前枪弹量等级
当前火力 = round(基础火力 × (1 + 0.08 × 火力等级))
当前弹量 = 基础弹量 + 每级弹量 × 弹量等级
金币倍率 = 1 + 0.03 × 金币等级
持续射击间隔 = 1 / 当前武器射速
下一次火力价格 = 当前枪价格表[当前火力等级]
下一次弹量价格 = 当前枪价格表[当前弹量等级]
```

每把武器分别保存 8 级火力和 8 级弹量，两条强化按自身等级独立计价。例如手枪 P1/A0 时，下一次火力升级为 110 金币，弹量升级仍为 100 金币。金币仍从同一个余额扣除。

八个枪段在使用对应武器时，第二/第三个 Boss 最低总强化仍为 `1/2、1/3、2/4、3/6、4/8、4/8、4/8、4/8`。这是弹匣伤害与生命值的关系；玩家可以攒钱提前购买下一把枪突破当前难关。

2026-09-16 调整后，原固定回刷次数与 26～30 分钟时长不再作为验收目标。价格表、伤害、弹量、Boss 生命与奖励未改动。配置位于 `Assets/BossClicker/Data/PrototypeBalance.asset`，最新规则见 [v4.1 制作方案](tasks/v4.1-hold-fire-balance-plan.md)。

## 场景与构建

- `Boss Clicker > Apply V4.1 Balance`：将数值资产恢复为本轮默认表。
- `Boss Clicker > Create Prototype Scene`：重建菜单与战斗场景，保留当前数值资产。
- `Boss Clicker > Create V4.1 WebGL Prototype`：恢复默认表、重建场景和中文字形。
- `Boss Clicker > Build WebGL Prototype`：输出 Gzip WebGL 构建到 `Builds/WebGL`。
- `Boss Clicker > Build And Run WebGL Prototype`：构建后由 Unity 启动本地服务器并打开浏览器。

中文字体已经烘焙进项目。重新生成字体时，本机需有 `Noto Sans SC`。

## 存档与重置

- WebGL：使用 PlayerPrefs 的独立 `lootshot-save-v4-test` 正式值和备份值，保存在浏览器站点存储中。
- Editor/Windows：`%USERPROFILE%\AppData\LocalLow\LootshotStudio\LOOTSHOT Prototype\lootshot-save-v4-test.json`，旁边 `.bak` 为上一份备份。
- v1、v2、v3 文件不会被读取、转换或覆盖。
- 数值试玩前在主菜单 4 秒内点击两次“重置进度”，从零开始验证 v4.1。
- 不保存半场生命、剩余弹药和在途子弹，也没有离线收益。

## 验证

- [独立强化计价与顺序购枪验证](TestResults/v4.1-purchase-rules-validation.md)
- [实施清单](tasks/todo.md)
- [v4.1 数值验证](TestResults/v4.1-balance-report.md)
- [v4.1 验证记录](TestResults/v4.1-validation.md)
- 旧版报告保留在 `TestResults/`，仅作为历史对照。

本轮未加入 CrazyGames SDK、广告、统计、正式插画、换弹操作、Boss 攻击或完整 16 枪 48 关内容。
