# 总结5 修改说明

来源：C:/Users/Sunbinbin/Downloads/总结5.docx 的正文和三张截图，以及用户明确说明的“学生图标按钮”。

- 禁用大厅下方 Student 学生图标：点击不再进入换装页，去除悬停高亮并灰显；保留按钮以便以后启用。
- 下方 Edit 编队图标标签改为“代理战斗”，仍打开原换装/编队页。邮箱旁的旧大厅学生选择按钮延续此前的禁用状态。
- 扫帚上次无效的根因是把坐骑45（SpookyWood）误当作扫帚。改为 Terraria.ID.MountID.WitchBroom（23），让已有的关闭坐姿切片、完整站立腿帧和绘制后恢复动画逻辑实际作用到扫帚。不改其他坐骑，未另添裙子饰品。
- 未花装备悬浮滑板22、Lazure屏障台28、星盘45时停止扑翼并归零扑翼帧。分别检查功能翼 wingsLogic 和可见翼 wings，避免时装覆盖功能翼后仍扑翼；普通飞行翼保留原动画和飞行数值。
- 优香射击间隔从本次前的5 tick改为6 tick。原始间隔10 tick，目标10/1.6=6.25 tick；现有整数间隔取最近的6 tick，实际约为原始射速1.667倍。伤害21不变。
- 音乐盒移除自定义 Toggle/RightClick/HitWire：注册到 MusicLoader 后，tML原生 Player.TileInteractionsUse 和 Wiring 已调用 WorldGen.SwitchMB。自定义处理叠加会把开关切回原状态。交给原生路径处理开关和联网；继续使用主界面 Step_by_Step.ogg。

验证：

- Tools/Summary5/check_pose.ps1 执行实际 ModifyDrawInfo/TransformDrawData：真实扫帚编号23、左右朝向、完整站立腿帧、恢复原帧；普通玩家及坐骑45不受影响，睡床旋转保持。引擎依赖为桩，未实测GPU渲染。
- Tools/Mika/check_rules.ps1 执行实际 PostUpdate 等生产方法，增加三种平台分别位于功能槽、显示槽的9种组合和卸下后恢复扑翼检查；原飞行动画、翼位、受伤语音离线回归通过。联网语音发包依赖为桩。
- Tools/Summary2/check_semantics.ps1：372源文件、231引用，0错误、7原有警告；未生成Mod程序集。
- 音乐盒接口依据：本机 MusicLoader/TileLoader/SceneMetrics 和官方 Player/Wiring 补丁，确认原生开关路径；尚未在游戏里验证播放和联网效果。

官方来源：
https://github.com/tModLoader/tModLoader/blob/stable/patches/tModLoader/Terraria/Player.cs.patch
https://github.com/tModLoader/tModLoader/blob/stable/patches/tModLoader/Terraria/Wiring.cs.patch
https://github.com/tModLoader/tModLoader/blob/stable/patches/tModLoader/Terraria/SceneMetrics.cs.patch

修改前备份：系统临时目录 KivotosSummary5/backup。没有构建或启动游戏，构建及游戏内测试由用户执行。扫帚完整裙摆对齐、音乐盒右键/电线开关与双端同步仍需实测。
