using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace KivotosMod.Content.Skills.BlackHole;

// Small numeric animation tables plus original textures; the MP4 is never decoded at runtime.
internal sealed class MikaDomainRenderer : IDisposable
{
    internal RenderTarget2D Composite;
    internal double TextureMiB { get; private set; }
    private readonly Effect _effect;
    private MikaResonanceRenderer _resonance;
    private MikaStarBloom _starBloom;
    private RenderTarget2D _opening;
    private readonly Layer[] _layers;
    private readonly float[] _motion;
    private readonly int _frames;
    private readonly float _duration;
    private readonly Dictionary<string,Texture2D> _textures = new();
    private readonly VertexPositionTexture[] _quad = {
        new(Vector3.Zero,new Vector2(0,0)), new(Vector3.Zero,new Vector2(1,0)), new(Vector3.Zero,new Vector2(0,1)),
        new(Vector3.Zero,new Vector2(0,1)), new(Vector3.Zero,new Vector2(1,0)), new(Vector3.Zero,new Vector2(1,1))
    };
    private sealed record Layer(int Index,Texture2D Source,Texture2D Mask,Vector2 Size,Vector2 Anchor,
        float Start,float End,bool Additive,float Matte,Vector3 Black,Vector3 White,float Tint,Vector4 Adjust,int MaskIndex,float MaskOpacity,Vector2 MaskScale);

