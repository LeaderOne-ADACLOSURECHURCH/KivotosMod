from pathlib import Path
from PIL import Image,ImageDraw
import zipfile,io,json,re
assert not (Path.cwd()/'Content/Items/Vanity/NpcSpritePack/PackOutfits.cs').exists(), 'One-time integration already applied; use check_pack.py to verify.'
R=Path.cwd();out=R/'Content/Items/Vanity/NpcSpritePack';out.mkdir(exist_ok=True)
rows=[('Himari','日鞠','AkeboshiHimari','Millennium'),('Ako','亚子','AmauAko','Gehenna'),('Mutsuki','睦月','AsagiMutsuki','Gehenna'),('GehennaStudent','格黑娜学生','GenericGehenna/GehennaStudent','Gehenna'),('Haruka','春香','IgusaHaruka','Gehenna'),('Nonomi','野乃美','IzayoiNonomi/IzayoiNonomiCosplay','Abydos'),('Serika','芹香','KuromiSerika/KuromiSerikaCosplay','Abydos'),('Koyuki','小雪','KurosakiKoyuki','Millennium'),('Iroha','伊吕波','NatsumeIroha','Gehenna'),('Kayoko','佳代子','OnikataKayoko','Gehenna'),('Aru','阿露','RikuhachimaAru','Gehenna'),('Megu','惠','ShimokuraMegu','Gehenna'),('Hina','日奈','SorasakiHina','Gehenna'),('Shiroko','白子','SunaookamiShiroko/SunaookamiShirokoCosplay','Abydos'),('Hikari','小光','Tachibanas/TachibanaHikari','Highlander'),('Nozomi','小望','Tachibanas/TachibanaNozomi','Highlander'),('Hoshino','星野','TakanashiHoshino/TakanashiHoshinoCosplay','Abydos'),('Noa','诺亚','UshioNoa','Millennium'),('Akari','明里','WanibuchiAkari','Gehenna'),('MeguSauna','惠·桑拿','ShimokuraMegu/Sauna','Gehenna')]
cat=R/'Content/Tablet/TabletStudentCatalog.cs';cs=cat.read_text(encoding='utf8');ap=R/'Content/Tablet/TabletStudentAppearancePlayer.cs';aps=ap.read_text(encoding='utf8');classes='using Terraria;\nusing Terraria.ID;\nusing Terraria.ModLoader;\nnamespace KivotosMod.Content.Items.Vanity.NpcSpritePack;\n';cases='';newids=[];meta='';formation='';manifest=[];thumbs=[]
with zipfile.ZipFile(r'C:/Users/Sunbinbin/Downloads/NPCsprites (1).zip') as z:
 for name,zh,base,faction in rows:
  if '/' not in base:base+='/'+base
  if name.startswith('MeguSauna'):
   base='ShimokuraMegu/Sauna/';paths={'Head_Head':base+('Sauna_ShimokuraMeguHeadSatisfy_Head.png' if name.endswith('Smile') else 'Sauna_ShimokuraMeguHead_Head.png'),'Body_Body':base+'Sauna_BodyB1_Body.png','Boots_Legs':base+'Sauna_Legs_Legs.png'}
  else:
   cosplay='Cosplay' in base
   paths={'Head_Head':base+('_Head.png' if cosplay else 'Head_Head.png'),'Body_Body':base+('_Body.png' if cosplay else 'Body_Body.png'),'Boots_Legs':base+('_Legs.png' if cosplay else 'Legs_Legs.png')}
   if name in ('Hikari','Nozomi'):paths['Boots_Legs']='Tachibanas/TachibanaSistersLegs_Legs.png'
  prefix='Pack'+name+'2x2';ims={}
  for suffix,path in paths.items():
   raw=z.read('NPCsprites/'+path);im=Image.open(io.BytesIO(raw)).convert('RGBA');assert im.size==((360,224) if suffix=='Body_Body' else (40,1120)),(name,path,im.size);(out/(prefix+suffix+'.png')).write_bytes(raw);ims[suffix]=im;manifest.append({'source':path,'target':prefix+suffix+'.png'})
  for part,equip,key in [('Head','Head','Head_Head'),('Body','Body','Body_Body'),('Boots','Legs','Boots_Legs')]:
   im=ims[key].crop((0,0,40,56));box=im.getbbox();assert box,(name,part);im.crop(box).save(out/(prefix+part+'.png'))
   flags='ArmorIDs.Head.Sets.DrawHead[Item.headSlot]=false; ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot]=false; ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot]=false; ArmorIDs.Head.Sets.PreventBeardDraw[Item.headSlot]=true;' if part=='Head' else 'ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot]=true; ArmorIDs.Body.Sets.HidesArms[Item.bodySlot]=true; ArmorIDs.Body.Sets.HidesHands[Item.bodySlot]=true;' if part=='Body' else 'ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot]=true;'
   classes+=f'[AutoloadEquip(EquipType.{equip})]\npublic sealed class {prefix}{part}:ModItem {{ public override string Texture=>"KivotosMod/Content/Items/Vanity/NpcSpritePack/{prefix}{part}";public override void SetStaticDefaults(){{{flags}}}public override void SetDefaults(){{Item.width=24;Item.height=28;Item.vanity=true;Item.rare=ItemRarityID.LightRed;}}}}\n'
  pic=Image.new('RGBA',(40,56))
  for k in ('Boots_Legs','Body_Body','Head_Head'):pic.alpha_composite(ims[k].crop((0,0,40,56)))
  for y in (0,112):pic.alpha_composite(ims['Body_Body'].crop((80,y,120,y+56)))
  pic.crop(pic.getbbox()).save(out/(prefix+'Thumbnail.png'));thumbs.append((name,pic))
  id=name+'2x2'
  if name not in ('Iroha','Hikari'):
   newids.append(id);meta+=f'        new(TabletStudentId.{id}, "{zh} · 2×2", "{zh} 2×2", "{faction}", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/{prefix}Thumbnail"),\n';formation+=f'        Get(TabletStudentId.{id}),\n'
  else:
   old=f'KivotosMod/Assets/Tablet/Students/{name}/Thumbnail2x2';cs=cs.replace(old,f'KivotosMod/Content/Items/Vanity/NpcSpritePack/{prefix}Thumbnail')
   aps=re.sub(rf'            case TabletStudentId\.{id}:.*?                return;', '',aps,flags=re.S)
  cases+=f'            case TabletStudentId.{id}:\n                target.head=ModContent.GetInstance<{prefix}Head>().Item.headSlot;\n                target.body=ModContent.GetInstance<{prefix}Body>().Item.bodySlot;\n                target.legs=ModContent.GetInstance<{prefix}Boots>().Item.legSlot;\n                return;\n'
  for lang in ('zh-Hans','en-US'):
   p=R/f'Localization/{lang}/Mods.KivotosMod.Items.hjson';s=p.read_text(encoding='utf8')
   for part,label in [('Head','头饰'),('Body','上衣'),('Boots','鞋子')]:s+=f'\n{prefix}{part}: {{\n DisplayName: {zh} 2×2 {label}\n Tooltip: 2×2 外观\n}}\n'
   p.write_text(s,encoding='utf8')
