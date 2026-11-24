"""Import supplied 2x2-authored art without resizing native pixels."""
from pathlib import Path
from PIL import Image,ImageDraw
import sys,json,hashlib
TOOLS=Path(__file__).resolve().parent
ROOT=TOOLS.parents[1]
sys.path.insert(0,str(ROOT/'Tools/StudentBatch'))
from import_assets import read_zip,DOWNLOADS
SPECS={
 'Nagisa':('渚.zip',{'Head_Head':'头','Body_Body':'手','Boots_Legs':'腿','BackHair':'后发','Wings':'翅膀'}),
 'Seia':('圣娅.zip',{'Head_Head':'头','Body_Body':'手','Boots_Legs':'腿','BackHair':'后发','Ears':'耳朵','Tail':'尾巴'}),
 'Mika2x2':('未花 (1).zip',{'Head_Head':'头','Body_Body':'手','Boots_Legs':'腿','BackHair':'后发','Wings':'翅膀'}),
 'Yuuka2x2':('优香2X2 2.zip',{'Head_Head':'头','Body_Body':'手臂','Boots_Legs':'腿','BackHair':'后发','Coat':'外套'}),
}
def main():
 report={};preview=Image.new('RGBA',(1280,750),(44,58,74,255));d=ImageDraw.Draw(preview)
 for col,(name,(fn,mapping)) in enumerate(SPECS.items()):
  source=read_zip(DOWNLOADS/fn);images={k:source[v].copy() for k,v in mapping.items()}
  if name=='Seia':images['Head_Head'].alpha_composite(source['鬓发'])
  atlas=images['Body_Body'];assert atlas.size==(360,224)
  for x in (0,1):
   torso=atlas.crop((x*40,112,(x+1)*40,168));assert torso.getbbox();atlas.paste(torso,(x*40,0))
  out=ROOT/'Content/Items/Vanity'/name.replace('2x2','');out.mkdir(exist_ok=True)
  for key,im in images.items():
   assert im.size==(360,224) if key=='Body_Body' else im.height in (1120,1200)
   im.save(out/f'{name}{key}.png')
  for item,key in [('Head','Head_Head'),('Body','Body_Body'),('Boots','Boots_Legs')]:
   im=images[key].crop((0,0,40,56));im.crop(im.getbbox()).save(out/f'{name}{item}.png')
  def character(frame=0,flip=False):
   pic=Image.new('RGBA',(100,100))
   def strip(key):
    if key not in images:return
    im=images[key];h=im.height//20;pad=4 if h==60 else 0
    pic.alpha_composite(im.crop((0,frame*h,im.width,(frame+1)*h)),(30-(im.width-40)//2,28-pad))
   strip('BackHair');strip('Tail');strip('Wings')
   pic.alpha_composite(atlas.crop((80,112,120,168)),(30,28));strip('Boots_Legs')
   pic.alpha_composite(atlas.crop((0,0,40,56)),(30,28));strip('Head_Head');strip('Ears');strip('Coat')
   pic.alpha_composite(atlas.crop((80,0,120,56)),(30,28))
   return pic.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if flip else pic
  if name in ('Nagisa','Seia'):
   target=ROOT/'Assets/Tablet/Students'/name;target.mkdir(parents=True,exist_ok=True)
   thumb=character();thumb.crop(thumb.getbbox()).save(target/'Thumbnail.png')
  for row,(frame,flip) in enumerate([(0,False),(0,True),(10,False)]):
   preview.alpha_composite(character(frame,flip).resize((240,240),Image.Resampling.NEAREST),(col*320,row*250+10))
   d.text((col*320+3,row*250+2),f'{name} frame {frame}'+(' flipped' if flip else ''),fill='white')
  report[name]={'sourceZip':str(DOWNLOADS/fn),'sha256':hashlib.sha256((DOWNLOADS/fn).read_bytes()).hexdigest(),
    'headTop':images['Head_Head'].crop((0,0,40,56)).getbbox()[1],
    'assets':{k:{'size':im.size,'sha256':hashlib.sha256((out/f'{name}{k}.png').read_bytes()).hexdigest()} for k,im in images.items()}}
 preview.save(TOOLS/'variants_preview.png');(TOOLS/'import_manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
 print('Imported two new students and two optional 2x2 outfits at native scale.')
if __name__=='__main__':main()
