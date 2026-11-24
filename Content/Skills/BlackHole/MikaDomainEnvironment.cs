using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;

namespace KivotosMod.Content.Skills.BlackHole;

internal sealed class MikaDomainEnvironment : IDisposable
{
    internal bool Drawing { get; private set; }
    private float _ambientDarken;
    private float? _worldBrightness;

    internal MikaDomainEnvironment()
    {
        On_Main.SetBackColor += SetBackColor;
        On_Main.DrawRain += DrawRain;
        On_Filter.IsVisible += FilterVisible;
        On_SimpleOverlay.Draw += DrawWeatherOverlay;
        On_SkyManager.DrawDepthRange += DrawSky;
        On_SkyManager.ProcessTileColor += ProcessTileColor;
        On_SkyManager.ProcessCloudAlpha += ProcessCloudAlpha;
    }

    internal void Draw(On_Main.orig_DoDraw orig, Main self, GameTime time, float visibility)
    {
        float originalAlpha = Main.shimmerAlpha;
        float originalDarken = Main.shimmerDarken;
        float originalClouds = Main.cloudAlpha;
        float originalLightning = Main.lightning;
        bool originalEclipse = Main.eclipse;
        float originalEclipseLight = Main.eclipseLight;
        Drawing = true;
        _ambientDarken = visibility * .32f;
        try
        {
            Main.shimmerAlpha = Math.Max(originalAlpha, visibility);
            Main.shimmerDarken = Math.Max(originalDarken, _ambientDarken);
            Main.cloudAlpha = Main.lightning = 0;
            Main.eclipse = false;
            Main.eclipseLight = 0;
            orig(self, time);
        }
        finally
        {
            RestoreWorldLighting();
            Main.shimmerAlpha = originalAlpha;
            Main.shimmerDarken = originalDarken;
            Main.cloudAlpha = originalClouds;
            Main.lightning = originalLightning;
            Main.eclipse = originalEclipse;
            Main.eclipseLight = originalEclipseLight;
            Drawing = false;
        }
    }

    internal void ApplyWorldLighting()
    {
        if (!Drawing || _worldBrightness.HasValue) return;
        _worldBrightness = Lighting.GlobalBrightness;
        // Shimmer's native sun-light attenuation only applies outdoors.
        // Let indoor player and NPC sprites pick up a lighter version through their normal light sampling.
        Lighting.GlobalBrightness = _worldBrightness.Value * (1f - _ambientDarken * .5f);
    }

    internal void RestoreWorldLighting()
    {
        if (!_worldBrightness.HasValue) return;
        Lighting.GlobalBrightness = _worldBrightness.Value;
        _worldBrightness = null;
    }

    private void SetBackColor(On_Main.orig_SetBackColor orig, Main.InfoToSetBackColor info,
        out Color sunColor, out Color moonColor)
    {
        if (Drawing)
            info = new Main.InfoToSetBackColor { isInGameMenuOrIsServer = info.isInGameMenuOrIsServer };
        orig(info, out sunColor, out moonColor);
    }

    private void DrawRain(On_Main.orig_DrawRain orig, Main self)
    {
        if (!Drawing) orig(self);
    }

    private bool FilterVisible(On_Filter.orig_IsVisible orig, Filter filter) => !Drawing && orig(filter);

    private void DrawWeatherOverlay(On_SimpleOverlay.orig_Draw orig, SimpleOverlay overlay, SpriteBatch batch)
    {
        if (!Drawing || (overlay != Overlays.Scene["Sandstorm"] && overlay != Overlays.Scene["Blizzard"]))
            orig(overlay, batch);
    }

    private void DrawSky(On_SkyManager.orig_DrawDepthRange orig, SkyManager sky, SpriteBatch batch, float min, float max)
    {
        if (!Drawing) orig(sky, batch, min, max);
    }

    private Color ProcessTileColor(On_SkyManager.orig_ProcessTileColor orig, SkyManager sky, Color color)
        => Drawing ? color : orig(sky, color);

    private float ProcessCloudAlpha(On_SkyManager.orig_ProcessCloudAlpha orig, SkyManager sky)
        => Drawing ? 1f : orig(sky);

    public void Dispose()
    {
        On_Main.SetBackColor -= SetBackColor;
        On_Main.DrawRain -= DrawRain;
        On_Filter.IsVisible -= FilterVisible;
        On_SimpleOverlay.Draw -= DrawWeatherOverlay;
        On_SkyManager.DrawDepthRange -= DrawSky;
        On_SkyManager.ProcessTileColor -= ProcessTileColor;
        On_SkyManager.ProcessCloudAlpha -= ProcessCloudAlpha;
    }
}
