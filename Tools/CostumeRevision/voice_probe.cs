using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using KivotosMod.Content.Tablet;
using KivotosMod.Content.Items.Vanity;
using KivotosMod.Content.Items.Vanity.Noa;

public static class StudentVoiceProbe
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static void Validate()
    {
        foreach(var student in new[]{TabletStudentId.Yuuka,TabletStudentId.Noa,TabletStudentId.Hina,TabletStudentId.Nagisa,TabletStudentId.Seia}) {
            var player=new Player();var appearance=player.GetModPlayer<TabletStudentAppearancePlayer>();appearance.Select(student);
            var voice=player.GetModPlayer<StudentHurtVoicePlayer>();
            SoundEngine.Calls.Clear();
            for(int tick=0;tick<=180;tick++) {
                Main.Tick=tick;if(tick>0)voice.PostUpdate();
                var modifiers=new Player.HurtModifiers();voice.ModifyHurt(ref modifiers);Check(modifiers.Muted,"Vanilla grunt during cooldown");
                voice.OnHurt(new Player.HurtInfo {Damage=20});
            }
            Check(SoundEngine.Calls.Count==5,"Voice cooldown not 45 ticks");
            int[] normal=StudentHurtVoicePlayer.Variants(student,false);
            Check(SoundEngine.Calls.All(c=>normal.Contains(c.Variant)&&c.Path.Contains("/"+student+"/Hurt")),"Wrong student/normal voice");
            for(int i=1;i<5;i++)Check(SoundEngine.Calls[i].Tick-SoundEngine.Calls[i-1].Tick==45,"Voice trigger interval");
            for(int i=0;i<45;i++)voice.PostUpdate();
            voice.OnHurt(new Player.HurtInfo {Damage=60});Check(StudentHurtVoicePlayer.Variants(student,true).Contains(SoundEngine.Calls.Last().Variant),"Heavy damage voice group");
            int count=SoundEngine.Calls.Count;for(int i=0;i<45;i++)voice.PostUpdate();
            Main.dedServ=true;voice.OnHurt(new Player.HurtInfo {Damage=60});Main.dedServ=false;
            player.whoAmI=1;voice.OnHurt(new Player.HurtInfo {Damage=60});player.whoAmI=0;
            voice.OnHurt(new Player.HurtInfo {Damage=0});Check(SoundEngine.Calls.Count==count,"Server/remote/zero damage voice");
            appearance.Select(TabletStudentId.Arona);var untouched=new Player.HurtModifiers();voice.ModifyHurt(ref untouched);
            Check(!untouched.Muted&&!voice.Speaker().HasValue,"Unsupported student's sound changed");
        }
        var physical=new Player();physical.armor[10].type=ModContent.ItemType<NoaHead>();
        physical.armor[11].type=ModContent.ItemType<NoaBody>();physical.armor[12].type=ModContent.ItemType<NoaBoots>();
        var physicalVoice=physical.GetModPlayer<StudentHurtVoicePlayer>();Check(physicalVoice.Speaker()==TabletStudentId.Noa,"Physical outfit voice rejected");
        physical.GetModPlayer<TabletStudentAppearancePlayer>().Select(TabletStudentId.Mika);
        Check(!physicalVoice.Speaker().HasValue,"Formation selection did not override physical outfit");

        // Exercise lighting before FrameEffects has assigned the costume head slot.
        var selected=new Player();var selectedAppearance=selected.GetModPlayer<TabletStudentAppearancePlayer>();selectedAppearance.Select(TabletStudentId.Mika);
        var light=new StudentHaloLightPlayer {Player=selected};Lighting.Calls.Clear();light.PostUpdate();Check(Lighting.Calls.Count==1,"Selection had no environment light before visual slots");
        selectedAppearance.Select(TabletStudentId.Arona);Lighting.Calls.Clear();light.PostUpdate();Check(Lighting.Calls.Count==0,"Stale selected-student light");

        var first=new Player();var second=new Player();
        first.head=ModContent.GetInstance<KivotosMod.Content.Items.Vanity.Mika.Mika2x2Head>().Item.headSlot;
        second.head=ModContent.GetInstance<KivotosMod.Content.Items.Vanity.Mika.MikaHead>().Item.headSlot;
        Check(StudentSpriteVariants.Mika2x2(first)&&!StudentSpriteVariants.Mika2x2(second),"Outfit selection leaked between players");
        foreach(var id in new[]{TabletStudentId.Mika2x2,TabletStudentId.Yuuka2x2,TabletStudentId.Ibuki2x2,TabletStudentId.Iroha2x2,TabletStudentId.Hikari2x2})
            Check(StudentSpriteVariants.OtherOutfit(StudentSpriteVariants.OtherOutfit(id))==id,"Wardrobe cycle is not reversible");
    }
}
