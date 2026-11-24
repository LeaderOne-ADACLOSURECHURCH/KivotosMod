"""Extract closed transparent regions and the authored shatter defaults."""
from pathlib import Path
from zipfile import ZipFile
from io import BytesIO
import json
import sys
import cv2
import mapbox_earcut
import numpy as np
from PIL import Image

root = Path(__file__).resolve().parents[3]
downloads = Path.home() / "Downloads"
sys.path.insert(0, str(Path.home()/".codex/tmp/blackhole-aep-libs"))
import py_aep

with ZipFile(downloads/"图片 (3).zip") as archive:
    entry = next(item for item in archive.infolist()
                 if (item.filename if item.flag_bits & 2048 else item.filename.encode("cp437").decode("gbk"))
                 .endswith("/裂纹全.png"))
    alpha = np.asarray(Image.open(BytesIO(archive.read(entry))).convert("RGBA"))[:,:,3]

height,width=alpha.shape
_, labels, stats, centers = cv2.connectedComponentsWithStats((alpha==0).astype(np.uint8),connectivity=4)
shards=[]
for label, stats_row in enumerate(stats[1:], 1):
    x,y,w,h,area=map(int,stats_row)
    if area<20: continue
    crop=(labels[y:y+h,x:x+w]==label).astype(np.uint8)
    contours,hierarchy=cv2.findContours(crop,cv2.RETR_CCOMP,cv2.CHAIN_APPROX_SIMPLE)
    for i,p in enumerate(contours):
        if hierarchy[0,i,3]>=0:continue
        rings=[p]+[q for j,q in enumerate(contours) if hierarchy[0,j,3]==i]
        points=[];ends=[]
        for ring in rings:
            ring=cv2.approxPolyDP(ring,.8,True).reshape(-1,2)
            if len(ring)<3:continue
            points.extend((ring+np.array([x,y])+0.5)/np.array([width,height]))
            ends.append(len(points))
        if not ends:continue
        triangles=mapbox_earcut.triangulate_float32(np.asarray(points,np.float32),np.asarray(ends,np.uint32))
        if not len(triangles):continue
        shards.append(dict(center=(centers[label]/[width,height]).round(6).tolist(),
                           vertices=np.asarray(points).round(6).flatten().tolist(),
                           rings=ends,triangles=triangles.tolist()))

project=py_aep.parse(str(downloads/"破碎模拟.aep")).project
comp=next(c for c in project.compositions if c.name=="破碎")
effect=comp.layers[0].property("ADBE Effect Parade").property("APC Shatter")
def prop(index):
    return effect.property(f"APC Shatter-{index:04}").value

settings=dict(extrusion=prop(16),forceDepth=-0.7,forceRadius=prop(28),strength=prop(30),
              spin=prop(40),randomness=prop(44),viscosity=prop(46),gravity=prop(50),
              gravityZ=-1,lightDepth=prop(106),lightIntensity=prop(100),ambient=prop(110),
              diffuse=prop(112),specular=prop(114),shininess=prop(116))
output=root/"Assets/Skills/MikaDomain/Resonance/Shards.json"
output.write_text(json.dumps(dict(width=comp.width,height=comp.height,settings=settings,shards=shards),
                             separators=(",",":")),encoding="utf-8")
print(f"{len(shards)} shards, {sum(len(s['triangles'])//3 for s in shards)} face triangles")
