using System;
using System.Buffers;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace KivotosMod.Content.Skills.BlackHole;

internal sealed class MikaShatter : IDisposable
{
    internal sealed class Physics
    {
        public float Extrusion { get; set; }
        public float ForceDepth { get; set; }
        public float ForceRadius { get; set; }
        public float Strength { get; set; }
        public float Spin { get; set; }
        public float Randomness { get; set; }
        public float Viscosity { get; set; }
        public float Gravity { get; set; }
        public float GravityZ { get; set; }
        public float LightDepth { get; set; }
        public float LightIntensity { get; set; }
        public float Ambient { get; set; }
        public float Diffuse { get; set; }
        public float Specular { get; set; }
        public float Shininess { get; set; }
        internal Physics Copy()=>(Physics)MemberwiseClone();
    }
    private sealed class ShatterData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Physics Settings { get; set; }
        public Piece[] Shards { get; set; }
    }
    private sealed class Piece
    {
        public float[] Center { get; set; }
        public float[] Vertices { get; set; }
        public int[] Rings { get; set; }
        public int[] Triangles { get; set; }
        internal Vector3 Axis, Variation;
        internal float Spin, Depth;
        internal Matrix Rotation;
        internal Vector3 Position;
    }

    private readonly GraphicsDevice _device;
    private readonly ShatterData _data;
    private readonly Effect _effect;
    private readonly Piece[] _sort;
    private readonly VertexPositionColorTexture[] _vertices;
    private Texture2D _capture;
    private bool _hasCapture;
    internal RenderTarget2D Composite { get; private set; }
    internal static Physics Defaults { get; private set; }

    internal MikaShatter(GraphicsDevice device)
    {
        _device=device;
        var mod=ModContent.GetInstance<KivotosMod>();
        _data=JsonSerializer.Deserialize<ShatterData>(mod.GetFileBytes("Assets/Skills/MikaDomain/Resonance/Shards.json"),
            new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
        Defaults=_data.Settings;
        _sort=(Piece[])_data.Shards.Clone();
        int count=0;
        foreach(var p in _sort)count+=p.Triangles.Length*2+p.Vertices.Length*3;
        _vertices=new VertexPositionColorTexture[count];
        _effect=new Effect(device,mod.GetFileBytes("Assets/Skills/MikaDomain/Shatter.fxc"));
    }

    internal void SetSeed(int seed)
    {
        var random=new Random(seed);
        float Signed()=>(float)(2*random.NextDouble()-1);
        foreach(var piece in _data.Shards)
        {
            var axis=new Vector3(Signed(),Signed(),Signed());
            piece.Axis=axis.LengthSquared()<.001f?Vector3.UnitY:Vector3.Normalize(axis);
            piece.Variation=new Vector3(Signed(),Signed(),Signed());
            piece.Spin=Signed();
        }
    }

    internal void Capture()
    {
        if(_hasCapture)return;
        var present=_device.PresentationParameters;
        int width=present.BackBufferWidth,height=present.BackBufferHeight,count=width*height;
        Color[] pixels=ArrayPool<Color>.Shared.Rent(count);
        try
        {
            _device.GetBackBufferData(null,pixels,0,count);
            SetCapture(width,height,pixels,count);
        }
        finally{ArrayPool<Color>.Shared.Return(pixels);}
    }

    internal void SetCapture(int width,int height,Color[] pixels,int count)
    {
        ClearCapture();
        PrepareCapture(width,height);
        _capture.SetData(pixels,0,count);
        _hasCapture=true;
    }
    internal void ClearCapture()=>_hasCapture=false;

    internal void PrepareCapture(int width,int height)
    {
        if(!_hasCapture && (_capture?.Width!=width || _capture.Height!=height))
        {
            _capture?.Dispose();
            _capture=new Texture2D(_device,width,height);
        }
        PrepareTarget(width,height);
    }

    private void PrepareTarget(int width,int height)
    {
        if(Composite?.Width!=width || Composite.Height!=height || Composite.IsContentLost)
        {
            Composite?.Dispose();
            Composite=new RenderTarget2D(_device,width,height,false,SurfaceFormat.Color,DepthFormat.None);
        }
    }

    internal void Prepare(int width,int height,float age,Physics settings)
    {
        PrepareTarget(width,height);
        float fade=MathHelper.Clamp(1-age/1.75f,0,1);
        float aspect=_data.Width/(float)_data.Height;
        float cover=Math.Max(width/aspect,height);
        Vector2 midpoint=new(width/2f,height/2f);
        float drag=Math.Max(0,settings.Viscosity)*2;
        float time=drag<.001f?age:(1-MathF.Exp(-drag*age))/drag;
        Vector3 force=new(0,0,settings.ForceDepth);
        foreach(var piece in _sort)
        {
            Vector3 center=new((piece.Center[0]-.5f)*aspect,piece.Center[1]-.5f,0);
            Vector3 offset=center-force;
            float distance=offset.Length();
            float influence=MathHelper.Clamp(1-distance/Math.Max(.001f,settings.ForceRadius),0,1);
            Vector3 velocity=distance>.0001f?offset/distance*(settings.Strength*.32f*influence):Vector3.Zero;
            velocity+=new Vector3(0,0,settings.Strength*.025f);
            velocity+=piece.Variation*Math.Abs(settings.Strength)*.025f*settings.Randomness*influence;
            piece.Position=center+velocity*time+new Vector3(0,0,settings.Gravity*settings.GravityZ)*(.5f*age*age);
            piece.Depth=piece.Position.Z;
            piece.Rotation=Matrix.CreateFromAxisAngle(piece.Axis,settings.Spin*8*time*(1+piece.Spin*settings.Randomness));
        }
        Array.Sort(_sort,(a,b)=>a.Depth.CompareTo(b.Depth));
        var light=new Vector3(0,0,settings.LightDepth);
        float thickness=settings.Extrusion*.045f;
        int count=0;
        var captureAspect=_hasCapture?_capture.Width/(float)_capture.Height:width/(float)height;
        Vector2 captureCover=new Vector2(aspect,1)/new Vector2(captureAspect,1)*Math.Max(captureAspect/aspect,1);
        var captureOpacity=_hasCapture ? .5f*MathHelper.Clamp(1-age/1.5f,0,1) : 0;
        foreach(var piece in _sort)
        {
            Color Shade(Vector3 normal,float edge)
            {
                normal=Vector3.TransformNormal(normal,piece.Rotation);
                Vector3 toLight=light-piece.Position;
                float dist=toLight.Length();
                toLight=dist>.001f?toLight/dist:Vector3.UnitZ;
                float facing=MathF.Abs(Vector3.Dot(normal,toLight));
                float diffuse=settings.LightIntensity*settings.Diffuse*facing;
                float highlight=MathF.Pow(facing,settings.Shininess)*settings.Specular*settings.LightIntensity;
                float intensity=MathHelper.Clamp(highlight*(.15f+.85f*Math.Min(1,age*3))+
                    edge*(settings.Ambient+diffuse)*.16f*Math.Min(1,age*4),0,1);
                return new Color(intensity,intensity,intensity,fade);
            }
            void Add(int index,float z,Color color)
            {
                var uv=new Vector2(piece.Vertices[index*2],piece.Vertices[index*2+1]);
                Vector3 local=new((uv.X-piece.Center[0])*aspect,uv.Y-piece.Center[1],z);
                Vector3 world=piece.Position+Vector3.Transform(local,piece.Rotation);
                float perspective=2/Math.Max(.4f,2-world.Z);
                Vector2 xy=midpoint+new Vector2(world.X,world.Y)*cover*perspective;
                _vertices[count++]=new VertexPositionColorTexture(new Vector3(xy,0),color,(uv-new Vector2(.5f))*captureCover+new Vector2(.5f));
            }
            Color front=Shade(Vector3.UnitZ,0);
            Color back=Shade(-Vector3.UnitZ,.25f);
            bool facingCamera=Vector3.TransformNormal(Vector3.UnitZ,piece.Rotation).Z>=0;
            if(!facingCamera)
                foreach(int i in piece.Triangles)Add(i,-thickness,back);
            if(thickness>0)
            {
                int first=0;
                foreach(int end in piece.Rings)
                {
                    for(int i=first;i<end;i++)
                    {
                        int j=(i+1==end)?first:i+1;
                        float dx=piece.Vertices[2*j]-piece.Vertices[2*i],dy=piece.Vertices[2*j+1]-piece.Vertices[2*i+1];
                        Vector3 side=new(dy,-dx*aspect,0);
                        Color edge=Shade(side.LengthSquared()>.000001f?Vector3.Normalize(side):Vector3.UnitX,1);
                        Add(i,0,edge);Add(j,0,edge);Add(i,-thickness,edge);
                        Add(i,-thickness,edge);Add(j,0,edge);Add(j,-thickness,edge);
                    }
                    first=end;
                }
            }
            if(facingCamera)
                foreach(int i in piece.Triangles)Add(i,0,front);
        }

        var targets=_device.GetRenderTargets();
        var viewport=_device.Viewport;
        var blend=_device.BlendState;
        var depth=_device.DepthStencilState;
        var raster=_device.RasterizerState;
        var sampler=_device.SamplerStates[0];
        var texture=_device.Textures[0];
        var buffers=_device.GetVertexBuffers();
        try
        {
            _device.Textures[0]=null;
            _device.SetRenderTarget(Composite);
            _device.Clear(Color.Transparent);
            _device.BlendState=BlendState.AlphaBlend;
            _device.DepthStencilState=DepthStencilState.None;
            _device.RasterizerState=RasterizerState.CullNone;
            _effect.Parameters["uProjection"].SetValue(Matrix.CreateOrthographicOffCenter(0,width,height,0,0,1));
            _effect.Parameters["uCaptureOpacity"].SetValue(captureOpacity);
            _effect.Parameters["uScreenshot"].SetValue(_hasCapture?_capture:TextureAssets.MagicPixel.Value);
            _effect.CurrentTechnique.Passes[0].Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleList,_vertices,0,count/3);
        }
        finally
        {
            _device.SetRenderTargets(targets);_device.Viewport=viewport;_device.BlendState=blend;
            _device.DepthStencilState=depth;_device.RasterizerState=raster;
            _device.SamplerStates[0]=sampler;_device.Textures[0]=texture;_device.SetVertexBuffers(buffers);
        }
    }

    public void Dispose(){_capture?.Dispose();Composite?.Dispose();_effect.Dispose();}
}
