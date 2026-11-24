using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace KivotosMod.Content.Skills.BlackHole;

internal sealed class MikaResonanceRenderer : IDisposable
{
    private const string Directory = "Assets/Skills/MikaDomain/Resonance";
    private const int SamplesPerSecond = 60;
    private const int Channels = 25;
    private readonly GraphicsDevice _device;
    private readonly Effect _effect;
    private readonly EffectTechnique _layerPass;
    private readonly EffectTechnique _adjustmentPass;
    private readonly MikaStarBloom _glowFilter;
    private readonly Dictionary<string, Composition> _comps = new();
    private readonly Dictionary<Point, Stack<RenderTarget2D>> _targets = new();
    private readonly float[] _motion;
    private readonly byte[] _maskMotion;
    private readonly BasicEffect _maskEffect;
    private readonly VertexPositionColor[] _maskVertices;
    private static readonly JsonSerializerOptions SceneOptions = new() { PropertyNameCaseInsensitive = true };
    private const int OpeningGradeIndex = 15; // Layer 16 in After Effects.
    private readonly BlendState _additive = new()
    {
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.InverseSourceAlpha
    };
    private readonly VertexPositionTexture[] _quad =
    {
        new(Vector3.Zero, Vector2.Zero), new(Vector3.Zero, Vector2.UnitX), new(Vector3.Zero, Vector2.UnitY),
        new(Vector3.Zero, Vector2.UnitY), new(Vector3.Zero, Vector2.UnitX), new(Vector3.Zero, Vector2.One)
    };