cs=cs.replace('    Hikari2x2 = 22,','    Hikari2x2 = 22,\n'+''.join(f'    {id} = {23+i},\n' for i,id in enumerate(newids)))
cs=cs.replace('        new(TabletStudentId.Arona,',meta+'        new(TabletStudentId.Arona,',1);cs=cs.replace('        Get(TabletStudentId.Hikari2x2),','        Get(TabletStudentId.Hikari2x2),\n'+formation);cat.write_text(cs,encoding='utf8')
aps='using KivotosMod.Content.Items.Vanity.NpcSpritePack;\n'+aps;aps=aps.replace('        if (HasSelection) switch (SelectedStudent)\n        {','        if (HasSelection) switch (SelectedStudent)\n        {\n'+cases);aps=aps.replace('or TabletStudentId.Hikari2x2 ;','or TabletStudentId.Hikari2x2 '+''.join('or TabletStudentId.'+id+' ' for id in newids)+';');aps=aps.replace('raw <= (byte)TabletStudentId.Hikari2x2','raw <= (byte)TabletStudentId.'+newids[-1]);aps=aps.replace('private bool UsesStudentOutfit() =>','private bool UsesStudentOutfit() => (HasSelection && (byte)SelectedStudent >= 23) ||');ap.write_text(aps,encoding='utf8');(out/'PackOutfits.cs').write_text(classes,encoding='utf8')
helper=R/'Content/Items/Vanity/StudentSpriteVariants.cs';s=helper.read_text(encoding='utf8');existing=['Mutsuki','Haruka','Kayoko','Hina','Shiroko','Nozomi','Hoshino','Noa'];s=s.replace('internal static TabletStudentId OtherOutfit(TabletStudentId id) => id switch {','internal static TabletStudentId OtherOutfit(TabletStudentId id) => id switch {\n'+''.join(f'        TabletStudentId.{n} => TabletStudentId.{n}2x2, TabletStudentId.{n}2x2 => TabletStudentId.{n},\n' for n in existing));helper.write_text(s,encoding='utf8')
(R/'Tools/NpcSpritePack/manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
preview=Image.new('RGBA',(600,((len(thumbs)+5)//6)*140),(40,44,53,255));d=ImageDraw.Draw(preview)
for i,(n,im) in enumerate(thumbs):preview.alpha_composite(im.resize((80,112),Image.Resampling.NEAREST),((i%6)*100,(i//6)*140));d.text(((i%6)*100,(i//6)*140+116),n,fill='white')
preview.save(R/'Tools/NpcSpritePack/preview.png');print('Added',len(newids),'IDs; imported',len(manifest),'exact atlases; refreshed Iroha/Hikari 2x2')
