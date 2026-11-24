import json, math
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np

base=Path(__file__).resolve().parent
icons=['Common_Icon_Cafe','Common_Icon_SchaleBilding','Common_Icon_Student','Common_Icon_Edit','Common_Icon_Social','Common_Icon_Craft','Common_Icon_Shop','Common_Icon_Gacha','Common_Bg_Raius5px_Shadow','Common_Icon_SpecialLobby','Common_Icon_Menu_Small']
sheet=Image.new('RGBA',(660,math.ceil(len(icons)/4)*150),'#bccddd')
draw=ImageDraw.Draw(sheet)
for i,name in enumerate(icons):
    im=Image.open(base/'ExtractedUI'/f'{name}.png').convert('RGBA'); im.thumbnail((140,112))
    x=(i%4)*165; y=(i//4)*150
    sheet.alpha_composite(im,(x+(150-im.width)//2,y)); draw.text((x,y+115),name.replace('Common_',''),fill='black')
sheet.save(base/'icons_preview.png')

def pose(stem):
    meshes=json.loads((base/f'{stem}_pose.json').read_text())
    coords=[np.asarray(m['vertices']).reshape(-1,2) for m in meshes]
    allv=np.concatenate(coords); lo=allv.min(axis=0); hi=allv.max(axis=0)
    scale=min(1000/(hi[0]-lo[0]),700/(hi[1]-lo[1]))
    size=(math.ceil((hi[0]-lo[0])*scale),math.ceil((hi[1]-lo[1])*scale))
    canvas=Image.new('RGBA',size); textures={}
    for m,xy in zip(meshes,coords):
        source=base/'SourceTextures'/Path(m['texture']).name
        tex=textures.setdefault(m['texture'],Image.open(source).convert('RGBA'))
        points=np.column_stack(((xy[:,0]-lo[0])*scale,(hi[1]-xy[:,1])*scale))
        uv=np.asarray(m['uvs']).reshape(-1,2)*tex.size
        for tri in np.asarray(m['triangles']).reshape(-1,3):
            dst=points[tri]; src=uv[tri]
            xmin,ymin=np.floor(dst.min(axis=0)).astype(int); xmax,ymax=np.ceil(dst.max(axis=0)).astype(int)
            xmin=max(0,xmin); ymin=max(0,ymin); xmax=min(size[0],xmax); ymax=min(size[1],ymax)
            if xmax<=xmin or ymax<=ymin: continue
            try: coeff=np.linalg.solve(np.column_stack((dst,np.ones(3))),src).T
            except np.linalg.LinAlgError: continue
            coeff[:,2]+=coeff[:,:2]@np.asarray([xmin,ymin])
            tile=tex.transform((xmax-xmin,ymax-ymin),Image.Transform.AFFINE,tuple(coeff.reshape(-1)),Image.Resampling.BILINEAR)
            mask=Image.new('L',tile.size); ImageDraw.Draw(mask).polygon([tuple(p-[xmin,ymin]) for p in dst],fill=255)
            mask=np.asarray(mask,dtype=np.float32)*np.asarray(tile.getchannel('A'),dtype=np.float32)/255*m['color']['a']
            tile.putalpha(Image.fromarray(mask.astype('uint8'))); canvas.alpha_composite(tile,(xmin,ymin))
    canvas.save(base/f'{stem}_preview.png')
pose('arona_workpage_daytime')
if not (base/'arona_spr_preview.png').exists(): pose('arona_spr')
