# LOOTSHOT v3 原型验收记录

日期：2026-09-14  
Unity：2022.3.51f1c1  
目标：WebGL

## 自动验证

- EditMode：`v3-preview-fix-editmode.xml`，19 项通过，0 失败，0 跳过；在隔离工程中验证新增预览菜单可编译。
- 覆盖一击一弹、独立在途子弹、弹尽等待、击杀取消剩余弹、失败收益保留、失败不推进、首通推进、回刷不推进、战斗中禁止购买、每枪独立强化、金币每级 +3%、固定首通奖励、v3 往返、备份恢复和 v1/v2 文件保留。
- WebGL：`v3-final-webgl.log`，`Build Finished, Result: Success.`
- Gzip 输出约 7.1 MiB：data 约 2.1 MiB、framework 约 72 KiB、wasm 约 5.0 MiB。

## 本地预览验证

- 直接双击 `Builds/WebGL/index.html` 会使用 `file://`，浏览器因此拒绝解析 Unity 的预压缩 Gzip 文件；这不是游戏代码或构建损坏。
- 项目根目录新增 `Preview-WebGL.cmd`，通过仅监听 `127.0.0.1` 的本地 HTTP 服务打开现有 WebGL 构建。
- Unity 菜单新增 `Boss Clicker > Build And Run WebGL Prototype`，可重新构建并由 Unity 启动本地预览。
- 实测 `index.html`、`.framework.js.gz`、`.wasm.gz` 和 `.data.gz` 均返回 HTTP 200；三个压缩文件分别返回正确 MIME 类型和 `Content-Encoding: gzip`。

## 浏览器验证

使用带正确 `Content-Encoding: gzip` 的本地 HTTP 服务加载 `Builds/WebGL`：

1. 主菜单与战斗界面分别显示，战斗画面没有升级或换枪入口。
2. Boss 1：一次点击后弹药 `12 → 11`，生命 `60 → 50`，金币 `0 → 2`。
3. 快速连续点击均被接收；Boss 1 用 6 发击败，结算为命中 `+17`、击败 `+7`、首通 `+20`，总金币 44。
4. 返回主菜单后自动选择 Boss 2；Boss 2 用 9 发击败并推进 Boss 3。
5. Boss 3 生命 132，基础手枪 12 发后剩余 12；失败结算保留本局 44 金币。
6. 失败结算只有“返回主菜单”。返回后仍选中 Boss 3，不存在直接再战按钮。
7. 页面重载后从 WebGL PlayerPrefs 恢复已有 v3 进度，未创建空白进度。
8. 最终地址运行时无 JavaScript 或 Unity 错误；Unity 2022 仅报告 PlayerPrefs 手动同步接口未来将废弃的提示，不影响当前保存。

## 实测修复

首次浏览器构建中，场景中的 `FireInput` 没有稳定 MonoScript 资源引用，导致透明点击层不触发。将组件拆为 `FireInput.cs` 独立文件、重新生成场景和构建后，场景引用包含有效 GUID，以上浏览器交互全部通过。

最终资产检查还发现 Unity 不会序列化 `decimal` 字段。`goldPerLevel` 已改为可序列化的 `double` 并在结算时转为 `decimal`；资源文件现明确保存 `goldPerLevel: 0.03`，最终浏览器烟雾测试仍显示 `×1.00 → ×1.03`。
