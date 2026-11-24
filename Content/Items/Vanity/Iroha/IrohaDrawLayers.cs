using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Iroha;

[Autoload(Side = ModSide.Client)]
public sealed class IrohaTorsoLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && IrohaVanitySet.BodyVisible(info.drawPlayer) && !StudentSpriteVariants.IrohaBody2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Torso);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Iroha/IrohaTorso", head: false, legs: false);
}

[Autoload(Side = ModSide.Client)]
public sealed class IrohaBackHairLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && IrohaVanitySet.HeadVisible(info.drawPlayer) && !StudentSpriteVariants.Iroha2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Iroha/IrohaBackHair", head: true, legs: false);
}

[Autoload(Side = ModSide.Client)]
public sealed class IrohaCoatLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && IrohaVanitySet.BodyVisible(info.drawPlayer) && !StudentSpriteVariants.IrohaBody2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Head, PlayerDrawLayers.ArmOverItem);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Iroha/IrohaCoat", head: false, legs: true);
}

[Autoload(Side = ModSide.Client)]
public sealed class IrohaHaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && IrohaVanitySet.HeadVisible(info.drawPlayer) && !StudentSpriteVariants.Iroha2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Halo(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Iroha/IrohaHalo", 4);
}

[Autoload(Side = ModSide.Client)]
public sealed class Iroha2x2BackHairLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && StudentSpriteVariants.Iroha2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Iroha/Iroha2x2BackHair", head: true, legs: false);
}

[Autoload(Side = ModSide.Client)]
public sealed class Iroha2x2CoatLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && StudentSpriteVariants.IrohaBody2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Head, PlayerDrawLayers.ArmOverItem);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Iroha/Iroha2x2Coat", head: false, legs: true);
}

[Autoload(Side = ModSide.Client)]
public sealed class Iroha2x2HaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && StudentSpriteVariants.Iroha2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Halo(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Iroha/Iroha2x2Halo", 6, 48);
}
