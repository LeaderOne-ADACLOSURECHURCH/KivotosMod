using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using KivotosMod.Globals.Classes;

namespace KivotosMod.Content.Tablet;

public sealed partial class TabletUiSystem
{
    private static readonly Rectangle StudentFavoriteRect = new(514, 128, 24, 24);
    private static readonly Rectangle FormationListClip = new(40, 100, 244, 246);
    private static readonly Rectangle ShopListClip = new(304, 80, 224, 300);
    private static readonly Rectangle WeaponListRect = new(44, 106, 198, 300);
    private static readonly Rectangle WeaponUpgradeButton = new(260, 376, 272, 30);
    private static float _formationScrollVisual, _shopScrollVisual, _cartScrollVisual, _weaponScrollVisual;
    private static int _weaponScroll, _weaponSlot = -1;
    private static int _weaponType;
    private static readonly List<int> FormationOrder = new();
    private static TabletUiPreferencesPlayer Preferences => Main.LocalPlayer.GetModPlayer<TabletUiPreferencesPlayer>();

    private static void RefreshFormationOrder()
    {
        FormationOrder.Clear();
        FormationOrder.AddRange(Enumerable.Range(0, TabletStudentCatalog.FormationStudents.Count)
            .OrderByDescending(i => Preferences.FavoriteStudents.Contains((int)TabletStudentCatalog.FormationStudents[i].Id)));
    }
    private static int FormationPosition(int index)
    {
        if (FormationOrder.Count != TabletStudentCatalog.FormationStudents.Count) RefreshFormationOrder();
        return FormationOrder.IndexOf(index);
    }
    private static IReadOnlyList<TabletShopProduct> VisibleProducts()
    {
        var products = _selectedCategory == TabletShopCategory.Favorites ? TabletShopCatalog.Products : TabletShopCatalog.GetProducts(_selectedCategory);
        return products.Where(p => p.Available && (_selectedCategory != TabletShopCategory.Favorites || Preferences.IsFavorite(p)))
            .ToList();
    }
    private static Rectangle ProductFavoriteRect(Rectangle card) => new(card.Right - 26, card.Y + 74, 16, 16);

    private static void SmoothLists()
    {
        if (_weaponClickFlashTicks > 0) _weaponClickFlashTicks--;
        _formationScrollVisual = Approach(_formationScrollVisual, _formationScrollRow * 106);
        _shopScrollVisual = Approach(_shopScrollVisual, _shopScrollRow * 150);
        _cartScrollVisual = Approach(_cartScrollVisual, _cartScrollIndex * 52);
        _weaponScrollVisual = Approach(_weaponScrollVisual, _weaponScroll * WeaponRowPitch);
    }
    private static float Approach(float value, float target) => Math.Abs(target - value) < .25f ? target : MathHelper.Lerp(value, target, .25f);