    internal MikaDomainRenderer()
    {
        var mod=ModContent.GetInstance<KivotosMod>();
        var device=Main.graphics.GraphicsDevice;
        using var scene=JsonDocument.Parse(mod.GetFileBytes("Assets/Skills/MikaDomain/Scene.json"));
        var root=scene.RootElement;
        _duration=30;
        int width=root.GetProperty("width").GetInt32(),height=root.GetProperty("height").GetInt32();
        Composite=CreateTarget(device,MikaDomainTuning.ViewWidth,MikaDomainTuning.ViewHeight);
        TextureMiB=width*height*4/1048576.0;
        var elements=root.GetProperty("layers"); _layers=new Layer[elements.GetArrayLength()];
        int i=0;
        foreach(var l in elements.EnumerateArray())
        {
            var tint=l.GetProperty("tint");var anchor=l.GetProperty("anchor");
            var mask=l.GetProperty("mask");int type=l.GetProperty("matte").GetInt32();
            _layers[i++]=new(l.GetProperty("index").GetInt32(),Texture(l.GetProperty("texture").GetString()),
                mask.ValueKind==JsonValueKind.Null ? Terraria.GameContent.TextureAssets.MagicPixel.Value : Texture(mask.GetString()),
                new(l.GetProperty("width").GetSingle(),l.GetProperty("height").GetSingle()),new(anchor[0].GetSingle(),anchor[1].GetSingle()),
                l.GetProperty("start").GetSingle(),l.GetProperty("end").GetSingle(),l.GetProperty("blend").GetInt32()==5223,
                type==5013 ? 1 : type==5015 ? 2 : 0,
                new(tint[0].GetSingle(),tint[1].GetSingle(),tint[2].GetSingle()),
                new(tint[4].GetSingle(),tint[5].GetSingle(),tint[6].GetSingle()),tint[3].GetSingle(),
                new(MathHelper.ToRadians(l.GetProperty("fx")[0].GetSingle()),l.GetProperty("fx")[1].GetSingle(),l.GetProperty("fx")[2].GetSingle(),l.GetProperty("fx")[3].GetSingle()),
                l.GetProperty("mask_index").GetInt32(),l.GetProperty("mask_opacity").GetSingle(),new Vector2(l.GetProperty("mask_scale")[0].GetSingle(),l.GetProperty("mask_scale")[1].GetSingle())/100);
        }
        using var reader=new BinaryReader(new MemoryStream(mod.GetFileBytes("Assets/Skills/MikaDomain/Motion.bin")));
        if(reader.ReadInt32()!=_layers.Length)throw new InvalidDataException("Mika EX layer count differs");
        _frames=reader.ReadInt32();if(reader.ReadInt32()!=6)throw new InvalidDataException("Mika EX motion layout differs");
        _motion=new float[_layers.Length*_frames*6];
        for(i=0;i<_motion.Length;i++)_motion[i]=reader.ReadSingle();
        _effect=new Effect(device,mod.GetFileBytes("Assets/Skills/MikaDomain/Domain.fxc"));
        _effect.Parameters["uMatrix"].SetValue(Matrix.CreateOrthographicOffCenter(0,width,height,0,0,1));
        _effect.Parameters["uCanvas"].SetValue(new Vector2(width,height));
    }
    internal Texture2D Texture(string name)
    {
        if(_textures.TryGetValue(name,out var cached))return cached;
        var t=ModContent.Request<Texture2D>("KivotosMod/Assets/Skills/MikaDomain/"+name,AssetRequestMode.ImmediateLoad).Value;
        _textures[name]=t;TextureMiB+=t.Width*t.Height*4/1048576.0;return t;
    }
    internal void Prepare(bool warmUp=false)
    {
        var d=Main.graphics.GraphicsDevice;
        int width=MikaDomainTuning.ViewWidth,height=MikaDomainTuning.ViewHeight;
        if(Composite.Width!=width || Composite.Height!=height || Composite.IsContentLost)
        {
            Composite.Dispose();Composite=CreateTarget(d,width,height);
            _resonance?.ClearTargets();
        }
        int resonanceTick=BlackHolePreviewSystem.ResonanceTick;
        Vector2 shake=BlackHolePreviewSystem.Shake(resonanceTick);
        float drift=BlackHolePreviewSystem.Drift;
        _effect.Parameters["uOffset"].SetValue(new Vector2((width-1559)/2f,(height-1007)/2f+drift)+shake);
        _effect.Parameters["uMatrix"].SetValue(Matrix.CreateOrthographicOffCenter(0,width,height,0,0,1));
        var targets=d.GetRenderTargets();var viewport=d.Viewport;var blend=d.BlendState;
        var depth=d.DepthStencilState;var raster=d.RasterizerState;
        var s0=d.SamplerStates[0];var s1=d.SamplerStates[1];var s2=d.SamplerStates[2];
        var t0=d.Textures[0];var t1=d.Textures[1];var t2=d.Textures[2];var vb=d.GetVertexBuffers();
        try
        {
            if(warmUp || !BlackHolePreviewSystem.PaintDemo && BlackHolePreviewSystem.CompositionDemo==null)
                MikaDomainStars.PrepareBloom(this,warmUp);
            d.Textures[0]=null;d.Textures[1]=null;d.Textures[2]=null;
            d.SetRenderTarget(Composite);d.Clear(Color.Transparent);
            if(warmUp)
            {
                Resonance.Begin(Composite);
                // Exercise the impact's short-lived passes and late precomps while loading.
                foreach(int tick in new[]{60,132,154,174,188,194,203,216,232})
                    Resonance.DrawFront(Composite,tick,Vector2.Zero);
                foreach(int tick in new[]{136,152,203,236,244,252,270,315,390,540})
                    Resonance.DrawLate(Composite,tick,Vector2.Zero);
                d.Clear(Color.Transparent);
            }
            if(BlackHolePreviewSystem.CompositionDemo is string demo)
            {
                Resonance.Begin(Composite);
                Resonance.DrawDemo(Composite,demo,BlackHolePreviewSystem.Ticks,BlackHolePreviewSystem.OpenDemo);
                return;
            }
            if(BlackHolePreviewSystem.PaintDemo)
            {
                float seconds=(BlackHolePreviewSystem.Ticks%300)/60f;
                string image=seconds<.6666667f?"PaintDemo0":seconds<2.1666667f?"PaintDemo1":seconds<3.5666667f?"PaintDemo3":"PaintDemo4_9";
                Sprite(image,new Vector2(779.5f,503.5f),Vector2.One,0,0,0,null,1,Vector3.Zero);return;
            }
            float time=(float)((BlackHolePreviewSystem.Ticks/60.0)%_duration);
            float frame=time*30;int a=Math.Min((int)frame,_frames-1),b=Math.Min(a+1,_frames-1);float f=frame-a;
            MikaDomainStars.Draw(this,true);
            for(int i=_layers.Length-1;i>=0;i--)
            {
                var l=_layers[i];if(time<l.Start || time>=l.End)continue;
                // Visibility of the separate base layer never controls the mask texture.

                float Value(int channel)=>MathHelper.Lerp(_motion[(i*_frames+a)*6+channel],_motion[(i*_frames+b)*6+channel],f);
                float opacity=Value(5);if(opacity<=0.00001f)continue;
                var p=_effect.Parameters;
                var adjust=l.Adjust;if(l.Index==75)adjust.Y=MikaDomainTuning.Saturation/100f;
                p["uAdjust"].SetValue(adjust);p["uGlow"].SetValue(Vector3.Zero);
                p["uMaskOpacity"].SetValue(MikaDomainTuning.Opacity(l.MaskIndex,l.MaskOpacity));p["uMaskScale"].SetValue(l.MaskScale);
                p["uSize"].SetValue(l.Size);p["uAnchor"].SetValue(l.Anchor);
                p["uPosition"].SetValue(new Vector2(Value(0),Value(1)));p["uScale"].SetValue(new Vector2(Value(2),Value(3)));
                float angle=Value(4);p["uRotation"].SetValue(new Vector2(MathF.Cos(angle),MathF.Sin(angle)));
                p["uOpacity"].SetValue(opacity);p["uTint"].SetValue(l.Tint);
                p["uBlack"].SetValue(l.Black);p["uWhite"].SetValue(l.White);p["uMatteType"].SetValue(l.Matte);
                p["uAdditive"].SetValue(l.Additive?1f:0f);p["uSource"].SetValue(l.Source);p["uMask"].SetValue(l.Mask);
                d.BlendState=BlendState.AlphaBlend;d.DepthStencilState=DepthStencilState.None;d.RasterizerState=RasterizerState.CullNone;
                _effect.CurrentTechnique.Passes[0].Apply();d.DrawUserPrimitives(PrimitiveType.TriangleList,_quad,0,2);

            }
            if(resonanceTick>=0 && Resonance.HasFront(resonanceTick))
            {
                Resonance.Begin(Composite);
                Resonance.DrawFront(Composite,resonanceTick,shake+new Vector2(0,drift));
            }
            MikaDomainStars.Draw(this,false);
            if(resonanceTick>=0)
            {
                Resonance.Begin(Composite);
                Resonance.DrawLate(Composite,resonanceTick,shake+new Vector2(0,drift));
            }
        }
        finally
        {
            d.SetRenderTargets(targets);d.Viewport=viewport;d.BlendState=blend;d.DepthStencilState=depth;d.RasterizerState=raster;
            d.SamplerStates[0]=s0;d.SamplerStates[1]=s1;d.SamplerStates[2]=s2;
            d.Textures[0]=t0;d.Textures[1]=t1;d.Textures[2]=t2;d.SetVertexBuffers(vb);
        }
    }
    internal void Sprite(string name,Vector2 position,Vector2 scale,float angle,float hue,float saturation,string mask,float opacity,Vector3 glow,bool additive=false)
        =>Sprite(Texture(name),position,scale,angle,hue,saturation,mask,opacity,glow,additive);

