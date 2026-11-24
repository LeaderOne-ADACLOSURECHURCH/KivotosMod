using System.Runtime.InteropServices;
using KivotosMod.Content.Skills.BlackHole;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

static class ResonanceProbe
{
    [DllImport("SDL2.dll")] private static extern int SDL_Init(uint flags);
    [DllImport("SDL2.dll")] private static extern IntPtr SDL_CreateWindow(string title,int x,int y,int w,int h,uint flags);
    [DllImport("SDL2.dll")] private static extern void SDL_DestroyWindow(IntPtr window);

    static void Main(string[] args)
    {
        VerifyTiming();
        SDL_Init(0x20);
        var native = typeof(GraphicsDevice).Assembly.GetTypes().First(t => t.Name == "FNA3D");
        var flags = (uint)native.GetMethod("FNA3D_PrepareWindowAttributes",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
        var window = SDL_CreateWindow("Hidden Mika EX probe", 0, 0, 64, 64, flags | 8);
        try
        {
            using var device = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef,
                new PresentationParameters { DeviceWindowHandle = window, BackBufferWidth = 64, BackBufferHeight = 64 });
            Terraria.Main.graphics = new Terraria.GraphicsHolder { GraphicsDevice = device };
            KivotosMod.ModContent.Instance.Root = args[0];
            using var owner = new MikaDomainRenderer();
            using var renderer = new MikaResonanceRenderer(owner);
            using var bloom = new MikaStarBloom(device);
            using var target = new RenderTarget2D(device, 2015, 1302, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            using var pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
            Terraria.GameContent.TextureAssets.MagicPixel.Value = pixel;
            var output = Path.Combine(Path.GetTempPath(), "KivotosMikaResonanceProbe");
            Directory.CreateDirectory(output);

            Color[] Draw(string name, Action render)
            {
                device.Textures[0] = null;
                device.Textures[1] = null;
                device.SetRenderTarget(target);
                device.Clear(Color.Transparent);
                renderer.Begin(target);
                render();
                device.SetRenderTarget(null);
                var pixels = new Color[target.Width * target.Height];
                target.GetData(pixels);
                using var file = File.Create(Path.Combine(output, name + ".png"));
                target.SaveAsPng(file, target.Width, target.Height);
                Console.WriteLine($"{name}: {pixels.Count(x => x.R != 0 || x.G != 0 || x.B != 0)} visible pixels");
                return pixels;
            }

            var cues = MikaResonanceRenderer.ReadAudio();
            if (cues.Length != 3 || cues[0].Tick != 70 || cues[1].Tick != 8 || !cues[2].Opening || cues[2].Tick!=96)
                throw new Exception("AE audio cue times were lost.");
            var crackEarly=Draw("crack-90",()=>renderer.DrawOpening(target,90));
            var crackLate=Draw("crack-220",()=>renderer.DrawOpening(target,220));
            if (Vector2.Distance(renderer.OpeningGrade,new Vector2(0,51)) > .01f)
                throw new Exception("Layer 16 lost its AE hue/saturation defaults.");
            var retunedCrack=Draw("crack-layer-16-retuned",
                ()=>renderer.DrawOpening(target,220,new Vector2(120,-70)));
            if(crackLate.SequenceEqual(retunedCrack))
                throw new Exception("Layer 16 hue/saturation controls did not change the opening.");
            var white=Draw("crack-white",()=>renderer.DrawOpening(target,240));
            if(crackEarly.SequenceEqual(crackLate) ||
                white.Count(p=>p.R==255&&p.G==255&&p.B==255&&p.A==255)<white.Length*.98)
                throw new Exception("Crack mask progression or opening flash was lost.");
            var polar = Draw("polar", () => renderer.DrawDemo(target, "实现极坐标", 72, false));
            var closed = Draw("scope-closed", () => renderer.DrawDemo(target, "预合成控制功能", 85, false));
            var open = Draw("scope-open", () => renderer.DrawDemo(target, "预合成控制功能", 85, true));
            var remap = Draw("remap", () => renderer.DrawDemo(target, "时间重映射", 150, false));
            var unremapped = Draw("linear", () => renderer.DrawDemo(target, "匀速运动", 150, false));
            int brighterOpaque = closed.Where((p, i) => p.A == 255 && open[i].A == 255 &&
                open[i].R + open[i].G + open[i].B > p.R + p.G + p.B + 20).Count();
            if (polar.All(p => p == Color.Transparent) || remap.SequenceEqual(unremapped) || brighterOpaque < 100)
                throw new Exception("Polar, scope or time-remap output did not change as expected.");
            foreach (int tick in new[] { 60, 154, 203, 236, 244, 270, 315, 390, 599 })
                Draw($"resonance-{tick}", () =>
                {
                    renderer.DrawFront(target, tick, Vector2.Zero);
                    renderer.DrawLate(target, tick, Vector2.Zero);
                });
            bloom.Prepare("T40", false, owner.Texture("T40"));
            var before = Read(bloom.Texture("T40", false, false));
            MikaDomainTuning.Settings = new Point(20, 100);
            bloom.Prepare("T40", false, owner.Texture("T40"));
            var after = Read(bloom.Texture("T40", false, false));
            if (before.SequenceEqual(after) || after.All(p => p == Color.Transparent))
                throw new Exception("Bloom settings did not affect the cached texture.");
            VerifySmoothBloom(device,bloom);
            VerifyShatter(device,output);
            Console.WriteLine(output);
        }
        finally
        {
            SDL_DestroyWindow(window);
        }
    }

    private static void VerifyShatter(GraphicsDevice device,string output)
    {
        using var shatter=new MikaShatter(device);
        shatter.SetSeed(1);
        using(var batch=new SpriteBatch(device))
        {
            device.SetRenderTarget(null);
            device.Clear(Color.Plum);
            shatter.Capture();
        }
        foreach(float time in new[]{0f,.45f,1.0f,1.74f})
        {
            shatter.Prepare(700,450,time,MikaShatter.Defaults);
            using var file=File.Create(Path.Combine(output,$"shatter-{time:0.00}.png"));
            shatter.Composite.SaveAsPng(file,700,450);
        }
        shatter.Prepare(700,450,1.75f,MikaShatter.Defaults);
        if(Read(shatter.Composite).Any(p=>p!=Color.Transparent))
            throw new Exception("Shards failed to clear after 1.75 seconds.");
        Console.WriteLine("Shatter: backbuffer capture, projected shards and end fade passed.");
    }

    private static void VerifySmoothBloom(GraphicsDevice device,MikaStarBloom bloom)
    {
        using var point = new Texture2D(device,101,101);
        var input = new Color[101*101];
        for(int y=46;y<=54;y++)
            for(int x=46;x<=54;x++)input[y*101+x]=Color.White;
        point.SetData(input);
        MikaDomainTuning.Settings=new Point(70,65);
        bloom.Prepare("point",false,point);
        var texture=bloom.Texture("point",false,false);
        var pixels=Read(texture);
        int mid=texture.Width/2, peak=pixels[mid*texture.Width+mid].R;
        if(peak<=0)throw new Exception("Bloom impulse produced no light.");
        for(int step=1;step<17;step++)
        {
            int row=mid*texture.Width;
            int h=pixels[row+mid+step].R, v=pixels[(mid+step)*texture.Width+mid].R;
            if(h-pixels[row+mid+step-1].R>1 || Math.Abs(h-v)>1)
                throw new Exception("Bloom impulse has a grid or unequal axis weights.");
        }
        Console.WriteLine("Bloom impulse: smooth falloff on both axes.");
    }

    private static Color[] Read(Texture2D texture)
    {
        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);
        return data;
    }

