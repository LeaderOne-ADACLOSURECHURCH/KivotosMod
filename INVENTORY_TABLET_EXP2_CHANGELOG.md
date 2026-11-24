# KivotosMod Inventory Tablet EXP2

## 本次修复 / 增补

1. **背包模式 ESC 关闭平板**
   - 不再直接硬关闭。
   - 现在会先触发平板离开/退出动画。
   - 动画结束后保留背包界面；再次按 ESC 才退出背包。

2. **Formation 新增学生：日奈**
   - 学生选择界面新增 `Hina`。
   - 暂时复用 `Yuuka` 的头像与平板立绘资源。
   - 名字显示为 `Hina`，阵营文本为 `Gehenna`。
   - 选择后玩家外观切换为新增的日奈礼服套装。

3. **日奈玩家外观**
   - 新增完整的 Hina vanity 套装实现：Head / Body / Boots。
   - 使用用户提供的 `头 / 身子 / 手 / 腿 / 翅膀` 资源。
   - 与优香一样通过 FrameEffects 强制渲染到玩家外观。
   - 隐藏外部翅膀层，改为使用日奈自带翅膀图层。
   - 靴子/鞋类层不额外屏蔽，保持可见。

4. **平板遮挡区屏蔽物品栏交互**
   - 背包打开平板后，屏蔽判定改为基于“平板在屏幕上的实际矩形范围”。
   - 比 EXP1 的坐标换算判定更稳定，减少点击穿透到背后物品栏的情况。

## 主要改动文件

- `Content/Tablet/TabletUiSystem.cs`
- `Content/Tablet/TabletStudentCatalog.cs`
- `Content/Tablet/TabletStudentAppearancePlayer.cs`
- `Content/Items/Vanity/Hina/*`
