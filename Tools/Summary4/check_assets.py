from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,re
root=Path(__file__).resolve().parents[2];skin=root/'Assets/Tablet/Upgrades';out=root/'Tools/Summary4'
m=json.loads((out/'import_manifest.json').read_text(encoding='utf-8'))
raw=(root/m['output']).read_bytes();assert raw==Path(m['source']).read_bytes();assert hashlib.sha256(raw).hexdigest()==m['sha256'];im=Image.open(root/m['output']);assert im.size==(30,30) and im.mode=='RGBA'
print('PASS: supplied selected-hover category image imported byte-for-byte as 30x30 RGBA.')
# Asset/geometry QA only: software composition, not a game or font rendering test.
main=(root/'Content/Tablet/TabletUiSystem.cs').read_text(encoding='utf-8');upgrade=(root/'Content/Tablet/TabletUiSystem.Upgrades.cs').read_text(encoding='utf-8')
a=re.search(r'Rectangle itemHoverRect = new\(card.X \+ (\d+), card.Y \+ (\d+), (\d+), (\d+)\);',main);ix,iy,iw,ih=map(int,a.groups())
sy=int(re.search(r'ProductFavoriteRect[^\n]*card.Y \+ (\d+)',upgrade).group(1))
canvas=Image.new('RGBA',(500,260),'#344b6b');d=ImageDraw.Draw(canvas);font=ImageFont.truetype(r'C:\Windows\Fonts\arial.ttf',10)
for i,name in enumerate(['ShopFavoriteNormal','ShopFavoriteHover','ShopFavoritePressed','ShopFavoriteSelectedHover']):
 t=Image.open(skin/(name+'.png'));canvas.alpha_composite(t,(12+i*120,12));d.text((12+i*120,46),['normal','hover','selected','selected + hover'][i],font=font,fill='white')
weapons=[]
for p in (root/'Assets/Texture').rglob('*Weapon.png'):
 t=Image.open(p)
 if 0<t.width<250 and t.height>0:weapons.append((t.height/t.width,p))
weapons.sort(key=lambda a:a[0],reverse=True)
chosen=[p for _,p in weapons[:2]]+[p for _,p in weapons[-2:]]
for i,p in enumerate(chosen):
 x=12+i*120;y=78
 card=Image.open(root/'Assets/Tablet/UI/ProductCard.png').convert('RGBA').resize((108,148),Image.Resampling.NEAREST);canvas.alpha_composite(card,(x,y))
 t=Image.open(p).convert('RGBA');s=min(iw/t.width,ih/t.height);w=max(1,round(t.width*s));h=max(1,round(t.height*s));canvas.alpha_composite(t.resize((w,h),Image.Resampling.NEAREST),(x+ix+(iw-w)//2,y+iy+(ih-h)//2))
 star=Image.open(skin/'StarActive.png').resize((16,16),Image.Resampling.NEAREST);canvas.alpha_composite(star,(x+88,y+sy))
 d.text((x+7,y+7),p.stem[:13],font=font,fill='#30445d');d.text((x+51,y+106),'1',font=font,fill='#30445d');d.text((x+32,y+128),'10000',font=font,fill='#30445d')
canvas.resize((1000,520),Image.Resampling.NEAREST).convert('RGB').save(out/'asset_layout_preview.jpg')
print('Software asset layout preview created; category state pixels and bounded weapon images inspected separately from the game.')
