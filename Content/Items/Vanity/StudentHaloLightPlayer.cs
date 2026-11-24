using KivotosMod.Content.Items.Vanity.Nagisa;
using KivotosMod.Content.Items.Vanity.Seia;
using KivotosMod.Content.Items.Vanity.Haruka;
using KivotosMod.Content.Items.Vanity.Kayoko;
using KivotosMod.Content.Items.Vanity.Mutsuki;
using KivotosMod.Content.Items.Vanity.Ibuki;
using KivotosMod.Content.Items.Vanity.Iroha;
using KivotosMod.Content.Items.Vanity.Hikari;
using KivotosMod.Content.Items.Vanity.Nozomi;
using KivotosMod.Content.Items.Vanity.Mika;
using KivotosMod.Content.Items.Vanity.Yuuka;
using KivotosMod.Content.Items.Vanity.Noa;
using KivotosMod.Content.Tablet;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity;

public sealed class StudentHaloLightPlayer : ModPlayer
{
    public override void PostUpdate()
    {
        // Update once per player, never from a draw pass (portraits/shadows can draw repeatedly).
        if (Main.dedServ || Main.gameMenu || !Player.active || Player.dead || Player.ghost || Player.invis)
            return;
        Vector3 color;
        var appearance = Player.GetModPlayer<TabletStudentAppearancePlayer>();
        // Formation visual slots can be assigned after this update hook. Resolve the
        // selected student directly instead of depending on last frame's head slot.
        bool Visible(TabletStudentId id, bool equippedHead) => appearance.HasSelection
            ? StudentSpriteVariants.Canonical(appearance.SelectedStudent) == id : equippedHead;
        if (Visible(TabletStudentId.Mika, MikaVanitySet.HeadVisible(Player))) color = new(1f, .64f, .80f);
        else if (Visible(TabletStudentId.Yuuka, YuukaVanitySet.HeadVisible(Player))) color = new(.22f, .68f, 1f);
        else if (Visible(TabletStudentId.Noa, NoaVanitySet.HeadVisible(Player))) color = new(.42f, .9f, 1f);
        else if (Visible(TabletStudentId.Haruka, HarukaVanitySet.HeadVisible(Player))) color = new(.85f, .45f, 1f);
        else if (Visible(TabletStudentId.Kayoko, KayokoVanitySet.HeadVisible(Player))) color = new(1f, .92f, .72f);
        else if (Visible(TabletStudentId.Mutsuki, MutsukiVanitySet.HeadVisible(Player))) color = new(1f, .28f, .22f);
        else if (Visible(TabletStudentId.Ibuki, IbukiVanitySet.HeadVisible(Player))) color = new(1f, .8f, .28f);
        else if (Visible(TabletStudentId.Iroha, IrohaVanitySet.HeadVisible(Player))) color = new(.56f, .48f, 1f);
        else if (Visible(TabletStudentId.Hikari, HikariVanitySet.HeadVisible(Player)) || Visible(TabletStudentId.Nozomi, NozomiVanitySet.HeadVisible(Player)))
            color = new(.95f, 1f, .48f);
        else if (Visible(TabletStudentId.Nagisa, NagisaVanitySet.HeadVisible(Player))) color = new(.9f, .83f, .56f);
        else if (Visible(TabletStudentId.Seia, SeiaVanitySet.HeadVisible(Player))) color = new(.9f, .78f, .56f);
        else return;
        Vector2 position = Player.Center + new Vector2(0, -(Player.height / 2f + 6f) * Player.gravDir);
        Lighting.AddLight(position + Player.headPosition, color * .55f);
    }
}
