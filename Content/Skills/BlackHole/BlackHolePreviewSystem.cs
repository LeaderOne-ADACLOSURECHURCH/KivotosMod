using System;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;
using KivotosMod.Globals.Configs;

namespace KivotosMod.Content.Skills.BlackHole;

[Autoload(Side = ModSide.Client)]
public sealed class BlackHolePreviewSystem : ModSystem
{
    internal static bool Enabled { get; private set; }
    internal static int Ticks { get; private set; }
    internal static bool PaintDemo;
    internal static string CompositionDemo { get; private set; }
    internal static bool OpenDemo { get; private set; }
    internal static int ResonanceTick { get; private set; } = -1;
    internal static float Left => 779.5f - Main.screenWidth / (2 * ScreenScale);
    internal static float ScreenScale => Math.Max(Main.screenWidth / (float)MikaDomainTuning.ViewWidth,
        Main.screenHeight / (float)MikaDomainTuning.ViewHeight);
    internal static float Drift => PaintDemo || CompositionDemo != null ? 0 : 20 * MathF.Sin(Ticks * MathHelper.TwoPi / 360f);
    internal static float Visibility { get; private set; }
    private static int _domainSerial = -1, _castStart = -1, _previewCrystalReady;
    private static int _fadeTicks;
    private static bool _previewResonance;
    private static uint _shakeSeed;
    private static int _lastAudioTick = -1;
    private static MikaResonanceRenderer.AudioCue[] _cues=Array.Empty<MikaResonanceRenderer.AudioCue>();
    private static ReLogic.Utilities.SlotId[] _audio=Array.Empty<ReLogic.Utilities.SlotId>();
    private MikaDomainRenderer _domain;
    private MikaDomainEnvironment _environment;
    private MikaDomainSilhouette _silhouette;
    private MikaShatter _shatter;
    private MikaDomainMusic _music;
    private Vector2 _lastAlivePosition;
    private int _openingSerial=-1, _localOpening=-1, _lastOpeningTick=-1;
    private bool _openingParticipant;
    private bool _captureAttempted;

    public override void Load()
    {
        _environment=new MikaDomainEnvironment();
        _silhouette=new MikaDomainSilhouette();
        _music=new MikaDomainMusic();
        On_Main.DoDraw+=DrawDomainFrame;
        IL_Main.DoDraw+=InsertDomainDraws;
    }
    public override void PostSetupContent()
    {
        Main.QueueMainThreadAction(()=>
        {
            if(Main.dedServ)return;
            _domain??=new MikaDomainRenderer();
            _domain.Preload();
            var device=Main.graphics.GraphicsDevice;
            _shatter??=new MikaShatter(device);
            var present=device.PresentationParameters;
            _shatter.SetSeed(0);
            _shatter.PrepareCapture(present.BackBufferWidth,present.BackBufferHeight);
            _shatter.Prepare(present.BackBufferWidth,present.BackBufferHeight,.05f,MikaShatter.Defaults);
            _music.Load(Mod);
            _cues=MikaResonanceRenderer.ReadAudio();
            foreach(var cue in _cues)
                ModContent.Request<Microsoft.Xna.Framework.Audio.SoundEffect>(
                    "KivotosMod/Assets/Skills/MikaDomain/Resonance/"+cue.Name,ReLogic.Content.AssetRequestMode.AsyncLoad);
        });
    }
    internal static void Toggle(){if(MikaExDomain.Active)return;if(Enabled)Reset();else Restart(false);}
    internal static void Restart(bool paint)
    {
        if(MikaExDomain.Active)return;
        Reset();Enabled=true;_fadeTicks=60;Visibility=1;PaintDemo=paint;
    }
    internal static void StartDemo(string name,bool open)
    {
        if(MikaExDomain.Active)return;
        Restart(false);CompositionDemo=name;OpenDemo=open;
    }
    internal static void TryPreviewCrystal()
    {
        if(_previewResonance || Ticks<_previewCrystalReady)return;
        PreviewResonance();
        _previewCrystalReady=Ticks+MikaExState.ResonanceDuration;
    }
    internal static void PreviewResonance()
    {
        if(MikaExDomain.Active)return;
        if(!Enabled || PaintDemo || CompositionDemo!=null)Restart(false);
        _previewResonance=true;ResonanceTick=0;_lastAudioTick=-1;
        StopAudio();
        _shakeSeed=(uint)Main.rand.Next();
        MikaDomainStars.Scatter(restart:true);
    }
    private static void Reset()
    {
        Enabled=PaintDemo=_previewResonance=OpenDemo=false;
        CompositionDemo=null;Ticks=_fadeTicks=0;Visibility=0;
        ResonanceTick=_lastAudioTick=_domainSerial=_castStart=-1;
        _previewCrystalReady=0;
        StopAudio();MikaDomainStars.Reset();
    }
    private static void StopAudio()
    {
        for(int i=0;i<_audio.Length;i++)
        {
            if(_audio[i].IsValid && SoundEngine.TryGetActiveSound(_audio[i],out var sound))sound.Stop();
            _audio[i]=ReLogic.Utilities.SlotId.Invalid;
        }
    }
    public override void OnWorldLoad()=>ResetWorldVisuals();
    public override void OnWorldUnload()=>ResetWorldVisuals();
    private void ResetWorldVisuals()
    {
        Reset();_openingSerial=_localOpening=_lastOpeningTick=-1;
        _openingParticipant=false;_lastAlivePosition=Main.LocalPlayer.Center;
        _music?.Reset();_shatter?.ClearCapture();
    }

