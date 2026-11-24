# 平板 Home：Blue Archive 登录后大厅

从本机 `Z:/YostarGames/BlueArchive_JP/BlueArchive_Data/StreamingAssets` 提取，不需要运行游戏。
登录动画主题已经按用户要求删除。本目录只处理平板大厅。

## 已接入

- `UILobby.prefab` 的 Top、NUITop、Left、Right、Bottom 分组布局，适配现有 514×386 平板显示区。
- 原 NGUI Common 图集的按钮、图标、圆角背景，保持比例并按原边框数据九宫格绘制。顶部资源栏、深蓝资料卡及底栏使用原生 10° inclineAngle，绘制后恢复 UI 变换。
- 底部：咖啡厅、日程、学生、编队、社交、制造、商店、招募。
- 左侧按参考图排列两列四项：公告、MomoTalk、任务、青辉石购买；左下招募横幅，右上活动素材轮播，右下独立工作入口；底部八个菜单不再覆盖工作区域。
- 阿罗娜站立优先使用用户提供的 `arona2.zip` / Spine 3.8.75：swing 待机和 blink 眼部叠加。原 `arona_spr` Spine 4.2.33 资产及备用加载路径保留。
- 教室 `arona_workpage_daytime` Spine 4.2.33：Idle_background_00 背景、Idle_01 场景、Idle_01_Touch_A / Idle_01_Touch_M 配对点击反应。
- 角色选择继续使用现有 TabletCompanionPlayer；右上角色图标打开选择，左右箭头循环选择角色，右上循环图标切换阿罗娜站立/教室场景，隐藏按钮收起界面，Escape 恢复。
- 站立阿罗娜按原始骨骼比例绘制，Home 以 (287,82) 为顶部中心、完整骨骼高度 620；编队也使用同一模型。背景保持原来的完整 Spine 4.2 教室。文字使用平板现有 Noto Sans 普通/粗体字体，按目标字号与可用宽度计算缩放。
- 商店连接原商店；学生、编队连接原编队选择；其余入口提供模态提示，尚未新增咖啡厅、抽卡、战斗等系统。
- 状态栏使用本地玩家姓名、生命、魔力、原平板钱包与换算青辉石。活动横幅是本地素材示例，不接日服服务器活动数据。

## 代码与资源

- `Content/Tablet/TabletUiSystem.Home.cs`：布局、按钮、角色选择、入口路由。
- `Content/Tablet/TabletSpineScene.cs`：参考 TMLive2DRuntime 的图集加载与直接网格绘制，实现 Spine 4.2 UI 渲染、裁切、混合模式、动画状态和卸载。
- `ThirdParty/Spine42`：官方 4.2 运行时，固定提交及来源见 PROVENANCE.txt；独立命名空间，无需依赖 TMLive2DRuntime。
- `Assets/Tablet/Home`：原始骨骼二进制、图集、PNG 与实际使用的 UI 素材。

原始主界面预制体在 `ui-_mxload-prefabs-2025-07-02_assets_all_256629774.bundle`。
UI 图集元数据和完整组件布局保留于 lobby_atlases.json、lobby_layout.json。
Spine 与资源来源见 asset_manifest.json。
横幅示例来自 `uis-00_images-banner-_mxload-textures-2025-07-02_assets_all_2445240650.bundle`。

## 离线工具

需要 Python 的 UnityPy、Pillow、NumPy，以及 Node.js。

```powershell
python Tools/BlueArchiveHome/extract_assets.py
python Tools/BlueArchiveHome/extract_lobby.py
python Tools/BlueArchiveHome/layout_report.py
python Tools/BlueArchiveHome/direct_textures.py
python Tools/BlueArchiveHome/prepare_textures.py
node Tools/BlueArchiveHome/check_assets.mjs
node Tools/BlueArchiveHome/check_interactions.mjs
```

