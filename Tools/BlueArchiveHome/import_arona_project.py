import hashlib,json
from pathlib import Path
import sys
from PIL import Image
from prepare_textures import MOD,SOURCES,TOOLS,publish_pma
source=Path(sys.argv[1] if len(sys.argv)>1 else 'E:/Spine3.8.75Pro/arona').resolve()
export=Path(sys.argv[2] if len(sys.argv)>2 else TOOLS/'NewAronaExport').resolve()
target=MOD/'Assets/Tablet/Home/Arona38';target.mkdir(parents=True,exist_ok=True)
files=[source/'Arona2.spine',source/'Arona.json',*sorted((source/'images').glob('*.png'))]
for file in files:
 if not file.is_file():raise FileNotFoundError(file)
image=Image.open(export/'Atlas/arona.png').convert('RGBA');image.save(SOURCES/'arona38.png')
atlas=(export/'Atlas/arona.atlas').read_text('utf8').lstrip('\ufeff\r\n')
if atlas.splitlines()[0] != 'arona.png':raise ValueError('Unexpected atlas page name')
(target/'arona.atlas.bin').write_text(atlas,'utf8')
skel=export/'Arona .skel';(target/'arona.skel.bin').write_bytes(skel.read_bytes())
pma=publish_pma(image,target/'arona.png')
report=dict(source=str(source),spineEditorVersion='3.8.75',project={'path':'Arona2.spine','sha256':hashlib.sha256(files[0].read_bytes()).hexdigest()},sourceFiles=[dict(path=str(f.relative_to(source)).replace('\\','/'),bytes=f.stat().st_size,sha256=hashlib.sha256(f.read_bytes()).hexdigest()) for f in files],export={'method':'Spine 3.8.75 Professional CLI','skeletonPath':'Arona .skel','skeletonBytes':skel.stat().st_size,'skeletonSha256':hashlib.sha256(skel.read_bytes()).hexdigest(),'atlasPath':'Atlas/arona.atlas','texturePath':'Atlas/arona.png','texturePmaBeforeModConversion':False},outputFolder=target.relative_to(MOD).as_posix(),texture=pma,animations=['swing','blink','Turning'],assetNamesUnchanged=True)
(TOOLS/'arona38_import.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),'utf8')
print('Replaced Arona38 from Spine project; skeleton bytes:',skel.stat().st_size,'images:',len(list((source/'images').glob('*.png'))))
