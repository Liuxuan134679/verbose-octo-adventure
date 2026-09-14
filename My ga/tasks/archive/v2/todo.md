# 巨兽锻坊 v2：实施清单

日期：2026-09-08  
方案：[plan.md](plan.md)  
状态：**v2 原型实现与构建完成；部分人工窗口复核受工具错误阻断**。旧版已完成记录：[archive/v1/todo.md](archive/v1/todo.md)。

以下进度基于本轮实际 v2 执行结果，不沿用 v1 测试记录。

## T1：离散攻击正确结算

依赖：无。预计文件：GameBalance.cs、GameSession.cs、GameSessionTests.cs。

- [x] 用单发伤害、武器间隔、0.15 秒首发和 0.08 秒连发定义攻击，单场上限 15 秒。
- [x] 逐发扣血，15 秒整先结算攻击；击杀取消尾发；1 秒刷新与玩家选关保持。
- [x] 大/小 Tick、时限前后、连击中途击杀、重复点击和切关用例通过。

验证：Unity EditMode，比较攻击数量、HP、金币和阶段，不只比较平均 DPS。

## T2：三强化与 v2 存档

依赖：T1。预计文件：GameBalance.cs、GameSession.cs、SaveStore.cs、GameSessionTests.cs、SaveStoreTests.cs。

- [x] 三条等级独立购买，生效时机符合方案；换枪不重置计时或赠送即时攻击。
- [x] 只放大基础掉落，末尾取整后加固定首通；金币/价格用 long，倍率和容量检查通过。
- [x] v2 独立保存恢复，v1 文件保留；读档 Idle，无离线收益，备份/损坏/失败提示正确。

验证：基础 100、首通 30，金币一级首通 142、重复 112；购买失败不扣款；全部新字段往返一致。

## T3：开局快速换枪

依赖：T2。预计文件：PrototypeBalance.asset、BalanceSimulationTests.cs、GameSessionTests.cs。

- [x] 录入开局三关、W1/W2、火力首级价格 10 和 W2 价格 120。
- [x] 参考路线到 W2 用时 26.95 秒（含刷新、零操作耗时），结余 8；W2/火力一级回刷 B1 一发击杀。
- [x] 使用真实 Session 验证逐发结果与换枪后的回刷收益交叉。

验证：输出首次击杀、首次强化、购买 W2 的时间和金币明细，时间误差不超过 0.01 秒。

### 检查点 A

- [x] 离散攻击、三强化、存档与开局算例均有实际通过结果。

## T4：完整 16 武器、48 Boss 经济表

依赖：T3。预计文件：PrototypeBalance.asset、BalanceSimulationTests.cs、tasks/plan.md、TestResults/v2-balance-report.md。

- [x] 确定全部武器价格、三条强化价格与 48 Boss HP/奖励，保存固定表。
- [x] 跑参考购买、攒枪优先、经常强化三条路线；参考 W4～W5 约 5 分钟、W16 在 180～300 分钟，末关可完成。
- [x] 记录购买、失败、推进、回刷选择、金币回本时间、最长成长空档；检查无死锁、无溢出和多次收益交叉。

验证：计入逐发攻击、刷新、首通、升级支出及金币倍率；增加约 1 秒操作耗时版本。金币强化按取整后的回本时间定价，不能套用火力的等待时长。达标后同步方案中的最终表与统计。

## T5：远程战斗与商店界面

依赖：T4。预计文件：GameView.cs、PrototypeSetup.cs、SampleScene.unity、PrototypeChinese.asset、枪械 Sprite 资产。

- [x] 展示单发、每轮发数、平均输出，真实射击事件触发枪口与命中反馈；枪械长度、枪托和瞄具随武器变化。
- [x] 三强化卡及下一把枪显示价格与购买后变化，购买不暂停或改变回刷。
- [x] 原生 ScrollRect 已生成 48 关，显示加成后的基础收入、固定首通和上次耗时；实际观察了 960×540 与滚动到 B18。标准窗口及滚动到底列为 T6 待人工复核。

验证：实际点击火力、金币、连击、换枪、切回刷目标，观察血条只在攻击事件下降；不新增自动选关。

### 检查点 B

- [x] 参考数值仿真达标，界面与完整流程连接，存读自动检查通过。

## T6：Windows 试玩与交付

依赖：T5。预计文件：必要修正涉及的原有文件、README.md、tasks/todo.md。

- [x] 全部 v2 EditMode 实际执行且无失败，Windows 构建成功（27/27，见 TestResults/v2-editmode.xml）。
- [ ] 从新档实际玩前 5～10 分钟，记录首个 Boss、首次强化、W2、W4～W5 与首次连击节点。
- [ ] 验证失败停止、手选回刷、关闭重开无离线收益，以及中文/大数字在标准与小窗口中的显示。

验证：全程仿真确认成长时长，桌面试玩确认前期操作与手感；交付记录区分模型结果和实际试玩结果。

### 完成标准

- [x] 参考路线满足 plan.md 的量化目标，当前构建包含全部新版规则；替代购买路线的差异已记录。
- [x] 代码、资产、数值与说明一致，v1 存档仍保留。
- [x] 按实际结果勾选，不借用旧版本通过记录。

## 本轮交付证据

- [固定数值和六条路线](../TestResults/v2-balance-report.md)。
- [27 项 EditMode](../TestResults/v2-editmode.xml)，Windows 构建日志 `TestResults/v2-windows-build.log`。
- [真实窗口试玩](../TestResults/v2-playtest.md)：已观察到 W6、B18、三条强化及手选回刷。
- T6 未勾选项为人工复核缺口：节点精确计时、滚动到底、标准窗口、关闭重开。窗口控制工具恢复后返回 `foreground window did not report a process id`，本轮未继续无效输入。
