from pathlib import Path
from PIL import Image,ImageChops
import sys,json
root=Path.cwd();sys.path.insert(0,str(root/'Tools/StudentBatch'))
from import_assets import read_zip,DOWNLOADS
specs={'Ibuki':('伊吹.zip',{'arms':'Body_Body','head':'Head_Head','legs':'Boots_Legs','tail':'Tail','halo':'Halo'}),'Iroha':('伊吕波.zip',{'arms':'Body_Body','head':'Head_Head','legs':'Boots_Legs','hair':'BackHair','overCoat':'Coat','halo':'Halo'}),'Hikari':('小光.zip',{'arms':'Body_Body','head':'Head_Head','legs':'Boots_Legs','hair':'BackHair','Tail':'Tail','halo':'Halo'})}
checks=0
for name,(archive,mapping) in specs.items():
 src=read_zip(DOWNLOADS/archive)
 for key,suffix in mapping.items():
  expected=src[key].copy()
  if expected.height==1118:
   im=Image.new('RGBA',(40,1120));im.alpha_composite(expected);expected=im
  if suffix=='Body_Body':
   for col in (0,1):
    if not expected.crop((col*40,0,(col+1)*40,56)).getbbox():expected.paste(expected.crop((col*40,112,(col+1)*40,168)),(col*40,0))
  actual=Image.open(root/f'Content/Items/Vanity/{name}/{name}2x2{suffix}.png').convert('RGBA')
  assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),(name,key)
  checks+=1
halos=read_zip(DOWNLOADS/'光环2X2.zip')
for name,key in [('Mika','未花'),('Yuuka','优香2X2'),('Nagisa','渚'),('Seia','圣娅')]:
 actual=Image.open(root/f'Content/Items/Vanity/{name}/{name}2x2Halo.png').convert('RGBA');assert actual.tobytes()==halos[key].tobytes();checks+=1
for name in ('Mika','Yuuka','Ibuki','Iroha','Hikari'):
 for suffix in ('Head','Head_Head','Body','Body_Body','Boots','Boots_Legs'):
  assert (root/f'Content/Items/Vanity/{name}/{name}2x2{suffix}.png').exists()
preview=Image.new('RGBA',(600,180),(38,47,62,255))
from PIL import ImageDraw
draw=ImageDraw.Draw(preview)
for i,name in enumerate(('Mika','Yuuka','Ibuki','Iroha','Hikari')):
 im=Image.open(root/f'Assets/Tablet/Students/{name}/Thumbnail2x2.png').convert('RGBA');im.thumbnail((108,145),Image.Resampling.NEAREST);preview.alpha_composite(im,(i*120+6,10));draw.text((i*120+10,160),name,fill='white')
preview.save(root/'Tools/Wardrobe/outfits_preview.png')
report={'passed':True,'exactSourceComparisons':checks,'nativeEquipmentAssets':30,'unscaledSourcePadding':True}
(root/'Tools/Wardrobe/assets_validation.json').write_text(json.dumps(report,indent=2));print(report)