    public override void PostUpdateEverything()
    {
        if(Main.gameMenu || Main.gamePaused)return;
        Player viewer=Main.LocalPlayer;
        if(!viewer.dead && !viewer.ghost)_lastAlivePosition=viewer.Center;
        else if(viewer.lastDeathPostion!=Vector2.Zero)_lastAlivePosition=viewer.lastDeathPostion;
        UpdateOpening(viewer);
        bool inVisualRange=MikaExDomain.VisualRange(viewer,_lastAlivePosition);
        bool useTrack=inVisualRange && (MikaExDomain.OpeningTick<0 || MikaExDomain.OpeningTick>=244);
        _music.Tick(_localOpening,useTrack);
        if(MikaExDomain.Active)
        {
            var state=MikaExDomain.State;
            if(_domainSerial!=MikaExDomain.Serial)
            {
                _domainSerial=MikaExDomain.Serial;
                PaintDemo=OpenDemo=_previewResonance=false;
                CompositionDemo=null;
                _fadeTicks=MikaExDomain.Owner==Main.myPlayer || _openingParticipant && _localOpening>=240?60:0;
                _castStart=-1;StopAudio();
            }
            Ticks=state.DomainTime;
            ResonanceTick=state.ResonanceTime;
            bool inRange=inVisualRange;
            _fadeTicks=Math.Clamp(_fadeTicks+(inRange?1:-1),0,60);
            Visibility=_fadeTicks/60f;
            Enabled=Visibility>0;
            if(_castStart!=state.ResonanceStart)
            {
                _castStart=state.ResonanceStart;
                _shakeSeed=(uint)MikaExDomain.ResonanceSeed;
                // New casts start together. A joining client skips cues already elapsed.
                _lastAudioTick=ResonanceTick<=8?-1:ResonanceTick-1;
                StopAudio();
            }
            UpdateAudio(inRange?Visibility:0);
            MikaDomainStars.Update();
            return;
        }
        if(_domainSerial>=0){Reset();return;}
        if(!Enabled)return;
        Ticks++;
        if(_previewResonance && ++ResonanceTick>=MikaExState.ResonanceDuration)
        {
            _previewResonance=false;ResonanceTick=_lastAudioTick=-1;
        }
        UpdateAudio(1);
        if(!PaintDemo && CompositionDemo==null)MikaDomainStars.Update();
    }

    private void UpdateOpening(Player viewer)
    {
        int tick=MikaExDomain.OpeningTick;
        if(tick<0 || tick>=MikaExState.OpeningEnd)
        {
            if(_localOpening>=0)_shatter?.ClearCapture();
            _localOpening=-1;
            return;
        }
        if(_openingSerial!=MikaExDomain.Serial)
        {
            _openingSerial=MikaExDomain.Serial;
            _openingParticipant=MikaExDomain.Owner==viewer.whoAmI ||
                MikaExDomain.Within(MikaExDomain.OpeningCenter,_lastAlivePosition,MikaExDomain.Radius);
            _lastOpeningTick=-1;_captureAttempted=false;
            _shatter?.ClearCapture();_shatter?.SetSeed(_openingSerial);
            if(!MikaExDomain.Active)
            {
                Enabled=PaintDemo=OpenDemo=_previewResonance=false;
                CompositionDemo=null;Visibility=0;ResonanceTick=-1;StopAudio();
            }
        }
        if(!_openingParticipant)return;
        _localOpening=tick;
        foreach(var cue in _cues)
            if(cue.Opening && _lastOpeningTick<cue.Tick && tick>=cue.Tick && tick-cue.Tick<=10)
                SoundEngine.PlaySound(new SoundStyle("KivotosMod/Assets/Skills/MikaDomain/Resonance/"+cue.Name));
        _lastOpeningTick=Math.Max(_lastOpeningTick,tick);
    }

