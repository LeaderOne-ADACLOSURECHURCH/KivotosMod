"""Check the measured editor-canvas alignment against the user's screenshot.

reference_canvas_crop.png is the original screenshot region (810,150)-(1340,750),
without resizing. Editor pixels are enlarged by 7.7 and its canvas starts at
(29,16) within this crop. RGB tolerance accounts for the screenshot's display
color conversion; it does not change the imported source textures.
"""
from pathlib import Path
from PIL import Image
import json, numpy as np

tools = Path(__file__).resolve().parent
root = tools.parents[1]
assets = root/'Content/Items/Vanity/Mika'
reference = np.asarray(Image.open(tools/'reference_canvas_crop.png').convert('RGB'))

def sample_score(name, offset):
    image = np.asarray(Image.open(assets/(name+'.png')).convert('RGBA'))
    image = image[:74 if name in ['MikaHalo','MikaDecorWings'] else 56]
    yy,xx = np.where(image[:,:,3]>0)
    x = np.rint(29+(xx+offset[0]+.5)*7.7).astype(int)
    y = np.rint(16+(yy+offset[1]+.5)*7.7).astype(int)
    actual = reference[y,x].astype(int)
    original = image[yy,xx,:3].astype(int)
    return float(np.mean(np.max(np.abs(actual-original),axis=1)<=20))

head = sample_score('MikaHead_Head',(13,18))
halo = sample_score('MikaHalo',(0,0))
wings = sample_score('MikaDecorWings',(0,0))
previous_wings = sample_score('MikaDecorWings',(-10,-7))
assert head>.98 and halo>.98, 'Native body/halo offset does not match the reference'
# The reference's foreground body and arms cover some wing pixels.
assert wings>.80 and wings>previous_wings+.50, 'Wings have not returned to the reference canvas position'
report = dict(passed=True, screenshotCrop=[810,150,1340,750], editorPixelScale=7.7,
    nativeBodyOffset=[13,18], decorativeWingOffsetFromBody=[-13,-18], flightWingOffsetFromBody=[-13,-12],
    perChannelColorTolerance=20, headMatchingFraction=head, haloMatchingFraction=halo,
    wingMatchingFraction=wings, previousWrongWingMatchingFraction=previous_wings)
(tools/'reference_alignment.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report,indent=2))