`check_assets.mjs` 使用官方 spine-core 4.2.120 读取资源，检查 3 张图集、7 个运行时所用动画的起始/中间/结束姿势、网格顶点、UV、索引及 29 张 UI 纹理，结果保存于 validation.json。
`check_interactions.mjs` 对两种形态各模拟 780 帧，检查连续点击、待机保持运动、身体附件及透明度、互动轨道结束清理，以及完整恢复待机附件；结果保存于 interaction_validation.json。
这些检查不编译或启动 tModLoader。编译与游戏内输入、UI 缩放和 GPU 渲染验证由用户完成。

离线布局预览：home_preview.jpg（图片合成预览，非游戏运行截图）。
重新生成：先运行 inspect_spine.mjs Idle_background_00，然后 preview.py、home_preview.py。
Tools/* 被 build.txt 排除，不打包临时预览、提取资料或 JavaScript 校验库。

## 透明度格式

Unity 导出的图集与 UI PNG 使用 straight alpha。tML 打包 PNG 时转为 rawimg，
ImageIO.ToRaw 只将 alpha=0 的像素清零，半透明 RGB 不会自动预乘；RawImgReader 也原样上传。
而平板 UI 与 Spine 绘制使用 PMA AlphaBlend，因此 Assets/Tablet/Home 下的 34 张 PNG
必须在发布时转换 RGB = RGB × alpha，alpha 保持原值。prepare_textures.py 始终从
Tools 的原始导出或游戏原图重建，不会对已处理的图片重复预乘。
alpha_validation.json 记录逐文件哈希，并检查 alpha 与不透明像素保持不变、RGB ≤ alpha。
离线预览继续读取 Tools 中的 straight-alpha 原图，避免 Pillow 合成时再次乘 alpha。

## 互动轨道

轨道 0 始终循环待机；轨道 1 为动作/颜色，轨道 2 为表情/附件，轨道 3 为眨眼。
教室触摸必须同时叠加 `_Touch_M` 与 `_Touch_A`，不能替换待机，否则 Idle_01 设置的身体附件会恢复成空。
站立的 02 是表情叠加，也不能替换待机，否则呼吸和头发等运动会停住。
互动结束以 empty animation 淡出恢复；互动期间暂停眨眼，连续点击不重启动作，切换场景清理所有互动状态。

## 2026-10-07 参考图核对

原生颜色：资料卡 RGB(21,55,87)，资源面板透明度 235/255，profile 与 goods 的 inclineAngle=10；九宫格原始边框为 20/20/15/30。
参考图为 16:9，平板玻璃为 4:3；位置按两侧和底部锚定适配，PNG 及角色保持比例。活动图片仍为已提取的本地示例，没有伪造当前活动日期、未读数量或活动奖励。
新增的 `Cafe_Icon_Arrow` 与 `Common_Plus_Btn` 来自原生 Common 图集，发布时预乘 alpha。
离线预览使用原始 PNG 与提取的 SourceHanSansSC-Bold 辅助排版，游戏内使用现有 Noto Sans 字体；预览用于构图核对，不等同于实际 GPU 截图。
阿罗娜称呼 TTS 与相关物品已由用户取消，本次不接入。

## Home 时钟

底栏招募右侧、工作入口左侧增加原生时钟及彩色 △×＋○ 装饰（UIWorkAronaSceneBG_DecoColor）。八个底栏入口间距改为 44，时钟占用独立 49×24 区域，避免覆盖招募图标。
默认显示系统本地时间，24 小时 HH:mm。平板右上菜单 → 设置 → 时钟来源，可选现实时间或游戏时间；选中的按钮高亮，偏好随角色保存，旧存档默认现实时间。重置 100% 只重置平板缩放。
游戏模式读取 Main.time / Main.dayTime，不增加计时器；世界暂停、快进或调整时间时显示同步变化，并额外显示白天/夜晚。凌晨从 23:59 到 00:00 正常跨日。
Terraria 的日间计时从 04:30 开始，夜间计时从 19:30 开始，依据官方说明：https://docs.tmodloader.net/docs/stable/class_main.html 。日夜标记使用 Main.dayTime，表示世界时钟所在阶段。
离线预览：`python Tools/BlueArchiveHome/home_preview.py`；日间/夜间示例分别添加 `--clock day`、`--clock night`。示例为 04:30 和 19:30，不读取运行中的游戏。

## 用户提供的 Arona Spine 3.8（2026-10-12）

输入 `E:/Spine3.8.75Pro/arona2.zip`，包含 `Arona .atlas`、`Arona .png` 和 `Arona .skel`。骨骼原始版本 3.8.75，68 骨骼、20 插槽，swing/blink 时长均为 4.133333 秒。
`import_arona38.py` 导入至 `Assets/Tablet/Home/Arona38`，仅重命名图集页面为 arona.png 并预乘 PNG；骨骼二进制完全保留。来源及 SHA-256 见 arona38_import.json。

`ThirdParty/Spine38` 为官方 3.8 分支提交 `8b4844bd4b193ba9e54487ed397a777993cbad56` 的 42 个 C# 文件，命名空间隔离为 KivotosMod.Runtime.Spine38。来源、许可及修改说明见该目录 PROVENANCE.txt/JSON 和 LICENSE.txt。3.8 与 4.2 分别由 TabletSpine38Scene、TabletSpineScene 绘制，不转换原有 4.2 资源。
上游因该导出版本的已知问题默认拒绝 3.8.75（维护者说明 https://github.com/EsotericSoftware/spine-runtimes/issues/2428）。新增 AllowLegacy3875 的显式 opt-in，仅供这份已逐帧验证的用户资产使用；默认拒绝仍保留，源骨骼版本没有更改。不修改 Spine 编辑器或许可代码。

`TabletSpine38Animation` 始终在轨道 0 循环 swing；从 blink 克隆 eyes* 骨骼的旋转/平移/缩放曲线，将原 1.0～1.4 秒的眼部片段移至 0～0.4 秒后叠加在轨道 1。保留原曲线插值与数据，不切换全身动画。每次眨眼结束后等待随机 3～6 秒；点击站立形态也触发一次眨眼，进行中不重启。间隔常量位于该文件顶部。教室形态继续使用原有 4.2 触摸动作。

离线验证：`pwsh -File Tools/BlueArchiveHome/check_arona38.ps1`。只在 PowerShell 进程内编译独立 runtime probe，不编译 KivotosMod/tML。验证完整读取、3600 帧/72000 个网格、自动间隔、待机连续性、连续点击、闭眼及恢复、原动画未改写，结果见 arona38_validation.json。
默认 Home 预览使用 3.8 模型；添加 `--arona-version 42` 可生成旧 4.2 站立模型的对照预览。GPU 渲染、实际 UI 缩放和游戏内切页仍由用户编译验证。

## 联合验证修正（2026-10-12）

用户 client.log 显示 238 个编译错误。之前的单运行时骨骼探针和语法检查没有覆盖两个 runtime 放在同一 Mod 中的命名冲突，不能视为 Mod 编译通过。
修复：两个 Json.cs 的 SharpJson 辅助类型分别放到 Spine38.SharpJson、Spine42.SharpJson；Skin.cs 用 global:: 限定集合类型，避免 KivotosMod 入口类遮蔽同名命名空间。fetch_spine_runtime.py 同步该隔离规则，防止重新提取 runtime 时恢复冲突。
新增 `pwsh -File Tools/BlueArchiveHome/check_integration.ps1`，按 C# 12 和本机 tML/net8 引用对整个工程做联合语义诊断，包含真实 Mod 入口和两个版本。结果见 integration_validation.json。只调用 GetDiagnostics，不 Emit，不产生 Mod DLL/.tmod，不启动游戏，也不替代用户的实际打包与渲染验证。
check_arona38.ps1 也改为把两个 runtime 一起加入探针编译，避免只检验单个版本。


## Sora 替换与两人视线跟随（2026-10-13）

`import_sora38.py` 导入 `E:/Spine3.8.75Pro/sora2` 原始三文件至 `Assets/Tablet/Home/Sora38`。
3.8.75，53 骨骼、18 插槽；待机 `animation` 和 `blink` 均为 3.333333 秒。骨骼原字节及版本保留，PNG 转 PMA，哈希见 sora38_import.json。
两人共用 TabletSpine38Scene/Animation；Sora 的 eyeW*/eyebow*/face3/face4 眨眼片段为 1.166667～1.6 秒，截取并克隆曲线后覆盖眼部，身体始终保持原待机。
Sora 在商店、Home 伴随和编队预览中优先使用新模型。只播放待机和自动眨眼；眨眼结束后等待随机 3～6 秒。保留 Spine 4.2 资源/运行时，原阿罗娜场景切换按钮保持禁用。

