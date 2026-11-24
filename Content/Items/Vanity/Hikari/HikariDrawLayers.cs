using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Hikari;

[Autoload(Side = ModSide.Client)]
public sealed class HikariBackHairLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && HikariVanitySet.HeadVisible(info.drawPlayer) && !StudentSpriteVariants.Hikari2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Hikari/HikariBackHair", head: true, legs: false);
}

[Autoload(Side = ModSide.Client)]
public sealed class HikariTailLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && HikariVanitySet.BodyVisible(info.drawPlayer) && !StudentSpriteVariants.HikariBody2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Nozomi/NozomiTail", head: false, legs: true);
}

[Autoload(Side = ModSide.Client)]
public sealed class HikariHaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && HikariVanitySet.HeadVisible(info.drawPlayer) && !StudentSpriteVariants.Hikari2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info)
        => StudentSpriteDraw.Halo(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Hikari/HikariHalo", 4);
}

[Autoload(Side = ModSide.Client)]
public sealed class Hikari2x2BackHairLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && StudentSpriteVariants.Hikari2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Hikari/Hikari2x2BackHair", head: true, legs: false);
}

[Autoload(Side = ModSide.Client)]
public sealed class Hikari2x2TailLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && StudentSpriteVariants.HikariBody2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => false;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Hikari/Hikari2x2Tail", head: false, legs: true);
}

[Autoload(Side = ModSide.Client)]
public sealed class Hikari2x2HaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis && StudentSpriteVariants.Hikari2x2(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => StudentSpriteDraw.Halo(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/Hikari/Hikari2x2Halo", 4, 44);
}