    private static void VerifyTiming()
    {
        for (int power = 1; power <= 3; power++)
        {
            var state = new MikaExState();
            for (int i = 0; i < 49; i++) state.Hit(2);
            if (state.Press()) throw new Exception("Domain started before full charge.");
            state.Hit(2);
            if (!state.Press() || !state.Active || state.Press()) throw new Exception("Invalid domain activation.");
            for (int i = 0; i < power * MikaExState.StoreInterval; i++) state.Advance();
            if (power < 3 && (state.Reserve != power || !state.Press()))
                throw new Exception("Manual resonance could not spend its reserve.");
            if (state.Reserve != 0 || state.Power != power || state.Final != (power==3))
                throw new Exception("Invalid resonance reserve.");
            var strikes = new List<(int Tick, int Damage)>();
            int duration=power==3?MikaExState.FinalDuration:MikaExState.ResonanceDuration;
            int started=state.DomainTime;
            for (int tick = 1; tick <= duration; tick++)
            {
                state.Advance();
                int damage = state.DamageAt(state.ResonanceTime);
                if (damage > 0) strikes.Add((tick, damage));
            }
            if (strikes.Count != 19 || strikes[0] != (194, 7000 + 10500 * (power - 1)) || state.Active != (power<3) ||
                strikes.Skip(1).Where((hit, index) => hit != (200 + 6 * index, 2000 + 3000 * (power - 1))).Any() ||
                (state.Active && state.DomainTime != started+duration))
                throw new Exception("Invalid resonance damage schedule.");
        }
        var crystal=new MikaExState{Charge=100};
        crystal.Press();
        for(int i=0;i<MikaExState.StoreInterval;i++)crystal.Advance();
        if(!crystal.UseCrystal() || crystal.Power!=1 || crystal.Reserve!=1 || crystal.UseCrystal())
            throw new Exception("Crystal spent reserve or bypassed cooldown.");
        for(int i=0;i<MikaExState.ResonanceDuration;i++)crystal.Advance();
        if(!crystal.Active || !crystal.UseCrystal())throw new Exception("Crystal cooldown did not expire.");
        var manual=new MikaExState{Active=true,Reserve=1,DomainTime=60,ResonanceStart=55};
        for(int i=0;i<414;i++)manual.Advance();
        if(manual.Press())throw new Exception("Manual cooldown ended before seven seconds.");
        manual.Advance();
        if(!manual.Press())throw new Exception("Manual cooldown did not end after seven seconds.");
        var cutoff=new MikaExState{Active=true,Reserve=1,DomainTime=82*60-1};
        cutoff.Advance();
        if(cutoff.Press())throw new Exception("Manual cast allowed at 82 seconds.");
        Console.WriteLine("EX timing: three reserves, 18 pulses, manual domain retention, six-second final and crystal cooldown passed.");
    }
}