    private void Sprite(Texture2D source,Vector2 position,Vector2 scale,float angle,float hue,float saturation,string mask,float opacity,Vector3 glow,bool additive,Vector2? logicalSize=null)
    {
        var p=_effect.Parameters;
        Vector2 size=logicalSize??source.Size();
        p["uSize"].SetValue(size);p["uAnchor"].SetValue(size/2);
        p["uPosition"].SetValue(position);p["uScale"].SetValue(scale);p["uRotation"].SetValue(new Vector2(MathF.Cos(angle),MathF.Sin(angle)));
        p["uOpacity"].SetValue(opacity);p["uTint"].SetValue(0f);p["uBlack"].SetValue(Vector3.Zero);p["uWhite"].SetValue(Vector3.One);
        p["uMatteType"].SetValue(mask==null?0f:1f);p["uAdditive"].SetValue(additive?1f:0f);
        p["uMaskOpacity"].SetValue(1f);p["uMaskScale"].SetValue(mask==null?Vector2.One:MikaDomainStars.BackMaskScale);
        p["uSource"].SetValue(source);p["uMask"].SetValue(mask==null?Terraria.GameContent.TextureAssets.MagicPixel.Value:Texture(mask));
        p["uAdjust"].SetValue(new Vector4(MathHelper.ToRadians(hue),saturation,0,0));p["uGlow"].SetValue(glow);
        var d=Main.graphics.GraphicsDevice;d.BlendState=BlendState.AlphaBlend;d.DepthStencilState=DepthStencilState.None;d.RasterizerState=RasterizerState.CullNone;
        _effect.CurrentTechnique.Passes[0].Apply();d.DrawUserPrimitives(PrimitiveType.TriangleList,_quad,0,2);
    }
    private MikaResonanceRenderer Resonance=>_resonance??=new MikaResonanceRenderer(this);
    internal void PrepareBloom(string name,bool back)
    {
        _starBloom??=new MikaStarBloom(Main.graphics.GraphicsDevice);
        _starBloom.Prepare(name,back,Texture(name));
    }
    internal void DrawBloom(string name,bool back,bool burst,Vector2 position,Vector2 scale,float angle,float hue,float saturation,string mask,float opacity)
        =>Sprite(_starBloom.Texture(name,back,burst),position,scale,angle,hue,saturation,mask,opacity,Vector3.Zero,true,
            Texture(name).Size()+new Vector2(2*MikaStarBloom.Padding));
    private static RenderTarget2D CreateTarget(GraphicsDevice device,int width,int height)=>
        new(device,width,height,false,SurfaceFormat.Color,DepthFormat.None,0,RenderTargetUsage.PreserveContents);

