using Terraria;using Terraria.ModLoader;using Terraria.DataStructures;using Microsoft.Xna.Framework.Graphics;using ReLogic.Content;using KivotosMod.Content.Items.Vanity.NpcSpritePack;
namespace KivotosMod.Content.Items.Vanity.Clothings2x2;
public abstract class ClothHaloItem:ModItem {public override void SetDefaults(){Item.width=30;Item.height=20;Item.vanity=true;Item.accessory=true;}}
[Autoload(Side=ModSide.Client)] public sealed class ClothHaloLayer:PlayerDrawLayer {
 private static Asset<Texture2D> _texture;
 public override void Unload()=>_texture=null;
 public override Position GetDefaultPosition()=>new AfterParent(PlayerDrawLayers.Head);
 public override bool IsHeadLayer=>true;
 protected override void Draw(ref PlayerDrawSet info){var p=info.drawPlayer;if(p.dead||p.ghost||p.invis)return;string name=null;int bottom=44;
 foreach(var item in p.armor) if(item.ModItem is ClothHaloItem halo){name=halo.Name;break;}
 if(name==null && p.head==ModContent.GetInstance<PackAko2x2Head>().Item.headSlot){name="ClothHaloAmauAkoHalo";bottom=36;}
 if(name==null && p.head==ModContent.GetInstance<PackMutsuki2x2Head>().Item.headSlot){name="ClothHaloAsagiMutsukiHalo";bottom=38;}
 if(name==null && p.head==ModContent.GetInstance<PackGehennaStudent2x2Head>().Item.headSlot){name="ClothHaloGehennaHalo";bottom=34;}
 if(name==null && p.head==ModContent.GetInstance<PackHaruka2x2Head>().Item.headSlot){name="ClothHaloIgusaHarukaHalo";bottom=38;}
 if(name==null && p.head==ModContent.GetInstance<PackNonomi2x2Head>().Item.headSlot){name="ClothHaloIzayoiNonomiHalo";bottom=42;}
 if(name==null && p.head==ModContent.GetInstance<PackSerika2x2Head>().Item.headSlot){name="ClothHaloKuromiSerikaHalo";bottom=36;}
 if(name==null && p.head==ModContent.GetInstance<PackKoyuki2x2Head>().Item.headSlot){name="ClothHaloKurosakiKoyukiHalo";bottom=37;}
 if(name==null && p.head==ModContent.GetInstance<PackIroha2x2Head>().Item.headSlot){name="ClothHaloNatsumeIrohaHalo";bottom=42;}
 if(name==null && p.head==ModContent.GetInstance<PackKayoko2x2Head>().Item.headSlot){name="ClothHaloOnikataKayokoHalo";bottom=40;}
 if(name==null && p.head==ModContent.GetInstance<PackAru2x2Head>().Item.headSlot){name="ClothHaloRikuhachimaAruHalo";bottom=34;}
 if(name==null && p.head==ModContent.GetInstance<PackMegu2x2Head>().Item.headSlot){name="ClothHaloShimokuraMeguHalo";bottom=38;}
 if(name==null && p.head==ModContent.GetInstance<PackMeguSauna2x2Head>().Item.headSlot){name="ClothHaloShimokuraMeguHalo";bottom=38;}
 if(name==null && p.head==ModContent.GetInstance<PackHina2x2Head>().Item.headSlot){name="ClothHaloSorasakiHinaHalo";bottom=32;}
 if(name==null && p.head==ModContent.GetInstance<PackShiroko2x2Head>().Item.headSlot){name="ClothHaloSunaookamiShirokoHalo";bottom=40;}
 if(name==null && p.head==ModContent.GetInstance<PackHikari2x2Head>().Item.headSlot){name="ClothHaloTachibanaHikariHalo";bottom=36;}
 if(name==null && p.head==ModContent.GetInstance<PackNozomi2x2Head>().Item.headSlot){name="ClothHaloTachibanaNozomiHalo";bottom=36;}
 if(name==null && p.head==ModContent.GetInstance<PackHoshino2x2Head>().Item.headSlot){name="ClothHaloTakanashiHoshinoHalo";bottom=38;}
 if(name==null && p.head==ModContent.GetInstance<PackNoa2x2Head>().Item.headSlot){name="ClothHaloUshioNoaHalo";bottom=36;}
 if(name==null && p.head==ModContent.GetInstance<PackAkari2x2Head>().Item.headSlot){name="ClothHaloWanibuchiAkariHalo";bottom=36;}
 bottom=name switch {"ClothHaloMillenniumHalo"=>34,"ClothHaloAmauAkoHalo"=>36,"ClothHaloAsagiMutsukiHalo"=>38,"ClothHaloGehennaHalo"=>34,"ClothHaloGenericHalo"=>34,"ClothHaloHayaseYuukaHalo"=>36,"ClothHaloHinomiyaChinatsuHalo"=>41,"ClothHaloIgusaHarukaHalo"=>38,"ClothHaloIzayoiNonomiHalo"=>42,"ClothHaloKasumiHalo"=>40,"ClothHaloKuromiSerikaHalo"=>36,"ClothHaloKurosakiKoyukiHalo"=>37,"ClothHaloMisonoMikaHalo"=>35,"ClothHaloNatsumeIrohaHalo"=>42,"ClothHaloOkusoraAyaneHalo"=>35,"ClothHaloOnikataKayokoHalo"=>40,"ClothHaloRikuhachimaAruHalo"=>34,"ClothHaloShimokuraMeguHalo"=>38,"ClothHaloShiromiIoriHalo"=>39,"ClothHaloSorasakiHinaHalo"=>32,"ClothHaloSunaookamiShirokoHalo"=>40,"ClothHaloTachibanaHikariHalo"=>36,"ClothHaloTachibanaNozomiHalo"=>36,"ClothHaloTakanashiHoshinoHalo"=>38,"ClothHaloUshioNoaHalo"=>36,"ClothHaloWanibuchiAkariHalo"=>36,_=>bottom};
 if(name!=null && (_texture==null || _texture.Name!="KivotosMod/Content/Items/Vanity/Clothings2x2/"+name))_texture=null;
 if(name!=null)StudentSpriteDraw.Halo(ref info,ref _texture,"KivotosMod/Content/Items/Vanity/Clothings2x2/"+name,4,bottom);
 }
}
