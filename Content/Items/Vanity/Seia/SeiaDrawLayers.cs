using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Seia;

[Autoload(Side = ModSide.Client)]
public sealed class SeiaBackHairLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && SeiaVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture,
        "KivotosMod/Content/Items/Vanity/Seia/SeiaBackHair", head: true, legs: false, frameHeight: 56, paddingTop: 0f);
}

[Autoload(Side = ModSide.Client)]
public sealed class SeiaEarsLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && SeiaVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture,
        "KivotosMod/Content/Items/Vanity/Seia/SeiaEars", head: true, legs: false, offsetX: -7f, frameHeight: 60, paddingTop: 4f);
}

[Autoload(Side = ModSide.Client)]
public sealed class SeiaTailLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && SeiaVanitySet.BodyVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture,
        "KivotosMod/Content/Items/Vanity/Seia/SeiaTail", head: false, legs: true, offsetX: -7f, frameHeight: 60, paddingTop: 4f);
}

[Autoload(Side = ModSide.Client)]
public sealed class SeiaHaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && SeiaVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Halo(ref info, ref _texture,
        "KivotosMod/Content/Items/Vanity/Seia/Seia2x2Halo", 6, 24);
}
