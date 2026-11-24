using Terraria;
using Terraria.ModLoader;
using KivotosMod.Content.Tablet;
using KivotosMod.Content.Items.Vanity.Mika;
using KivotosMod.Content.Items.Vanity.Yuuka;
using KivotosMod.Content.Items.Vanity.Ibuki;
using KivotosMod.Content.Items.Vanity.Iroha;
using KivotosMod.Content.Items.Vanity.Hikari;
namespace KivotosMod.Content.Items.Vanity;
internal static class StudentSpriteVariants
{
    internal static bool Mika2x2(Player p) => p.head == ModContent.GetInstance<Mika2x2Head>().Item.headSlot;
    internal static bool MikaBody2x2(Player p) => p.body == ModContent.GetInstance<Mika2x2Body>().Item.bodySlot;
    internal static bool Yuuka2x2(Player p) => p.head == ModContent.GetInstance<Yuuka2x2Head>().Item.headSlot;
    internal static bool YuukaBody2x2(Player p) => p.body == ModContent.GetInstance<Yuuka2x2Body>().Item.bodySlot;
    internal static bool Ibuki2x2(Player p) => p.head == ModContent.GetInstance<Ibuki2x2Head>().Item.headSlot;
    internal static bool IbukiBody2x2(Player p) => p.body == ModContent.GetInstance<Ibuki2x2Body>().Item.bodySlot;
    internal static bool Iroha2x2(Player p) => p.head == ModContent.GetInstance<Iroha2x2Head>().Item.headSlot;
    internal static bool IrohaBody2x2(Player p) => p.body == ModContent.GetInstance<Iroha2x2Body>().Item.bodySlot;
    internal static bool Hikari2x2(Player p) => p.head == ModContent.GetInstance<Hikari2x2Head>().Item.headSlot;
    internal static bool HikariBody2x2(Player p) => p.body == ModContent.GetInstance<Hikari2x2Body>().Item.bodySlot;
    internal static TabletStudentId Canonical(TabletStudentId id) => id switch {
        TabletStudentId.Mika2x2 => TabletStudentId.Mika,
        TabletStudentId.Yuuka2x2 => TabletStudentId.Yuuka,
        TabletStudentId.Ibuki2x2 => TabletStudentId.Ibuki,
        TabletStudentId.Iroha2x2 => TabletStudentId.Iroha,
        TabletStudentId.Hikari2x2 => TabletStudentId.Hikari,
        _ => id,
    };
    internal static TabletStudentId OtherOutfit(TabletStudentId id) => id switch {
        TabletStudentId.Megu2x2 => TabletStudentId.MeguSauna2x2,
        TabletStudentId.MeguSauna2x2 => TabletStudentId.Megu2x2,
        TabletStudentId.MeguSaunaSmile2x2 => TabletStudentId.Megu2x2,
        TabletStudentId.Mutsuki => TabletStudentId.Mutsuki2x2, TabletStudentId.Mutsuki2x2 => TabletStudentId.Mutsuki,
        TabletStudentId.Haruka => TabletStudentId.Haruka2x2, TabletStudentId.Haruka2x2 => TabletStudentId.Haruka,
        TabletStudentId.Kayoko => TabletStudentId.Kayoko2x2, TabletStudentId.Kayoko2x2 => TabletStudentId.Kayoko,
        TabletStudentId.Hina => TabletStudentId.Hina2x2, TabletStudentId.Hina2x2 => TabletStudentId.Hina,
        TabletStudentId.Shiroko => TabletStudentId.Shiroko2x2, TabletStudentId.Shiroko2x2 => TabletStudentId.Shiroko,
        TabletStudentId.Nozomi => TabletStudentId.Nozomi2x2, TabletStudentId.Nozomi2x2 => TabletStudentId.Nozomi,
        TabletStudentId.Hoshino => TabletStudentId.Hoshino2x2, TabletStudentId.Hoshino2x2 => TabletStudentId.Hoshino,
        TabletStudentId.Noa => TabletStudentId.Noa2x2, TabletStudentId.Noa2x2 => TabletStudentId.Noa,

        TabletStudentId.Mika => TabletStudentId.Mika2x2, TabletStudentId.Mika2x2 => TabletStudentId.Mika,
        TabletStudentId.Yuuka => TabletStudentId.Yuuka2x2, TabletStudentId.Yuuka2x2 => TabletStudentId.Yuuka,
        TabletStudentId.Ibuki => TabletStudentId.Ibuki2x2, TabletStudentId.Ibuki2x2 => TabletStudentId.Ibuki,
        TabletStudentId.Iroha => TabletStudentId.Iroha2x2, TabletStudentId.Iroha2x2 => TabletStudentId.Iroha,
        TabletStudentId.Hikari => TabletStudentId.Hikari2x2, TabletStudentId.Hikari2x2 => TabletStudentId.Hikari,
        _ => id,
    };
}
