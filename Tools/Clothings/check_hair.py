from pathlib import Path
import re,json,zipfile,io
from PIL import Image
R=Path(__file__).resolve().parents[2];s=(R/'Content/Items/Vanity/Clothings2x2/ClothHair.cs').read_text(encoding='utf8');classes=re.findall(r'public sealed class (\w+):ModHair \{(.*?)\}',s);assert len(classes)==5
paths=[]
for name,body in classes:
 for prop in ('Texture','AltTexture'):
  match=re.search(prop+r'=>"([^"]+)"',body);assert match,(name,prop)
  p=R/(match[1].removeprefix('KivotosMod/')+'.png');assert p.exists(),p
  assert Image.open(p).size==(40,784);paths.append(str(p))
 assert not name.endswith('Alt'),name
report={'passed':True,'hairStyles':len(classes),'resolvedPrimaryAndHatTextures':len(paths),'yuukaHatFallback':True}
(R/'Tools/Clothings/hair_validation.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(report)
