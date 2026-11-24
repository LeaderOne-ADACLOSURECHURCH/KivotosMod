using KivotosMod.Content.Items.Vanity.NpcSpritePack;
using KivotosMod.Content.Items.Vanity;
using KivotosMod.Content.Items.Vanity.Hina;
using KivotosMod.Content.Items.Vanity.Noa;
using KivotosMod.Content.Items.Vanity.Hoshino;
using KivotosMod.Content.Items.Vanity.Ibuki;
using KivotosMod.Content.Items.Vanity.Iroha;
using KivotosMod.Content.Items.Vanity.Hikari;
using KivotosMod.Content.Items.Vanity.Mika;
using KivotosMod.Content.Items.Vanity.Haruka;
using KivotosMod.Content.Items.Vanity.Kayoko;
using KivotosMod.Content.Items.Vanity.Mutsuki;
using KivotosMod.Content.Items.Vanity.Nozomi;
using KivotosMod.Content.Items.Vanity.Nagisa;
using KivotosMod.Content.Items.Vanity.Seia;
using KivotosMod.Content.Items.Vanity.Yuuka;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace KivotosMod.Content.Tablet;

/// <summary>
/// Player-only Formation selection state.  No student NPC is registered or spawned.
/// The selected student survives closing the tablet and leaving the Formation page.
/// Yuuka, Hina and Noa provide a player-format costume. Other selections
/// keep the player's ordinary visual equipment while remaining selected in the tablet UI.
/// </summary>
public sealed class TabletStudentAppearancePlayer : ModPlayer
{
    public TabletStudentId SelectedStudent { get; private set; } = TabletStudentId.Yuuka;
    public bool HasSelection { get; private set; }

    public bool IsYuukaAppearance => HasSelection && StudentSpriteVariants.Canonical(SelectedStudent) == TabletStudentId.Yuuka;
    public bool IsHinaAppearance => HasSelection && SelectedStudent == TabletStudentId.Hina;
    public bool IsNoaAppearance => HasSelection && SelectedStudent == TabletStudentId.Noa;
    public bool IsHoshinoAppearance => HasSelection && SelectedStudent == TabletStudentId.Hoshino;
    public bool IsIbukiAppearance => HasSelection && StudentSpriteVariants.Canonical(SelectedStudent) == TabletStudentId.Ibuki;
    public bool IsIrohaAppearance => HasSelection && StudentSpriteVariants.Canonical(SelectedStudent) == TabletStudentId.Iroha;
    public bool IsHikariAppearance => HasSelection && StudentSpriteVariants.Canonical(SelectedStudent) == TabletStudentId.Hikari;
    public bool IsMikaAppearance => HasSelection && StudentSpriteVariants.Canonical(SelectedStudent) == TabletStudentId.Mika;
    public bool IsHarukaAppearance => HasSelection && SelectedStudent == TabletStudentId.Haruka;
    public bool IsKayokoAppearance => HasSelection && SelectedStudent == TabletStudentId.Kayoko;
    public bool IsMutsukiAppearance => HasSelection && SelectedStudent == TabletStudentId.Mutsuki;
    public bool IsNozomiAppearance => HasSelection && SelectedStudent == TabletStudentId.Nozomi;
    public bool IsNagisaAppearance => HasSelection && SelectedStudent == TabletStudentId.Nagisa;
    public bool IsSeiaAppearance => HasSelection && SelectedStudent == TabletStudentId.Seia;


    // Elaina reference: use the standard ModPlayer rendering lifecycle. Keeping the
    // appearance active from BOTH FrameEffects and DrawEffects avoids renderer-specific
    // detours and helps with auto-pause, inventory display and player previews.
    // Do not install global LegacyPlayerRenderer hooks for costume selection.

    // ElainaModplayer's actual PreKill IL sets genGore=false and then returns to
    // the vanilla death routine. Its DrawEffects controls head/body/leg velocities
    // and death pose; it does not modify player.position or clear fragment positions.
    // Do not hook KillMe or touch headPosition/bodyPosition/legPosition here.
    private void HoldDeathVisuals(Player player)
    {
        if (!UsesStudentOutfit() || !player.dead)
            return;

        // Stop the detached visual body parts falling away from the death location.
        // This changes rendering state, NOT Player.position/velocity or world physics.
        player.headVelocity = Vector2.Zero;
        player.bodyVelocity = Vector2.Zero;
        player.legVelocity = Vector2.Zero;
    }

