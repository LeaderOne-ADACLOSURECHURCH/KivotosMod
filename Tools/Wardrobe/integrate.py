"""Integrate explicit outfit entries without changing persisted original student IDs."""
from pathlib import Path
import re,json
ROOT=Path(__file__).resolve().parents[2]
names=['Mika','Yuuka','Ibuki','Iroha','Hikari'];zh={'Mika':'未花','Yuuka':'优香','Ibuki':'伊吹','Iroha':'伊吕波','Hikari':'小光'}
for name in names:
 out=ROOT/'Content/Items/Vanity'/name
 text=f'using Terraria;\nusing Terraria.ID;\nusing Terraria.ModLoader;\nnamespace KivotosMod.Content.Items.Vanity.{name};\n'
 for part,equip,slot in [('Head','Head','headSlot'),('Body','Body','bodySlot'),('Boots','Legs','legSlot')]:
  flags='ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false; ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = false; ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = false; ArmorIDs.Head.Sets.PreventBeardDraw[Item.headSlot] = true;' if part=='Head' else 'ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = true; ArmorIDs.Body.Sets.HidesArms[Item.bodySlot] = true; ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = true;' if part=='Body' else 'ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = true;'
  text+=f'''\n[AutoloadEquip(EquipType.{equip})]
public sealed class {name}2x2{part} : ModItem
{{
    public override string Texture => "KivotosMod/Content/Items/Vanity/{name}/{name}2x2{part}";
    public override void SetStaticDefaults() {{ {flags} }}
    public override void SetDefaults() {{ Item.width=24; Item.height=28; Item.vanity=true; Item.rare=ItemRarityID.LightRed; }}
}}
'''
 (out/f'{name}2x2Set.cs').write_text(text,encoding='utf8')
 p=out/f'{name}Set.cs';s=p.read_text('utf8')
 s=re.sub(r'    public override string Texture => StudentSpriteVariants\.[^;]+;\n','',s)
 for part,slot in [('Head','headSlot'),('Body','bodySlot'),('Boots','legSlot')]:
  pattern=rf'([a-z]+)\.(head|body|legs) == ModContent.GetInstance<{name}{part}>\(\).Item.{slot}'
  s=re.sub(pattern,lambda m:m[0]+f' || {m[1]}.{m[2]} == ModContent.GetInstance<{name}2x2{part}>().Item.{slot}',s)
 if name=='Mika':
  for idx,part in enumerate(['Head','Body','Boots']):
   s=s.replace(f'VisibleType({idx}) == ModContent.ItemType<Mika{part}>()',f'(VisibleType({idx}) == ModContent.ItemType<Mika{part}>() || VisibleType({idx}) == ModContent.ItemType<Mika2x2{part}>())')
 p.write_text(s,encoding='utf8')

helper='using Terraria;\nusing Terraria.ModLoader;\nusing KivotosMod.Content.Tablet;\n'+''.join(f'using KivotosMod.Content.Items.Vanity.{n};\n' for n in names)+'namespace KivotosMod.Content.Items.Vanity;\ninternal static class StudentSpriteVariants\n{\n'
for name in names:
 helper+=f'    internal static bool {name}2x2(Player p) => p.head == ModContent.GetInstance<{name}2x2Head>().Item.headSlot;\n'
 helper+=f'    internal static bool {name}Body2x2(Player p) => p.body == ModContent.GetInstance<{name}2x2Body>().Item.bodySlot;\n'
helper+='    internal static TabletStudentId Canonical(TabletStudentId id) => id switch {\n'+''.join(f'        TabletStudentId.{n}2x2 => TabletStudentId.{n},\n' for n in names)+'        _ => id,\n    };\n'
helper+='    internal static TabletStudentId OtherOutfit(TabletStudentId id) => id switch {\n'+''.join(f'        TabletStudentId.{n} => TabletStudentId.{n}2x2, TabletStudentId.{n}2x2 => TabletStudentId.{n},\n' for n in names)+'        _ => id,\n    };\n}\n'
(ROOT/'Content/Items/Vanity/StudentSpriteVariants.cs').write_text(helper,encoding='utf8')
for file in ['Mika/MikaDrawLayers.cs','Mika/MikaSideLocksLayers.cs','Yuuka/YuukaDrawLayers.cs']:
 p=ROOT/'Content/Items/Vanity'/file;s=p.read_text('utf8')
 # Explicit player context: two players can wear different outfits simultaneously.
 for n in ('Mika','Yuuka'):
  if n not in file:continue
  s=s.replace(f'StudentSpriteVariants.{n}2x2',f'StudentSpriteVariants.{n}2x2('+('drawInfo.drawPlayer' if n=='Yuuka' else 'info.drawPlayer')+')')
 if file.endswith('DrawLayers.cs'):
  for cls in (['MikaTorsoLayer','MikaWingLayer'] if file.startswith('Mika') else ['YuukaBodyBaseLayer','Yuuka2x2CoatLayer']):
   start=s.index('public sealed class '+cls);end=s.find('\n[Autoload',start)
   if end<0:end=len(s)
   chunk=s[start:end].replace('Variants.Mika2x2(','Variants.MikaBody2x2(').replace('Variants.Yuuka2x2(','Variants.YuukaBody2x2(')
   if cls=='Yuuka2x2CoatLayer':chunk=chunk.replace('drawInfo.drawPlayer','info.drawPlayer')
   s=s[:start]+chunk+s[end:]
 p.write_text(s,encoding='utf8')