鼠标从平板坐标按实际绘制锚点、缩放及 Y 翻转转换到骨骼坐标。两份 Hitomi 网格都受眼部其他骨骼共同影响，因此只在算完动画顶点后平移 Hitomi，不修改骨骼、眼白、眼皮或源权重。
水平最大 2.5、垂直最大 1.4 骨骼单位；指数平滑，眨眼期间保持触发瞬间的视线位移，睁眼后平滑追踪当前鼠标；瞳孔透明度固定为不变；闭眼期间保持视线，不再淡出瞳孔。位置每帧从原骨骼计算，防止累计漂移。
UI 待机与眨眼使用 Stopwatch 单调时间并限制单步 0.1 秒，打开/关闭时重置；不依赖暂停或高帧率插件可能返回 0 的 GameTime。

`check_arona38.ps1` 现在同时检查两人，每人 3600 帧，待机身体与源动画一致，眼睛闭合/恢复、自动间隔、瞳孔四方向边界，以及自动/手动眨眼触发、闭眼、睁眼期间视线不回中；眼皮、眼白和身体保持原网格。
另在 60/144 FPS、宿主 elapsed=0、无点击条件下各模拟 30 秒，两人各自动眨眼 5 次，待机持续运动。
结果分别为 arona38_validation.json、sora38_validation.json；整套工程联合语义检查仍由 check_integration.ps1 执行（不 Emit）。
`eye_tracking_preview.png` 是软件网格合成的左看/默认/右看/闭眼对照，非游戏截图。实际 GPU、UI 缩放及输入体验由用户编译验证。


