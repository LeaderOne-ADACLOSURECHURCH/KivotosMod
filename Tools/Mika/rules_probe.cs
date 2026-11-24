using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using KivotosMod.Content.Items.Vanity.Mika;
using KivotosMod.Content.Tablet;

public static class MikaRulesProbe
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Validate()
    {
        Require(MikaPlayer.FlightFrameTicks == 4, "Wing animation should advance every 4 ticks");
        Require(MikaPlayer.VoiceIntervalTicks == 45, "Hurt voice cooldown should be 0.75 seconds");
        Require(Math.Abs(MikaArtLayout.HaloOffset(0)) < .0001f, "Halo bob starts away from its anchor");
        Require(Math.Abs(MikaArtLayout.HaloOffset(.6)-1.25f) < .0001f, "Halo upper bob bound");
        Require(Math.Abs(MikaArtLayout.HaloOffset(1.8)+1.25f) < .0001f, "Halo lower bob bound");
        for (int frame=0; frame<=240; frame++) {
            float y=MikaArtLayout.HaloOffset(frame/60.0);
            Require(Math.Abs(y)<=1.2501f, "Halo bob is too large");
            Require(Math.Abs(y-MikaArtLayout.HaloOffset(frame/60.0+2.4))<.0001f, "Halo bob period changed");
        }
        foreach (int height in new[] {74,68})
        foreach (SpriteEffects effects in new[] {SpriteEffects.None, SpriteEffects.FlipHorizontally, SpriteEffects.FlipVertically,
            SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically})
        {
            var origin = MikaArtLayout.CanvasOrigin(height, effects);
            // Recover the unflipped pixel coordinates of the native body anchor.
            float x = (effects & SpriteEffects.FlipHorizontally) != 0 ? 63-origin.X : origin.X;
            float y = (effects & SpriteEffects.FlipVertically) != 0 ? height-origin.Y : origin.Y;
            Require(x-20 == 13 && y-28 == height-56, "Wide canvas lost reference body offset");
        }
        var decorated = MikaArtLayout.CanvasOrigin(74, SpriteEffects.None);
        var flying = MikaArtLayout.CanvasOrigin(68, SpriteEffects.None);
        Require(46-decorated.Y == 40-flying.Y, "Switching wing strips moved the wing root vertically");
        var player = new Player(); var state = player.GetModPlayer<MikaPlayer>();
        var appearance = player.GetModPlayer<TabletStudentAppearancePlayer>();
        Require(!MikaVanitySet.IsMika(player), "Ordinary player has Mika skin");
        player.armor[10].type = 101; player.armor[11].type = 102; player.armor[12].type = 103;
        Require(MikaVanitySet.IsMika(player), "Full physical costume was rejected");
        appearance.HasSelection = true; appearance.IsMikaAppearance = false;
        Require(!MikaVanitySet.IsMika(player), "Formation override was ignored");
        appearance.IsMikaAppearance = true;
        Require(MikaVanitySet.IsMika(player), "Mika formation selection rejected");
        player.wingsLogic = 1; player.wingTimeMax = 180;
        player.velocity.Y = -2; player.controlJump = false;
        var seen = new System.Collections.Generic.HashSet<int>();
        for (int i=0; i<60; i++) { state.PostUpdate(); Require(state.Flapping,"Rising jump did not flap"); seen.Add(state.FlightFrame); }
        Require(seen.SetEquals(new[] {0,1,2,3,4}), "Missing flight frames");
        Require(player.wingsLogic == 1 && player.wingTimeMax == 180, "Wing skin changed ordinary flight stats");
        player.velocity.Y = 0; state.PostUpdate(); Require(!state.Flapping,"Grounded flap");
        player.velocity.Y = 2; state.PostUpdate(); Require(!state.Flapping,"Unassisted fall flap");
        player.controlJump = true; state.PostUpdate(); Require(state.Flapping,"Airborne jump did not flap");
        player.wingTime = 0; state.PostUpdate(); Require(!state.Flapping && state.FlightFrame == 0,"Exhausted flight kept flapping");
        player.velocity.Y = -2; state.PostUpdate(); Require(!state.Flapping,"Exhausted rising flight kept flapping");
        player.wingTime = 100; state.PostUpdate(); Require(state.Flapping,"Restored wing time did not resume flapping");
        foreach (int board in new[] { 22, 28, 45 }) {
            player.wingsLogic = board; player.wings = board; state.PostUpdate();
            Require(!state.Flapping && state.FlightFrame == 0,"Special board flapped Mika's wings");
            player.wings = 1; state.PostUpdate();
            Require(!state.Flapping,"Hidden functional board flapped Mika's wings");
            player.wingsLogic = 1; player.wings = board; state.PostUpdate();
            Require(!state.Flapping,"Visible vanity board flapped Mika's wings");
        }
        player.wingsLogic = player.wings = 1; state.PostUpdate();
        Require(state.Flapping,"Ordinary wings did not resume flapping after removing board");
        player.mount.Active = true; state.PostUpdate(); Require(!state.Flapping,"Mounted flap");
        player.mount.Active = false; player.wingsLogic = 0; state.PostUpdate(); Require(!state.Flapping,"Unequipped flap");
        player.wings = 20; state.PostUpdate(); Require(!state.Flapping,"Vanity-only wings granted flight skin");
        player.wingsLogic = 200; player.wingTimeMax = 0; state.PostUpdate(); Require(!state.Flapping,"Zero-flight wings flapped");
        player.wingTimeMax = 100; appearance.IsMikaAppearance = false; state.PostUpdate();
        Require(!state.Flapping && player.wingsLogic == 200 && player.wingTimeMax == 100,"Other student skin or stats changed");
        appearance.IsMikaAppearance = true; state.PostUpdate(); Require(state.Flapping,"Modded wing slot failed");
        Require(!MikaPlayer.IsHeavyHit(59,400) && MikaPlayer.IsHeavyHit(60,400), "15 percent damage boundary");
        Require(!MikaPlayer.IsHeavyHit(39,100) && MikaPlayer.IsHeavyHit(40,100), "40 damage floor");
        Require(!MikaPlayer.IsHeavyHit(74,500) && MikaPlayer.IsHeavyHit(75,500), "Life scaling");
        var mute = new Player.HurtModifiers(); state.ModifyHurt(ref mute); Require(mute.Muted,"Ready voice did not replace grunt");
        for (int tick=0; tick<=180; tick++) {
            Main.Tick = tick;
            if (tick>0) state.PostUpdate();
            var modifiers = new Player.HurtModifiers(); state.ModifyHurt(ref modifiers);
            Require(modifiers.Muted,"Vanilla grunt returned during voice cooldown");
            state.OnHurt(new Player.HurtInfo {Damage=20});
        }
        Require(SoundEngine.Calls.Count == 5 && SoundEngine.Calls.All(c=>c.Voice is 1 or 3), "Ordinary voice pool/cooldown");
        for (int i=1;i<SoundEngine.Calls.Count;i++) Require(SoundEngine.Calls[i].Tick-SoundEngine.Calls[i-1].Tick==45,"Voice cooldown is not 0.75 seconds");
        for(int i=0;i<45;i++) state.PostUpdate();
        Main.Tick=225; state.OnHurt(new Player.HurtInfo {Damage=60});
        Require(SoundEngine.Calls.Last().Voice is 2 or 4,"Heavy voice pool");
        int count=SoundEngine.Calls.Count;
        for(int i=0;i<45;i++) state.PostUpdate();
        state.OnHurt(new Player.HurtInfo {Damage=0});
        player.whoAmI=1; state.OnHurt(new Player.HurtInfo {Damage=60});
        player.whoAmI=0; Main.dedServ=true; state.OnHurt(new Player.HurtInfo {Damage=60});
        Main.dedServ=false; appearance.IsMikaAppearance=false; state.OnHurt(new Player.HurtInfo {Damage=60});
        Require(SoundEngine.Calls.Count==count,"Voice on zero damage, remote player, server or other student");
        var normalHurt = new Player.HurtModifiers(); state.ModifyHurt(ref normalHurt);
        Require(!normalHurt.Muted,"Other student's ordinary hurt sound was muted");
    }
}
