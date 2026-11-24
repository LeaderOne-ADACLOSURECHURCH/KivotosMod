using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using KivotosMod.Content.Tablet;
using KivotosMod.Content.Items.Vanity;

public static class StudentBatchProbe
{
    private static void Check(bool condition,string message) { if (!condition) throw new Exception(message); }
    public static void Validate()
    {
        var slots=typeof(TabletStudentAppearancePlayer).GetMethod("ApplyStudentVisualSlots",BindingFlags.NonPublic|BindingFlags.Instance);
        string[] names={"Yuuka","Hoshino","Shiroko","Arona","Sora","Plana","Hina","Noa","Ibuki","Iroha","Hikari","Mika","Haruka","Kayoko","Mutsuki","Nozomi","Nagisa","Seia","Mika2x2","Yuuka2x2","Ibuki2x2","Iroha2x2","Hikari2x2"};
        for (byte id=0;id<names.Length;id++) {
            var student=(TabletStudentId)id;
            Check(student.ToString()==names[id],"Persisted student IDs changed");
            Check(TabletStudentAppearancePlayer.IsFormationStudent(student)==(id!=3&&id!=4&&id!=5),"Whitelist mismatch");
            var original=new TabletStudentAppearancePlayer();original.Select(student);
            var tag=new TagCompound();original.SaveData(tag);
            var loaded=new TabletStudentAppearancePlayer();loaded.LoadData(tag);
            if (id==3||id==4||id==5) { Check(!loaded.HasSelection,"Companion entered Formation");continue; }
            Check(loaded.HasSelection&&loaded.SelectedStudent==student,"Selection did not round-trip");
            loaded.ReceiveSelection(true,student);
            if (id>=12) {
                slots.Invoke(loaded,new object[]{loaded.Player});
                foreach(var part in new[]{("Head","head"),("Body","body"),("Boots","legs")}) {
                    var type=typeof(StudentBatchProbe).Assembly.GetType("KivotosMod.Content.Items.Vanity."+names[id].Replace("2x2","")+"."+names[id]+part.Item1);
                    var item=(OutfitItem)typeof(ModContent).GetMethod("GetInstance").MakeGenericMethod(type).Invoke(null,null);
                    int expected=part.Item2=="head"?item.Item.headSlot:part.Item2=="body"?item.Item.bodySlot:item.Item.legSlot;
                    Check((int)typeof(Player).GetField(part.Item2).GetValue(loaded.Player)==expected,"Wrong outfit equip slot");
                }
                var light=new StudentHaloLightPlayer { Player=loaded.Player };
                Lighting.Calls.Clear();light.PostUpdate();Check(Lighting.Calls.Count==1,"Selected outfit halo illumination mismatch");
            }
            loaded.ClearSelection();Check(!loaded.HasSelection,"Clear selection failed");
        }
        Check(!TabletStudentAppearancePlayer.IsFormationStudent((TabletStudentId)255),"Invalid network ID accepted");
        var invalid=new TabletStudentAppearancePlayer();invalid.LoadData(new TagCompound { ["HasSelection"]=true,["SelectedStudent"]=(byte)255 });
        // Retain the existing save behavior: out-of-range IDs fall back to Yuuka.
        Check(invalid.SelectedStudent==TabletStudentId.Yuuka,"Invalid save fallback failed");

        foreach (string name in new[]{"Haruka","Kayoko","Mutsuki","Ibuki","Iroha","Hikari","Nozomi","Mika","Yuuka","Noa","Nagisa","Seia"}) {
            var player=new Player();
            var type=typeof(StudentBatchProbe).Assembly.GetType("KivotosMod.Content.Items.Vanity."+name+"."+name+"Head");
            var item=(OutfitItem)typeof(ModContent).GetMethod("GetInstance").MakeGenericMethod(type).Invoke(null,null);
            player.head=item.Item.headSlot;
            var light=new StudentHaloLightPlayer {Player=player};
            Lighting.Calls.Clear();light.PostUpdate();Check(Lighting.Calls.Count==1,"Missing halo light: "+name);
            var call=Lighting.Calls[0];
            Check(call.Position.Y<player.Center.Y,"Normal light must be above player");
            Check(call.Color.X<=.5501f&&call.Color.Y<=.5501f&&call.Color.Z<=.5501f,"Light exceeds gentle intensity");
            player.gravDir=-1;Lighting.Calls.Clear();light.PostUpdate();Check(Lighting.Calls[0].Position.Y>player.Center.Y,"Reverse-gravity light position");
            player.gravDir=1;
            foreach(string flag in new[]{"dead","ghost","invis","active","server","menu"}) {
                if(flag=="server") Main.dedServ=true;
                else if(flag=="menu") Main.gameMenu=true;
                else typeof(Player).GetField(flag).SetValue(player,flag!="active");
                Lighting.Calls.Clear();light.PostUpdate();Check(Lighting.Calls.Count==0,"Unexpected halo light: "+flag);
                if(flag=="server") Main.dedServ=false;
                else if(flag=="menu") Main.gameMenu=false;
                else typeof(Player).GetField(flag).SetValue(player,flag=="active");
            }
            player.head=-1;Lighting.Calls.Clear();light.PostUpdate();Check(Lighting.Calls.Count==0,"Light persisted after removing outfit");
        }
    }
}
