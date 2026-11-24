using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Haruka;

[Autoload(Side = ModSide.Client)]
public sealed class HarukaTorsoLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && HarukaVanitySet.BodyVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Torso);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Haruka/HarukaTorso", head: false, legs: false);
}

[Autoload(Side = ModSide.Client)]
public sealed class HarukaHaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && HarukaVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Halo(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Haruka/HarukaHalo", 5);
}

[Autoload(Side = ModSide.Client)]
public sealed class HarukaSideLocksLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    internal static bool Grounded(PlayerDrawSet info) => !info.drawPlayer.mount.Active
        && info.drawPlayer.itemAnimation <= 0 && System.Math.Abs(info.drawPlayer.velocity.Y) < .1f;
    internal static bool HasHair(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost
        && !info.drawPlayer.invis && HarukaVanitySet.HeadVisible(info.drawPlayer);
    public override bool GetDefaultVisibility(PlayerDrawSet info) => HasHair(info) && !Grounded(info);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    internal static void DrawHair(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture,
        "KivotosMod/Content/Items/Vanity/Haruka/HarukaSideLocks", head: true);
    protected override void Draw(ref PlayerDrawSet info) => DrawHair(ref info);
}

[Autoload(Side = ModSide.Client)]
public sealed class HarukaIdleSideLocksLayer : PlayerDrawLayer
{
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => HarukaSideLocksLayer.HasHair(info) && HarukaSideLocksLayer.Grounded(info);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.ArmOverItem, PlayerDrawLayers.BladedGlove);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => HarukaSideLocksLayer.DrawHair(ref info);
}
