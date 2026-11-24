using System;
using System.Collections.Generic;
namespace Microsoft.Xna.Framework {
    public struct Vector2 {
        public float X,Y;public Vector2(float x,float y){X=x;Y=y;}
        public static Vector2 operator +(Vector2 a,Vector2 b)=>new(a.X+b.X,a.Y+b.Y);
    }
    public struct Rectangle {public int X,Y,Width,Height;public Rectangle(int x,int y,int w,int h){X=x;Y=y;Width=w;Height=h;}}
    public struct Color {
        public float Brightness;public static Color White=>new(){Brightness=1};
        public static Color operator *(Color a,float f)=>new(){Brightness=a.Brightness*f};
    }
}
namespace Microsoft.Xna.Framework.Graphics {
    [Flags] public enum SpriteEffects {None=0,FlipHorizontally=1,FlipVertically=2}
    public class Texture2D {public int Width,Height;public Texture2D(int w,int h){Width=w;Height=h;}}
}
namespace ReLogic.Content {
    public enum AssetRequestMode {AsyncLoad,ImmediateLoad}
    public class Asset<T> {public T Value;public bool IsLoaded;}
}
namespace Terraria {
    public static class Main {public static Microsoft.Xna.Framework.Vector2 screenPosition;public static double GlobalTimeWrappedHourly;}
    public class Player {
        public int width=20,height=42;public float headRotation,bodyRotation,gravDir=1;
        public Microsoft.Xna.Framework.Rectangle bodyFrame=new(0,560,40,56),legFrame=new(0,336,40,56);
        public Microsoft.Xna.Framework.Vector2 headPosition,bodyPosition;
    }
}
namespace Terraria.DataStructures {
    using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;
    public struct DrawData {
        public Rectangle? Source;public Texture2D Texture;public Vector2 Origin;public Color Color;public int shader;
        public DrawData(Texture2D t,Vector2 p,Rectangle? source,Color color,float rotation,Vector2 origin,float scale,SpriteEffects effects,float depth)
        {Texture=t;Source=source;Origin=origin;Color=color;shader=0;}
    }
    public struct PlayerDrawSet {
        public Terraria.Player drawPlayer;public Vector2 Position,helmetOffset,headVect,bodyVect;
        public SpriteEffects playerEffect;public Color colorArmorHead,colorArmorBody;public int cHead,cBody;public float shadow;
        public List<DrawData> DrawDataCache;
    }
}
namespace Terraria.ModLoader {
    using ReLogic.Content;using Microsoft.Xna.Framework.Graphics;
    public static class ModContent {
        public static int Requests;public static AssetRequestMode LastMode;
        public static Asset<T> Request<T>(string path,AssetRequestMode mode=AssetRequestMode.AsyncLoad) {
            Requests++;LastMode=mode;bool loaded=mode==AssetRequestMode.ImmediateLoad;
            object texture=loaded?(path=="Tail60"?new Texture2D(50,1200):path=="Halo"?new Texture2D(40,71):new Texture2D(40,1120)):new Texture2D(1,1);
            return new Asset<T>{Value=(T)texture,IsLoaded=loaded};
        }
    }
}
public static class PlayerTextureLoadingProbe {
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Validate() {
        var placeholder=Terraria.ModLoader.ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>("Strip");
        bool reproduced=false;try{Math.Clamp(560,0,placeholder.Value.Height-56);}catch(ArgumentException){reproduced=true;}
        Check(reproduced,"Async fixture did not reproduce the logged 1x1/-55 crash");
        var info=new Terraria.DataStructures.PlayerDrawSet {drawPlayer=new Terraria.Player(),headVect=new(20,28),bodyVect=new(20,28),
            DrawDataCache=new(),shadow=.25f};
        ReLogic.Content.Asset<Microsoft.Xna.Framework.Graphics.Texture2D> asset=null;
        KivotosMod.Content.Items.Vanity.StudentSpriteDraw.Strip(ref info,ref asset,"Strip",head:true);
        Check(asset.IsLoaded&&asset.Value.Height==1120,"First draw used placeholder");
        Check(info.DrawDataCache[0].Source.Value.Y==560&&info.DrawDataCache[0].Source.Value.Height==56,"Wrong body frame after load");
        int count=Terraria.ModLoader.ModContent.Requests;
        KivotosMod.Content.Items.Vanity.StudentSpriteDraw.Strip(ref info,ref asset,"Strip");
        Check(Terraria.ModLoader.ModContent.Requests==count,"Loaded texture was requested again");
        asset=placeholder;KivotosMod.Content.Items.Vanity.StudentSpriteDraw.Strip(ref info,ref asset,"Strip");
        Check(asset.IsLoaded,"Previously queued async asset was not completed");
        asset=null;KivotosMod.Content.Items.Vanity.StudentSpriteDraw.Strip(ref info,ref asset,"Tail60",frameHeight:60,paddingTop:4);
        var tail=info.DrawDataCache[^1];Check(tail.Source.Value.Y==600&&tail.Source.Value.Height==60&&tail.Origin.Y==32,"60px source frame");
        asset=null;KivotosMod.Content.Items.Vanity.StudentSpriteDraw.Halo(ref info,ref asset,"Halo",6,20);
        var halo=info.DrawDataCache[^1];Check(halo.Texture.Height==71&&Math.Abs(halo.Color.Brightness-.75f)<.0001f,"Halo used placeholder or lost full-bright fade");
        Check(Terraria.ModLoader.ModContent.LastMode==ReLogic.Content.AssetRequestMode.ImmediateLoad,"Player texture request remained asynchronous");
    }
}
