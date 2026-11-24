using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
namespace KivotosMod.Content.Items.Vanity.NpcSpritePack;
public sealed class PackOutfitPlayer:ModPlayer
{
 public override void HideDrawLayers(PlayerDrawSet info)
 {
  int head=info.drawPlayer.head;
  if (!(head==ModContent.GetInstance<PackHimari2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackAko2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackMutsuki2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackGehennaStudent2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackHaruka2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackNonomi2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackSerika2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackKoyuki2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackIroha2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackKayoko2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackAru2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackMegu2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackHina2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackShiroko2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackHikari2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackNozomi2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackHoshino2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackNoa2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackAkari2x2Head>().Item.headSlot || head==ModContent.GetInstance<PackMeguSauna2x2Head>().Item.headSlot)) return;
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
  PlayerDrawLayers.JimsCloak.Hide();
  PlayerDrawLayers.LeinforsHairShampoo.Hide();
  PlayerDrawLayers.SkinLongCoat.Hide();
  PlayerDrawLayers.ArmorLongCoat.Hide();
 }
}