// Minimal asset and graphics adapters for the production classes linked by run_probe.ps1.
namespace Terraria
{
    internal sealed class GraphicsHolder { public GraphicsDevice GraphicsDevice; }
    internal static class Main { public static GraphicsHolder graphics; }
    internal static class TextureExtensions
    {
        public static Vector2 Size(this Texture2D texture) => new(texture.Width, texture.Height);
    }
}

namespace Terraria.GameContent
{
    internal static class TextureAssets
    {
        internal sealed class Pixel { public Texture2D Value; }
        internal static readonly Pixel MagicPixel = new();
    }
}

namespace Terraria.ModLoader { }

namespace KivotosMod
{
    internal sealed class KivotosMod
    {
        internal string Root;
        internal byte[] GetFileBytes(string name) => File.ReadAllBytes(Path.Combine(Root, name));
    }

    internal static class ModContent
    {
        internal static readonly KivotosMod Instance = new();
        internal static T GetInstance<T>() => (T)(object)Instance;
    }
}

namespace KivotosMod.Content.Skills.BlackHole
{
    internal sealed class MikaDomainRenderer : IDisposable
    {
        private readonly Dictionary<string, Texture2D> _textures = new();
        internal Texture2D Texture(string name)
        {
            if (_textures.TryGetValue(name, out var texture)) return texture;
            using var file = File.OpenRead(Path.Combine(ModContent.Instance.Root, "Assets/Skills/MikaDomain", name + ".png"));
            return _textures[name] = Texture2D.FromStream(Terraria.Main.graphics.GraphicsDevice, file);
        }

        public void Dispose()
        {
            foreach (var texture in _textures.Values) texture.Dispose();
        }
    }

    internal static class MikaDomainTuning
    {
        internal static Point Settings = new(84, 65);
        internal static Point StarBloom(bool back) => Settings;
    }
}
