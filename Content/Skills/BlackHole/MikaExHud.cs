using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace KivotosMod.Content.Skills.BlackHole;

[Autoload(Side = ModSide.Client)]
public sealed class MikaExHud : ModSystem
{
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index >= 0)
            layers.Insert(index, new LegacyGameInterfaceLayer("Kivotos: Mika EX", Draw, InterfaceScaleType.UI));
    }

    private static bool Draw()
    {
        var player = Main.LocalPlayer.GetModPlayer<MikaExPlayer>();
        var state = MikaExDomain.State;
        bool inDomain = MikaExDomain.InRange(Main.LocalPlayer);
        bool openingOwner=MikaExDomain.OpeningTick>=0 && !MikaExDomain.Active &&
            MikaExDomain.Owner==Main.myPlayer;
        if (Main.LocalPlayer.dead || Main.mapFullscreen || (!inDomain && !player.Equipped && !openingOwner))
            return true;

        var batch = Main.spriteBatch;
        var pixel = TextureAssets.MagicPixel.Value;
        int width = 14;
        int screenHeight = Main.screenHeight;
        int x = Main.screenWidth - 52 - width;
        int y = Main.mapEnabled && Main.mapStyle == 1 ? 90 + (int)(240 * Main.MapScale) + 45 : 185;
        y = Math.Min(y, Math.Max(75, screenHeight - 140));
        int height = Math.Min(176, screenHeight - y - 64);
        if (height < 30)
            return true;
        var frame = new Rectangle(x, y, width, height);
        int current = inDomain ? Math.Min(MikaExState.MaxDomainTime, state.DomainTime) / 60 :
            MikaExDomain.Active ? 0 : player.Charge;
        int max = inDomain ? MikaExState.MaxDomainTime / 60 : MikaExState.MaxCharge;
        int fill = (int)MathF.Round((height - 6) * current / (float)max);
        bool ready = inDomain ? state.Reserve > 0 && state.CanCastManually : current == max && !openingOwner;

        batch.Draw(pixel, frame, new Color(104, 54, 152));
        batch.Draw(pixel, new Rectangle(x + 2, y + 2, width - 4, height - 4), new Color(26, 16, 48));
        for (int row = 0; row < fill; row += 4)
        {
            var color = Color.Lerp(new Color(89, 45, 181), new Color(251, 122, 225), row / (float)height);
            batch.Draw(pixel, new Rectangle(x + 3, y + height - 3 - fill + row, width - 6, Math.Min(4, fill - row)), color);
        }

        if (inDomain)
        {
            for (int step = 1; step < 3; step++)
            {
                int mark = y + height - 3 - (height - 6) * step / 3;
                batch.Draw(pixel, new Rectangle(x - 4, mark, 14, 2), new Color(239, 204, 255));
                Utils.DrawBorderString(batch, (step * 30).ToString(), new Vector2(x - 28, mark - 7), Color.Lavender, .58f);
            }

            var disk = ModContent.Request<Texture2D>("KivotosMod/Assets/Skills/MikaDomain/Resonance/DomainIcon").Value;
            batch.Draw(disk, new Rectangle(x - 7, y - 37, 32, 32), Color.White);
            var infinity = ModContent.Request<Texture2D>("KivotosMod/Assets/Skills/MikaDomain/Resonance/FlightIcon").Value;
            batch.Draw(infinity, new Rectangle(x - 5, y + height + 19, 28, 16), Color.Plum);
        }
        string value = inDomain ? $"{current}/{max}s" : $"{current}/{max}";
        Utils.DrawBorderString(batch, value, new Vector2(x + width / 2, y + height + 2),
            Color.Lavender, .62f, .5f);
        if (ready)
        {
            int length = 2 * (width + height);
            int offset = (int)(Main.GameUpdateCount % (ulong)length) * 3;
            for (int i = 0; i < length; i += 3)
            {
                int pos = (i + offset) % length;
                int px = pos < height ? x : pos < height + width ? x + pos - height :
                    pos < 2 * height + width ? x + width - 1 : x + length - pos - 1;
                int py = pos < height ? y + pos : pos < height + width ? y + height - 1 :
                    pos < 2 * height + width ? y + 2 * height + width - pos - 1 : y;
                float energy = MathF.Pow(.5f + .5f * MathF.Cos(i * MathHelper.TwoPi * 3 / length), 5);
                batch.Draw(pixel, new Rectangle(px - 1, py - 1, 3, 3), Color.Lerp(Color.MediumPurple, Color.White, energy));
            }
        }
        return true;
    }
}
