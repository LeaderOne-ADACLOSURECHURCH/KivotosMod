from pathlib import Path
import sys,json,struct,math,zipfile,io
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
sys.path.insert(0,str(Path.home()/'.codex/tmp/blackhole-aep-libs'))
import py_aep
R=Path(__file__).resolve().parents[2];O=R/'Assets/Skills/MikaDomain';O.mkdir(parents=True,exist_ok=True)
p=py_aep.parse(str(Path.home()/'Downloads/未花EX (2).aep')).project
comp=next(c for c in p.compositions if c.name=='黑洞底图');W,H=comp.width,comp.height
images={};textures={};paintlog=[]
def publish(im,name):
 a=np.array(im.convert('RGBA'));a[:,:,:3]=((a[:,:,:3].astype(np.uint16)*a[:,:,3:4]+127)//255).astype(np.uint8)
 Image.fromarray(a).save(O/(name+'.png'))
with zipfile.ZipFile(Path.home()/'Downloads/图片 (1).zip') as z:
 for e in z.infolist():
  name=e.filename.encode('cp437').decode('gbk').split('/')[-1]
  im=Image.open(io.BytesIO(z.read(e))).convert('RGBA');images[name]=im
  tex='T'+str(len(textures)).zfill(2);textures[name]=dict(texture=tex,width=im.width,height=im.height);publish(im,tex)
def painted(layer,t=0):
 im=images[Path(str(layer.source.file)).name].copy()
 for e in layer.property('ADBE Effect Parade'):
  if e.match_name!='ADBE Paint':continue
  for atom in e.property('ADBE Paint Group'):
   # py-aep decodes this two-number property as source-scaled coordinates.
   raw=atom.property('ADBE Paint Duration').value;start,duration=raw[0]/im.width,raw[1]/im.height
   if not start<=t<start+duration:continue
   shape=atom.property('ADBE Paint Shape').value;pos=atom.property('ADBE Paint Transform').property('ADBE Paint Position').value
   props={q.match_name:q.value for q in atom.property('ADBE Paint Properties') if hasattr(q,'value')}
   diameter=props.get('ADBE Paint Diameter',13);hardness=props.get('ADBE Paint Hardness',0)/100
   erase=Image.new('L',im.size);draw=ImageDraw.Draw(erase)
   points=[(pos[0]+v[0]*im.width,pos[1]+v[1]*im.height) for v in shape.vertices]
   if points:
    draw.line(points,fill=255,width=max(1,round(diameter)),joint='curve')
    for x,y in [points[0],points[-1]]:draw.ellipse((x-diameter/2,y-diameter/2,x+diameter/2,y+diameter/2),fill=255)
   if hardness<1:erase=erase.filter(ImageFilter.GaussianBlur(diameter*(1-hardness)/6))
   alpha=np.asarray(im.getchannel('A')).astype(np.uint16)*(255-np.asarray(erase).astype(np.uint16))//255
   im.putalpha(Image.fromarray(alpha.astype('uint8')))
   paintlog.append(dict(layer=layer.name,stroke=atom.name,start=start,duration=duration,diameter=diameter,points=len(points)))
 return im
maskcache={}
def masktex(l):
 key=l.index
 if key in maskcache:return maskcache[key]
 im=painted(l);name='Mask'+str(key);publish(im,name);maskcache[key]=name;return name
used=[l for l in comp.layers if l.enabled and getattr(l,'source',None) is not None and getattr(l.source,'file',None) and l.name!='星空背景.png']
records=[]
with (O/'Motion.bin').open('wb') as f:
 f.write(struct.pack('<iii',len(used),901,6))
 for l in used:
  tex=textures[Path(str(l.source.file)).name];m=l.track_matte_layer;tint=[0,0,0,0,1,1,1,0];fx=[0,0,0,0]
  for e in l.property('ADBE Effect Parade'):
   d={q.match_name:q.value for q in e if hasattr(q,'value')}
   if e.match_name=='ADBE Tint':tint=list(e.property(0).value[:3])+[e.property(2).value/100]+list(e.property(1).value[:3])+[0]
   if e.match_name=='ADBE HUE SATURATION':fx[:2]=[float(d.get('ADBE HUE SATURATION-0004',0)),float(d.get('ADBE HUE SATURATION-0005',0))/100]
   if e.match_name=='ADBE Brightness & Contrast 2':fx[2:]=[float(e.property(0).value)/100,float(e.property(1).value)/100]
  records.append(dict(index=l.index,name=l.name,**tex,anchor=l.transform.anchor_point.value[:2],start=l.in_point,end=l.out_point,
   blend=int(l.blending_mode),matte=int(l.track_matte_type),mask=masktex(m) if m else None,
   mask_index=m.index if m else -1,mask_opacity=m.transform.opacity.value/100 if m else 1,
   mask_scale=m.transform.scale.value[:2] if m else [100,100],tint=tint,fx=fx))
  for frame in range(901):
   t=frame/30;pos=l.transform.position.value_at_time(t);s=l.transform.scale.value_at_time(t);ang=l.transform.property('ADBE Rotate Z').value_at_time(t)
   if int(l.auto_orient)==4213:
    a=l.transform.position.value_at_time(t-.001);b=l.transform.position.value_at_time(t+.001);ang+=math.degrees(math.atan2(b[1]-a[1],b[0]-a[0]))
   f.write(struct.pack('<6f',pos[0],pos[1],s[0]/100,s[1]/100,math.radians(ang),max(0,min(1,l.transform.opacity.value_at_time(t)/100))))
  print('Layer',l.index,flush=True)
(O/'Scene.json').write_text(json.dumps(dict(width=W,height=H,duration=30,frames=901,layers=records,textures=textures),ensure_ascii=False,indent=2),encoding='utf-8')
stars=[]
front=next(c for c in p.compositions if c.name=='星光带前');null=next(l for l in reversed(list(front.layers)) if l.transform.position.keyframes)
keys=null.transform.position.keyframes;v=[(keys[-1].value[i]-keys[0].value[i])/(keys[-1].time-keys[0].time) for i in range(2)]
for cname,back in [('星光带前',False),('星光带后',True)]:
 c=next(c for c in p.compositions if c.name==cname)
 paired_bottoms=set()
 for i,l in enumerate(c.layers):
  if not getattr(l,'source',None) or not getattr(l.source,'file',None):continue
  name=Path(str(l.source.file)).name
  if name not in ['星1.png','星2.png','星3.png']:continue
  expected={'星1.png':'星发光底部.png','星2.png':'球发光底部.png','星3.png':'心发光底部.png'}[name]
  glow=c.layers[i+1]
  if not getattr(glow.source,'file',None) or Path(str(glow.source.file)).name!=expected:
   # The source has one reversed leading pair. Never bind another body as a bottom.
   glow=c.layers[i-1] if i>0 else None
  assert glow is not None and getattr(glow.source,'file',None) and Path(str(glow.source.file)).name==expected,(cname,i,expected)
  assert glow.index not in paired_bottoms,(cname,i,'duplicate glow bottom')
  paired_bottoms.add(glow.index);params={};ef={}
  for e in l.property('ADBE Effect Parade'):
   d={q.match_name:q.value for q in e if hasattr(q,'value')};ef.update(d)
  stars.append(dict(back=back,body_layer=l.index,glow_layer=glow.index,
   effects={e.match_name:{q.match_name:q.value for q in e if hasattr(q,'value')} for e in l.property('ADBE Effect Parade')},
   glow_effects={e.match_name:{q.match_name:q.value for q in e if hasattr(q,'value')} for e in glow.property('ADBE Effect Parade')},
   kind=2 if name=='星1.png' else 1 if name=='星3.png' else 0,texture=textures[name]['texture'],glow=textures[Path(str(glow.source.file)).name]['texture'],
   position=[l.transform.position.value[0]+(W-c.width)/2,l.transform.position.value[1]+(H-c.height)/2],scale=l.transform.scale.value[:2],angle=l.transform.property('ADBE Rotate Z').value,
   glowPosition=[glow.transform.position.value[0]+(W-c.width)/2,glow.transform.position.value[1]+(H-c.height)/2],glowScale=glow.transform.scale.value[:2],glowAngle=glow.transform.property('ADBE Rotate Z').value,
   hue=float(ef.get('ADBE HUE SATURATION-0004',0)),saturation=float(ef.get('ADBE HUE SATURATION-0005',0))/100,
   threshold=float(ef.get('ADBE Glo2-0002',220))/255,radius=float(ef.get('ADBE Glo2-0003',65)),intensity=float(ef.get('ADBE Glo2-0004',1.31))))
control=next(c for c in p.compositions if c.name=='合成后控制')
(O/'Stars.json').write_text(json.dumps(dict(velocity=v,mask=textures['光谱2.png']['texture'],maskScale=control.layers[1].transform.scale.value[:2],stars=stars),ensure_ascii=False,indent=2),encoding='utf-8')
# Independent paint repro records snapshots at each stroke boundary, not video frames.
demo=py_aep.parse(str(Path.home()/'Downloads/实现绘制功能.aep')).project.compositions[0]
for t in [0,1,3,4.9]:publish(painted(demo.layers[0],t),'PaintDemo'+str(t).replace('.','_'))
(R/'Tools/MikaDomain/paint_records.json').write_text(json.dumps(paintlog,ensure_ascii=False,indent=2),encoding='utf-8')
print('DONE',len(records),'layers',len(stars),'stars','V=',v)
import runpy
runpy.run_path(str(R/'Tools/MikaDomain/Revision2/build_glow.py'),run_name='__main__')
