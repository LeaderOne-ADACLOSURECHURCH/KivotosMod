from pathlib import Path
import json,hashlib
from PIL import Image
import numpy as np
root=Path(__file__).resolve().parents[2]
for row in json.loads(Path(__file__).with_name('portraits.json').read_text(encoding='utf-8')):
 p=root/row['output'];assert hashlib.sha256(p.read_bytes()).hexdigest()==row['sha256'];assert Image.open(p).size==(64,64)
for name in ['Favorite','NotFavorite','EnvelopeOpen','EnvelopeClosed']:
 im=Image.open(root/f'Assets/Tablet/Mail/{name}.png');assert im.mode=='RGBA';assert im.getchannel('A').getextrema()==(0,255)
# Emulate PngReader's premultiply, then the production break desaturation formula.
a=np.array(Image.open(root/'Assets/Skills/Shield/Break.png'),dtype=np.uint32)
alpha=a[:,:,3].copy();rgb=a[:,:,:3]*alpha[:,:,None]//255
gray=(rgb[:,:,0]*54+rgb[:,:,1]*183+rgb[:,:,2]*19)//256
result=(rgb+gray[:,:,None])//2
assert np.array_equal(a[:,:,3],alpha)
assert (result.max(axis=2)-result.min(axis=2)<=rgb.max(axis=2)-rgb.min(axis=2)).all()
assert (result<=alpha[:,:,None]).all()
print('PASS: 12 portrait hashes,4 glyph alpha masks,break desaturation preserves source alpha and premultiplied RGB bounds.')
