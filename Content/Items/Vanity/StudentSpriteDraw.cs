using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity;

// Shared transforms for the supplied 40x56 / 56x56 costume layers.
internal static class StudentSpriteDraw
{
    // Native wings slots: Hoverboard, Lazure barrier platform, Celestial Starboard.
    internal static bool SpecialFlightBoard(Player player) => player.wings is 22 or 28 or 45;

    internal static Texture2D LoadedTexture(ref Asset<Texture2D> asset, string path)
    {
        // Asset.Value can be the 1x1 placeholder until an asynchronous request
        // completes. Drawing player frames requires the real dimensions now.
        if (asset == null || !asset.IsLoaded)
            asset = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
        return asset.Value;
    }

    private static Vector2 Anchor(ref PlayerDrawSet info, bool head)
    {
        Player p = info.drawPlayer;
        Vector2 pos = new(
            (int)(info.Position.X - Main.screenPosition.X - p.bodyFrame.Width / 2f + p.width / 2f),
            (int)(info.Position.Y - Main.screenPosition.Y + p.height - p.bodyFrame.Height + 4f));
        return head ? pos + info.helmetOffset + p.headPosition + info.headVect
            : pos + p.bodyPosition + info.bodyVect;
    }

    internal static void Strip(ref PlayerDrawSet info, ref Asset<Texture2D> asset, string path,
        bool head = false, bool legs = false, float offsetX = 0f, int frameHeight = 56, float paddingTop = 0f)
    {
        Texture2D texture = LoadedTexture(ref asset, path);
        Player p = info.drawPlayer;
        int frame = (legs ? p.legFrame.Y : p.bodyFrame.Y) / 56;
        int y = System.Math.Clamp(frame * frameHeight, 0, texture.Height - frameHeight);
        Vector2 origin = head ? info.headVect : info.bodyVect;
        // Wide hair/tail sheets have 8px padding on each side of the 40px body.
        origin.X += (texture.Width - 40) / 2f;
        origin.X -= (info.playerEffect & SpriteEffects.FlipHorizontally) != 0 ? -offsetX : offsetX;
        origin.Y += (info.playerEffect & SpriteEffects.FlipVertically) != 0 ? frameHeight - 56 - paddingTop : paddingTop;
        info.DrawDataCache.Add(new DrawData(texture, Anchor(ref info, head), new Rectangle(0, y, texture.Width, frameHeight),
            head ? info.colorArmorHead : info.colorArmorBody, head ? p.headRotation : p.bodyRotation,
            origin, 1f, info.playerEffect, 0f) { shader = head ? info.cHead : info.cBody });
    }

    internal static void Halo(ref PlayerDrawSet info, ref Asset<Texture2D> asset, string path, int headTop,
        int? opaqueBottom = null)
    {
        Texture2D texture = LoadedTexture(ref asset, path);
        Player p = info.drawPlayer;
        Vector2 pos = Anchor(ref info, true);
        pos.Y += 1.25f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * System.Math.PI * 2 / 2.4) * p.gravDir;
        // Float the bottom edge 3px above the supplied head; retain native halo size.
        Vector2 origin = new(texture.Width / 2f, (opaqueBottom ?? texture.Height) + 31f - headTop);
        if ((info.playerEffect & SpriteEffects.FlipVertically) != 0) origin.Y = texture.Height - origin.Y;
        info.DrawDataCache.Add(new DrawData(texture, pos, null, Color.White * (1f - info.shadow),
            p.headRotation, origin, 1f, info.playerEffect, 0f) { shader = info.cHead });
    }
}
