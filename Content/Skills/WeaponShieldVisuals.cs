using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace KivotosMod.Content.Skills;

[Autoload(Side = ModSide.Client)]
public sealed class WeaponShieldVisuals : ModSystem
{
    // Published PNGs are premultiplied by Tools/ShieldEx/prepare_alpha.py.
    // tML's rawimg loader preserves those bytes. One + One adds light without
    // multiplying alpha twice or dimming the character underneath.
    private static BlendState _shieldBlend;
    private static readonly Dictionary<string, Asset<Texture2D>> Textures = new();
    internal static Texture2D Texture(string name)
    {
        if (!Textures.TryGetValue(name, out var asset))
            Textures[name] = asset = ModContent.Request<Texture2D>("KivotosMod/Assets/Skills/Shield/" + name, AssetRequestMode.ImmediateLoad);
        return asset.Value;
    }
    private static Texture2D _softBreak;
    internal static Texture2D BreakTexture()
    {
        if (_softBreak != null) return _softBreak;
        var source = Texture("Break");
        var pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            byte gray = (byte)((c.R * 54 + c.G * 183 + c.B * 19) / 256);
            // Published RGB is already premultiplied. Desaturate without changing alpha.
            pixels[i] = new Color((byte)((c.R + gray) / 2), (byte)((c.G + gray) / 2), (byte)((c.B + gray) / 2), c.A);
        }
        _softBreak = new Texture2D(Main.graphics.GraphicsDevice, source.Width, source.Height);
        _softBreak.SetData(pixels);
        return _softBreak;
    }
    public override void Unload()
    {
        var blend = _shieldBlend; _shieldBlend = null;
        if (blend != null) Main.QueueMainThreadAction(blend.Dispose);
        Textures.Clear();
        var texture = _softBreak; _softBreak = null;
        if (texture != null) Main.QueueMainThreadAction(texture.Dispose);
    }
    public override void PostDrawTiles()
    {
        if (Main.gameMenu) return;
        _shieldBlend ??= new BlendState {
            ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One
        };
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, _shieldBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            foreach (Player player in Main.ActivePlayers)
                if (!player.dead && !player.ghost) WeaponShieldRenderer.Draw(Main.spriteBatch, player);
        }
        finally { Main.spriteBatch.End(); }
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        layers.Insert(index < 0 ? layers.Count : index, new LegacyGameInterfaceLayer("KivotosMod: Weapon shield", DrawHud, InterfaceScaleType.UI));
    }
    private static bool DrawHud()
    {
        if (Main.gameMenu || Main.LocalPlayer.dead || Main.LocalPlayer.ghost) return true;
        var player = Main.LocalPlayer.GetModPlayer<WeaponShieldPlayer>();
        if (!player.Unlocked) return true;
        var state = player.Shield;
        // InterfaceScaleType.UI already calls PlayerInput.SetZoom_UI, which converts
        // screen dimensions and mouse coordinates to UI units. Do not divide twice.
        Vector2 position = new(Main.screenWidth - 310, Main.screenHeight - 110);
        Color color = state.CooldownLeft == 0 ? Color.White : new Color(100, 125, 150);
        Main.spriteBatch.Draw(Texture("Icon"), new Rectangle((int)position.X, (int)position.Y, 40, 40), color);
        string label = state.Active ? $"{state.Health}/100" : state.CooldownLeft > 0 ? $"{(state.CooldownLeft + 59) / 60}s" : "就绪";
        Utils.DrawBorderString(Main.spriteBatch, label, position + new Vector2(20, 43), Color.White, .65f, .5f);
        var keys = WeaponShieldPlayer.ShieldKey?.GetAssignedKeys();
        string key = keys != null && keys.Count > 0 ? keys[0] : "未绑定";
        Utils.DrawBorderString(Main.spriteBatch, key, position + new Vector2(20, -16), Color.LightBlue, .55f, .5f);
        if (new Rectangle((int)position.X, (int)position.Y, 40, 40).Contains(Main.MouseScreen.ToPoint()))
            Main.instance.MouseText("护盾：吸收100伤害，持续10秒，冷却20秒");
        return true;
    }
}

internal static class WeaponShieldRenderer
{
    internal static void Draw(SpriteBatch sb, Player player)
    {
        var state = player.GetModPlayer<WeaponShieldPlayer>().Shield;
        Vector2 center = player.Center - Main.screenPosition + new Vector2(0, player.gfxOffY);
        if (player.mount.Active && player.mount.Type == Terraria.ID.MountID.WitchBroom
            && player.GetModPlayer<global::KivotosMod.Content.Tablet.TabletStudentAppearancePlayer>().UsesStudentOutfit())
            center.Y -= 11f * player.gravDir;
        if (state.Active)
        {
            if (state.Age >= 30 || state.HitTicks > 0)
                Add(sb, center, "Idle", 100, state.Age / WeaponShieldState.FrameTicks % 552, 24);
            else Add(sb, center, "Spawn", 100, Math.Min(14, state.Age / WeaponShieldState.FrameTicks));
            if (state.HitTicks > 0)
            {
                int clock = state.HitDirection;
                bool reverse = clock >= 3 && clock <= 8;
                if (reverse) clock = (clock + 6) % 12;
                int frame = (30 - state.HitTicks) / WeaponShieldState.FrameTicks;
                Add(sb, center, "Hit" + (clock == 0 ? 12 : clock), 100, reverse ? 14 - frame : frame);
            }
        }
        if (state.BreakTicks > 0)
            Add(sb, center, "Break", 250, (44 - state.BreakTicks) / WeaponShieldState.FrameTicks);
    }
    private static void Add(SpriteBatch sb, Vector2 center, string name, int size, int frame, int columns = 1)
    {
        var texture = name == "Break" ? WeaponShieldVisuals.BreakTexture() : WeaponShieldVisuals.Texture(name);
        Rectangle source = new(frame % columns * size, frame / columns * size, size, size);
        // The AE sheets already contain a dense translucent fill. Keep the rider visible.
        // Multiply RGB and alpha together for Terraria's premultiplied-alpha draw pass.
        float opacity = name == "Idle" ? .30f : name.StartsWith("Hit") ? .60f : name == "Break" ? .30f : .40f;
        sb.Draw(texture, center, source, Color.White * opacity, 0,
            new Vector2(size / 2f), .8f, SpriteEffects.None, 0);
    }
}
