from pathlib import Path
import zipfile,io,re,json
from PIL import Image
R=Path.cwd();O=R/'Content/Items/Vanity/Clothings2x2';O.mkdir(exist_ok=True)
code='using Terraria;using Terraria.ID;using Terraria.ModLoader;\nnamespace KivotosMod.Content.Items.Vanity.Clothings2x2;\n';halos=[];gear=[];manifest=[]
with zipfile.ZipFile(r'C:/Users/Sunbinbin/Downloads/Clothings.zip') as z:
 for n in z.namelist():
  if not n.endswith('.png') or '.aseprite' in n or 'example' in n:continue
  im=Image.open(io.BytesIO(z.read(n))).convert('RGBA');isHalo='/Halos/' in n or n.endswith('/MillenniumHalo.png')
  if isHalo:
   name='ClothHalo'+Path(n).stem;im.save(O/(name+'.png'));bottom=im.getbbox()[3] if im.getbbox() else im.height;halos.append((name,Path(n).stem,bottom));code+=f'public sealed class {name}:ClothHaloItem {{public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/{name}";}}\n';manifest.append(n);continue
  if '/Hairstyles/' in n:continue
  if im.size not in ((40,1120),(360,224),(40,784)):continue
  # Source parts with a back/front suffix are layers, not replacement head armor.
  if n.endswith(('_Head_Back.png','_Head_Front.png','_Neck.png','_Tail.png','Tail Sheet.png','Coat Sheet.png')):continue
  key=re.sub('[^A-Za-z0-9]','',n.removeprefix('Clothings/').removesuffix('.png'));name='Cloth'+key
  equip='Body' if im.width==360 else 'Head' if any(k in n for k in ('Head','Hat','Horns','Headpiece')) else 'Legs';suffix={'Body':'_Body','Head':'_Head','Legs':'_Legs'}[equip]
  if im.height==784:
   # Native equipment uses 20 frames; repeat the last authored hair frame for extra poses.
   padded=Image.new('RGBA',(40,1120));padded.alpha_composite(im)
   for y in range(784,1120,56):padded.paste(im.crop((0,728,40,784)),(0,y))
   im=padded
  im.save(O/(name+suffix+'.png'));icon=im.crop((0,0,40,56));box=icon.getbbox()
  if not box:icon=Image.new('RGBA',(24,24),(0,0,0,0));box=(0,0,24,24)
  icon.crop(box).save(O/(name+'.png'))
  code+=f'[AutoloadEquip(EquipType.{equip})] public sealed class {name}:ModItem {{public override string Texture=>"KivotosMod/Content/Items/Vanity/Clothings2x2/{name}";public override void SetDefaults(){{Item.width=24;Item.height=24;Item.vanity=true;Item.rare=ItemRarityID.Blue;}}}}\n';gear.append((name,n,equip));manifest.append(n)
(O/'ClothItems.cs').write_text(code,encoding='utf8')
# Native 2x2 student halos are bound to their exact pack head slot, never canonical 1x1 identity.
mapping={'AmauAko':'Ako','AsagiMutsuki':'Mutsuki','Gehenna':'GehennaStudent','IgusaHaruka':'Haruka','IzayoiNonomi':'Nonomi','KuromiSerika':'Serika','KurosakiKoyuki':'Koyuki','NatsumeIroha':'Iroha','OnikataKayoko':'Kayoko','RikuhachimaAru':'Aru','ShimokuraMegu':'Megu','SorasakiHina':'Hina','SunaookamiShiroko':'Shiroko','TachibanaHikari':'Hikari','TachibanaNozomi':'Nozomi','TakanashiHoshino':'Hoshino','UshioNoa':'Noa','WanibuchiAkari':'Akari'}
s='''using Terraria;using Terraria.ModLoader;using Terraria.DataStructures;using Microsoft.Xna.Framework.Graphics;using ReLogic.Content;using KivotosMod.Content.Items.Vanity.NpcSpritePack;
namespace KivotosMod.Content.Items.Vanity.Clothings2x2;
public abstract class ClothHaloItem:ModItem {public override void SetDefaults(){Item.width=30;Item.height=20;Item.vanity=true;Item.accessory=true;}}
[Autoload(Side=ModSide.Client)] public sealed class ClothHaloLayer:PlayerDrawLayer {
 private static Asset<Texture2D> _texture;
 public override Position GetDefaultPosition()=>new AfterParent(PlayerDrawLayers.Head);
 public override bool IsHeadLayer=>true;
 protected override void Draw(ref PlayerDrawSet info){var p=info.drawPlayer;if(p.dead||p.ghost||p.invis)return;string name=null;int bottom=44;
 foreach(var item in p.armor) if(item.ModItem is ClothHaloItem halo){name=halo.Name;break;}
'''
for name,stem,bottom in halos:
 key=stem.removesuffix('Halo');student=mapping.get(key)
 if student:
  s+=f' if(name==null && p.head==ModContent.GetInstance<Pack{student}2x2Head>().Item.headSlot){{name="{name}";bottom={bottom};}}\n'
  if student=='Megu':s+=f' if(name==null && p.head==ModContent.GetInstance<PackMeguSauna2x2Head>().Item.headSlot){{name="{name}";bottom={bottom};}}\n'
s+=' if(name!=null)StudentSpriteDraw.Halo(ref info,ref _texture,"KivotosMod/Content/Items/Vanity/Clothings2x2/"+name,4,bottom);\n }\n}\n'
# A single cached asset cannot be reused for different selected halo textures.
s=s.replace('if(name!=null)StudentSpriteDraw.Halo', 'if(name!=null && (_texture==null || _texture.Name!="KivotosMod/Content/Items/Vanity/Clothings2x2/"+name))_texture=null;\n if(name!=null)StudentSpriteDraw.Halo')
(O/'ClothHaloLayer.cs').write_text(s,encoding='utf8')
for lang in ('zh-Hans','en-US'):
 p=R/f'Localization/{lang}/Mods.KivotosMod.Items.hjson';s=p.read_text(encoding='utf8')
 for name,n,equip in gear:s+=f'\n{name}: {{\n DisplayName: "{Path(n).stem} · 2×2"\n Tooltip: "2×2 玩家服装"\n}}\n'
 for name,stem,_ in halos:s+=f'\n{name}: {{\n DisplayName: "{stem} · 2×2"\n Tooltip: "2×2 时装光环"\n}}\n'
 p.write_text(s,encoding='utf8')
(R/'Tools/Clothings/manifest.json').write_text(json.dumps({'gear':gear,'halos':halos,'sources':manifest},ensure_ascii=False,indent=2),encoding='utf8');print(len(gear),'native clothing pieces;',len(halos),'halos')
