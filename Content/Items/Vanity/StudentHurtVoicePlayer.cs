using KivotosMod.Content.Tablet;
using KivotosMod.Content.Items.Vanity.Yuuka;
using KivotosMod.Content.Items.Vanity.Noa;
using KivotosMod.Content.Items.Vanity.Hina;
using KivotosMod.Content.Items.Vanity.Nagisa;
using KivotosMod.Content.Items.Vanity.Seia;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity;

public sealed class StudentHurtVoicePlayer : ModPlayer
{
    internal const int VoiceIntervalTicks = 45;
    private int _cooldown;
    private static readonly SoundStyle Voice = new("KivotosMod/Assets/Sounds/Students/Yuuka/Hurt")
    {
        MaxInstances = 1,
        SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
        PlayOnlyIfFocused = true,
    };

    internal static bool Supported(TabletStudentId id) => id is TabletStudentId.Yuuka or TabletStudentId.Noa
        or TabletStudentId.Hina or TabletStudentId.Nagisa or TabletStudentId.Seia;

    private bool Wearing<Head, Body, Boots>() where Head : ModItem where Body : ModItem where Boots : ModItem
    {
        int VisibleType(int index) => Player.armor[index + 10].IsAir ? Player.armor[index].type : Player.armor[index + 10].type;
        return VisibleType(0) == ModContent.ItemType<Head>() && VisibleType(1) == ModContent.ItemType<Body>()
            && VisibleType(2) == ModContent.ItemType<Boots>();
    }

    internal TabletStudentId? Speaker()
    {
        var appearance = Player.GetModPlayer<TabletStudentAppearancePlayer>();
        if (appearance.HasSelection) { var id = StudentSpriteVariants.Canonical(appearance.SelectedStudent); return Supported(id) ? id : null; }
        if (Wearing<YuukaHead, YuukaBody, YuukaBoots>() || Wearing<Yuuka2x2Head, Yuuka2x2Body, Yuuka2x2Boots>()) return TabletStudentId.Yuuka;
        if (Wearing<NoaHead, NoaBody, NoaBoots>()) return TabletStudentId.Noa;
        if (Wearing<HinaHead, HinaBody, HinaBoots>()) return TabletStudentId.Hina;
        if (Wearing<NagisaHead, NagisaBody, NagisaBoots>()) return TabletStudentId.Nagisa;
        if (Wearing<SeiaHead, SeiaBody, SeiaBoots>()) return TabletStudentId.Seia;
        return null;
    }

    // Loudest measured clip is reserved for larger hits; every supplied clip is retained.
    internal static int[] Variants(TabletStudentId student, bool heavy) => (student, heavy) switch
    {
        (TabletStudentId.Noa, true) => new[] { 1 },
        (TabletStudentId.Noa, false) => new[] { 2, 4 },
        (TabletStudentId.Hina, true) => new[] { 1 },
        (TabletStudentId.Hina, false) => new[] { 3, 4, 7 },
        (TabletStudentId.Seia, true) => new[] { 3 },
        (TabletStudentId.Seia, false) => new[] { 1, 4 },
        (TabletStudentId.Yuuka, true) => new[] { 1 },
        (TabletStudentId.Yuuka, false) => new[] { 3, 4, 5 },
        (TabletStudentId.Nagisa, true) => new[] { 4 },
        (TabletStudentId.Nagisa, false) => new[] { 1, 2, 3, 5, 6 },
        _ => System.Array.Empty<int>(),
    };

    internal static bool ValidVoice(TabletStudentId student, int variant)
        => student == TabletStudentId.Mika ? variant >= 1 && variant <= 4
            : Supported(student) && (System.Array.IndexOf(Variants(student, false), variant) >= 0
                || System.Array.IndexOf(Variants(student, true), variant) >= 0);

    internal static void PlayRemote(Player player, TabletStudentId student, int variant)
    {
        SoundEngine.PlaySound(Voice with { SoundPath = $"KivotosMod/Assets/Sounds/Students/{student}/Hurt",
            Variants = new[] { variant } }, player.Center);
    }

    public override void PostUpdate() { if (_cooldown > 0) _cooldown--; }
    public override void UpdateDead() { if (_cooldown > 0) _cooldown--; }
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (!Main.dedServ && Player.whoAmI == Main.myPlayer && Speaker().HasValue)
            modifiers.DisableSound();
    }
    public override void OnHurt(Player.HurtInfo info)
    {
        if (Main.dedServ || Player.whoAmI != Main.myPlayer || info.Damage <= 0 || _cooldown > 0) return;
        TabletStudentId? student = Speaker();
        if (!student.HasValue) return;
        bool heavy = info.Damage >= System.Math.Max(40, (int)System.Math.Ceiling(Player.statLifeMax2 * .15));
        int[] pool = Variants(student.Value, heavy);
        int variant = pool[Main.rand.Next(pool.Length)];
        SoundEngine.PlaySound(Voice with { SoundPath = $"KivotosMod/Assets/Sounds/Students/{student.Value}/Hurt",
            Variants = new[] { variant } }, Player.Center);
        if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient) TabletNet.SendHurtVoice(Player.whoAmI, student.Value, variant);
        _cooldown = VoiceIntervalTicks;
    }
}
