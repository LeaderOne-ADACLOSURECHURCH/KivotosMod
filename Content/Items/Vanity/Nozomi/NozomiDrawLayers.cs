using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Nozomi;

[Autoload(Side = ModSide.Client)]
public sealed class NozomiBackHairLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && NozomiVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Nozomi/NozomiBackHair", head: true, legs: false, offsetX: -8f);
}

[Autoload(Side = ModSide.Client)]
public sealed class NozomiTailLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && NozomiVanitySet.BodyVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Nozomi/NozomiTail", head: false, legs: true);
}

[Autoload(Side = ModSide.Client)]
public sealed class NozomiHaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && NozomiVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Halo(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Nozomi/NozomiHalo", 4);
}
