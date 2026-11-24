using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace KivotosMod.Content.Tablet;

public sealed partial class TabletUiSystem
{
    private static Asset<Texture2D> _craftingBackground, _weaponCard, _weaponCardPressed, _upgradeBorder;
    private static Asset<Texture2D> _heartActive, _heartInactive, _starActive, _starInactive;
    private static Asset<Texture2D> _shopFavoriteNormal, _shopFavoriteHover, _shopFavoritePressed, _shopFavoriteSelectedHover;
    private static readonly Rectangle WeaponLeftPanel = new(40, 74, 206, 336);
    private static readonly Rectangle WeaponRightPanel = new(252, 74, 288, 336);
    private static readonly Rectangle WeaponTitleRect = new(46, 79, 194, 24);
    private const int WeaponRowPitch = 44;
    private static int _weaponClickFlashSlot = -1;
    private static int _weaponClickFlashTicks;

    private static void LoadUpgradeSkin()
    {
        Asset<Texture2D> Load(string name) => ModContent.Request<Texture2D>("KivotosMod/Assets/Tablet/Upgrades/" + name, AssetRequestMode.ImmediateLoad);
        _craftingBackground = Load("CraftingBackground");
        _weaponCard = Load("WeaponCard"); _weaponCardPressed = Load("WeaponCardPressed");
        _upgradeBorder = Load("WhiteBorder");
        _heartActive = Load("HeartActive"); _heartInactive = Load("HeartInactive");
        _starActive = Load("StarActive"); _starInactive = Load("StarInactive");
        _shopFavoriteNormal = Load("ShopFavoriteNormal"); _shopFavoriteHover = Load("ShopFavoriteHover");
        _shopFavoritePressed = Load("ShopFavoritePressed");
        _shopFavoriteSelectedHover = Load("ShopFavoriteSelectedHover");
    }
    private static void UnloadUpgradeSkin()
    {
        MailTextures.Clear(); _selectedMail = null; _mailScroll = _mailBodyScroll = 0;
        _craftingBackground = _weaponCard = _weaponCardPressed = _upgradeBorder = null;
        _heartActive = _heartInactive = _starActive = _starInactive = null;
        _shopFavoriteNormal = _shopFavoriteHover = _shopFavoritePressed = _shopFavoriteSelectedHover = null;
        _weaponClickFlashSlot = -1; _weaponClickFlashTicks = 0;
    }

    private static void DrawCraftingBackground(SpriteBatch sb, Layout layout)
    {
        Texture2D texture = _craftingBackground.Value;
        // Cover the screen without stretching the supplied background's proportions.
        float aspect = DisplayRect.Width / (float)DisplayRect.Height;
        int width = Math.Min(texture.Width, (int)MathF.Round(texture.Height * aspect));
        int height = Math.Min(texture.Height, (int)MathF.Round(texture.Width / aspect));
        Rectangle source = new((texture.Width - width) / 2, (texture.Height - height) / 2, width, height);
        DrawTexture(sb, texture, DisplayRect, layout, source);
    }

    // Keep authored corners intact while resizing the provided nine-slice frame.
    private static void DrawNineSlice(SpriteBatch sb, Texture2D texture, Rectangle target, Layout layout,
        Color tint, int border = 6, Rectangle? source = null, int gutter = 0)
    {
        Rectangle src = source ?? texture.Bounds;
        int edge = Math.Min(border, Math.Min(Math.Min(src.Width, src.Height), Math.Min(target.Width, target.Height)) / 2);
        // WhiteBorder's authored nine patches have 2px separators between them.
        // Those separators are atlas padding, and must never be stretched into gaps.
        int[] sx = { src.X, src.X + edge + gutter, src.Right - edge };
        int[] sy = { src.Y, src.Y + edge + gutter, src.Bottom - edge };
        int[] sw = { edge, src.Width - 2 * (edge + gutter), edge };
        int[] sh = { edge, src.Height - 2 * (edge + gutter), edge };
        int[] dx = { target.X, target.X + edge, target.Right - edge, target.Right };
        int[] dy = { target.Y, target.Y + edge, target.Bottom - edge, target.Bottom };
        // Round shared boundaries, never each patch's width independently. Adjacent
        // pieces then share exactly the same screen edge at fractional UI scales.
        int[] screenX = new int[4];
        int[] screenY = new int[4];
        for (int i = 0; i < 4; i++)
        {
            Vector2 position = layout.ToScreen(new Vector2(dx[i], dy[i]));
            screenX[i] = (int)MathF.Round(position.X);
            screenY[i] = (int)MathF.Round(position.Y);
        }
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
            {
                Rectangle destination = new(screenX[x], screenY[y], screenX[x + 1] - screenX[x], screenY[y + 1] - screenY[y]);
                if (destination.Width > 0 && destination.Height > 0)
                    sb.Draw(texture, destination, new Rectangle(sx[x], sy[y], sw[x], sh[y]), tint);
            }
    }
    private static void DrawUpgradeLabel(SpriteBatch sb, string text, Rectangle bounds, Color color, Layout layout,
        float scale = .34f, bool centered = false)
    {
        // Use exactly the same font as Formation's Select label, including Chinese glyphs.
        DynamicSpriteFont font = _uiFontBold?.Value ?? FontAssets.MouseText.Value;
        Vector2 measure = font.MeasureString(text);
        scale = Math.Min(scale, Math.Min(bounds.Width / Math.Max(1f, measure.X), bounds.Height / Math.Max(1f, measure.Y)));
        Vector2 position = new(bounds.X, bounds.Y + (bounds.Height - measure.Y * scale) * .5f);
        if (centered) position.X += (bounds.Width - measure.X * scale) * .5f;
        DrawText(sb, text, position, color, scale, layout, bold: true);
    }
    private static void DrawUpgradeGrayPanel(SpriteBatch sb, Rectangle panel, Layout layout) =>
        DrawNineSlice(sb, _formationSelectionBackground.Value, panel, layout, Color.White * .72f, 8);

    private static void DrawShopFavoriteCategory(SpriteBatch sb, Rectangle rect, bool selected, bool hover, Layout layout)
    {
        Texture2D texture = selected
            ? (hover ? _shopFavoriteSelectedHover.Value : _shopFavoritePressed.Value)
            : (hover ? _shopFavoriteHover.Value : _shopFavoriteNormal.Value);
        // Match the other categories' native overhang around their 24px hit boxes.
        DrawNativeCentered(sb, texture, rect, layout);
    }
    private static Rectangle WeaponFavoriteRect(Rectangle row) => new(row.Right - 23, row.Y + 3, 18, 18);
}