    private static void UpdateAudio(float volume)
    {
        if(ResonanceTick<0)return;
        if(_audio.Length!=_cues.Length)
        {
            _audio=new ReLogic.Utilities.SlotId[_cues.Length];
            Array.Fill(_audio,ReLogic.Utilities.SlotId.Invalid);
        }
        for(int i=0;i<_cues.Length;i++)
        {
            if(!_cues[i].Opening && _lastAudioTick<_cues[i].Tick && ResonanceTick>=_cues[i].Tick)
                _audio[i]=SoundEngine.PlaySound(new SoundStyle("KivotosMod/Assets/Skills/MikaDomain/Resonance/"+_cues[i].Name),
                    updateCallback:sound=>{sound.Volume=MikaExDomain.Active?Visibility:1;return true;});
            if(_audio[i].IsValid && SoundEngine.TryGetActiveSound(_audio[i],out var active))
                active.Volume=volume;
        }
        _lastAudioTick=Math.Max(_lastAudioTick,ResonanceTick);
    }

    internal static Vector2 Shake(int tick)
    {
        const int start=160, peak=216, end=320;
        if(tick<start || tick>=end || MikaDomainTuning.ShakeFrequency==0)return Vector2.Zero;
        int step=(tick-start)*MikaDomainTuning.ShakeFrequency/60;
        uint seed=unchecked(_shakeSeed+(uint)(step+1)*0x9E3779B9u);
        seed^=seed>>16;seed=unchecked(seed*0x85EBCA6Bu);seed^=seed>>13;
        float angle=seed/4294967296f*MathHelper.TwoPi;
        float ramp=tick<peak?(tick-start)/(float)(peak-start):(end-tick)/(float)(end-peak);
        return new Vector2(MathF.Cos(angle),MathF.Sin(angle))*(MikaDomainTuning.ShakeAmplitude*ramp);
    }
    internal static float Silhouette(int tick)
    {
        // The reference animation runs at 30 fps, with two game ticks per frame.
        const int start=174, black=190, fade=252, end=314;
        if(tick<start || tick>=end)return 0;
        if(tick<black)return (tick-start)/(float)(black-start);
        return tick<fade?1:(end-tick)/(float)(end-fade);
    }
    public override void ModifyScreenPosition()
    {
        if(Enabled && !Main.gameMenu)
            Main.screenPosition-=Shake(ResonanceTick)*Visibility*(KivotosClientConfig.Instance?.ScreenShakeStrength??1);
    }