    private sealed class Composition
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public float Duration { get; set; }
        public Layer[] Layers { get; set; }
        internal readonly Dictionary<int, Layer> ByIndex = new();
        internal Vector2 Size => new(Width, Height);
    }

    private sealed class Layer
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public int Index { get; set; }
        public float InTime { get; set; }
        public float OutTime { get; set; }
        public float SourceStart { get; set; }
        public float Stretch { get; set; }
        public int[] Size { get; set; }
        public bool Additive { get; set; }
        public bool Adjustment { get; set; }
        public int? Matte { get; set; }
        public int MatteMode { get; set; }
        public int[][] MaskFrames { get; set; }
        public bool MaskInverted { get; set; }
        public float[] MaskFeather { get; set; }
        public int? Parent { get; set; }
        public string Comp { get; set; }
        public bool OpenScope { get; set; }
        public string Texture { get; set; }
        public JsonElement[][] Paint { get; set; }
        public int Motion { get; set; }
        public bool Remap { get; set; }
        public bool Warp { get; set; }
        internal ImageSource Source;
        internal (float Time, ImageSource Source)[] PaintSources;
        internal Vector2 SourceSize => new(Size[0], Size[1]);
        internal bool Visible(float time) => time >= InTime - .00001f && time < OutTime - .00001f;
    }

    private readonly record struct ImageSource(Texture2D Texture, Vector2 Origin, Vector2 Size);
    internal sealed record AudioCue(int Tick, string Name, bool Opening);

    internal static AudioCue[] ReadAudio()
    {
        var mod = ModContent.GetInstance<KivotosMod>();
        using var scene = JsonDocument.Parse(mod.GetFileBytes(Directory + "/Scene.json"));
        return scene.RootElement.GetProperty("audio").Deserialize<AudioCue[]>(SceneOptions);
    }

    private readonly struct Sample
    {
        private readonly float[] _data;
        private readonly int _left;
        private readonly int _right;
        private readonly float _fraction;

        internal Sample(float[] data, int offset, float frame, int last)
        {
            frame = Math.Clamp(frame, 0, last);
            int left = (int)frame;
            _data = data;
            _left = offset + left * Channels;
            _right = offset + Math.Min(last, left + 1) * Channels;
            _fraction = frame - left;
        }

        internal float this[int channel] =>
            MathHelper.Lerp(_data[_left + channel], _data[_right + channel], _fraction);

        internal Matrix Pose(int start = 0) =>
            Matrix.CreateTranslation(-this[start + 2], -this[start + 3], 0) *
            Matrix.CreateScale(this[start + 4], this[start + 5], 1) *
            Matrix.CreateRotationZ(this[start + 6]) *
            Matrix.CreateTranslation(this[start], this[start + 1], 0);
    }

    internal MikaResonanceRenderer(MikaDomainRenderer owner)
    {
        _device = Main.graphics.GraphicsDevice;
        var mod = ModContent.GetInstance<KivotosMod>();
        using var document = JsonDocument.Parse(mod.GetFileBytes(Directory + "/Scene.json"));
        var images = new Dictionary<string, ImageSource>();
        foreach (var item in document.RootElement.GetProperty("textures").EnumerateObject())
        {
            var bounds = item.Value;
            images.Add(item.Name, new ImageSource(owner.Texture("Resonance/" + item.Name),
                new Vector2(bounds[0].GetSingle(), bounds[1].GetSingle()),
                new Vector2(bounds[2].GetSingle(), bounds[3].GetSingle())));
        }
        int maxMaskVertices = 0;
        foreach (var item in document.RootElement.GetProperty("comps").EnumerateObject())
        {
            var comp = item.Value.Deserialize<Composition>(SceneOptions);
            foreach (var layer in comp.Layers)
            {
                comp.ByIndex.Add(layer.Index, layer);
                if(layer.MaskFrames!=null)
                    foreach(var frame in layer.MaskFrames)
                        maxMaskVertices=Math.Max(maxMaskVertices,frame[2]);
                if (layer.Texture != null)
                    layer.Source = images[layer.Texture];
                if (layer.Paint is { Length: > 0 })
                {
                    layer.PaintSources = new (float, ImageSource)[layer.Paint.Length];
                    for (int i = 0; i < layer.Paint.Length; i++)
                        layer.PaintSources[i] = (layer.Paint[i][0].GetSingle(),
                            images[layer.Paint[i][1].GetString()]);
                }
            }
            _comps.Add(item.Name, comp);
        }

        byte[] binary = mod.GetFileBytes(Directory + "/Motion.bin");
        _motion = new float[binary.Length / sizeof(float)];
        Buffer.BlockCopy(binary, 0, _motion, 0, binary.Length);
        _maskMotion=mod.GetFileBytes(Directory+"/MaskMotion.bin");
        _maskVertices=new VertexPositionColor[maxMaskVertices];
        _maskEffect=new BasicEffect(_device){VertexColorEnabled=true};
        _effect = new Effect(_device, mod.GetFileBytes("Assets/Skills/MikaDomain/Resonance.fxc"));
        _layerPass = _effect.Techniques["DrawLayer"];
        _adjustmentPass = _effect.Techniques["ApplyAdjustment"];
        _glowFilter = new MikaStarBloom(_device);
    }

    internal void DrawFront(RenderTarget2D target, int tick, Vector2 shake)
    {
        var comp = _comps["冲击后段"];
        float time = tick / 60f;
        Matrix map = Center(target, comp, shake);
        DrawLayer(comp, comp.ByIndex[8], time, target, map, 1, true, false);
    }

    internal bool HasFront(int tick) => _comps["冲击后段"].ByIndex[8].Visible(tick / 60f);

    internal void DrawLate(RenderTarget2D target, int tick, Vector2 shake)
    {
        var comp = _comps["冲击后段"];
        float time = tick / 60f;
        Matrix moving = Center(target, comp, shake);
        Matrix stationary = Center(target, comp, Vector2.Zero);
        for (int i = 7; i >= 0; i--)
            DrawLayer(comp, comp.ByIndex[i], time, target, i >= 6 ? stationary : moving, 1, true, false, i >= 6);
    }

    internal Vector2 OpeningGrade
    {
        get
        {
            var comp=_comps["裂纹动画"];
            var sample=At(comp,comp.ByIndex[OpeningGradeIndex],0);
            return new Vector2(MathHelper.ToDegrees(sample[9]),sample[10]*100);
        }
    }

    internal void DrawOpening(RenderTarget2D target,int tick,Vector2? openingGrade=null)
    {
        var comp=_comps["裂纹动画"];
        DrawStack(comp,tick/60f,target,Center(target,comp,Vector2.Zero),1,true,openingGrade:openingGrade);
    }

    internal void DrawDemo(RenderTarget2D target, string name, int tick, bool open)
    {
        var comp = _comps[name];
        DrawStack(comp, tick / 60f % comp.Duration, target, Center(target, comp, Vector2.Zero), 1, true, open);
    }

    private static Matrix Center(RenderTarget2D target, Composition comp, Vector2 shake) =>
        Matrix.CreateTranslation((target.Width - comp.Width) / 2f + shake.X,
            (target.Height - comp.Height) / 2f + shake.Y, 0);

    private Sample At(Composition comp, Layer layer, float time) =>
        new(_motion, layer.Motion, time * SamplesPerSecond, (int)MathF.Round(comp.Duration * SamplesPerSecond));

    private Matrix Pose(Composition comp, Layer layer, Sample sample, float time)
    {
        Matrix result = sample.Pose();
        for (int? index = layer.Parent; index.HasValue;)
        {
            var parent = comp.ByIndex[index.Value];
            result *= At(comp, parent, time).Pose();
            index = parent.Parent;
        }
        return result;
    }

    private void DrawStack(Composition comp, float time, RenderTarget2D target, Matrix map, float opacity,
        bool openDestination, bool openDemo = false, Vector2? openingGrade = null)
    {
        for (int i = comp.Layers.Length - 1; i >= 0; i--)
            DrawLayer(comp, comp.Layers[i], time, target, map, opacity, openDestination, openDemo,
                gradeOverride:comp.Layers[i].Index==OpeningGradeIndex?openingGrade:null);
    }

    private void DrawLayer(Composition comp, Layer layer, float time, RenderTarget2D target, Matrix map,
        float opacity, bool openDestination, bool openDemo, bool fillTarget = false, Vector2? gradeOverride = null)
    {
        if (!layer.Enabled || !layer.Visible(time))
            return;
        var sample = At(comp, layer, time);
        if (sample[7] <= .00001f)
            return;
        opacity *= MathHelper.Clamp(sample[7], 0, 1);
        Vector4 grade = new(sample[9], sample[10], sample[11], sample[12]);
        if(gradeOverride is Vector2 adjusted)
        {
            grade.X=MathHelper.ToRadians(adjusted.X);
            grade.Y=adjusted.Y/100;
        }
        if (layer.Adjustment)
        {
            if (layer.Warp || grade != Vector4.Zero)
                ApplyAdjustment(target, comp.Size, map, sample, grade, opacity);
            return;
        }

        Matrix pose = Pose(comp, layer, sample, time) * map;
        if (layer.Comp != null && (layer.OpenScope || openDemo && layer.Comp == "发光"))
        {
            DrawStack(_comps[layer.Comp], SourceTime(layer, sample, time), target, pose, opacity, openDestination);
            return;
        }

        var image = ResolveSource(layer, sample, time, out var nested);
        if (image.Texture == null)
            return;
        RenderTarget2D warped = null;
        var glow = new Vector3(sample[13], sample[14], sample[15]);
        if (layer.Warp)
        {
            image = WarpSource(image, layer.SourceSize, sample, grade, glow, out warped);
            grade = Vector4.Zero;
            glow = Vector3.Zero;
        }
        var matte = TextureAssets.MagicPixel.Value;
        RenderTarget2D matteTarget = null;
        Matrix matteMap = Matrix.Identity;
        float matteOpacity = 0;
        if (layer.Matte is int index)
        {
            var mask = comp.ByIndex[index];
            if (mask.Visible(time))
            {
                var maskSample = At(comp, mask, time);
                matteOpacity = MathHelper.Clamp(maskSample[7], 0, 1);
                Matrix maskPose = Pose(comp, mask, maskSample, time) * map;
                if (matteOpacity > 0 && MathF.Abs(maskPose.Determinant()) > .0000001f)
                {
                    var source = ResolveSource(mask, maskSample, time, out matteTarget);
                    if (source.Texture != null)
                    {
                        matte = source.Texture;
                        matteMap = Matrix.Invert(maskPose) *
                            Matrix.CreateTranslation(-source.Origin.X, -source.Origin.Y, 0) *
                            Matrix.CreateScale(1 / source.Size.X, 1 / source.Size.Y, 1);
                    }
                    else
                        matteOpacity = 0;
                }
                else
                    matteOpacity = 0;
            }
        }

        var bloom = CacheGlow(image, glow);
        Bind(target);
        pose = fillTarget ? Matrix.Identity : Matrix.CreateTranslation(image.Origin.X, image.Origin.Y, 0) * pose;
        DrawTexture(image.Texture, matte, fillTarget ? target.Size() : image.Size, pose, matteMap, grade,
            opacity, layer.MatteMode, matteOpacity, layer.Additive, openDestination, glow: bloom, glowStrength: glow.Z*.25f);
        Release(matteTarget);
        Release(warped);
        Release(nested);
    }

    private static float SourceTime(Layer layer, Sample sample, float time) =>
        layer.Remap ? sample[8] : (time - layer.SourceStart) / layer.Stretch;

    private ImageSource ResolveSource(Layer layer, Sample sample, float time, out RenderTarget2D nested)
    {
        nested = null;
        float sourceTime = SourceTime(layer, sample, time);
        if (layer.Comp != null)
        {
            var comp = _comps[layer.Comp];
            if (sourceTime < -.00001f || sourceTime >= comp.Duration)
                return default;
            nested = Acquire(comp.Width, comp.Height);
            _device.Clear(Color.Transparent);
            DrawStack(comp, sourceTime, nested, Matrix.Identity, 1, false);
            return new ImageSource(nested, Vector2.Zero, comp.Size);
        }
        var source = layer.Source;
        if (layer.PaintSources != null)
            foreach (var variant in layer.PaintSources)
            {
                if (sourceTime < variant.Time)
                    break;
                source = variant.Source;
            }
        if(layer.MaskFrames is { Length: >0 } && source.Texture!=null)
            source=MaskSource(source,layer,time,out nested);
        return source;
    }

    private ImageSource MaskSource(ImageSource source,Layer layer,float time,out RenderTarget2D result)
    {
        var frame=layer.MaskFrames[0];
        foreach(var sample in layer.MaskFrames)
        {
            if(sample[0]>time*30+.001f)break;
            frame=sample;
        }
        int width=source.Texture.Width,height=source.Texture.Height;
        var mask=Acquire(width,height);
        _device.Clear(Color.Transparent);
        int offset=frame[1],count=frame[2];
        if(count>0)
        {
            Vector2 scale=new Vector2(width,height)/source.Size;
            for(int i=0;i<count;i++,offset+=5)
            {
                short x=(short)(_maskMotion[offset]|_maskMotion[offset+1]<<8);
                short y=(short)(_maskMotion[offset+2]|_maskMotion[offset+3]<<8);
                var pos=(new Vector2(x,y)*.25f-source.Origin)*scale;
                _maskVertices[i]=new VertexPositionColor(new Vector3(pos,0),Color.White*(_maskMotion[offset+4]/255f));
            }
            _device.BlendState=BlendState.AlphaBlend;
            _device.DepthStencilState=DepthStencilState.None;
            _device.RasterizerState=RasterizerState.CullNone;
            _maskEffect.Projection=Matrix.CreateOrthographicOffCenter(0,width,height,0,0,1);
            _maskEffect.CurrentTechnique.Passes[0].Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleList,_maskVertices,0,count/3);
        }
        result=Acquire(width,height);
        var dimensions=new Vector2(width,height);
        var feather=layer.MaskFeather is { Length:2 } ?new Vector2(layer.MaskFeather[0],layer.MaskFeather[1])/source.Size:Vector2.Zero;
        DrawTexture(source.Texture,mask,dimensions,Matrix.Identity,Matrix.CreateScale(1f/width,1f/height,1),
            Vector4.Zero,1,layer.MaskInverted?2:1,1,false,false,BlendState.Opaque,matteFeather:feather);
        Release(mask);
        return source with { Texture=result };
    }

    private ImageSource WarpSource(ImageSource image, Vector2 canvas, Sample sample, Vector4 grade,
        Vector3 glow, out RenderTarget2D target)
    {
        float resolution = Math.Min(1, 2015 / Math.Max(canvas.X, canvas.Y));
        int width = Math.Max(1, (int)MathF.Round(canvas.X * resolution));
        int height = Math.Max(1, (int)MathF.Round(canvas.Y * resolution));
        Matrix map = Matrix.CreateScale(width / canvas.X, height / canvas.Y, 1);
        var bloom=CacheGlow(image,glow);
        target = Acquire(width, height);
        _device.Clear(Color.Transparent);
        var pose = Matrix.CreateTranslation(image.Origin.X, image.Origin.Y, 0) * map;
        DrawTexture(image.Texture, TextureAssets.MagicPixel.Value, image.Size, pose, Matrix.Identity,
            grade, 1, 0, 0, false, false, glow:bloom, glowStrength:glow.Z*.25f);
        ApplyAdjustment(target, canvas, map, sample, Vector4.Zero, 1);
        return new ImageSource(target, Vector2.Zero, canvas);
    }

    private Texture2D CacheGlow(ImageSource image,Vector3 glow)
    {
        if(glow.Z<=0)return null;
        var texture=_glowFilter.PrepareLayer(image.Texture,image.Size,glow.X,glow.Y,out bool drew);
        if(drew)_bound=null;
        return texture;
    }

    private void ApplyAdjustment(RenderTarget2D destination, Vector2 size, Matrix map, Sample sample,
        Vector4 grade, float opacity)
    {
        Matrix warp = sample.Pose(17) * map;
        if (MathF.Abs(warp.Determinant()) < .0000001f)
            return;

        var scratch = Acquire(destination.Width, destination.Height);
        DrawTexture(destination, TextureAssets.MagicPixel.Value, destination.Size(), Matrix.Identity,
            Matrix.Identity, Vector4.Zero, 1, 0, 0, false, false, BlendState.Opaque);

        Bind(destination);
        SetCommon(scratch, TextureAssets.MagicPixel.Value, destination.Size(), Matrix.Identity, map, grade);
        var p = _effect.Parameters;
        p["uInverseWarp"].SetValue(Matrix.Invert(warp));
        p["uCanvas"].SetValue(destination.Size());
        p["uWarpSize"].SetValue(size);
        p["uWarp"].SetValue(new Vector2(sample[16], opacity * sample[24]));
        Draw(_adjustmentPass, BlendState.Opaque);
        Release(scratch);
    }

    private void SetCommon(Texture2D texture, Texture2D matte, Vector2 size, Matrix pose, Matrix mattePose, Vector4 grade)
    {
        var p = _effect.Parameters;
        p["uProjection"].SetValue(Matrix.CreateOrthographicOffCenter(0, _device.Viewport.Width,
            _device.Viewport.Height, 0, 0, 1));
        p["uSource"].SetValue(texture);
        p["uMatte"].SetValue(matte);
        p["uSize"].SetValue(size);
        p["uPose"].SetValue(pose);
        p["uMattePose"].SetValue(mattePose);
        p["uAdjust"].SetValue(grade);
    }

    private void DrawTexture(Texture2D texture, Texture2D matte, Vector2 size, Matrix pose, Matrix mattePose,
        Vector4 grade, float opacity, int mode, float matteOpacity, bool additive,
        bool openDestination, BlendState overrideBlend = null, Texture2D glow = null, float glowStrength = 0,
        Vector2 matteFeather=default)
    {
        SetCommon(texture, matte, size, pose, mattePose, grade);
        _effect.Parameters["uLayer"].SetValue(new Vector4(opacity, mode, matteOpacity, additive && openDestination ? 1 : 0));
        _effect.Parameters["uGlowTexture"].SetValue(glow??TextureAssets.MagicPixel.Value);
        _effect.Parameters["uGlowStrength"].SetValue(glowStrength);
        _effect.Parameters["uMatteFeather"].SetValue(matteFeather);
        Draw(_layerPass, overrideBlend ?? (additive && !openDestination ? _additive : BlendState.AlphaBlend));
    }

    private void Draw(EffectTechnique technique, BlendState blend)
    {
        _device.BlendState = blend;
        _device.DepthStencilState = DepthStencilState.None;
        _device.RasterizerState = RasterizerState.CullNone;
        _effect.CurrentTechnique = technique;
        technique.Passes[0].Apply();
        _device.DrawUserPrimitives(PrimitiveType.TriangleList, _quad, 0, 2);
    }

    private RenderTarget2D _bound;

    internal void Begin(RenderTarget2D target) => _bound = target;

    private void Bind(RenderTarget2D target)
    {
        if (_bound == target)
            return;
        _device.Textures[0] = null;
        _device.Textures[1] = null;
        _device.Textures[2] = null;
        _device.SetRenderTarget(target);
        _bound = target;
    }

    private RenderTarget2D Acquire(int width, int height)
    {
        var size = new Point(width, height);
        if (!_targets.TryGetValue(size, out var available))
            _targets[size] = available = new Stack<RenderTarget2D>();
        var target = available.Count > 0 ? available.Pop() : new RenderTarget2D(_device,
            width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        Bind(target);
        return target;
    }

    private void Release(RenderTarget2D target)
    {
        if (target == null)
            return;
        _targets[new Point(target.Width, target.Height)].Push(target);
    }

    internal void ClearTargets()
    {
        foreach (var pool in _targets.Values)
            foreach (var target in pool)
                target.Dispose();
        _targets.Clear();
        _bound = null;
    }

    public void Dispose()
    {
        ClearTargets();
        _effect.Dispose();
        _maskEffect.Dispose();
        _glowFilter.Dispose();
        _additive.Dispose();
    }
}
