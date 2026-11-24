using System;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace KivotosMod.Content.Skills.BlackHole;

// Local visual prototype, deliberately independent of EX combat and networking.
[Autoload(Side = ModSide.Client)]
public sealed class BlackHolePreviewSystem : ModSystem
{
    internal static bool Enabled { get; private set; }
    private const int Width = BlackHoleTimeline.Width, Height = BlackHoleTimeline.Height;
    private Asset<Texture2D> _core, _band, _glow, _spectrum, _mask;
    private SpriteBatch _batch;
    private RenderTarget2D _composite, _clippedSpectrum;
    private BlendState _add, _clip;
    private static int _ticks;

    public override void Load()
    {
        On_Main.DoDraw += DrawWithShimmer;
        IL_Main.DoDraw += InsertIntoShimmerStars;
    }

    internal static void Toggle() { Enabled = !Enabled; if (Enabled) _ticks = 0; }
    public override void OnWorldLoad() { Enabled = false; _ticks = 0; }
    public override void OnWorldUnload() { Enabled = false; _ticks = 0; }
    public override void PostUpdateEverything()
    {
        if (Enabled && !Main.gameMenu && _ticks < BlackHoleTimeline.LastFrame * 2) _ticks++;
    }

    private void DrawWithShimmer(On_Main.orig_DoDraw orig, Main self, GameTime gameTime)
    {
        if (!Enabled || Main.gameMenu) { orig(self, gameTime); return; }
        PrepareComposite();
        float previous = Main.shimmerAlpha;
        try
        {
            // Render-only override: use the real shimmer stars, background suppression
            // and light-dependent terrain transparency. Never change biome/gameplay.
            Main.shimmerAlpha = 1f;
            orig(self, gameTime);
        }
        finally { Main.shimmerAlpha = previous; }
    }

    private void InsertIntoShimmerStars(ILContext il)
    {
        var cursor = new ILCursor(il);
        // DoDraw calls this once for the ordinary sky and once with shimmer=true.
        // Insert immediately after the latter, while its unscaled AlphaBlend batch
        // is still active; no End/Begin or shared graphics state changes here.
        while (cursor.TryGotoNext(MoveType.After,
            i => i.MatchLdcI4(1),
            i => i.MatchCall<Main>("DrawStarsInBackground")))
        {
            cursor.EmitDelegate<Action>(DrawComposite);
            return;
        }
        throw new InvalidOperationException("Black-hole preview: shimmer star draw hook not found.");
    }

    private Asset<Texture2D> LoadTexture(string name) => ModContent.Request<Texture2D>(
        "KivotosMod/Assets/Skills/BlackHole/" + name, AssetRequestMode.ImmediateLoad);

    private void PrepareComposite()
    {
        var device = Main.graphics.GraphicsDevice;
        _core ??= LoadTexture("Core"); _band ??= LoadTexture("Band");
        _glow ??= LoadTexture("Glow"); _spectrum ??= LoadTexture("Spectrum");
        _mask ??= LoadTexture("GlowMask");
        _batch ??= new SpriteBatch(device);
        if (_composite == null || _composite.IsDisposed || _composite.IsContentLost
            || _clippedSpectrum == null || _clippedSpectrum.IsDisposed || _clippedSpectrum.IsContentLost)
        {
            _composite?.Dispose(); _clippedSpectrum?.Dispose();
            // AE's 32-bpc working precision; the final Terraria backbuffer is still SDR.
            // Each spectrum is masked separately then composited. Preserve the base
            // when temporarily switching away to the next spectrum's render target.
            _composite = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Vector4,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _clippedSpectrum = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Vector4, DepthFormat.None);
        }
        _add ??= new BlendState {
            ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
        _clip ??= new BlendState {
            ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.SourceAlpha,
            AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.SourceAlpha
        };
        var targets = device.GetRenderTargets();
        var viewport = device.Viewport;
        var blend = device.BlendState;
        var sampler = device.SamplerStates[0];
        var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState;
        try
        {
            device.SetRenderTarget(_composite);
            device.Clear(Color.Transparent);
            Begin(BlendState.AlphaBlend);
            DrawAt(_core.Value, 640f); DrawAt(_band.Value, 618f);
            _batch.End();
            Begin(_add);
            DrawAt(_glow.Value, 618f);
            _batch.End();
            // AE top-first layers 2,1,0 are drawn bottom-to-top. Applying the
            // matte after merging all copies would produce different overlap alpha.
            for (int layer = 2; layer >= 0; layer--)
            {
                if (!BlackHoleTimeline.TryGetSpectrumX(layer, _ticks / 2, out float x)) continue;
                device.SetRenderTarget(_clippedSpectrum);
                device.Clear(Color.Transparent);
                Begin(BlendState.AlphaBlend);
                DrawAt(_spectrum.Value, x);
                _batch.End();
                Begin(_clip);
                _batch.Draw(_mask.Value, Vector2.Zero, Color.White);
                _batch.End();
                device.SetRenderTarget(_composite);
                Begin(BlendState.AlphaBlend);
                _batch.Draw(_clippedSpectrum, Vector2.Zero, Color.White);
                _batch.End();
            }
        }
        finally
        {
            device.SetRenderTargets(targets);
            device.Viewport = viewport;
            device.BlendState = blend; device.SamplerStates[0] = sampler;
            device.DepthStencilState = depth; device.RasterizerState = rasterizer;
        }
    }

    private void Begin(BlendState blend) => _batch.Begin(SpriteSortMode.Deferred, blend,
        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);

    private void DrawAt(Texture2D texture, float centerX) => _batch.Draw(texture,
        new Vector2(centerX - texture.Width / 2f, (Height - texture.Height) / 2f), Color.White);

    private void DrawComposite()
    {
        if (!Enabled || Main.gameMenu || _composite == null) return;
        // Double the source size at 1920x1080; adapt uniformly to other screens.
        float scale = 2f * Math.Min(Main.screenWidth / 1920f, Main.screenHeight / 1080f);
        Vector2 center = new(Main.screenWidth / 2f, Main.screenHeight / 2f);
        Main.spriteBatch.Draw(_composite, center, null, Color.White, 0,
            new Vector2(Width / 2f, Height / 2f), scale, SpriteEffects.None, 0);
    }

    public override void Unload()
    {
        On_Main.DoDraw -= DrawWithShimmer;
        IL_Main.DoDraw -= InsertIntoShimmerStars;
        Enabled = false;
        var composite = _composite; var spectrum = _clippedSpectrum;
        var batch = _batch; var add = _add; var clip = _clip;
        _composite = _clippedSpectrum = null; _batch = null; _add = _clip = null;
        _core = _band = _glow = _spectrum = _mask = null;
        Main.QueueMainThreadAction(() => {
            composite?.Dispose(); spectrum?.Dispose(); batch?.Dispose(); add?.Dispose(); clip?.Dispose();
        });
    }
}
