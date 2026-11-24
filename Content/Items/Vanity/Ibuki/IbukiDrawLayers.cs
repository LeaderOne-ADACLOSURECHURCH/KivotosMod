using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Ibuki;

[Autoload(Side = ModSide.Client)]
public sealed class IbukiTorsoLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && IbukiVanitySet.BodyVisible(info.drawPlayer) && !StudentSpriteVariants.IbukiBody2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Torso);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Ibuki/IbukiTorso", head: false, legs: false);
}

[Autoload(Side = ModSide.Client)]
public sealed class IbukiBackHairLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && IbukiVanitySet.HeadVisible(info.drawPlayer) && !StudentSpriteVariants.Ibuki2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Ibuki/IbukiBackHair", head: true, legs: false);
}

[Autoload(Side = ModSide.Client)]
public sealed class IbukiTailLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && IbukiVanitySet.BodyVisible(info.drawPlayer) && !StudentSpriteVariants.IbukiBody2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Ibuki/IbukiTail", head: false, legs: true);
}

[Autoload(Side = ModSide.Client)]
public sealed class IbukiWingsLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && IbukiVanitySet.FullSetVisible(info.drawPlayer) && !StudentSpriteVariants.IbukiBody2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Ibuki/IbukiWings", head: false, legs: false);
}

[Autoload(Side = ModSide.Client)]
public sealed class IbukiCoatLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && IbukiVanitySet.BodyVisible(info.drawPlayer) && !StudentSpriteVariants.IbukiBody2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Head, PlayerDrawLayers.ArmOverItem);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Ibuki/IbukiCoat", head: false, legs: true);
}

[Autoload(Side = ModSide.Client)]
public sealed class IbukiHaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && IbukiVanitySet.HeadVisible(info.drawPlayer) && !StudentSpriteVariants.Ibuki2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Halo(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Ibuki/IbukiHalo", 6);
}

[Autoload(Side = ModSide.Client)]
public sealed class Ibuki2x2TailLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && StudentSpriteVariants.IbukiBody2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Ibuki/Ibuki2x2Tail", head: false, legs: true);
}

[Autoload(Side = ModSide.Client)]
public sealed class Ibuki2x2HaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && StudentSpriteVariants.Ibuki2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Halo(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Ibuki/Ibuki2x2Halo", 2, 60);
}
