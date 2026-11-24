using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Hoshino;

[Autoload(Side = ModSide.Client)]
// This native 40x1120 torso sheet uses 2x2 authored pixels like the source
// 360x224 composite arm texture. Do not stretch or scale its pixels at runtime.
public sealed class HoshinoTorsoLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        => !drawInfo.drawPlayer.dead && !drawInfo.drawPlayer.ghost && HoshinoVanitySet.BodyVisible(drawInfo.drawPlayer);
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Torso);
    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player p = drawInfo.drawPlayer;
        _texture ??= ModContent.Request<Texture2D>("KivotosMod/Content/Items/Vanity/Hoshino/HoshinoTorso", AssetRequestMode.ImmediateLoad);
        Vector2 pos = new(
            (int)(drawInfo.Position.X - Main.screenPosition.X - p.bodyFrame.Width / 2f + p.width / 2f),
            (int)(drawInfo.Position.Y - Main.screenPosition.Y + p.height - p.bodyFrame.Height + 4f));
        pos += p.bodyPosition + new Vector2(p.bodyFrame.Width / 2f, p.bodyFrame.Height / 2f);
        drawInfo.DrawDataCache.Add(new DrawData(_texture.Value, pos, p.bodyFrame,
            drawInfo.colorArmorBody, p.bodyRotation, drawInfo.bodyVect, 1f, drawInfo.playerEffect, 0f)
        { shader = drawInfo.cBody });
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class HoshinoBackHairLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost
            && HoshinoVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player p = drawInfo.drawPlayer;
        _texture ??= ModContent.Request<Texture2D>("KivotosMod/Content/Items/Vanity/Hoshino/HoshinoBackHair", AssetRequestMode.ImmediateLoad);
        Texture2D texture = _texture.Value;
        Vector2 pos = drawInfo.helmetOffset + new Vector2(
            (int)(drawInfo.Position.X - Main.screenPosition.X - p.bodyFrame.Width / 2f + p.width / 2f),
            (int)(drawInfo.Position.Y - Main.screenPosition.Y + p.height - p.bodyFrame.Height + 4f))
            + p.headPosition + drawInfo.headVect;
        drawInfo.DrawDataCache.Add(new DrawData(texture, pos, p.bodyFrame,
            drawInfo.colorArmorHead, p.headRotation, drawInfo.headVect, 1f, drawInfo.playerEffect, 0f)
        { shader = drawInfo.cHead });
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class HoshinoHaloLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost
            && HoshinoVanitySet.HeadVisible(info.drawPlayer);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player p = drawInfo.drawPlayer;
        _texture ??= ModContent.Request<Texture2D>("KivotosMod/Content/Items/Vanity/Hoshino/HoshinoHalo", AssetRequestMode.ImmediateLoad);
        Texture2D texture = _texture.Value;
        Vector2 pos = drawInfo.helmetOffset + new Vector2(
            (int)(drawInfo.Position.X - Main.screenPosition.X - p.bodyFrame.Width / 2f + p.width / 2f),
            (int)(drawInfo.Position.Y - Main.screenPosition.Y + p.height - p.bodyFrame.Height + 4f))
            + p.headPosition + drawInfo.headVect;
        // FIX2: this halo atlas is not centered above the character's head.
        // Raise by 37px to clear the top of the 40x56 head frame, while
        // keeping head rotation / horizontal facing tied to the same anchor.
        const float haloRaise = 37f;
        Vector2 origin = new(texture.Width / 2f, texture.Height / 2f + 6f + haloRaise);
        drawInfo.DrawDataCache.Add(new DrawData(texture, pos, new Rectangle(0, 0, texture.Width, texture.Height),
            drawInfo.colorArmorHead, p.headRotation, origin, 1f, drawInfo.playerEffect, 0f)
        { shader = drawInfo.cHead });
    }
}