## 新阿罗娜工程（2026-10-13）

`E:/Spine3.8.75Pro/arona/Arona2.spine` 是 3.8.75 Professional 工程；CLI 项目信息确认 68 骨骼、20 插槽、`swing`/`blink`。同目录 `Arona.json` 是单骨骼空动画占位导出，故从 `.spine` 工程重新导出。随附 21 张 images 由 Spine 打包为 PMA 关闭的 atlas；导出的骨骼二进制为 3.8.75，原文件不手改。新 atlas、源 PNG 和二进制覆盖 `Assets/Tablet/Home/Arona38`，替换之前 `arona2.zip` 的阿罗娜，同时沿用瞳孔跟随、闭眼眨眼片段提取及随机自动眨眼逻辑。输入/导出哈希在 arona38_import.json。

重新导出：`Spine.com -i E:/Spine3.8.75Pro/arona/Arona2.spine -o Tools/BlueArchiveHome/NewAronaExport -e E:/Spine3.8.75Pro/examples/export/binary.json`；图集打包：`Spine.com -i E:/Spine3.8.75Pro/arona/images -o Tools/BlueArchiveHome/NewAronaExport/Atlas -n arona -p E:/Spine3.8.75Pro/examples/export/atlas-1.0.json`。然后运行 `python Tools/BlueArchiveHome/import_arona38.py` 和离线验证脚本。Spine CLI 可读取本机授权并导出；日志中的可选 Welcome 数据下载失败不影响导出。


## 阿罗娜单点转头（2026-10-14）

更新源 `E:/Spine3.8.75Pro/arona/Arona2.spine`：Spine 3.8.75，73 骨骼、21 插槽、22 张源贴图，`swing`/`blink`/`Turning`。重新由本机 Spine CLI 导出原版本骨骼（63966 bytes）并打包非 PMA atlas，然后按原流程导入 Arona38。Sora 保持眼睛跟随。

