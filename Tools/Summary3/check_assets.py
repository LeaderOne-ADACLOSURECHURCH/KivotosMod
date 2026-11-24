from pathlib import Path
import json,zipfile,hashlib
from PIL import Image
root=Path(__file__).resolve().parents[2]
manifest=json.loads((root/'Tools/Summary3/import_manifest.json').read_text(encoding='utf-8'))
with zipfile.ZipFile(r'C:\Users\Sunbinbin\Downloads\UI.zip') as z:
 originals={}
 for n in z.namelist():
  if n.startswith('UI/') and n.endswith('.png'):
   try:name=n.encode('cp437').decode('utf-8')
   except:name=n
   originals[name]=z.read(n)
 for item in manifest:
  data=(root/item['output']).read_bytes()
  assert hashlib.sha256(data).hexdigest()==item['sha256'],item['output']
  source=originals.get(item['source'])
  if source is None:source=Path(item['source']).read_bytes()
  assert data==source,item['output']
  im=Image.open(root/item['output']);assert im.mode=='RGBA',item['output']
assert len(manifest)==11
print('PASS: all 10 supplied UI textures and attached background imported byte-for-byte; dimensions and RGBA verified.')
