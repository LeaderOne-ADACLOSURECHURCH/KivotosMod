using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;

namespace KivotosMod.Content.Skills.BlackHole;

internal sealed class MikaDomainSilhouette : IDisposable
{
    private delegate void SpriteDraw(SpriteBatch batch, Texture2D texture, float sx, float sy, float sw, float sh,
        float dx, float dy, float dw, float dh, Color color, float ox, float oy, float sin, float cos, float depth, byte effects);
    private delegate void TileDraw(TileBatch batch, Texture2D texture, Vector4 destination, Rectangle? source,
        VertexColors colors, float rotation, Vector2 origin, SpriteEffects effects, float depth);

    private readonly Hook _spriteHook;
    private readonly Hook _tileHook;
    private float _brightness = 1f;

    internal MikaDomainSilhouette()
    {
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
        _spriteHook = new Hook(typeof(SpriteBatch).GetMethod("PushSprite", instance), DrawSprite);
        _tileHook = new Hook(typeof(TileBatch).GetMethod("InternalDraw", instance), DrawTile);
        On_PlayerDrawLayers.DrawPlayer_RenderAllLayers += DrawPlayer;
        On_Main.DrawMap += DrawMap;
    }

    internal void Begin(float opacity) => _brightness = 1f - opacity;
    internal void End() => _brightness = 1f;

    private Color Tint(Color color)
    {
        // Keep the source coverage so the backdrop remains visible through transparent pixels.
        color.R = (byte)(color.R * _brightness);
        color.G = (byte)(color.G * _brightness);
        color.B = (byte)(color.B * _brightness);
        return color;
    }

    private void DrawSprite(SpriteDraw orig, SpriteBatch batch, Texture2D texture, float sx, float sy, float sw, float sh,
        float dx, float dy, float dw, float dh, Color color, float ox, float oy, float sin, float cos, float depth, byte effects)
    {
        if (_brightness < 1f && batch == Main.spriteBatch)
            color = Tint(color);
        orig(batch, texture, sx, sy, sw, sh, dx, dy, dw, dh, color, ox, oy, sin, cos, depth, effects);
    }

    private void DrawTile(TileDraw orig, TileBatch batch, Texture2D texture, Vector4 destination, Rectangle? source,
        VertexColors colors, float rotation, Vector2 origin, SpriteEffects effects, float depth)
    {
        if (_brightness < 1f)
        {
            colors.TopLeftColor = Tint(colors.TopLeftColor);
            colors.TopRightColor = Tint(colors.TopRightColor);
            colors.BottomLeftColor = Tint(colors.BottomLeftColor);
            colors.BottomRightColor = Tint(colors.BottomRightColor);
        }
        orig(batch, texture, destination, source, colors, rotation, origin, effects, depth);
    }

    private void DrawPlayer(On_PlayerDrawLayers.orig_DrawPlayer_RenderAllLayers orig, ref PlayerDrawSet info)
    {
        // Player vertices use SpriteDrawBuffer rather than the world SpriteBatch.
        if (_brightness < 1f)
        {
            var sprites = info.DrawDataCache;
            for (int i = 0; i < sprites.Count; i++)
            {
                DrawData sprite = sprites[i];
                sprite.color = Tint(sprite.color);
                sprites[i] = sprite;
            }
        }
        orig(ref info);
    }

    private void DrawMap(On_Main.orig_DrawMap orig, Main self, GameTime time)
    {
        float brightness = _brightness;
        End();
        try { orig(self, time); }
        finally { _brightness = brightness; }
    }

    public void Dispose()
    {
        On_Main.DrawMap -= DrawMap;
        On_PlayerDrawLayers.DrawPlayer_RenderAllLayers -= DrawPlayer;
        _tileHook.Dispose();
        _spriteHook.Dispose();
    }
}
