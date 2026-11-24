using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Mika;

public sealed class MikaPlayer : ModPlayer
{
    internal const int VoiceIntervalTicks = 45;
    internal const int FlightFrameTicks = 4;
    internal const int FlightFrames = 5;
    private static readonly int[] NormalVoices = { 1, 3 };
    private static readonly int[] HeavyVoices = { 2, 4 };
    private static readonly SoundStyle HurtVoice = new("KivotosMod/Assets/Sounds/Students/Mika/Hurt")
    {
        MaxInstances = 1,
        SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
        PlayOnlyIfFocused = true,
    };
    internal bool HasFlightWings => Player.wingsLogic > 0 && Player.wingTimeMax > 0;
    internal int FlightFrame { get; private set; }
    internal bool Flapping { get; private set; }
    private int _flightTimer;
    private int _voiceCooldown;

    internal static bool ShouldFlap(bool equipped, bool airborne, bool jumpingOrRising, bool mounted)
        => equipped && airborne && jumpingOrRising && !mounted;
    internal static bool IsHeavyHit(int damage, int maxLife) => damage >= System.Math.Max(40, (int)System.Math.Ceiling(maxLife * .15));

    public override void PostUpdate()
    {
        if (_voiceCooldown > 0) _voiceCooldown--;
        // Use the equipped wings' existing flight state/stats; Mika only
        // replaces their appearance, just like Hina's integrated wing layer.
        Flapping = ShouldFlap(HasFlightWings && !StudentSpriteDraw.SpecialFlightBoard(Player)
            && Player.wingsLogic is not (22 or 28 or 45) && Player.wingTime > 0 && MikaVanitySet.IsMika(Player), Player.velocity.Y != 0,
            Player.controlJump || Player.velocity.Y * Player.gravDir < 0, Player.mount.Active);
        if (Flapping)
        {
            _flightTimer = (_flightTimer + 1) % (FlightFrames * FlightFrameTicks);
            FlightFrame = _flightTimer / FlightFrameTicks;
        }
        else { _flightTimer = 0; FlightFrame = 0; }
    }
    public override void UpdateDead()
    {
        Flapping = false;
        _flightTimer = FlightFrame = 0;
        if (_voiceCooldown > 0) _voiceCooldown--;
    }
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        // Mika never falls back to the vanilla grunt, including while the
        // voice is cooling down. The cooldown only gates the custom voice.
        if (!Main.dedServ && Player.whoAmI == Main.myPlayer && MikaVanitySet.IsMika(Player))
            modifiers.DisableSound();
    }
    public override void OnHurt(Player.HurtInfo info)
    {
        if (Main.dedServ || Player.whoAmI != Main.myPlayer || info.Damage <= 0
            || _voiceCooldown > 0 || !MikaVanitySet.IsMika(Player)) return;
        int[] pool = IsHeavyHit(info.Damage, Player.statLifeMax2) ? HeavyVoices : NormalVoices;
        int variant = pool[Main.rand.Next(pool.Length)];
        SoundEngine.PlaySound(HurtVoice with { Variants = new[] { variant } }, Player.Center);
        if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
            global::KivotosMod.Content.Tablet.TabletNet.SendHurtVoice(Player.whoAmI, global::KivotosMod.Content.Tablet.TabletStudentId.Mika, variant);
        _voiceCooldown = VoiceIntervalTicks;
    }

    public override void HideDrawLayers(PlayerDrawSet info)
    {
        if (!MikaVanitySet.FullSetVisible(info.drawPlayer)) return;
        PlayerDrawLayers.HairBack.Hide();
        PlayerDrawLayers.BackAcc.Hide();
        PlayerDrawLayers.Backpacks.Hide();
        PlayerDrawLayers.BalloonAcc.Hide();
        PlayerDrawLayers.FaceAcc.Hide();
        PlayerDrawLayers.FrontAccBack.Hide();
        PlayerDrawLayers.FrontAccFront.Hide();
        PlayerDrawLayers.HandOnAcc.Hide();
        PlayerDrawLayers.OffhandAcc.Hide();
        PlayerDrawLayers.NeckAcc.Hide();
        PlayerDrawLayers.Shield.Hide();
        PlayerDrawLayers.SolarShield.Hide();
        PlayerDrawLayers.Tails.Hide();
        PlayerDrawLayers.WaistAcc.Hide();
        if (!StudentSpriteDraw.SpecialFlightBoard(info.drawPlayer))
            PlayerDrawLayers.Wings.Hide();
        PlayerDrawLayers.JimsCloak.Hide();
        PlayerDrawLayers.LeinforsHairShampoo.Hide();
        PlayerDrawLayers.Magiluminescence.Hide();
        PlayerDrawLayers.SkinLongCoat.Hide();
        PlayerDrawLayers.ArmorLongCoat.Hide();
    }
}
