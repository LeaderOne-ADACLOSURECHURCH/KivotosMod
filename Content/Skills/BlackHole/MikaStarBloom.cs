using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace KivotosMod.Content.Skills.BlackHole;

internal sealed class MikaStarBloom : IDisposable
{
    internal const int Padding = 100;
    private readonly Effect _effect;
    private readonly GraphicsDevice _device;
    private readonly Dictionary<(string, bool), Entry> _cache = new();
    private readonly Dictionary<Texture2D, (Point Settings, RenderTarget2D Texture)> _layerGlow = new();
    private readonly Dictionary<Point,RenderTarget2D> _scratchBySize = new();
    private RenderTarget2D _scratch;
    private readonly VertexPositionTexture[] _quad =
    {
        new(Vector3.Zero, Vector2.Zero), new(Vector3.Zero, Vector2.UnitX), new(Vector3.Zero, Vector2.UnitY),
        new(Vector3.Zero, Vector2.UnitY), new(Vector3.Zero, Vector2.UnitX), new(Vector3.Zero, Vector2.One)
    };

    private sealed record Entry(Point Settings, RenderTarget2D Normal, RenderTarget2D Burst);

    internal MikaStarBloom(GraphicsDevice device)
    {
        _device = device;
        _effect = new Effect(device, ModContent.GetInstance<KivotosMod>()
            .GetFileBytes("Assets/Skills/MikaDomain/StarBloom.fxc"));
    }

    internal void Prepare(string name, bool back, Texture2D body)
    {
        var key = (name, back);
        var settings = MikaDomainTuning.StarBloom(back);
        if (!_cache.TryGetValue(key, out var entry))
        {
            int width = (body.Width + 2 * Padding + 3) / 4;
            int height = (body.Height + 2 * Padding + 3) / 4;
            entry = new Entry(new Point(-1, -1), Target(width, height), Target(width, height));
            _cache.Add(key, entry);
        }
        if (entry.Settings == settings && !entry.Normal.IsContentLost && !entry.Burst.IsContentLost)
            return;

        int w = entry.Normal.Width, h = entry.Normal.Height;
        GetScratch(w,h);
        var p = _effect.Parameters;
        p["uProjection"].SetValue(Matrix.CreateOrthographicOffCenter(0, w, h, 0, 0, 1));
        p["uSize"].SetValue(new Vector2(w, h));
        p["uBodySize"].SetValue(body.Size() * new Vector2(w / (body.Width + 2f * Padding), h / (body.Height + 2f * Padding)));
        float radius = settings.Y * .25f;
        float threshold = settings.X / 100f;
        Blur(body, entry.Normal, threshold, new Vector2(radius));
        Blur(body, entry.Burst, threshold * .7f, new Vector2(radius));
        _cache[key] = entry with { Settings = settings };
    }

    internal Texture2D PrepareLayer(Texture2D body, Vector2 logicalSize, float threshold, float radius, out bool drew)
    {
        var settings = new Point((int)MathF.Round(threshold * 255), (int)MathF.Round(radius));
        if (!_layerGlow.TryGetValue(body, out var entry))
        {
            float scale = Math.Min(1, 512f / Math.Max(body.Width, body.Height));
            entry = (new Point(-1, -1), Target(Math.Max(1, (int)(body.Width * scale)),
                Math.Max(1, (int)(body.Height * scale))));
        }
        var target = entry.Texture;
        drew = entry.Settings != settings || target.IsContentLost;
        if (!drew) return target;
        int w = target.Width, h = target.Height;
        GetScratch(w,h);
        var p = _effect.Parameters;
        p["uProjection"].SetValue(Matrix.CreateOrthographicOffCenter(0, w, h, 0, 0, 1));
        p["uSize"].SetValue(new Vector2(w,h));
        p["uBodySize"].SetValue(new Vector2(w,h));
        Blur(body,target,settings.X/255f,new Vector2(w/logicalSize.X,h/logicalSize.Y)*settings.Y);
        _layerGlow[body]=(settings,target);
        return target;
    }

    private void GetScratch(int width,int height)
    {
        var size = new Point(width,height);
        if (!_scratchBySize.TryGetValue(size,out _scratch) || _scratch.IsContentLost)
        {
            _scratch?.Dispose();
            _scratchBySize[size] = _scratch = Target(width,height);
        }
    }

    private void Blur(Texture2D body, RenderTarget2D result, float threshold, Vector2 radius)
    {
        var p = _effect.Parameters;
        p["uThreshold"].SetValue(threshold);
        Draw(result, body, Vector2.Zero, 0);
        if (radius == Vector2.Zero) return;
        p["uRadius"].SetValue(Math.Min(25,radius.X));
        Draw(_scratch, result, new Vector2(1f / result.Width, 0), 1);
        p["uRadius"].SetValue(Math.Min(25,radius.Y));
        Draw(result, _scratch, new Vector2(0, 1f / result.Height), 1);
    }

    private void Draw(RenderTarget2D target, Texture2D input, Vector2 step, int pass)
    {
        _device.Textures[0] = null;
        _device.Textures[1] = null;
        _device.Textures[2] = null;
        _device.SetRenderTarget(target);
        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.None;
        _device.RasterizerState = RasterizerState.CullNone;
        _effect.Parameters["uSource"].SetValue(input);
        _effect.Parameters["uStep"].SetValue(step);
        _effect.CurrentTechnique.Passes[pass].Apply();
        _device.DrawUserPrimitives(PrimitiveType.TriangleList, _quad, 0, 2);
    }

    internal Texture2D Texture(string name, bool back, bool burst)
    {
        var entry = _cache[(name, back)];
        return burst ? entry.Burst : entry.Normal;
    }

    private RenderTarget2D Target(int width, int height) =>
        new(_device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

    public void Dispose()
    {
        foreach (var entry in _cache.Values)
        {
            entry.Normal.Dispose();
            entry.Burst.Dispose();
        }
        foreach (var entry in _layerGlow.Values) entry.Texture.Dispose();
        foreach (var target in _scratchBySize.Values) target.Dispose();
        _effect.Dispose();
    }
}
