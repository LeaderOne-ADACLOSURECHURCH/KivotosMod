from pathlib import Path
from PIL import Image
import sys,json
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/StudentBatch'))
from import_assets import read_zip,DOWNLOADS
specs={'Ibuki':('伊吹.zip',{'arms':'Body_Body','head':'Head_Head','legs':'Boots_Legs','tail':'Tail','halo':'Halo'}),
 'Iroha':('伊吕波.zip',{'arms':'Body_Body','head':'Head_Head','legs':'Boots_Legs','hair':'BackHair','overCoat':'Coat','halo':'Halo'}),
 'Hikari':('小光.zip',{'arms':'Body_Body','head':'Head_Head','legs':'Boots_Legs','hair':'BackHair','Tail':'Tail','halo':'Halo'})}
report={}
for student,(archive,mapping) in specs.items():
 src=read_zip(DOWNLOADS/archive);out=ROOT/'Content/Items/Vanity'/student;ims={}
 for raw,key in mapping.items():
  im=src[raw].copy()
  if im.height==1118:
   padded=Image.new('RGBA',(40,1120));padded.alpha_composite(im);im=padded
  if key=='Body_Body':
   for col in (0,1):
    if not im.crop((col*40,0,(col+1)*40,56)).getbbox():im.paste(im.crop((col*40,112,(col+1)*40,168)),(col*40,0))
  im.save(out/f'{student}2x2{key}.png');ims[key]=im
 for part,key in [('Head','Head_Head'),('Body','Body_Body'),('Boots','Boots_Legs')]:
  im=ims[key].crop((0,0,40,56));im.crop(im.getbbox()).save(out/f'{student}2x2{part}.png')
 report[student]={'source':archive,'headTop':ims['Head_Head'].crop((0,0,40,56)).getbbox()[1],'haloBottom':ims['Halo'].getbbox()[3]}
halos=read_zip(DOWNLOADS/'光环2X2.zip')
for student,key in [('Mika','未花'),('Yuuka','优香2X2'),('Nagisa','渚'),('Seia','圣娅')]:
 im=halos[key];im.save(ROOT/f'Content/Items/Vanity/{student}/{student}2x2Halo.png')
 report.setdefault(student,{})['newHalo']={'size':im.size,'bottom':im.getbbox()[3]}
for student in ('Mika','Yuuka','Ibuki','Iroha','Hikari'):
 out=ROOT/'Content/Items/Vanity'/student;pic=Image.new('RGBA',(96,110))
 for key in ('BackHair','Tail','Boots_Legs'):
  path=out/f'{student}2x2{key}.png'
  if path.exists():pic.alpha_composite(Image.open(path).convert('RGBA').crop((0,0,40,56)),(28,30))
 atlas=Image.open(out/f'{student}2x2Body_Body.png').convert('RGBA');pic.alpha_composite(atlas.crop((0,0,40,56)),(28,30))
 for key in ('Head_Head','Coat'):
  path=out/f'{student}2x2{key}.png'
  if path.exists():pic.alpha_composite(Image.open(path).convert('RGBA').crop((0,0,40,56)),(28,30))
 for row in (0,2):pic.alpha_composite(atlas.crop((80,row*56,120,(row+1)*56)),(28,30))
 target=ROOT/'Assets/Tablet/Students'/student;pic.crop(pic.getbbox()).save(target/'Thumbnail2x2.png')
(ROOT/'Tools/Wardrobe/import_manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps(report,ensure_ascii=True))
