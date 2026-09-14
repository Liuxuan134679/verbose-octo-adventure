# LOOTSHOT · Unity WebGL 第一版原型

项目位置：`C:\Users\root\Desktop\My ga`  
Unity：`2022.3.51f1c1`  
当前范围：4 把现代远程武器、12 个 Boss。完整 16 枪 48 关仍属于后续内容。

## 当前玩法

- 主菜单负责选枪、选 Boss、购买武器、升级当前武器火力与弹量、升级全局金币收益。
- 点击“开始战斗”进入独立战斗界面；战斗期间不能购买、换枪或选关。
- 鼠标每点击一次发射一颗子弹，不限点击速度，没有倒计时和自动射击。
- 每次有效命中立即掉落金币；击败后再发放击败奖励，首次通关奖励固定且不受金币倍率影响。
- 弹药清空且所有在途子弹结算后，Boss 仍有生命则失败。已经获得的命中金币保留。
- 结算只有“返回主菜单”。失败后当前 Boss 不变；玩家强化后再次点击“开始战斗”。
- 首次击败最前沿 Boss 才推进并默认选择下一关。已通关 Boss 可由玩家手动选择回刷。

## 开始试玩

在 Unity Hub 中打开本项目，载入 `Assets/Scenes/SampleScene.unity` 后点击 Play。

WebGL 成品位于 `Builds/WebGL/index.html`，但不能直接双击它。最方便的方式是双击项目根目录的 `Preview-WebGL.cmd`：脚本会启动仅限本机的 HTTP 服务并打开浏览器，预览期间保持命令窗口开启，结束时按 `Ctrl+C`。

也可以在 Unity 中选择 `Boss Clicker > Build And Run WebGL Prototype`。出现 “Unable to parse ... Loading pre-compressed content via a file:// URL” 就说明仍在直接打开文件，应改用上述两个入口。

## 原型数值

```text
当前火力 = round(基础火力 × (1 + 0.08 × 当前枪火力等级))
当前弹量 = 当前枪基础弹量 + 当前枪弹量等级
金币倍率 = 1 + 0.03 × 金币等级
```

每把武器分别保存 8 级火力和 8 级弹量。四个枪段各含三个 Boss：前两关用于获得金币和感受提升，第三关构成明确的火力/弹量门槛。固定表位于 `Assets/BossClicker/Data/PrototypeBalance.asset`，策划依据见 [v3 策划方案](tasks/v3-crazygames-game-design.md)。

## 场景与构建

- `Boss Clicker > Apply V3 Balance`：将数值资产恢复为本轮默认表。
- `Boss Clicker > Create Prototype Scene`：重建菜单与战斗场景，保留当前数值资产。
- `Boss Clicker > Create V3 WebGL Prototype`：恢复默认表并重建场景。
- `Boss Clicker > Build WebGL Prototype`：输出 Gzip WebGL 构建到 `Builds/WebGL`。
- `Boss Clicker > Build And Run WebGL Prototype`：构建后由 Unity 启动本地服务器并打开浏览器。

中文字体已经烘焙进项目。重新生成字体时，本机需有 `Noto Sans SC`。

## 存档与重置

- WebGL：使用 PlayerPrefs 的独立 v3 正式值和备份值，保存在浏览器站点存储中。
- Editor/Windows：`%USERPROFILE%\AppData\LocalLow\LootshotStudio\LOOTSHOT Prototype\lootshot-save-v3.json`，旁边 `.bak` 为上一份备份。
- v1/v2 文件不会被读取、转换或覆盖。
- 主菜单“重置进度”需在 4 秒内点击两次，清空 v3 金币、武器、强化和关卡进度。
- 不保存半场生命、剩余弹药和在途子弹，也没有离线收益。

## 验证

- [实施清单](tasks/todo.md)
- [EditMode 结果](TestResults/v3-preview-fix-editmode.xml)：19 项通过，0 失败；包含新增预览菜单的编译验证。
- [WebGL 构建日志](TestResults/v3-final-webgl.log)：`Build Finished, Result: Success.`
- [浏览器验收记录](TestResults/v3-validation.md)：覆盖单击单发、快速连点、胜利推进、失败保留金币、失败目标保持、无再战入口、页面重载和本地 Gzip 预览。

本轮未加入 CrazyGames SDK、广告、统计、音效、正式插画和完整 16 枪 48 关内容。
