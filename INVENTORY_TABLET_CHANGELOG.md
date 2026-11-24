# 物品栏平板快捷入口（实验源码版）

## 本次修改

- 打开原版背包后，在右侧显示什亭之匣图标（直接使用已有的 `ShittimTablet.png` 亮屏帧），无需在背包中持有或获得平板。
- **左键**点击快捷图标：显示原有 TabletUiSystem 平板（HOME、Shop、Formation 等），保持 `Main.playerInventory` 开启。
- **右键按住拖动**：移动图标，自动限制在 UI 屏幕范围内；位置写入 `TabletUiPreferencesPlayer` 角色存档，下次进入仍保留。
- 从快捷图标打开的平板，第一次 **ESC** 关闭平板、返回仍然开启的背包；第二次 **ESC** 由泰拉瑞亚原版关闭背包。
- 原本**手持平板物品**启动、自动收起、动画等行为保持不变；新的入口不需要平板物品。
- 新增原版物品栏 DrawInventory 时的临时鼠标命中屏蔽与恢复，防止平板控件和它下面的物品槽同时响应。

## 修改文件

- `Content/Tablet/TabletUiSystem.cs`
- `Content/Tablet/TabletUiPreferencesPlayer.cs`

## 测试建议

1. 不持有什亭之匣，ESC 打开背包，找到右侧图标，左键打开。确认平板和背包同时显示。
2. 在 HOME、Shop、Formation 界面点击按钮，确认不会同时拿走背包后面的物品。
3. ESC 一次返回背包，ESC 再次关闭背包。检查切换平板页面后同样可以退出。
4. 右键拖动图标至任意边角，退出世界重进，确认位置保存；更改分辨率后检查图标未跑到屏幕外。
5. 把什亭之匣放在快捷栏手持，确认原有物品打开/关闭行为仍可用。

> 说明：此环境没有 .NET SDK/tModLoader 编译环境，已做源码静态检查，仍需在你的 tModLoader 本地编译和验证输入顺序。
