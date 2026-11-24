"""PMA runtime-byte software reference using extracted AEP parameters, not AE rendering."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib, json, math
import numpy as np

here=Path(__file__).resolve().parent
root=here.parents[1]
folder=root/'Assets/Skills/BlackHole'
project=json.loads((here/'AE/project_inspection.json').read_text(encoding='utf-8'))
w,h=project['comp']['width'],project['comp']['height']
for row in json.loads((here/'assets.json').read_text(encoding='utf-8')):
    path=root/row['output']
    assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256']
    assert list(Image.open(path).size)==row['size']

def rgba(name):
    result=np.array(Image.open(folder/(name+'.png')),dtype=np.float32)/255
    assert np.all(result[:,:,:3]<=result[:,:,3:]+1e-6)
    return result

def place(image,x,y):
    result=np.zeros((h,w,4),np.float32)
    ix,iy=math.floor(x),math.floor(y)
    fx,fy=x-ix,y-iy
    ih,iw=image.shape[:2]
    for dx,wx in [(0,1-fx),(1,fx)]:
        for dy,wy in [(0,1-fy),(1,fy)]:
            xx,yy=ix+dx,iy+dy
            l,r=max(0,xx),min(w,xx+iw)
            t,b=max(0,yy),min(h,yy+ih)
            if l<r and t<b and wx*wy:
                result[t:b,l:r]+=image[t-yy:b-yy,l-xx:r-xx]*(wx*wy)
    return result

def over(dst,src):
    return src+dst*(1-src[:,:,3:])

def property(layer,name):
    return next(p for p in layer['properties'] if p['match_name']==name)

def at(layer,frame):
    prop=property(layer,'ADBE Position');keys=prop['keyframes']
    if not keys:return prop['value'][:2]
    a,b=keys
    f=min(1,max(0,(frame-a['frame_time'])/(b['frame_time']-a['frame_time'])))
    return [a['value'][k]+(b['value'][k]-a['value'][k])*f for k in range(2)]

def layer_image(name,layer,frame):
    pos=at(layer,frame);anchor=property(layer,'ADBE Anchor Point')['value']
    return place(rgba(name),pos[0]-anchor[0],pos[1]-anchor[1])

layers=project['layers']
core=layer_image('Core',layers[5],0)
band=layer_image('Band',layers[4],0)
glow=layer_image('Glow',layers[3],0)
mask=rgba('GlowMask')[:,:,3:]
assert mask.shape==(h,w,1)
assert np.max(np.abs(mask-glow[:,:,3:]))<=.5/255+1e-6, 'AE matte alignment differs'
base=over(core,band)
base[:,:,:3]+=glow[:,:,:3]
base[:,:,3:]=glow[:,:,3:]+base[:,:,3:]*(1-glow[:,:,3:])
frames=[0,102,200,301,461,899]
sheet=Image.new('RGB',(640*3,435*2))
for panel,frame in enumerate(frames):
    composed=base.copy()
    for i in [2,1,0]:
        if frame<round(layers[i]['in_point']*30):continue
        clipped=layer_image('Spectrum',layers[i],frame)*mask
        assert not np.any(clipped[mask[:,:,0]==0])
        composed=over(composed,clipped)
    assert np.isfinite(composed).all()
    # Composite once over a dark neutral background, retaining float32 until SDR export.
    bg=np.array([.012,.015,.035],np.float32)
    rgb=composed[:,:,:3]+bg*(1-composed[:,:,3:])
    image=Image.fromarray((np.clip(rgb,0,1)*255).astype('uint8')).resize((640,409))
    x,y=panel%3*640,panel//3*435
    sheet.paste(image,(x,y+26))
    ImageDraw.Draw(sheet).text((x+8,y+7),f'AEP frame {frame} / {frame/30:.3f}s - software reference',fill='white')
sheet.save(here/'blackhole_preview.jpg',quality=92)
print('PASS: runtime PMA hashes; AEP 1280x817 matte offset/subpixel alignment; per-layer matte, layer order and finite float32 composition at six AEP times (software reference only).')
