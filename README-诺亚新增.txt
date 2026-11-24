诺亚 / Noa Formation Appearance 补丁（基于 FIX15 前后的学生数据结构）

功能：
- 新增 Formation 可选角色：诺亚（Millennium），ID 7；保留旧角色 0-6。
- 已接入 FrameEffects / DrawEffects / UpdateAutopause（暂停保留外观）。
- 使用提供的 40x1120 头部、后发、鬓发、身体、大衣、腿 + 360x224 复合手臂图。
- 后发绘制在背面，鬓发独立绑定头部绘制在头部前景，避免跟随躯干及持枪手臂错位。
- 仅在装备完整诺亚视觉外观或从 Formation 选择时触发学生尸块抑制。
- Formation 临时缩略图从提供的像素美术合成；没有提供高清立绘，因此未冒充官方大立绘或加入 HOME 伴随轮播。

安装：把补丁 Content 和 Assets 目录覆盖进现有 KivotosMod 源码根目录，然后重新编译。
只覆盖本 ZIP 的文件，不要覆盖旧版 TabletUiSystem.cs 或 FIX15 的鼠标遮挡修复。

注意：仅做资源、源码结构静态检查；没有 tModLoader 工具链做实际编译和持枪动作/暂停游戏实测。
