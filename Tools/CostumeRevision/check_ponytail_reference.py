"""Compare original hair pixels against the two supplied editor reference images."""
from pathlib import Path
from PIL import Image
import numpy as np,json
TOOLS=Path(__file__).resolve().parent;ROOT=TOOLS.parents[1]
alignment=json.loads((TOOLS/'ponytail_alignment.json').read_text('utf8'));report={}
references={'Mutsuki':'1f8226a5-b5dd-4c68-a458-57e3700cc2b6','Nozomi':'dc28b8f7-ca12-4394-a5ea-51b78ad619a5'}
for name,identifier in references.items():
    reference=np.asarray(Image.open(Path('C:/Users/SUNBIN~1/AppData/Local/Temp')/f'codex-clipboard-{identifier}.png').convert('RGB')).astype(float)
    hair=np.asarray(Image.open(ROOT/f'Content/Items/Vanity/{name}/{name}BackHair.png').convert('RGBA'))[:56]
    yy,xx=np.where(hair[:,:,3]>0);rgb=hair[yy,xx,:3].astype(float)
    tx,ty,sx,sy=alignment[name]['bodyTopAndPixelPitch']
    def score(dx):
        x=np.rint(tx+(xx+dx+.5)*sx).astype(int);y=np.rint(ty+(yy+.5)*sy).astype(int)
        return float(np.mean(np.max(abs(reference[y,x]-rgb),axis=1)<25))
    correct,previous=score(-16),score(-8)
    assert correct>(.90 if name=='Mutsuki' else .74) and correct>previous+.25,(name,correct,previous)
    report[name]={'matchedFraction':correct,'previousCenteredFraction':previous,'offset':[-16,0]}
(TOOLS/'reference_validation.json').write_text(json.dumps({'passed':True,'students':report},indent=2),encoding='utf8')
print(json.dumps(report,indent=2))
