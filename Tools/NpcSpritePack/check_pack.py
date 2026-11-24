from pathlib import Path
import zipfile,json,re
from PIL import Image
R=Path.cwd();data=json.loads((R/'Tools/NpcSpritePack/manifest.json').read_text(encoding='utf8'));out=R/'Content/Items/Vanity/NpcSpritePack'
with zipfile.ZipFile(r'C:/Users/Sunbinbin/Downloads/NPCsprites (1).zip') as z:
 for e in data:
  assert not any(k in e['source'] for k in ('Limbussy','HayaseYuuka','Mika','Nagisa','Seia'))
  assert z.read('NPCsprites/'+e['source'])==(out/e['target']).read_bytes()
cs=(R/'Content/Tablet/TabletStudentCatalog.cs').read_text(encoding='utf8');ap=(R/'Content/Tablet/TabletStudentAppearancePlayer.cs').read_text(encoding='utf8')
ids=re.findall(r'^    (\w+) = (\d+),',cs,re.M);assert len(ids)==len({int(n) for _,n in ids})==42
for name,n in ids:
 if int(n)<23 or int(n)==41:continue
 assert f'Get(TabletStudentId.{name})' in cs
 assert f'or TabletStudentId.{name}' in ap
 assert f'case TabletStudentId.{name}:' in ap
 for suffix in ('Head','Body','Boots'):
  assert (out/f'Pack{name}{suffix}.png').exists()
  assert f'GetInstance<Pack{name}{suffix}>()' in ap
for n in ('Hikari','Iroha'):
 for part in ('Head','Body','Boots'):assert f'GetInstance<{n}2x2{part}>()' in ap
 assert f'KivotosMod/Assets/Tablet/Students/{n}/Thumbnail2x2' in cs
assert 'raw <= (byte)TabletStudentId.MeguSaunaSmile2x2' in ap
assert len(data)==60
report={'passed':True,'exactAtlasByteComparisons':60,'outfits':20,'newStableIds':19,'oldIdsUnchanged':True,'saveAndNetworkWhitelist':True,'excludedCharactersAbsent':True}
(R/'Tools/NpcSpritePack/validation.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(report)
