using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Nagisa;

[Autoload(Side = ModSide.Client)]
public sealed class NagisaBackHairLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && NagisaVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture,
        "KivotosMod/Content/Items/Vanity/Nagisa/NagisaBackHair", head: true, legs: false, frameHeight: 56, paddingTop: 0f);
}

[Autoload(Side = ModSide.Client)]
public sealed class NagisaWingsLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && NagisaVanitySet.FullSetVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture,
        "KivotosMod/Content/Items/Vanity/Nagisa/NagisaWings", head: false, legs: false, frameHeight: 56, paddingTop: 0f);
}

[Autoload(Side = ModSide.Client)]
public sealed class NagisaHaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && NagisaVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Halo(ref info, ref _texture,
        "KivotosMod/Content/Items/Vanity/Nagisa/Nagisa2x2Halo", 4, 40);
}
