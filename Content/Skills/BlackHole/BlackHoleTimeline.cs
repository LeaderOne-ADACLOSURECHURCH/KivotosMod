namespace KivotosMod.Content.Skills.BlackHole;

// Position keys extracted from 黑洞.aep (30 fps). Layer numbers are top-first.
internal static class BlackHoleTimeline
{
    internal const int Width = 1280, Height = 817, LastFrame = 899;
    internal static bool TryGetSpectrumX(int layer, int frame, out float x)
    {
        frame = System.Math.Clamp(frame, 0, LastFrame);
        int inFrame, first, last;
        float start, end;
        switch (layer)
        {
            case 0: inFrame = first = 286; last = 461; start = -516; end = 640; break;
            case 1: inFrame = 102; first = 106; last = 393; start = -506; end = 1690; break;
            case 2: inFrame = first = 0; last = 301; start = 640; end = 1812; break;
            default: x = 0; return false;
        }
        float progress = System.Math.Clamp((frame - first) / (float)(last - first), 0f, 1f);
        x = start + (end - start) * progress;
        return frame >= inFrame;
    }
}