    public void Dispose(){Composite.Dispose();_opening?.Dispose();_effect.Dispose();_resonance?.Dispose();_starBloom?.Dispose();}
    internal void Preload()
    {
        MikaDomainStars.Load();MikaDomainStars.Preload(this);
        foreach(string n in new[]{"PaintDemo0","PaintDemo1","PaintDemo3","PaintDemo4_9"})Texture(n);
        // Warm the effect and graphics pipeline while entering the world.
        Prepare(warmUp:true);
        MikaShatterTuning.DefaultOpeningGrade=Resonance.OpeningGrade;
        foreach(int tick in new[]{30,96,140,180,210,232,240,245})
            PrepareOpening(tick);
    }
    internal RenderTarget2D PrepareOpening(int tick)
    {
        var d=Main.graphics.GraphicsDevice;
        var targets=d.GetRenderTargets();var viewport=d.Viewport;
        var blend=d.BlendState;var depth=d.DepthStencilState;var raster=d.RasterizerState;
        var t0=d.Textures[0];var t1=d.Textures[1];var t2=d.Textures[2];
        var s0=d.SamplerStates[0];var s1=d.SamplerStates[1];var s2=d.SamplerStates[2];
        var buffers=d.GetVertexBuffers();
        _opening??=CreateTarget(d,2015,1302);
        try
        {
            d.Textures[0]=null;d.Textures[1]=null;d.Textures[2]=null;
            d.SetRenderTarget(_opening);d.Clear(Color.Transparent);
            Resonance.Begin(_opening);
            Resonance.DrawOpening(_opening,tick,MikaShatterTuning.OpeningGrade);
            return _opening;
        }
        finally
        {
            d.SetRenderTargets(targets);d.Viewport=viewport;d.BlendState=blend;d.DepthStencilState=depth;d.RasterizerState=raster;
            d.Textures[0]=t0;d.Textures[1]=t1;d.Textures[2]=t2;
            d.SamplerStates[0]=s0;d.SamplerStates[1]=s1;d.SamplerStates[2]=s2;d.SetVertexBuffers(buffers);
        }
    }
}