    // Draw and input use the same animated positions. The scissor confines partial rows.
    private static void DrawClippedList(SpriteBatch sb, Layout layout, Rectangle clip, Action draw)
    {
        GraphicsDevice device = sb.GraphicsDevice;
        sb.End();
        Rectangle oldScissor = device.ScissorRectangle;
        var rasterizer = device.RasterizerState;
        var blend = device.BlendState;
        var depth = device.DepthStencilState;
        var sampler = device.SamplerStates[0];
        Rectangle screen = layout.ToScreen(clip);
        Vector2 tl = Vector2.Transform(new Vector2(screen.Left, screen.Top), Main.UIScaleMatrix);
        Vector2 br = Vector2.Transform(new Vector2(screen.Right, screen.Bottom), Main.UIScaleMatrix);
        device.ScissorRectangle = Rectangle.Intersect(device.Viewport.Bounds, new Rectangle((int)MathF.Ceiling(tl.X), (int)MathF.Ceiling(tl.Y),
            Math.Max(1, (int)(br.X - tl.X)), Math.Max(1, (int)(br.Y - tl.Y))));
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, _legacyPresentationRasterizer, null, Main.UIScaleMatrix);
        try { draw(); }
        finally
        {
            sb.End();
            device.ScissorRectangle = oldScissor;
            sb.Begin(SpriteSortMode.Deferred, blend, sampler, depth, rasterizer, null, Main.UIScaleMatrix);
        }
    }

    private static void DrawFavoriteIcon(SpriteBatch sb, Rectangle rect, bool active, bool heart, Layout layout)
    {
        Texture2D texture = heart ? (active ? _heartActive.Value : _heartInactive.Value) : (active ? _starActive.Value : _starInactive.Value);
        DrawTextureFit(sb, texture, rect, layout);
    }
    private static void DrawAnvilIcon(SpriteBatch sb, Rectangle rect, Layout layout)
    {
        Color steel = new(173, 200, 220);
        DrawSolid(sb, new Rectangle(rect.X, rect.Y, rect.Width, 7), steel, layout);
        DrawSolid(sb, new Rectangle(rect.X + 6, rect.Y + 7, rect.Width - 12, 5), steel, layout);
        DrawSolid(sb, new Rectangle(rect.Center.X - 4, rect.Y + 12, 8, 8), steel, layout);
        DrawSolid(sb, new Rectangle(rect.X + 5, rect.Y + 20, rect.Width - 10, 6), steel, layout);
    }
    private static List<int> WeaponSlots() => Enumerable.Range(0, 58)
        .Where(i => Main.LocalPlayer.inventory[i].ModItem is StudentWeaponClass)
        .OrderByDescending(i => Preferences.IsFavoriteWeapon(Main.LocalPlayer.inventory[i].type)).ToList();
    private static Rectangle WeaponRow(int position) => new(48, 108 + position * WeaponRowPitch - (int)_weaponScrollVisual, 190, 40);

    private static void HandleUpgradeInput(Point p)
    {
        var slots = WeaponSlots();
        int wheel = PlayerInput.ScrollWheelDeltaForUI;
        if (WeaponListRect.Contains(p) && wheel != 0)
        {
            _weaponScroll = Math.Clamp(_weaponScroll + (wheel > 0 ? -1 : 1), 0, Math.Max(0, slots.Count - 6));
            PlayerInput.LockVanillaMouseScroll("KivotosMod:Weapons");
        }
    }
    private static void HandleUpgradeClick(Point p)
    {
        var slots = WeaponSlots();
        if (WeaponListRect.Contains(p))
            for (int i = 0; i < slots.Count; i++)
                if (WeaponRow(i).Contains(p))
                {
                    if (WeaponFavoriteRect(WeaponRow(i)).Contains(p))
                    {
                        Preferences.ToggleWeapon(Main.LocalPlayer.inventory[slots[i]].type);
                        _weaponScroll = 0;
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        return;
                    }
                    _weaponClickFlashSlot = slots[i];
                    _weaponClickFlashTicks = 8;
                    _weaponSlot = slots[i];
                    _weaponType = Main.LocalPlayer.inventory[_weaponSlot].type;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    return;
                }
        if (WeaponUpgradeButton.Contains(p) && SelectedWeapon() is Item item && TabletWeaponUpgrade.CanUpgrade(Main.LocalPlayer, item))
            TabletWeaponUpgrade.Request(_weaponSlot);
    }
    private static Item SelectedWeapon()
    {
        if (_weaponSlot < 0 || _weaponSlot >= 58) return null;
        Item item = Main.LocalPlayer.inventory[_weaponSlot];
        return item.type == _weaponType && item.ModItem is StudentWeaponClass ? item : null;
    }
    private static void UpgradePanel(SpriteBatch sb, Rectangle rect, Layout layout) =>
        DrawNineSlice(sb, _upgradeBorder.Value, rect, layout, Color.White, 12, gutter: 2);

    private static void DrawWeaponUpgrades(SpriteBatch sb, Layout layout, Vector2 mouse)
    {
        DrawUpgradeGrayPanel(sb, WeaponLeftPanel, layout);
        DrawUpgradeGrayPanel(sb, WeaponRightPanel, layout);
        DrawNineSlice(sb, _formationNameplateBackground.Value, WeaponTitleRect, layout, Color.White);
        DrawUpgradeLabel(sb, "武器列表", new Rectangle(52, 80, 182, 22), Color.White, layout, .4f, true);
        var slots = WeaponSlots();
        _weaponScroll = Math.Clamp(_weaponScroll, 0, Math.Max(0, slots.Count - 6));
        DrawClippedList(sb, layout, WeaponListRect, () =>
        {
            for (int i = 0; i < slots.Count; i++)
            {
                Rectangle row = WeaponRow(i);
                if (!row.Intersects(WeaponListRect)) continue;
                Item weapon = Main.LocalPlayer.inventory[slots[i]];
                bool flash = slots[i] == _weaponClickFlashSlot && _weaponClickFlashTicks > 0;
                DrawTexture(sb, flash ? _weaponCardPressed.Value : _weaponCard.Value, row, layout);
                DrawItemIcon(sb, weapon.type, new Rectangle(row.X + 4, row.Y + 4, 44, 32), layout);
                DrawUpgradeLabel(sb, weapon.Name, new Rectangle(row.X + 52, row.Y + 2, 112, 17), new Color(41, 63, 85), layout, .32f);
                DrawUpgradeLabel(sb, $"Lv.{weapon.GetGlobalItem<TabletWeaponLevel>().Level} / 10", new Rectangle(row.X + 52, row.Y + 21, 112, 16), new Color(54, 105, 147), layout, .3f);
                Rectangle star = WeaponFavoriteRect(row);
                DrawFavoriteIcon(sb, star, Preferences.IsFavoriteWeapon(weapon.type), false, layout);
                if (WeaponListRect.Contains(mouse.ToPoint()) && star.Contains(mouse.ToPoint()))
                    Main.instance.MouseText(Preferences.IsFavoriteWeapon(weapon.type) ? "取消武器收藏" : "收藏武器并置顶");
            }
        });
        if (slots.Count == 0) DrawUpgradeLabel(sb, "背包中没有学员武器", new Rectangle(52, 212, 182, 30), Color.White, layout, .34f, true);
        Rectangle preview = new(260, 82, 272, 86);
        Rectangle stats = new(260, 174, 272, 104);
        Rectangle materials = new(260, 284, 272, 86);
        UpgradePanel(sb, preview, layout); UpgradePanel(sb, stats, layout); UpgradePanel(sb, materials, layout);
        Item item = SelectedWeapon();
        bool enabled = item != null && TabletWeaponUpgrade.CanUpgrade(Main.LocalPlayer, item);
        int frameWidth = _formationSelectButton.Value.Width / 3;
        bool hover = enabled && WeaponUpgradeButton.Contains(mouse.ToPoint());
        DrawNineSlice(sb, _formationSelectButton.Value, WeaponUpgradeButton, layout,
            enabled ? Color.White : Color.White * .36f, 8, new Rectangle((hover ? 1 : 0) * frameWidth, 0, frameWidth, _formationSelectButton.Value.Height));
        if (!enabled) DrawSolid(sb, new Rectangle(268, 382, 256, 18), Color.Black * .3f, layout);
        DrawUpgradeLabel(sb, "升级", new Rectangle(268, 378, 256, 25), enabled ? Color.White : Color.LightGray, layout, .38f, true);
        if (item == null) return;
        int level = item.GetGlobalItem<TabletWeaponLevel>().Level;
        int next = level + 1;
        DrawUpgradeLabel(sb, $"Lv.{level} / 10", new Rectangle(268, 86, 256, 20), Color.White, layout, .36f);
        DrawItemIcon(sb, item.type, new Rectangle(286, 110, 220, 50), layout);
        Rectangle shieldIcon = new(preview.Right - 31, preview.Bottom - 29, 24, 24);
        sb.Draw(global::KivotosMod.Content.Skills.WeaponShieldVisuals.Texture("Icon"), layout.ToScreen(shieldIcon),
            level >= 5 ? Color.White : Color.White * .25f);
        if (shieldIcon.Contains(mouse.ToPoint())) Main.instance.MouseText(level >= 5 ? "护盾技能已解锁（默认鼠标中键）" : "武器Lv.5解锁护盾技能");
        Player player = Main.LocalPlayer;
        string damageLabel = item.DamageType == DamageClass.Magic ? "魔法伤害" : "远程伤害";
        DrawUpgradeLabel(sb, $"{damageLabel}：{player.GetWeaponDamage(item)}    暴击率：{player.GetWeaponCrit(item):0.##}%", new Rectangle(268, 179, 256, 18), Color.White, layout, .32f);
        DrawUpgradeLabel(sb, $"击退：{item.knockBack:0.##}    射速：{3600f / Math.Max(1, item.useTime):0} 发/分钟 (RPM)", new Rectangle(268, 199, 256, 18), Color.White, layout, .32f);
        DrawUpgradeLabel(sb, "特殊效果：保留武器原有效果", new Rectangle(268, 222, 256, 18), Color.White, layout, .31f);
        if (level == 10)
        {
            DrawUpgradeLabel(sb, "已达到最高等级", new Rectangle(268, 291, 256, 22), Color.White, layout, .35f);
            return;
        }
        DrawUpgradeLabel(sb, "需要击败：" + TabletWeaponUpgrade.BossNames[next], new Rectangle(268, 252, 256, 18), TabletWeaponUpgrade.BossUnlocked(next) ? Color.White : Color.Salmon, layout, .30f);
        var recipe = ((StudentWeaponClass)item.ModItem).UpgradeMaterials(next);
        for (int i = 0; i < recipe.Length; i++)
        {
            var m = recipe[i];
            Rectangle cell = new(268 + i % 4 * 64, 290 + i / 4 * 36, 60, 34);
            DrawSolid(sb, cell, Color.Black * .18f, layout);
            DrawItemIcon(sb, m.ItemType, new Rectangle(cell.X + 15, cell.Y, 30, 20), layout);
            int owned = TabletWeaponUpgrade.Count(player, m.ItemType);
            DrawUpgradeLabel(sb, $"{owned}/{m.Count}", new Rectangle(cell.X + 1, cell.Y + 21, 58, 12), owned >= m.Count ? Color.White : Color.Salmon, layout, .27f, true);
            if (cell.Contains(mouse.ToPoint())) ShowItemTooltip(m.ItemType);
        }
        long cost = TabletWeaponUpgrade.Cost(next);
        DrawTinyIcon(sb, _creditIcon.Value, new Rectangle(268, 348, 14, 9), layout);
        DrawUpgradeLabel(sb, $"联合币：{cost:N0}", new Rectangle(287, 343, 236, 20), TabletWallet.GetTotalCredits(player) >= cost ? Color.White : Color.Salmon, layout, .32f);
    }
}