新骨骼的 `bone18` 是单点控制器，`change` 约束对 `bone17` 使用负混合，驱动脸部、眼部、头发、光环和头饰的相对位移。TabletSpine38Animation 每帧只控制 bone18 的 XY，不播放 Turning 全身动画；Turning 的控制点轨道提供安全边界：X 位移 -32.42096～166.29459、Y 位移 -107.52103～15.345032。负约束使方向相反，因此控制位移对鼠标方向反向映射。

鼠标按实际 UI 锚点/缩放/Y 反转转到骨骼坐标；转头和瞳孔共用一个归一化焦点。先恢复控制器的 setup 位置、求出未转头脸部，再计算焦点，避免累计漂移和转头位置反馈。TrackingResponseSeconds=0.18，MaxTrackingSpeed=4（归一化单位/秒），指数响应加平滑速度限制；两项常量位于 TabletSpine38Animation 顶部。改变目标时平滑追随，不保存固定帧数，30/60/144 FPS 响应一致。

转头在眨眼期间继续，瞳孔位移保持并在睁眼后平滑追上焦点；EyeOpacity 始终为 1，不作瞳孔淡出。原自动间隔和待机轨道保持。

check_arona38.ps1 增加 headTracking 结果：单控制器、四方向正确、身体骨骼基准相同、网格有限、动画控制范围、30/60/144 FPS 半秒响应约 0.915、快速交替目标限速与抖动幅度。原有眨眼/视线检查一起通过；整工程语义检查 309 源文件 / 231 引用，0 错误、7 警告。此为离线验证，未打包或运行游戏。
`head_tracking_preview.png` 是实际 Spine 网格的软件方向预览。`calibrate_head.ps1` 可生成不跟随鼠标的直接控制器样本以核对 rig 方向。上一轮未重跑的瞳孔检查中存在一个工具变量作用域错误，本次联合探针运行前已修正。


## 旧版什亭之匣背景与 S 启动页（2026-10-16）

来源：`Z:/GPT-SoVITS-v2pro-20250604/Kivotos-StudentNPC`。用户授权沿用旧版代码和素材。
Home 现在直接绘制旧版 Classroom 的 13 层，不加载、更新或绘制 Spine 4.2 教室背景；Spine 3.8 阿罗娜的转头/视线/待机/眨眼仍走现有角色渲染路径。
按旧版 Background_Classroom 的顺序合成 Sky、Sea、Ground、Wall、Lockers、Board、Light、ChairBack、TableBack、ChairMid、TableMid、ChairFront、TableFront，保留 880×466 原尺寸与 X=250 的 514×386 裁切。
Ground 使用旧 WaterCaustic.xnb 原 pass，参数 (20, 0.1, 8, 35)。原 Light 含有 13932 个半透明像素，导入时从旧版原图做一次 PMA，避免直通 alpha 配合 AlphaBlend 发白。

每次打开平板显示旧 UnlockLayer 的 S 启动页。保留原 Background、Logo、Triangle 三图、70 个三角位置、7 个同时活动、90～239 tick 生存时间和 InOutQuad 呼吸透明度。Logo 沿用旧 Glitch.xnb 的 Glitch pass 与 (5, .025, .1, 100)、Saturation=0 参数。
点击屏幕后启动页一秒淡出；淡出结束前屏蔽下面菜单的点击。已有 CRT 开关屏效果继续覆盖其上，收起/重新打开时重置启动状态。没有导入旧角色剧情、密码存档或旧 UI 层级依赖。
所有新绘制使用当前平板的缩放/拖动变换，S 页和水纹 pass 设置屏幕裁切并恢复之前的 SpriteBatch/光栅/采样/混合/深度状态。旧版 522×394 启动图在中央按 4px 裁切到屏幕，三角边缘不会溢出边框。