p=ROOT/'Content/Tablet/TabletStudentCatalog.cs';s=p.read_text('utf8')
s=s.replace('    Seia = 17,','    Seia = 17,\n'+''.join(f'    {n}2x2 = {18+i},\n' for i,n in enumerate(names)))
entries=''.join(f'        new(TabletStudentId.{n}2x2, "{zh[n]} · 2×2", "{zh[n]} 2×2", "{("Trinity" if n=="Mika" else "Millennium" if n=="Yuuka" else "Highlander" if n=="Hikari" else "Gehenna")}", 7, 0, 0,\n            "KivotosMod/Assets/Tablet/Students/{n}/Thumbnail2x2"),\n' for n in names)
marker='        new(TabletStudentId.Arona,';s=s.replace(marker,entries+'\n'+marker)
s=s.replace('        Get(TabletStudentId.Seia),','        Get(TabletStudentId.Seia),\n'+''.join(f'        Get(TabletStudentId.{n}2x2),\n' for n in names))
p.write_text(s,encoding='utf8')
p=ROOT/'Content/Tablet/TabletStudentAppearancePlayer.cs';s=p.read_text('utf8');s='using KivotosMod.Content.Items.Vanity;\n'+s
for n in names:s=s.replace(f'HasSelection && SelectedStudent == TabletStudentId.{n};',f'HasSelection && StudentSpriteVariants.Canonical(SelectedStudent) == TabletStudentId.{n};')
s=s.replace('or TabletStudentId.Nagisa or TabletStudentId.Seia;','or TabletStudentId.Nagisa or TabletStudentId.Seia\n        '+''.join(f'or TabletStudentId.{n}2x2 ' for n in names)+';')
s=s.replace('raw <= (byte)TabletStudentId.Seia','raw <= (byte)TabletStudentId.Hikari2x2')
cases='        if (HasSelection) switch (SelectedStudent)\n        {\n'
for n in names:
 cases+=f'            case TabletStudentId.{n}2x2:\n                target.head = ModContent.GetInstance<{n}2x2Head>().Item.headSlot;\n                target.body = ModContent.GetInstance<{n}2x2Body>().Item.bodySlot;\n                target.legs = ModContent.GetInstance<{n}2x2Boots>().Item.legSlot;\n                return;\n'
cases+='        }\n'
s=s.replace('    private void ApplyStudentVisualSlots(Player target)\n    {','    private void ApplyStudentVisualSlots(Player target)\n    {\n'+cases)
p.write_text(s,encoding='utf8')
# Retire the old global/reload-required config controls.
p=ROOT/'Globals/Configs/ClientsSide.cs';s=p.read_text('utf8');s=re.sub(r'\n        \[ReloadRequired\]\n        \[DefaultValue\(false\)\]\n        public bool Use(?:Mika|Yuuka)2x2 \{ get; set; \}\n','\n',s);p.write_text(s,encoding='utf8')
for locale in ('zh-Hans','en-US'):
 p=ROOT/f'Localization/{locale}/Mods.KivotosMod.Configs.hjson';s=p.read_text('utf8');s=re.sub(r'\n\tUse(?:Mika|Yuuka)2x2: \{[^}]+\}\n','\n',s);p.write_text(s,encoding='utf8')
 p=ROOT/f'Localization/{locale}/Mods.KivotosMod.Items.hjson';s=p.read_text('utf8')
 for n in names:
  for part,label in [('Head','发型'),('Body','制服'),('Boots','鞋装')]:
   display=f'{zh[n]}{label}（2×2）' if locale=='zh-Hans' else f'{n} {part} (2x2)'
   s+=f'\n{n}2x2{part}: {{\n\tDisplayName: {display}\n\tTooltip: '+('可在什亭之匣学生列表切换服装。' if locale=='zh-Hans' else 'Select this outfit in tablet Formation.')+'\n}\n'
 p.write_text(s,encoding='utf8')
print('Integrated five per-player outfit entries and removed global texture switches.')
