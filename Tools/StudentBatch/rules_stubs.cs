using System;
using System.Collections.Generic;
namespace Microsoft.Xna.Framework {
    public struct Vector2 {
        public float X,Y; public Vector2(float x,float y) { X=x;Y=y; }
        public static Vector2 operator +(Vector2 a,Vector2 b) => new(a.X+b.X,a.Y+b.Y);
    }
    public struct Vector3 {
        public float X,Y,Z; public Vector3(float x,float y,float z) { X=x;Y=y;Z=z; }
        public static Vector3 operator *(Vector3 v,float f) => new(v.X*f,v.Y*f,v.Z*f);
    }
}
namespace Terraria.ID { }
namespace Terraria {
    public static class Main { public static bool dedServ,gameMenu; public static int myPlayer,Tick; public static Random rand=new(42); }
    public class Item { public int headSlot,bodySlot,legSlot,type; public bool IsAir => type==0; }
    public class Player {
        public int head=-1,body=-1,legs=-1,height=42; public bool active=true,dead,ghost,invis; public float gravDir=1;
        public Microsoft.Xna.Framework.Vector2 Center=new(100,100),headPosition;
        public int whoAmI,statLifeMax2=400;
        public Item[] armor=Array.ConvertAll(new int[20],_=>new Item());
        private readonly Dictionary<Type,object> mods=new();
        public T GetModPlayer<T>() where T:new() {
            if(!mods.TryGetValue(typeof(T),out var value)) { value=new T();mods[typeof(T)]=value;
                if(value is ModLoader.ModPlayer mp) mp.Player=this; }
            return (T)value;
        }
        public struct HurtInfo { public int Damage; }
        public struct HurtModifiers { public bool Muted; public void DisableSound() => Muted=true; }
    }
    public static class Lighting {
        public static List<(Microsoft.Xna.Framework.Vector2 Position,Microsoft.Xna.Framework.Vector3 Color)> Calls=new();
        public static void AddLight(Microsoft.Xna.Framework.Vector2 position,Microsoft.Xna.Framework.Vector3 color) => Calls.Add((position,color));
    }
}
namespace Terraria.ModLoader.IO {
    public class TagCompound : Dictionary<string,object> {
        public bool GetBool(string key) => ContainsKey(key) && (bool)this[key];
        public byte GetByte(string key) => (byte)this[key];
    }
}
namespace Terraria.ModLoader {
    public class ModPlayer {
        public Terraria.Player Player=new();
        public virtual void PostUpdate() { }
        public virtual void SaveData(IO.TagCompound tag) { }
        public virtual void LoadData(IO.TagCompound tag) { }
        public virtual void UpdateDead() { }
        public virtual void ModifyHurt(ref Terraria.Player.HurtModifiers modifiers) { }
        public virtual void OnHurt(Terraria.Player.HurtInfo info) { }
    }
    public static class ModContent {
        private static readonly Dictionary<Type,object> Items=new();
        public static T GetInstance<T>() where T:new() {
            if (!Items.TryGetValue(typeof(T),out var item)) { item=new T();Items[typeof(T)]=item; }
            return (T)item;
        }
        public static int ItemType<T>() => Math.Abs(typeof(T).FullName.GetHashCode())+1;
    }
    public class ModItem { public virtual string Texture => GetType().Name; }
    public class OutfitItem : ModItem {
        static int next=100;
        public Terraria.Item Item=new() {headSlot=++next,bodySlot=++next,legSlot=++next};
    }
}
namespace Terraria.Audio {
    public enum SoundLimitBehavior { ReplaceOldest }
    public record struct SoundStyle(string Path) {
        public string SoundPath {get;set;}
        public int MaxInstances {get;set;} public SoundLimitBehavior SoundLimitBehavior {get;set;}
        public bool PlayOnlyIfFocused {get;set;} public int[] Variants {get;set;}
    }
    public static class SoundEngine {
        public static List<(int Tick,string Path,int Variant)> Calls=new();
        public static void PlaySound(SoundStyle style,Microsoft.Xna.Framework.Vector2 position) {
            if(style.MaxInstances!=1 || style.SoundLimitBehavior!=SoundLimitBehavior.ReplaceOldest) throw new Exception("Voice overlap policy");
            Calls.Add((Terraria.Main.Tick,style.SoundPath,style.Variants[0]));
        }
    }
}
namespace KivotosMod.Globals.Configs {
    public class KivotosClientConfig { public bool UseYuuka2x2,UseMika2x2; }
}