    public override void UpdateDead()
    {
        // Alive updates stop at death. Preserve the visual slots during the death
        // countdown, so the appearance does not turn back into the vanilla character.
        ApplyStudentVisualSlots(Player);
        HoldDeathVisuals(Player);
    }

    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g,
        ref float b, ref float a, ref bool fullBright)
    {
        Player target = drawInfo.drawPlayer;
        if (target == null)
            return;

        // This is the missing half of the Elaina pattern: restore the current
        // student's head/body/legs whenever Terraria builds a draw pass, even if
        // FrameEffects was not updated because the world is auto-paused.
        // Assign VISUAL equip slots only, never armor/vanity inventory contents.
        ApplyStudentVisualSlots(target);

        if (!target.dead || !UsesStudentOutfit())
            return;

        HoldDeathVisuals(target);

        // Match Elaina's death frame selection: final head/body animation frame and
        // second leg frame. Player-format sheets have 20 rows of 56 pixels.
        // Avoid modifying frame.X, since Kivotos' custom sheets are only 40 px wide.
        if (target.headFrame.Height > 0)
            target.headFrame.Y = 19 * target.headFrame.Height;
        if (target.bodyFrame.Height > 0)
            target.bodyFrame.Y = 19 * target.bodyFrame.Height;
        if (target.legFrame.Height > 0)
            target.legFrame.Y = target.legFrame.Height;
    }

    internal bool UsesStudentOutfit() => (HasSelection && (byte)SelectedStudent >= 23) || IsYuukaAppearance || IsHinaAppearance || IsNoaAppearance
        || YuukaVanitySet.FullSetVisible(Player) || HinaVanitySet.FullSetVisible(Player)
        || NoaVanitySet.FullSetVisible(Player)
        || IsHoshinoAppearance || HoshinoVanitySet.FullSetVisible(Player)
        || IsIbukiAppearance || IbukiVanitySet.FullSetVisible(Player)
        || IsIrohaAppearance || IrohaVanitySet.FullSetVisible(Player)
        || IsHikariAppearance || HikariVanitySet.FullSetVisible(Player)
        || IsMikaAppearance || MikaVanitySet.FullSetVisible(Player)
        || IsHarukaAppearance || HarukaVanitySet.FullSetVisible(Player)
        || IsKayokoAppearance || KayokoVanitySet.FullSetVisible(Player)
        || IsMutsukiAppearance || MutsukiVanitySet.FullSetVisible(Player)
        || IsNozomiAppearance || NozomiVanitySet.FullSetVisible(Player)
        || IsNagisaAppearance || NagisaVanitySet.FullSetVisible(Player)
        || IsSeiaAppearance || SeiaVanitySet.FullSetVisible(Player);

    public void Select(TabletStudentId student)
    {
        SelectedStudent = student;
        HasSelection = true;
    }

    public void ClearSelection()
    {
        HasSelection = false;
    }

    internal static bool IsFormationStudent(TabletStudentId student) => student is
        TabletStudentId.Yuuka or TabletStudentId.Hina or TabletStudentId.Noa
        or TabletStudentId.Shiroko or TabletStudentId.Hoshino
        or TabletStudentId.Ibuki or TabletStudentId.Iroha or TabletStudentId.Hikari or TabletStudentId.Mika
        or TabletStudentId.Haruka or TabletStudentId.Kayoko or TabletStudentId.Mutsuki or TabletStudentId.Nozomi
        or TabletStudentId.Nagisa or TabletStudentId.Seia
        or TabletStudentId.Mika2x2 or TabletStudentId.Yuuka2x2 or TabletStudentId.Ibuki2x2 or TabletStudentId.Iroha2x2 or TabletStudentId.Hikari2x2 or TabletStudentId.Himari2x2 or TabletStudentId.Ako2x2 or TabletStudentId.Mutsuki2x2 or TabletStudentId.GehennaStudent2x2 or TabletStudentId.Haruka2x2 or TabletStudentId.Nonomi2x2 or TabletStudentId.Serika2x2 or TabletStudentId.Koyuki2x2 or TabletStudentId.Kayoko2x2 or TabletStudentId.Aru2x2 or TabletStudentId.Megu2x2 or TabletStudentId.Hina2x2 or TabletStudentId.Shiroko2x2 or TabletStudentId.Nozomi2x2 or TabletStudentId.Hoshino2x2 or TabletStudentId.Noa2x2 or TabletStudentId.Akari2x2 or TabletStudentId.MeguSauna2x2 or TabletStudentId.MeguSaunaSmile2x2 ;

    internal void ReceiveSelection(bool hasSelection, TabletStudentId student)
    {
        HasSelection = hasSelection;
        SelectedStudent = student;
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        TabletNet.SendAppearance(Player.whoAmI, HasSelection, SelectedStudent, toWho, fromWho);
    }

    public override void CopyClientState(ModPlayer targetCopy)
    {
        TabletStudentAppearancePlayer copy = (TabletStudentAppearancePlayer)targetCopy;
        copy.HasSelection = HasSelection;
        copy.SelectedStudent = SelectedStudent;
    }

    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        TabletStudentAppearancePlayer previous = (TabletStudentAppearancePlayer)clientPlayer;
        if (HasSelection != previous.HasSelection || SelectedStudent != previous.SelectedStudent)
            SyncPlayer(-1, -1, false);
    }

    public override void SaveData(TagCompound tag)
    {
        tag[nameof(HasSelection)] = HasSelection;
        tag[nameof(SelectedStudent)] = (byte)SelectedStudent;
    }

    public override void LoadData(TagCompound tag)
    {
        HasSelection = tag.GetBool(nameof(HasSelection));
        byte raw = tag.ContainsKey(nameof(SelectedStudent)) ? tag.GetByte(nameof(SelectedStudent)) : (byte)TabletStudentId.Yuuka;
        TabletStudentId decoded = raw <= (byte)TabletStudentId.MeguSaunaSmile2x2 ? (TabletStudentId)raw : TabletStudentId.Yuuka;

        // Keep old save values stable (Arona=3, Sora=4, Plana=5) and reject
        // companion-only values in Formation. Hina is the new value 6.
        if (!IsFormationStudent(decoded))
        {
            SelectedStudent = TabletStudentId.Yuuka;
            HasSelection = false;
        }
        else
        {
            SelectedStudent = decoded;
        }
    }

    /// <summary>
    /// Set visual slots without modifying the player's armor or vanity inventory.
    /// Call during FrameEffects, UpdateAutopause and DrawEffects. This mirrors the
    /// direct visual slot assignments used by ElainaModplayer.
    /// </summary>
    private void ApplyStudentVisualSlots(Player target)
    {
        if (HasSelection) switch (SelectedStudent)
        {
            case TabletStudentId.Himari2x2:
                target.head=ModContent.GetInstance<PackHimari2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackHimari2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackHimari2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Ako2x2:
                target.head=ModContent.GetInstance<PackAko2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackAko2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackAko2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Mutsuki2x2:
                target.head=ModContent.GetInstance<PackMutsuki2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackMutsuki2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackMutsuki2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.GehennaStudent2x2:
                target.head=ModContent.GetInstance<PackGehennaStudent2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackGehennaStudent2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackGehennaStudent2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Haruka2x2:
                target.head=ModContent.GetInstance<PackHaruka2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackHaruka2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackHaruka2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Nonomi2x2:
                target.head=ModContent.GetInstance<PackNonomi2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackNonomi2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackNonomi2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Serika2x2:
                target.head=ModContent.GetInstance<PackSerika2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackSerika2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackSerika2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Koyuki2x2:
                target.head=ModContent.GetInstance<PackKoyuki2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackKoyuki2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackKoyuki2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Iroha2x2:
                target.head=ModContent.GetInstance<Iroha2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<Iroha2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<Iroha2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Kayoko2x2:
                target.head=ModContent.GetInstance<PackKayoko2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackKayoko2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackKayoko2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Aru2x2:
                target.head=ModContent.GetInstance<PackAru2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackAru2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackAru2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Megu2x2:
                target.head=ModContent.GetInstance<PackMegu2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackMegu2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackMegu2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Hina2x2:
                target.head=ModContent.GetInstance<PackHina2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackHina2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackHina2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Shiroko2x2:
                target.head=ModContent.GetInstance<PackShiroko2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackShiroko2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackShiroko2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Hikari2x2:
                target.head=ModContent.GetInstance<Hikari2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<Hikari2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<Hikari2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Nozomi2x2:
                target.head=ModContent.GetInstance<PackNozomi2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackNozomi2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackNozomi2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Hoshino2x2:
                target.head=ModContent.GetInstance<PackHoshino2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackHoshino2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackHoshino2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Noa2x2:
                target.head=ModContent.GetInstance<PackNoa2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackNoa2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackNoa2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Akari2x2:
                target.head=ModContent.GetInstance<PackAkari2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackAkari2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackAkari2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.MeguSauna2x2:
                target.head=ModContent.GetInstance<PackMeguSauna2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackMeguSauna2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackMeguSauna2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.MeguSaunaSmile2x2: // Retired save ID: redirect to clothed sauna outfit.
                target.head=ModContent.GetInstance<PackMeguSauna2x2Head>().Item.headSlot;
                target.body=ModContent.GetInstance<PackMeguSauna2x2Body>().Item.bodySlot;
                target.legs=ModContent.GetInstance<PackMeguSauna2x2Boots>().Item.legSlot;
                return;

            case TabletStudentId.Mika2x2:
                target.head = ModContent.GetInstance<Mika2x2Head>().Item.headSlot;
                target.body = ModContent.GetInstance<Mika2x2Body>().Item.bodySlot;
                target.legs = ModContent.GetInstance<Mika2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Yuuka2x2:
                target.head = ModContent.GetInstance<Yuuka2x2Head>().Item.headSlot;
                target.body = ModContent.GetInstance<Yuuka2x2Body>().Item.bodySlot;
                target.legs = ModContent.GetInstance<Yuuka2x2Boots>().Item.legSlot;
                return;
            case TabletStudentId.Ibuki2x2:
                target.head = ModContent.GetInstance<Ibuki2x2Head>().Item.headSlot;
                target.body = ModContent.GetInstance<Ibuki2x2Body>().Item.bodySlot;
                target.legs = ModContent.GetInstance<Ibuki2x2Boots>().Item.legSlot;
                return;


        }

        if (IsYuukaAppearance)
        {
            target.head = ModContent.GetInstance<YuukaHead>().Item.headSlot;
            target.body = ModContent.GetInstance<YuukaBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<YuukaBoots>().Item.legSlot;
        }
        else if (IsHinaAppearance)
        {
            target.head = ModContent.GetInstance<HinaHead>().Item.headSlot;
            target.body = ModContent.GetInstance<HinaBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<HinaBoots>().Item.legSlot;
        }
        else if (IsNoaAppearance)
        {
            target.head = ModContent.GetInstance<NoaHead>().Item.headSlot;
            target.body = ModContent.GetInstance<NoaBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<NoaBoots>().Item.legSlot;
        }
        else if (IsHoshinoAppearance)
        {
            target.head = ModContent.GetInstance<HoshinoHead>().Item.headSlot;
            target.body = ModContent.GetInstance<HoshinoBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<HoshinoBoots>().Item.legSlot;
        }
        else if (IsIbukiAppearance)
        {
            target.head = ModContent.GetInstance<IbukiHead>().Item.headSlot;
            target.body = ModContent.GetInstance<IbukiBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<IbukiBoots>().Item.legSlot;
        }
        else if (IsIrohaAppearance)
        {
            target.head = ModContent.GetInstance<IrohaHead>().Item.headSlot;
            target.body = ModContent.GetInstance<IrohaBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<IrohaBoots>().Item.legSlot;
        }
        else if (IsHikariAppearance)
        {
            target.head = ModContent.GetInstance<HikariHead>().Item.headSlot;
            target.body = ModContent.GetInstance<HikariBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<HikariBoots>().Item.legSlot;
        }
        else if (IsMikaAppearance)
        {
            target.head = ModContent.GetInstance<MikaHead>().Item.headSlot;
            target.body = ModContent.GetInstance<MikaBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<MikaBoots>().Item.legSlot;
        }
        else if (IsHarukaAppearance)
        {
            target.head = ModContent.GetInstance<HarukaHead>().Item.headSlot;
            target.body = ModContent.GetInstance<HarukaBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<HarukaBoots>().Item.legSlot;
        }
        else if (IsKayokoAppearance)
        {
            target.head = ModContent.GetInstance<KayokoHead>().Item.headSlot;
            target.body = ModContent.GetInstance<KayokoBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<KayokoBoots>().Item.legSlot;
        }
        else if (IsMutsukiAppearance)
        {
            target.head = ModContent.GetInstance<MutsukiHead>().Item.headSlot;
            target.body = ModContent.GetInstance<MutsukiBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<MutsukiBoots>().Item.legSlot;
        }
        else if (IsNozomiAppearance)
        {
            target.head = ModContent.GetInstance<NozomiHead>().Item.headSlot;
            target.body = ModContent.GetInstance<NozomiBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<NozomiBoots>().Item.legSlot;
        }
        else if (IsNagisaAppearance)
        {
            target.head = ModContent.GetInstance<NagisaHead>().Item.headSlot;
            target.body = ModContent.GetInstance<NagisaBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<NagisaBoots>().Item.legSlot;
        }
        else if (IsSeiaAppearance)
        {
            target.head = ModContent.GetInstance<SeiaHead>().Item.headSlot;
            target.body = ModContent.GetInstance<SeiaBody>().Item.bodySlot;
            target.legs = ModContent.GetInstance<SeiaBoots>().Item.legSlot;
        }

    }

    // The vanilla Player.legFrame is animation state, not just draw state. Modifying it
    // in FrameEffects makes the NEXT vanilla walk tick start from the mirrored frame.
    // The temporary override below is scoped to drawing and always restored afterwards.
    private Rectangle _originalLegFrame;
    private bool _legFrameOverriddenForDraw;

    private void RestoreLegFrameAfterDraw()
    {
        if (!_legFrameOverriddenForDraw)
            return;

        Player.legFrame = _originalLegFrame;
        _legFrameOverriddenForDraw = false;
    }

    // Keep existing Elaina-style appearance handling for pause and ordinary gameplay.
    public override void UpdateAutopause()
    {
        RestoreLegFrameAfterDraw(); // Recovery if a draw pass was interrupted.
        ApplyStudentVisualSlots(Player);
    }

    public override void FrameEffects()
    {
        RestoreLegFrameAfterDraw();
        ApplyStudentVisualSlots(Player);
    }

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        // Called AFTER normal animation was calculated, BEFORE the player draw layers.
        // Never accumulate edits to the real walking animation or change gameplay movement.
        RestoreLegFrameAfterDraw();
        Player target = drawInfo.drawPlayer;
        if (target == Player && !target.dead && UsesStudentOutfit())
        {
            // Keep the whole outfit in one native draw transform while resting.
            if (drawInfo.isSleeping && !drawInfo.headOnlyRender)
                drawInfo.rotation = -target.direction * MathHelper.PiOver2;
            // Broom's native sitting slicer discards the skirt's upper pixels.
            // Use the complete standing leg frame, aligned to the unchanged torso.
            if (target.mount.Active && target.mount.Type == Terraria.ID.MountID.WitchBroom)
            {
                drawInfo.isSitting = false;
                _originalLegFrame = target.legFrame;
                _legFrameOverriddenForDraw = true;
                target.legFrame.Y = 0;
            }
        }
        if (target != Player || target.dead || target.ghost || !UsesStudentOutfit()
            || target.mount.Active || System.Math.Abs(target.velocity.X) < 0.10f)
            return;

        // When aiming left and moving right (or vice versa), play ONLY the visible
        // stride backwards. Keep air, sitting, idle, mount, and non-stride frames intact.
        if (target.direction * target.velocity.X >= 0f)
            return;

        int frameHeight = target.legFrame.Height;
        if (frameHeight <= 0)
            return;

        int frame = target.legFrame.Y / frameHeight;
        if (frame < 6 || frame > 16)
            return;

        _originalLegFrame = target.legFrame;
        _legFrameOverriddenForDraw = true;
        target.legFrame.Y = (22 - frame) * frameHeight;
    }

    public override void TransformDrawData(ref PlayerDrawSet drawInfo)
    {
        // Every leg/shoe layer has now copied its source frame into DrawDataCache.
        // Restore the real frame BEFORE Terraria or another mod reads it next time.
        RestoreLegFrameAfterDraw();
        if (!UsesStudentOutfit() || Player.dead || Player.ghost || drawInfo.headOnlyRender
            || !Player.mount.Active || Player.mount.Type != Terraria.ID.MountID.WitchBroom) return;
        var mount = Player.mount._data;
        var mountTextures = new[] { mount.backTexture, mount.backTextureGlow, mount.backTextureExtra,
            mount.backTextureExtraGlow, mount.frontTexture, mount.frontTextureGlow,
            mount.frontTextureExtra, mount.frontTextureExtraGlow };
        // Move only the rider; native mount DrawData stays at its original position.
        for (int i = 0; i < drawInfo.DrawDataCache.Count; i++)
        {
            var data = drawInfo.DrawDataCache[i];
            bool isMount = System.Array.Exists(mountTextures, asset => asset != null && asset.IsLoaded && asset.Value == data.texture);
            if (isMount) continue;
            data.position.Y -= 11f * Player.gravDir;
            drawInfo.DrawDataCache[i] = data;
        }
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp,
        ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
    {
        // Elaina reference: suppress death gore without interfering with vanilla
        // death positioning, respawn timer, item drops or network state.
        // Do not affect unselected players, or student entries that have no outfit yet.
        if (UsesStudentOutfit())
            genGore = false;

        // Let Terraria process the death normally (items, respawn, sounds, statistics, etc.).
        return true;
    }

}
