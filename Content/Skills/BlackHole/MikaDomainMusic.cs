using System;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace KivotosMod.Content.Skills.BlackHole;

internal sealed class MikaDomainMusic : IDisposable
{
    private const int FadeOutTicks = 5 * 60;
    private int _slot=-1;
    private int _ending;
    private float _normalVolume=1;
    private bool _exclusive;

    internal MikaDomainMusic()
    {
        IL_Main.UpdateAudio+=OverrideSelection;
        On_Main.UpdateAudio+=Mix;
    }

    internal void Load(Mod mod)=>_slot=MusicLoader.GetMusicSlot(mod,"Assets/Music/MikaElizaP2");

    internal void Tick(int openingTick,bool useDomainTrack)
    {
        if(useDomainTrack)
        {
            _ending=FadeOutTicks;
            _exclusive=true;
            return;
        }
        if(_ending>0)
        {
            _ending--;
            _exclusive=_ending>0;
            _normalVolume=_exclusive?0:1;
            return;
        }
        _exclusive=false;
        _normalVolume=openingTick<0?1:1-MathHelper.Clamp(openingTick/240f,0,1);
    }

    internal void Reset()
    {
        _exclusive=false;
        _ending=0;
        _normalVolume=1;
        if(_slot>0 && _slot<Main.musicFade.Length)
            Main.musicFade[_slot]=0;
    }

    private void Mix(On_Main.orig_UpdateAudio orig,Main self)
    {
        if(Main.gameMenu){orig(self);return;}
        float original=Main.musicVolume;
        int countdown=NPC.MoonLordCountdown;
        try
        {
            if(_exclusive)
            {
                Main.musicVolume=original*(_ending/(float)FadeOutTicks);
                NPC.MoonLordCountdown=0;
            }
            else if(original>0)
                Main.musicVolume=original*Math.Max(.000001f,_normalVolume);
            orig(self);
        }
        finally
        {
            Main.musicVolume=original;
            NPC.MoonLordCountdown=countdown;
        }
    }

    private void OverrideSelection(ILContext il)
    {
        var c=new ILCursor(il);
        if(!c.TryGotoNext(MoveType.AfterLabel,i=>i.MatchLdsfld<Main>("musicFade"),i=>i.MatchLdsfld<Main>("curMusic"),
            i=>i.MatchLdelemR4()))
            throw new InvalidOperationException("Mika domain music selection anchor not found.");
        c.EmitDelegate<Action>(ApplyTrackSelection);
    }

    private void ApplyTrackSelection()
    {
        if(_slot<=0 || _slot>=Main.musicFade.Length)return;
        if(!_exclusive || Main.gameMenu || Main.musicVolume<=0)
        {
            // Terraria can leave the old track playing at its prior fade
            // while the newly selected track is still below 25% volume.
            Main.musicFade[_slot]=0;
            return;
        }
        for(int i=1;i<Main.musicFade.Length;i++)
            if(i!=28 && i!=45)Main.musicFade[i]=i==_slot?1:0;
        Main.curMusic=_slot;
    }

    public void Dispose(){IL_Main.UpdateAudio-=OverrideSelection;On_Main.UpdateAudio-=Mix;}
}
