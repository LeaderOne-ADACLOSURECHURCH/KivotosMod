using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria;
using KivotosMod.Content.Items.Vanity.Mika;

namespace KivotosMod.Content.Items.Vanity.Hina;

public sealed class HinaVanityPlayer : ModPlayer
{
    private const int FlightFrames = 4;
    private int _flightTimer;
    internal bool Flapping { get; private set; }
    internal int FlightFrame => _flightTimer / MikaPlayer.FlightFrameTicks;

    public override void PostUpdate()
    {
        Player player = Player;
        bool flying = player.wingsLogic > 0 && player.wingTimeMax > 0 && player.wingTime > 0
            && !StudentSpriteDraw.SpecialFlightBoard(player) && player.wingsLogic is not (22 or 28 or 45)
            && HinaVanitySet.FullSetVisible(player);
        bool flapping = MikaPlayer.ShouldFlap(flying, player.velocity.Y != 0,
            player.controlJump || player.velocity.Y * player.gravDir < 0, player.mount.Active);
        _flightTimer = flapping && Flapping ? (_flightTimer + 1) % (FlightFrames * MikaPlayer.FlightFrameTicks) : 0;
        Flapping = flapping;
    }

    public override void UpdateDead()
    {
        _flightTimer = 0;
        Flapping = false;
    }

    public override void HideDrawLayers(PlayerDrawSet drawInfo)
    {
        if (!HinaVanitySet.FullSetVisible(drawInfo.drawPlayer))
            return;

        PlayerDrawLayers.HairBack.Hide();
        PlayerDrawLayers.BackAcc.Hide();
        PlayerDrawLayers.Backpacks.Hide();
        PlayerDrawLayers.BalloonAcc.Hide();
        PlayerDrawLayers.FaceAcc.Hide();
        PlayerDrawLayers.FrontAccBack.Hide();
        PlayerDrawLayers.FrontAccFront.Hide();
        PlayerDrawLayers.HandOnAcc.Hide();
        PlayerDrawLayers.NeckAcc.Hide();
        PlayerDrawLayers.OffhandAcc.Hide();
        PlayerDrawLayers.Shield.Hide();
        PlayerDrawLayers.SolarShield.Hide();
        PlayerDrawLayers.Tails.Hide();
        PlayerDrawLayers.WaistAcc.Hide();
        if (!StudentSpriteDraw.SpecialFlightBoard(drawInfo.drawPlayer))
            PlayerDrawLayers.Wings.Hide();

        PlayerDrawLayers.JimsCloak.Hide();
        PlayerDrawLayers.LeinforsHairShampoo.Hide();
        PlayerDrawLayers.Magiluminescence.Hide();
        PlayerDrawLayers.SkinLongCoat.Hide();
        PlayerDrawLayers.ArmorLongCoat.Hide();
        // Flying Carpet and Shoes stay visible on purpose.
    }
}
