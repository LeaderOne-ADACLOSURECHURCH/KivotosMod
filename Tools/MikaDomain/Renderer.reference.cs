using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace BlackHoleLab;

// Small numeric animation tables plus original textures; the MP4 is never decoded at runtime.
internal sealed class MikaExRenderer : IDisposable
{
    internal readonly RenderTarget2D Composite;
    internal double TextureMiB { get; private set; }
    private readonly Effect _effect;
    private readonly Layer[] _layers;
    private readonly float[] _motion;
    private readonly int _frames;
    private readonly string _textureFolder;
    private readonly float _duration;
    private readonly bool _demo;
    private readonly Dictionary<string,Texture2D> _textures = new();
    private readonly SpriteBatch _batch;
    private Texture2D[] _explosion;
    private readonly VertexPositionTexture[] _quad = {
        new(Vector3.Zero,new Vector2(0,0)), new(Vector3.Zero,new Vector2(1,0)), new(Vector3.Zero,new Vector2(0,1)),
        new(Vector3.Zero,new Vector2(0,1)), new(Vector3.Zero,new Vector2(1,0)), new(Vector3.Zero,new Vector2(1,1))
    };
    private sealed record Layer(int Index,Texture2D Source,Texture2D Mask,Vector2 Size,Vector2 Anchor,
        float Start,float End,bool Additive,float Matte,Vector3 Black,Vector3 White,float Tint);

    internal MikaExRenderer(string sceneFolder="MikaEx")
    {
        var mod=ModContent.GetInstance<BlackHoleLab>();
        var device=Main.graphics.GraphicsDevice;
        using var scene=JsonDocument.Parse(mod.GetFileBytes("Assets/"+sceneFolder+"/Scene.json"));
        var root=scene.RootElement;
        _textureFolder=root.TryGetProperty("texture_folder",out var folder)?folder.GetString():sceneFolder;
        _duration=root.GetProperty("duration").GetSingle();_demo=sceneFolder=="TrackMatte";
        int width=root.GetProperty("width").GetInt32(),height=root.GetProperty("height").GetInt32();
        Composite=new RenderTarget2D(device,width,height,false,SurfaceFormat.Color,DepthFormat.None);
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
                new(tint[4].GetSingle(),tint[5].GetSingle(),tint[6].GetSingle()),tint[3].GetSingle());
        }
        using var reader=new BinaryReader(new MemoryStream(mod.GetFileBytes("Assets/"+sceneFolder+"/Motion.bin")));
        if(reader.ReadInt32()!=_layers.Length)throw new InvalidDataException("Mika EX layer count differs");
        _frames=reader.ReadInt32();if(reader.ReadInt32()!=6)throw new InvalidDataException("Mika EX motion layout differs");
        _motion=new float[_layers.Length*_frames*6];
        for(i=0;i<_motion.Length;i++)_motion[i]=reader.ReadSingle();
        _effect=new Effect(device,mod.GetFileBytes("Assets/Effects/MikaEx.fxc"));
        _effect.Parameters["uMatrix"].SetValue(Matrix.CreateOrthographicOffCenter(0,width,height,0,0,1));
        _effect.Parameters["uCanvas"].SetValue(new Vector2(width,height));
        _batch=new SpriteBatch(device);
    }
    private Texture2D Texture(string name)
    {
        if(_textures.TryGetValue(name,out var cached))return cached;
        var t=ModContent.Request<Texture2D>("BlackHoleLab/Assets/"+_textureFolder+"/"+name,AssetRequestMode.ImmediateLoad).Value;
        _textures[name]=t;TextureMiB+=t.Width*t.Height*4/1048576.0;return t;
    }
    internal void Prepare()
    {
        var d=Main.graphics.GraphicsDevice;
        if(LabSystem.ExplosionTick>=0 && _explosion==null)
        {
            _explosion=new Texture2D[SequenceData.ExplosionPages];
            for(int i=0;i<_explosion.Length;i++)
            {
                var t=ModContent.Request<Texture2D>("BlackHoleLab/Assets/Explosion"+i.ToString("D2"),AssetRequestMode.ImmediateLoad).Value;
                _explosion[i]=t;TextureMiB+=t.Width*t.Height*4/1048576.0;
            }
        }
        var targets=d.GetRenderTargets();var viewport=d.Viewport;var blend=d.BlendState;
        var depth=d.DepthStencilState;var raster=d.RasterizerState;
        var s0=d.SamplerStates[0];var s1=d.SamplerStates[1];var t0=d.Textures[0];var t1=d.Textures[1];var vb=d.GetVertexBuffers();
        try
        {
            d.SetRenderTarget(Composite);d.Clear(Color.Black);
            float time=(float)((LabSystem.Ticks/60.0)%_duration);
            float frame=time*30;int a=Math.Min((int)frame,_frames-1),b=Math.Min(a+1,_frames-1);float f=frame-a;
            for(int i=_layers.Length-1;i>=0;i--)
            {
                var l=_layers[i];if(time<l.Start || time>=l.End)continue;
                // Visibility of the separate base layer never controls the mask texture.
                if(_demo && l.Index==1 && !ModContent.GetInstance<LabConfig>().MatteBaseVisible)continue;
                float Value(int channel)=>MathHelper.Lerp(_motion[(i*_frames+a)*6+channel],_motion[(i*_frames+b)*6+channel],f);
                float opacity=Value(5);if(opacity<=0.00001f)continue;
                var p=_effect.Parameters;
                p["uSize"].SetValue(l.Size);p["uAnchor"].SetValue(l.Anchor);
                p["uPosition"].SetValue(new Vector2(Value(0),Value(1)));p["uScale"].SetValue(new Vector2(Value(2),Value(3)));
                float angle=Value(4);p["uRotation"].SetValue(new Vector2(MathF.Cos(angle),MathF.Sin(angle)));
                p["uOpacity"].SetValue(opacity);p["uTint"].SetValue(l.Tint);
                p["uBlack"].SetValue(l.Black);p["uWhite"].SetValue(l.White);p["uMatteType"].SetValue(l.Matte);
                p["uAdditive"].SetValue(l.Additive?1f:0f);p["uSource"].SetValue(l.Source);p["uMask"].SetValue(l.Mask);
                d.BlendState=BlendState.AlphaBlend;d.DepthStencilState=DepthStencilState.None;d.RasterizerState=RasterizerState.CullNone;
                _effect.CurrentTechnique.Passes[0].Apply();d.DrawUserPrimitives(PrimitiveType.TriangleList,_quad,0,2);
                // Above the starfield, below every black-hole layer.
                if(l.Index==76 && LabSystem.ExplosionTick>=0)DrawExplosion();
            }
        }
        finally
        {
            d.SetRenderTargets(targets);d.Viewport=viewport;d.BlendState=blend;d.DepthStencilState=depth;d.RasterizerState=raster;
            d.SamplerStates[0]=s0;d.SamplerStates[1]=s1;d.Textures[0]=t0;d.Textures[1]=t1;d.SetVertexBuffers(vb);
        }
    }
    private void DrawExplosion()
    {
        int frame=(LabSystem.Ticks-LabSystem.ExplosionTick)/2;if(frame<0 || frame>=SequenceData.Explosion.Length)return;
        var f=SequenceData.Explosion[frame];Vector2 scale=new(Composite.Width/640f,Composite.Height/409f);
        _batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullNone);
        _batch.Draw(_explosion[f.Page],f.Offset*scale,f.Rect,Color.White,0,Vector2.Zero,scale,SpriteEffects.None,0);_batch.End();
    }
    public void Dispose(){Composite.Dispose();_effect.Dispose();_batch.Dispose();}
}
