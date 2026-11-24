using System;
using System.Linq;
using System.Text.Json;
using KivotosMod.Content.Tablet;

public static class TabletStartupProbe
{
    public static string Validate(int fps)
    {
        var startup = new TabletStartupAnimation(new Random(2026));
        if (startup.Visible || startup.Opacity != 0) throw new Exception("Startup visible before opening");
        startup.Start();
        for (int i = 0; i < fps * 8; i++)
        {
            startup.Update(1f / fps);
            if (!startup.Visible || startup.Fading || startup.Opacity != 1) throw new Exception("Startup did not wait for tap");
            if (startup.Triangles.Count(t => t.Remaining > 0) != 7) throw new Exception("Wrong active triangle count");
            if (startup.Triangles.Any(t => !float.IsFinite(t.Opacity) || t.Opacity < 0 || t.Opacity > 0.25001f))
                throw new Exception("Invalid triangle opacity");
        }
        startup.Tap();
        for (int i = 0; i < fps / 2; i++) startup.Update(1f / fps);
        if (Math.Abs(startup.Opacity - 0.5f) > 0.0001f) throw new Exception("Unlock fade is not one second");
        float opacity = startup.Opacity;
        startup.Tap();
        if (startup.Opacity != opacity) throw new Exception("Repeated tap restarted the fade");
        for (int i = 0; i < fps - fps / 2; i++) startup.Update(1f / fps);
        if (startup.Visible || startup.Fading || startup.Opacity != 0) throw new Exception("Unlock did not complete");
        startup.Start(); startup.Tap(); startup.Update(0.1f); startup.Reset(); startup.Start();
        if (!startup.Visible || startup.Fading || startup.Opacity != 1) throw new Exception("Reopening retained an old fade");
        startup.Reset();
        if (startup.Triangles.Any(t => t.Remaining != 0)) throw new Exception("Close did not reset particles");
        return JsonSerializer.Serialize(new { passed = true, fps, activeTriangles = 7, totalTriangles = 70,
            waitsForTap = true, fadeSeconds = 1, repeatedTapDoesNotRestart = true, reopenResets = true });
    }
}
