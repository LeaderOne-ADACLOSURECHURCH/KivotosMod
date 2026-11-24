// Standalone hidden-window FNA probe, not a Terraria launch or Mod build.
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
class P {
 [DllImport("SDL2.dll")] static extern int SDL_Init(uint flags);
 [DllImport("SDL2.dll")] static extern IntPtr SDL_CreateWindow(string title,int x,int y,int w,int h,uint flags);
 [DllImport("SDL2.dll")] static extern void SDL_DestroyWindow(IntPtr w);
 static void Main(string[] args) {
  SDL_Init(0x20);
  var ft=typeof(GraphicsDevice).Assembly.GetTypes().First(t=>t.Name=="FNA3D");
  uint flags=(uint)ft.GetMethod("FNA3D_PrepareWindowAttributes",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)!.Invoke(null,null)!;
  var window=SDL_CreateWindow("Hidden track matte probe",0,0,64,64,flags|8);
  try {
   using var d=new GraphicsDevice(GraphicsAdapter.DefaultAdapter,GraphicsProfile.HiDef,new PresentationParameters{DeviceWindowHandle=window,BackBufferWidth=64,BackBufferHeight=64});
   string root=args[0],output=Path.Combine(root,"Tools/MikaDomain");Directory.CreateDirectory(output);
   using var scene=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root,"Assets/Skills/MikaDomain/Scene.json")));
   string maskName=scene.RootElement.GetProperty("textures").GetProperty("光带.png").GetProperty("texture").GetString()!;
   string sourceName=scene.RootElement.GetProperty("textures").GetProperty("光球浅.png").GetProperty("texture").GetString()!;
   using var sourceStream=File.OpenRead(Path.Combine(root,"Assets/Skills/MikaDomain",sourceName+".png"));
   using var maskStream=File.OpenRead(Path.Combine(root,"Assets/Skills/MikaDomain",maskName+".png"));
   using var source=Texture2D.FromStream(d,sourceStream);using var mask=Texture2D.FromStream(d,maskStream);
   using var zero=new Texture2D(d,1,1);zero.SetData(new[]{Color.Transparent});
   using var e=new Effect(d,File.ReadAllBytes(Path.Combine(root,"Assets/Skills/MikaDomain/Domain.fxc")));
   int w=1559,h=1007;using var target=new RenderTarget2D(d,w,h,false,SurfaceFormat.Color,DepthFormat.None);
   var p=e.Parameters;p["uMatrix"].SetValue(Matrix.CreateOrthographicOffCenter(0,w,h,0,0,1));p["uCanvas"].SetValue(new Vector2(w,h));
   p["uScale"].SetValue(Vector2.One);p["uRotation"].SetValue(new Vector2(1,0));p["uOpacity"].SetValue(1f);
   p["uMaskOpacity"].SetValue(1f);p["uMaskScale"].SetValue(Vector2.One);p["uTint"].SetValue(0f);p["uBlack"].SetValue(Vector3.Zero);p["uWhite"].SetValue(Vector3.One);p["uAdditive"].SetValue(0f);
   VertexPositionTexture[] quad={new(Vector3.Zero,new(0,0)),new(Vector3.Zero,new(1,0)),new(Vector3.Zero,new(0,1)),new(Vector3.Zero,new(0,1)),new(Vector3.Zero,new(1,0)),new(Vector3.Zero,new(1,1))};
   Color[] Render(Texture2D image,Vector2 position,bool clip,Texture2D matte,string file) {
    d.SetRenderTarget(target);d.Clear(Color.Transparent);
    p["uSize"].SetValue(new Vector2(image.Width,image.Height));p["uAnchor"].SetValue(new Vector2(image.Width,image.Height)*.5f);
    p["uPosition"].SetValue(position);p["uSource"].SetValue(image);p["uMask"].SetValue(matte);p["uMatteType"].SetValue(clip?1f:0f);
    d.BlendState=BlendState.AlphaBlend;d.DepthStencilState=DepthStencilState.None;d.RasterizerState=RasterizerState.CullNone;
    e.CurrentTechnique.Passes[0].Apply();d.DrawUserPrimitives(PrimitiveType.TriangleList,quad,0,2);d.SetRenderTarget(null);
    Color[] pixels=new Color[w*h];target.GetData(pixels);
    if(file!="") { using var stream=File.Create(Path.Combine(output,file+".png"));target.SaveAsPng(stream,w,h); }
    return pixels;
   }
   var maskImage=Render(mask,new(w*.5f,h*.5f),false,zero,"gpu_mask");var results=new List<object>();
   foreach(var (position,name) in new[]{(new Vector2(779.5f,503.5f),"center"),(new Vector2(1100,623),"moving"),(new Vector2(1329.5f,709.5f),"right")}) {
    var full=Render(source,position,false,zero,"gpu_source_"+name);
    var clipped=Render(source,position,true,mask,"gpu_result_"+name);
    var empty=Render(source,position,true,zero,"");
    int error=0,positive=0;
    for(int i=0;i<full.Length;i++) {
     int expected=(full[i].A*maskImage[i].A+127)/255;
     error=Math.Max(error,Math.Abs(expected-clipped[i].A));
     if(clipped[i].A>0)positive++;
     if(empty[i].PackedValue!=0)throw new Exception("Empty hidden matte failed to hide source");
    }
    Console.WriteLine($"{name}: max alpha-product error={error}, visible pixels={positive}");
    if(error>2 || positive==0)throw new Exception("GPU track matte alpha-product check failed");
    p["uMaskOpacity"].SetValue(.58f);
    var partial=Render(source,position,true,mask,"gpu_opacity58_"+name);int partialError=0;
    for(int i=0;i<full.Length;i++)partialError=Math.Max(partialError,Math.Abs((int)MathF.Round(full[i].A*maskImage[i].A/255f*.58f)-partial[i].A));
    p["uMaskOpacity"].SetValue(1f);
    Console.WriteLine($"{name}: 58% matte opacity error={partialError}");if(partialError>2)throw new Exception("Matte opacity failed");
    results.Add(new{name,maxAlphaError=error,visiblePixels=positive,emptyMatteHidesSource=true});
   }
   p["uScale"].SetValue(new Vector2(1.4105196f));
   var enlarged=Render(mask,new(w*.5f,h*.5f),false,zero,"");p["uScale"].SetValue(Vector2.One);
   var unmasked=Render(source,new(w*.5f,h*.5f),false,zero,"");p["uMaskScale"].SetValue(new Vector2(1.4105196f));
   var scaledResult=Render(source,new(w*.5f,h*.5f),true,mask,"gpu_mask141");int scaleError=0;
   for(int i=0;i<unmasked.Length;i++)scaleError=Math.Max(scaleError,Math.Abs((unmasked[i].A*enlarged[i].A+127)/255-scaledResult[i].A));
   Console.WriteLine($"141% mask scale error={scaleError}");if(scaleError>2)throw new Exception("Matte scale failed");p["uMaskScale"].SetValue(Vector2.One);
   var layers=scene.RootElement.GetProperty("layers").EnumerateArray().ToArray();
   using var br=new BinaryReader(File.OpenRead(Path.Combine(root,"Assets/Skills/MikaDomain/Motion.bin")));
   int layerCount=br.ReadInt32(),frames=br.ReadInt32();br.ReadInt32();float[] motion=new float[layerCount*frames*6];for(int j=0;j<motion.Length;j++)motion[j]=br.ReadSingle();
   var cache=new Dictionary<string,Texture2D>();
   Texture2D Load(string name){if(cache.TryGetValue(name,out var image))return image;using var stream=File.OpenRead(Path.Combine(root,"Assets/Skills/MikaDomain",name+".png"));return cache[name]=Texture2D.FromStream(d,stream);}
   foreach(int frame in new[]{0,450}) {
    d.SetRenderTarget(target);d.Clear(Color.Transparent);
    for(int j=layerCount-1;j>=0;j--){var l=layers[j];float Value(int ch)=>motion[(j*frames+frame)*6+ch];if(Value(5)<=0)continue;
     var tint=l.GetProperty("tint");var adjust=l.GetProperty("fx");var anchor=l.GetProperty("anchor");
     p["uSize"].SetValue(new Vector2(l.GetProperty("width").GetSingle(),l.GetProperty("height").GetSingle()));p["uAnchor"].SetValue(new Vector2(anchor[0].GetSingle(),anchor[1].GetSingle()));
     p["uPosition"].SetValue(new Vector2(Value(0),Value(1)));p["uScale"].SetValue(new Vector2(Value(2),Value(3)));p["uRotation"].SetValue(new Vector2(MathF.Cos(Value(4)),MathF.Sin(Value(4))));
     p["uMaskOpacity"].SetValue(l.GetProperty("mask_opacity").GetSingle());p["uMaskScale"].SetValue(new Vector2(l.GetProperty("mask_scale")[0].GetSingle(),l.GetProperty("mask_scale")[1].GetSingle())/100);p["uOpacity"].SetValue(Value(5));p["uTint"].SetValue(tint[3].GetSingle());p["uBlack"].SetValue(new Vector3(tint[0].GetSingle(),tint[1].GetSingle(),tint[2].GetSingle()));p["uWhite"].SetValue(new Vector3(tint[4].GetSingle(),tint[5].GetSingle(),tint[6].GetSingle()));
     p["uAdjust"].SetValue(new Vector4(MathHelper.ToRadians(adjust[0].GetSingle()),adjust[1].GetSingle(),adjust[2].GetSingle(),adjust[3].GetSingle()));p["uGlow"].SetValue(Vector3.Zero);
     int type=l.GetProperty("matte").GetInt32();p["uMatteType"].SetValue(type==5013?1f:type==5015?2f:0f);p["uAdditive"].SetValue(l.GetProperty("blend").GetInt32()==5223?1f:0f);
     p["uSource"].SetValue(Load(l.GetProperty("texture").GetString()!));var mk=l.GetProperty("mask");p["uMask"].SetValue(mk.ValueKind==JsonValueKind.Null?zero:Load(mk.GetString()!));
     e.CurrentTechnique.Passes[0].Apply();d.DrawUserPrimitives(PrimitiveType.TriangleList,quad,0,2);
    }
    d.SetRenderTarget(null);using var png=File.Create(Path.Combine(output,$"gpu_domain_{frame/30:00}.png"));target.SaveAsPng(png,w,h);
   }
   foreach(var image in cache.Values)image.Dispose();
   File.WriteAllText(Path.Combine(output,"gpu_validation.json"),JsonSerializer.Serialize(new{backend="FNA D3D11",hiddenWindow=true,matteDrawnIntoResult=false,results},new JsonSerializerOptions{WriteIndented=true}));
  } finally { SDL_DestroyWindow(window); }
 }
}
