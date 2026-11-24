using Terraria;using Terraria.ModLoader;using Terraria.DataStructures;using Microsoft.Xna.Framework.Graphics;using ReLogic.Content;
namespace KivotosMod.Content.Items.Vanity.Clothings2x2;
public abstract class ClothOverlayItem:ModItem {public bool BackLayer=>Wing || Name.Contains("Tail") || Name.EndsWith("HeadBack");public virtual bool HeadLayer=>false; public virtual bool Wing=>false;public override void SetDefaults(){Item.width=24;Item.height=24;Item.vanity=true;Item.accessory=true;}}
public sealed class ClothOverlayKuromiSerikaHeadHeadBack:ClothOverlayItem {public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/ClothOverlayKuromiSerikaHeadHeadBack";public override bool HeadLayer=>true;public override bool Wing=>false;}
public sealed class ClothOverlayKuromiSerikaHeadHeadFront:ClothOverlayItem {public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/ClothOverlayKuromiSerikaHeadHeadFront";public override bool HeadLayer=>true;public override bool Wing=>false;}
public sealed class ClothOverlayMidoriTailSheet:ClothOverlayItem {public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/ClothOverlayMidoriTailSheet";public override bool HeadLayer=>false;public override bool Wing=>false;}
public sealed class ClothOverlayMomoiCoatSheet:ClothOverlayItem {public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/ClothOverlayMomoiCoatSheet";public override bool HeadLayer=>false;public override bool Wing=>false;}
public sealed class ClothOverlayMomoiTailSheet:ClothOverlayItem {public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/ClothOverlayMomoiTailSheet";public override bool HeadLayer=>false;public override bool Wing=>false;}
public sealed class ClothOverlaySunaookamiShirokoHeadHeadBack:ClothOverlayItem {public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/ClothOverlaySunaookamiShirokoHeadHeadBack";public override bool HeadLayer=>true;public override bool Wing=>false;}
public sealed class ClothOverlaySunaookamiShirokoHeadHeadFront:ClothOverlayItem {public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/ClothOverlaySunaookamiShirokoHeadHeadFront";public override bool HeadLayer=>true;public override bool Wing=>false;}
public sealed class ClothOverlaySunaookamiShirokoNeckNeck:ClothOverlayItem {public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/ClothOverlaySunaookamiShirokoNeckNeck";public override bool HeadLayer=>false;public override bool Wing=>false;}
public sealed class ClothOverlaySunaookamiShirokoTailTail:ClothOverlayItem {public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/ClothOverlaySunaookamiShirokoTailTail";public override bool HeadLayer=>false;public override bool Wing=>false;}
public sealed class ClothOverlayShimoeKoharuWingsWings:ClothOverlayItem {public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/ClothOverlayShimoeKoharuWingsWings";public override bool HeadLayer=>false;public override bool Wing=>true;}
public sealed class ClothOverlayShimoeKoharuWingsWingsHead:ClothOverlayItem {public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/ClothOverlayShimoeKoharuWingsWingsHead";public override bool HeadLayer=>true;public override bool Wing=>true;}
[Autoload(Side=ModSide.Client)] public abstract class ClothOverlayLayer:PlayerDrawLayer {
 private static readonly System.Collections.Generic.Dictionary<string,Asset<Texture2D>> Textures=new();
 public override void Unload()=>Textures.Clear();
 protected abstract bool Back {get;}
 public override Position GetDefaultPosition()=>Back ? new Between(PlayerDrawLayers.Wings,PlayerDrawLayers.BackAcc) : new AfterParent(PlayerDrawLayers.Head);
 protected override void Draw(ref PlayerDrawSet info){var p=info.drawPlayer;if(p.dead||p.invis)return;
 var seen=new System.Collections.Generic.HashSet<string>();
 foreach(var item in p.armor)if(item.ModItem is ClothOverlayItem part && part.BackLayer==Back && seen.Add(part.Texture)){
 Textures.TryGetValue(part.Texture,out var asset);StudentSpriteDraw.Strip(ref info,ref asset,part.Texture,head:part.HeadLayer,frameHeight:part.Wing?62:56,paddingTop:part.Wing?3:0);Textures[part.Texture]=asset;
 }}
}

[Autoload(Side=ModSide.Client)]public sealed class ClothBackOverlayLayer:ClothOverlayLayer {protected override bool Back=>true;}
[Autoload(Side=ModSide.Client)]public sealed class ClothFrontOverlayLayer:ClothOverlayLayer {protected override bool Back=>false;}
