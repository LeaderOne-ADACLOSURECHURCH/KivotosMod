using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Mutsuki;

[Autoload(Side = ModSide.Client)]
public sealed class MutsukiTorsoLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && MutsukiVanitySet.BodyVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Torso);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Mutsuki/MutsukiTorso", head: false, legs: false);
}

[Autoload(Side = ModSide.Client)]
public sealed class MutsukiBackHairLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && MutsukiVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Mutsuki/MutsukiBackHair", head: true, legs: false, offsetX: -8f);
}

[Autoload(Side = ModSide.Client)]
public sealed class MutsukiHaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && MutsukiVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Halo(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Mutsuki/MutsukiHalo", 9);
}
