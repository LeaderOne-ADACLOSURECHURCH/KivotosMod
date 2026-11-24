from pathlib import Path
import sys, json, struct, hashlib
import numpy as np
from PIL import Image

here=Path(__file__).resolve().parent
root=here.parents[1]
records=json.loads((here/'alpha_validation.json').read_text())
bad_before=0
raw_bad_before=0
for row in records:
    src=np.array(Image.open(root/row['source']))
    published=np.array(Image.open(root/row['output']))
    expected=src.copy()
    expected[:,:,:3]=((src[:,:,:3].astype(np.uint16)*src[:,:,3:4].astype(np.uint16)+127)//255).astype(np.uint8)
    assert np.array_equal(published,expected), row['output']
    assert hashlib.sha256((root/row['source']).read_bytes()).hexdigest()==row['source_sha256']
    assert np.all(published[:,:,:3]<=published[:,:,3:4])
    assert np.array_equal(src[:,:,3],published[:,:,3])
    assert np.array_equal(src[src[:,:,3]==255],published[src[:,:,3]==255])
    if Path(row['output']).name=='Break.png':
        # Same integer desaturation as WeaponShieldVisuals.BreakTexture.
        rgb=published[:,:,:3].astype(np.uint16)
        gray=(rgb[:,:,0]*54+rgb[:,:,1]*183+rgb[:,:,2]*19)//256
        softened=(rgb+gray[:,:,None])//2
        assert np.all(softened<=published[:,:,3:4]), 'Break desaturation lost PMA'
    bad_before+=row['original_rgb_above_alpha']
    if len(sys.argv)>1:
        p=Path(row['output'])
        data=(Path(sys.argv[1])/(p.parent.name+'_'+p.stem+'.rawimg')).read_bytes()
        version,w,h=struct.unpack('<iii',data[:12])
        assert (version,w,h)==(1,published.shape[1],published.shape[0])
        raw=np.frombuffer(data[12:],dtype=np.uint8).reshape(h,w,4)
        assert np.array_equal(raw,published), p
        assert np.all(raw[:,:,:3]<=raw[:,:,3:4]), p
        old_data=(Path(sys.argv[1])/('original_'+p.parent.name+'_'+p.stem+'.rawimg')).read_bytes()
        old=np.frombuffer(old_data[12:],dtype=np.uint8).reshape(h,w,4)
        original_raw=src.copy()
        original_raw[src[:,:,3]==0]=0
        assert np.array_equal(old,original_raw), 'Unexpected premultiplication in installed ToRaw'
        raw_bad_before+=int(np.any(old[:,:,:3]>old[:,:,3:4],axis=2).sum())
assert bad_before>0, 'Regression fixtures must include straight-alpha violations'
print(f'PASS: {len(records)} textures exact one-time PMA; original alpha/opaque pixels unchanged; {bad_before} original RGB>Alpha pixels corrected; '+('installed tML rawimg bytes verified.' if len(sys.argv)>1 else 'PNG bytes verified.'))
if len(sys.argv)>1:
    assert raw_bad_before>0
    print(f'REPRODUCED: original real rawimg has {raw_bad_before} RGB>Alpha pixels; fixed real rawimg has zero.')
