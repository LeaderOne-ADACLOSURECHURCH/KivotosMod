using System;
using System.Collections.Generic;
namespace Microsoft.Xna.Framework { public struct Vector2 { public float X, Y; public Vector2(float x, float y) { X=x; Y=y; } } }
namespace Microsoft.Xna.Framework.Graphics { [Flags] public enum SpriteEffects { None=0, FlipHorizontally=1, FlipVertically=2 } }
namespace Terraria.DataStructures { public struct PlayerDrawSet { } }
namespace Terraria.ID { public static class NetmodeID { public const int MultiplayerClient = 1; } }
namespace Terraria {
    public static class Main { public static bool dedServ; public static int myPlayer, Tick, netMode; public static Random rand = new(42); }
    public class Mount { public bool Active; }
    public class Item { public int type, headSlot, bodySlot, legSlot; public bool IsAir => type == 0; }
    public class Player {
        public int whoAmI, head, body, legs, wingsLogic, wings, wingTimeMax, statLifeMax2 = 400;
        public float wingTime = 999; public bool controlJump; public float gravDir = 1;
        public Microsoft.Xna.Framework.Vector2 velocity, Center;
        public Mount mount = new();
        public Item[] armor = Array.ConvertAll(new int[20], _ => new Item());
        readonly Dictionary<Type,object> mods = new();
        public T GetModPlayer<T>() where T:new() {
            if (!mods.TryGetValue(typeof(T), out var value)) { value = new T(); mods[typeof(T)] = value;
                if (value is ModLoader.ModPlayer mp) mp.Player = this; }
            return (T)value;
        }
        public struct HurtInfo { public int Damage; }
        public struct HurtModifiers { public bool Muted; public void DisableSound() => Muted = true; }
    }
}
namespace Terraria.ModLoader {
    public class ModPlayer {
        public Terraria.Player Player;
        public virtual void PostUpdate() { } public virtual void UpdateDead() { }
        public virtual void ModifyHurt(ref Terraria.Player.HurtModifiers modifiers) { } public virtual void OnHurt(Terraria.Player.HurtInfo info) { }
    }
    public static class ModContent {
        public static int ItemType<T>() => typeof(T).Name switch { "MikaHead" => 101,"MikaBody" => 102,"MikaBoots" => 103,_ => 104 };
        public static T GetInstance<T>() where T:new() => new T();
    }
}
namespace Terraria.Audio {
    public enum SoundLimitBehavior { ReplaceOldest }
    public record struct SoundStyle(string Path) {
        public int MaxInstances { get; set; } public SoundLimitBehavior SoundLimitBehavior { get; set; }
        public bool PlayOnlyIfFocused { get; set; } public int[] Variants { get; set; }
    }
    public static class SoundEngine {
        public static List<(int Tick,int Voice)> Calls = new();
        public static void PlaySound(SoundStyle sound,Microsoft.Xna.Framework.Vector2 position) {
            if (sound.MaxInstances != 1 || sound.SoundLimitBehavior != SoundLimitBehavior.ReplaceOldest)
                throw new Exception("New hurt voice must replace the old one without overlap");
            Calls.Add((Terraria.Main.Tick,sound.Variants[0]));
        }
    }
}
namespace KivotosMod.Content.Tablet {
    public sealed class TabletStudentAppearancePlayer {
        public bool HasSelection, IsMikaAppearance;
    }
}
namespace KivotosMod.Content.Items.Vanity.Mika {
    public class MikaHead { public Terraria.Item Item = new() {headSlot = 11}; }
    public class MikaBody { public Terraria.Item Item = new() {bodySlot = 12}; }
    public class MikaBoots { public Terraria.Item Item = new() {legSlot = 13}; }
}

namespace KivotosMod.Content.Items.Vanity.Mika { public class Mika2x2Head { public Terraria.Item Item=new(){headSlot=21}; } public class Mika2x2Body { public Terraria.Item Item=new(){bodySlot=22}; } public class Mika2x2Boots { public Terraria.Item Item=new(){legSlot=23}; } }

namespace KivotosMod.Content.Items.Vanity {
 internal static class StudentSpriteDraw { internal static bool SpecialFlightBoard(Terraria.Player p)=>p.wings is 22 or 28 or 45; }
}
namespace KivotosMod.Content.Tablet {
 public enum TabletStudentId { Mika }
 public static class TabletNet { public static void SendHurtVoice(int player,TabletStudentId student,int variant){} }
}