素材和引用代码哈希见 legacy_tablet_import.json；原图/原 shader 源存于 LegacyTabletSources。Assets/Effects/TabletWaterCaustic.xnb 与 TabletGlitch.xnb 保持旧版原字节。
离线检查：check_startup.ps1 在 30/60/144 FPS 验证等待点击、7 个活动粒子、1 秒淡出、重复点击不重置、收起和重开重置。整工程语义检查 311 源文件、231 引用，0 错误、7 警告；不 Emit、不打包、不启动游戏。
home_preview.jpg 和 startup_preview.jpg 是软件合成布局预览，未运行 GPU 水纹和故障 shader；shader 的实际游戏运行效果仍需用户编译验证。


## 昼夜搭档与夜间教室（2026-10-19）

阿罗娜素材目录与已导入哈希比较，仅 images/hairM.png 变化，Arona2.spine 工程哈希保持不变。本次重新打包图集并导入更新的 PNG，骨骼沿用已导出的 3.8.75 版本。
AronaLeftTurnScale=0.5 仅作用于鼠标在角色左侧时的转头位移，右侧转头、抬头低头和瞳孔移动范围保持原值。实际左侧控制范围从约 +166.29459 降至 +83.14730；平滑/限速机制保留。

夜间背景来自用户 Downloads/什厅之箱背景.zip 的 13 张独立 880×466 分层图；文件名修复 ZIP 中未标记 UTF-8 的名称后映射到 ClassroomNight。import_night_tablet.py 保存原图至 NightTabletSources，输出一次 PMA 转换后的 Assets/Tablet/UI/ClassroomNight。主界面依据 Main.dayTime 选择整套白天/夜间层，沿用原裁切和绘制顺序。夜间地面水纹使用冷色和较低强度，避免日间白色水光冲淡夜间颜色。

Plana.zip 只提取 Plana.spine 和角色图片，明确跳过 images/123.png；原工程的 123 槽及皮肤附件也在 Plana.filtered.json 中移除后，再由 Spine 3.8.75 CLI 导入临时工程并导出 binary。因此角色与夜间教室完全独立，没有打包或加载 123.png。最终骨骼 62 bones/22 slots，文件 plana.skel.bin。原存档 Plana ID=5 保留，并加入已有的大厅角色列表。
Plana 的 animation 作为循环待机；从 blink 中克隆 eyelash*/eyelids*/pupil* 的眼部片段，原片段 1.166667～1.533333 秒压到 0～0.366667 秒，间隔仍为随机 3～6 秒。pupil 网格支持视线跟随、眨眼保持及不透明绘制；没有新增未经提供的转头动作。

昼夜搭档最初由 UpdateLobby 在日夜变化时立即切换，现已改为仅在重新打开平板时更新，见下方「开箱时切换昼夜」记录。该规则读取 Terraria 世界时间，与时钟显示选择现实时间/游戏时间独立。

验证：check_arona38.ps1 同时检查 Arona/Sora/Plana，每人 3600 帧、自动/手动眨眼、眼部复原、身体待机、视线、不透明绘制及 UI 计时；Plana 完整读入并生成 79200 个网格样本。Arona 单控制点范围检查按左侧缩放更新。整工程联合语义检查 323 源文件、231 引用、0 错误/7 警告，不生成 Mod 程序集或启动游戏。
PMA 检查覆盖更新的两位角色图集和 13 张夜间背景，并确认 123 不在提取的图片目录、图集、骨骼槽、打包素材中。night_tablet_import.json 记录压缩包和输出哈希。
预览：home_preview.jpg 为日间；`python Tools/BlueArchiveHome/home_preview.py --scene night --clock night` 生成 home_preview_clock_night_night.jpg。两者均为软件合成，不包含实际 GPU 水纹效果，实际游戏日夜切换由用户编译验证。

## 普拉娜转头与教室取景调整（2026-10-19）

普拉娜使用原模型的 face2 单点控制器，通过已有负混合 change 约束驱动 face3 及头部网格。没有新增或重写源动画。控制点的父骨骼有旋转，因此先把鼠标方向对应的世界位移转换为父骨骼局部坐标，避免轴向错位。世界位移上限水平 ±90、垂直 ±60；实际面部中心最大水平移动约 4.77、垂直约 3.18 个骨骼单位，保持轻微转头。与视线共用 0.18 秒平滑焦点和每秒 4 的速度上限。转头在眨眼期间继续，瞳孔仍保持最后视线、不淡出，自动眨眼和待机沿用原逻辑。