    private void DrawDomainFrame(On_Main.orig_DoDraw orig,Main self,GameTime time)
    {
        if(Main.gameMenu || (!Enabled && _localOpening<0)){orig(self,time);return;}
        _domain??=new MikaDomainRenderer();
        var d=Main.graphics.GraphicsDevice;
        var present=d.PresentationParameters;
        int width=present.BackBufferWidth,height=present.BackBufferHeight;
        bool drawDomain=Enabled && !Main.mapFullscreen;
        if(drawDomain)_domain.Prepare();
        if(_localOpening is >=0 and <MikaExState.OpeningDuration)
            _shatter?.PrepareCapture(width,height);
        bool shards=_localOpening>=MikaExState.OpeningDuration && !Main.mapFullscreen;
        if(shards)_shatter?.Prepare(width,height,(_localOpening-MikaExState.OpeningDuration)/60f,MikaShatterTuning.Current);
        var overlay=_localOpening is >=0 and <252 ? _domain.PrepareOpening(_localOpening):null;
        if(drawDomain)
        {
            try { _environment.Draw(orig,self,time,Visibility); }
            finally { _silhouette.End(); }
        }
        else orig(self,time);
        if(!_captureAttempted && _localOpening>=MikaExState.OpeningCapture && _localOpening<MikaExState.OpeningDuration)
        {
            _captureAttempted=true;
            try{_shatter?.Capture();}
            catch(InvalidOperationException error){Mod.Logger.Warn("Mika shatter frame capture failed",error);}
        }
        if(overlay!=null)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                DepthStencilState.None,RasterizerState.CullNone);
            float scale=Math.Max(width/(float)overlay.Width,height/(float)overlay.Height);
            Main.spriteBatch.Draw(overlay,new Vector2(width/2f,height/2f),null,Color.White,0,overlay.Size()/2,
                scale,SpriteEffects.None,0);
            Main.spriteBatch.End();
        }
    }
    private void InsertDomainDraws(ILContext il)
    {
        var c=new ILCursor(il);
        if(!c.TryGotoNext(MoveType.AfterLabel,i=>i.MatchLdsfld<Main>("spriteBatch"),
                i=>i.MatchCall(typeof(ScreenDarkness),nameof(ScreenDarkness.DrawBack))))
            throw new InvalidOperationException("Mika domain: background draw hook not found");
        c.EmitDelegate<Action>(DrawComposite);
        if(!c.TryGotoNext(MoveType.AfterLabel,i=>i.MatchLdarg(0),i=>i.MatchCall<Main>("DoDraw_WallsTilesNPCs")))
            throw new InvalidOperationException("Mika domain: silhouette start hook not found");
        c.EmitDelegate<Action>(BeginSilhouette);
        // Leave the vanilla shimmer pass and the interface outside the silhouette.
        if(!c.TryGotoNext(MoveType.AfterLabel,i=>i.MatchLdsfld<Main>("shimmerAlpha")))
            throw new InvalidOperationException("Mika domain: silhouette end hook not found");
        c.EmitDelegate<Action>(_silhouette.End);
        if(!c.TryGotoNext(MoveType.AfterLabel,i=>i.MatchLdsfld<Main>("spriteBatch"),
                i=>i.MatchCall(typeof(ScreenDarkness),nameof(ScreenDarkness.DrawFront))))
            throw new InvalidOperationException("Mika domain: world lighting end hook not found");
        c.EmitDelegate<Action>(_environment.RestoreWorldLighting);
    }
    private void BeginSilhouette()
    {
        if(_environment.Drawing)
            _silhouette.Begin(Silhouette(ResonanceTick)*Visibility);
    }
    private void DrawComposite()
    {
        if(Main.gameMenu || Main.mapFullscreen || _domain==null)return;
        var shards=_localOpening>=MikaExState.OpeningDuration?_shatter?.Composite:null;
        if(!Enabled && shards==null)return;

        // Vanilla already drew the Shimmer backdrop and stars below the skill art.
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,Main.DefaultSamplerState,
            DepthStencilState.None,RasterizerState.CullNone);
        var bounds=new Rectangle(0,0,Main.screenWidth,Main.screenHeight);
        Vector2 center=new(Main.screenWidth/2f,Main.screenHeight/2f);
        if(Enabled)
        {
            Main.spriteBatch.Draw(_domain.Composite,center,null,Color.White*Visibility,0,_domain.Composite.Size()/2,
                ScreenScale,SpriteEffects.None,0);
            _environment.ApplyWorldLighting();
        }
        if(shards!=null)Main.spriteBatch.Draw(shards,bounds,Color.White);
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,Main.DefaultSamplerState,
            DepthStencilState.None,Main.Rasterizer,null,Main.Transform);
    }
    public override void PostDrawTiles()
    {
        if(!Enabled || Main.gameMenu || PaintDemo || CompositionDemo!=null)return;
        Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullNone,null,Main.GameViewMatrix.TransformationMatrix);
        MikaDomainStars.DrawSparks();Main.spriteBatch.End();
    }
    public override void Unload()
    {
        On_Main.DoDraw-=DrawDomainFrame;IL_Main.DoDraw-=InsertDomainDraws;Reset();
        _silhouette?.Dispose();
        _environment?.Dispose();
        _music?.Dispose();
        _cues=Array.Empty<MikaResonanceRenderer.AudioCue>();
        var renderer=_domain;var shatter=_shatter;_domain=null;_shatter=null;
        Main.QueueMainThreadAction(()=>{renderer?.Dispose();shatter?.Dispose();});
    }
}
