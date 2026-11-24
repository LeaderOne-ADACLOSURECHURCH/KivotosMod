from pathlib import Path
from PIL import Image
import json, hashlib
root=Path(__file__).resolve().parents[2]
for row in json.loads((Path(__file__).with_name('assets.json')).read_text(encoding='utf-8')):
 p=root/row['output'];assert hashlib.sha256(p.read_bytes()).hexdigest()==row['sha256']
 im=Image.open(p);assert list(im.size)==row['size'];assert im.mode=='RGBA'
for name,size,columns,count in [('Idle',100,24,552),('Spawn',100,1,15),('Break',250,1,22)]+[(f'Hit{n}',100,1,15) for n in [9,10,11,12,1,2]]:
 im=Image.open(root/f'Assets/Skills/Shield/{name}.png')
 for frame in range(count):
  x=frame%columns*size;y=frame//columns*size
  assert x+size<=im.width and y+size<=im.height,(name,frame)
 assert im.width*im.height==count*size*size
assert Image.open(root/'Assets/Skills/Shield/Icon.png').size==(32,32)
print('PASS: all 9 published PMA sheet hashes/RGBA, 679 frame bounds, row-major idle grid, 32px derived icon.')