日夜背景共用 ClassroomSourceRect=(210,20,594,446)，从旧 (250,0,514,386) 扩大可见范围约 15.6%，水平中心保持 507，取到原图底部 466，让桌腿和地面露出更多。角色尺寸和 UI 布局保持原值。普通分层与 Ground 的 GPU 水纹 pass 使用同一取景，避免地面对不齐。Sea 先绘制，Sky 后覆盖海平线亮边，再按原顺序绘制地面、墙壁和桌椅。

check_arona38.ps1 已为 Plana 增加 headTracking：face2 单点、四方向、控制范围、身体待机不变、网格有限、30/60/144 FPS 响应和快速交替目标限速检查全部通过，既有三人待机/自动眨眼/视线检查同时通过。整工程 no-emit 语义检查 323 源文件 / 231 引用，0 错误、7 警告。未生成 Mod DLL、未打包或运行游戏。

已核对软件预览 home_preview.jpg、home_preview_clock_night_night.jpg 和 plana_head_tracking_preview.png。软件预览不包含 GPU 水纹效果。`calibrate_head.ps1 -Stem plana` 可重现控制点方向标定样本。

## 按完整示例显示教室（2026-10-19，已回退）

以用户最后确认的 codex-clipboard-49d31a02-6d31-4fd9-8b41-823f49368542.png 为准；该 880×466 示例与现有夜间 13 层按 Sea、Sky、Ground 等顺序的软件合成逐像素一致。无需更换或重画素材。日夜 ClassroomSourceRect 统一为 (0,0,880,466)，整幅映射到平板 514×386 屏幕，保留两边、左侧柜子、三组桌椅及完整地面。整幅随屏幕宽高分别缩放，不再使用局部裁切。Ground 水纹 pass 自动共用同一窗口，原角色与菜单布局保持。

重新生成并目视核对日夜 home_preview.jpg / home_preview_clock_night_night.jpg；离线软件预览不含 GPU 水纹。整工程 no-emit 语义检查仍为 323 源文件 / 231 引用，0 错误、7 警告。未构建 Mod 或启动游戏。

用户随后要求回退到上一版：已撤回整幅铺满，恢复 ClassroomSourceRect=(210,20,594,446)，并同步恢复预览取景。普拉娜转头、自动眨眼、天空覆盖海洋及水纹共用取景均保留。

## 普拉娜右转幅度与开箱时切换昼夜（2026-10-19）

PlanaRightTurnScale=0.5，只对归一化焦点 X>0 的转头水平控制位移生效。右转控制范围由世界位移 -90 降至 -45；左转 +90、上下 ±60 保持。实际面部中心右移由约 4.765 降为 2.383 个骨骼单位，左移仍约 4.765；眼睛视线范围、平滑和自动眨眼保留。check_arona38.ps1 增加左右实际位移比例断言，方向、范围、快速鼠标目标限速和三人原有眨眼/视线回归全部通过。

Open() 在可见状态检查后调用 OpenLobby()，读取 Main.dayTime 并固定 _lobbyDayTime，同时选择阿罗娜或普拉娜。UpdateLobby 不再读取世界日夜或重新选角。普通背景层、Ground 水纹贴图、颜色和强度统一使用这次开箱快照；打开期间世界跨过黄昏/黎明，当前场景和手动选角保持。关闭后下次 Open() 才更新。游戏内时钟继续显示真实世界时间，不冻结。

check_lobby_cycle.ps1 读取并执行实际 Open() 的可见状态检查与场景初始化部分、OpenLobby() 和 UpdateLobby()，资产/GPU 依赖使用桩：白天开箱跨入夜晚保持、夜晚开箱跨入白天保持、重新打开更新、重复 Open 不更新、手动选角不被覆盖，均通过；并检查背景和水纹统一使用快照。整工程 no-emit 语义检查 323 源文件 / 231 引用，0 错误、7 警告；未构建 Mod、未启动游戏。
