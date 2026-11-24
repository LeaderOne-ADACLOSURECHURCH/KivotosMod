using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;
namespace KivotosMod.Content.Items.Vanity.Kayoko;
[Autoload(Side = ModSide.Client)]
public sealed class KayokoSideLocksLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    internal static bool Grounded(PlayerDrawSet info) => !info.drawPlayer.mount.Active
        && info.drawPlayer.itemAnimation <= 0 && System.Math.Abs(info.drawPlayer.velocity.Y) < .1f;
    internal static bool HasHair(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost
        && !info.drawPlayer.invis && KayokoVanitySet.HeadVisible(info.drawPlayer);
    public override bool GetDefaultVisibility(PlayerDrawSet info) => HasHair(info) && !Grounded(info);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    internal static void DrawHair(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture,
        "KivotosMod/Content/Items/Vanity/Kayoko/KayokoSideLocks", head: true);
    protected override void Draw(ref PlayerDrawSet info) => DrawHair(ref info);
}

[Autoload(Side = ModSide.Client)]
public sealed class KayokoIdleSideLocksLayer : PlayerDrawLayer
{
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => KayokoSideLocksLayer.HasHair(info) && KayokoSideLocksLayer.Grounded(info);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.ArmOverItem, PlayerDrawLayers.BladedGlove);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => KayokoSideLocksLayer.DrawHair(ref info);
}
