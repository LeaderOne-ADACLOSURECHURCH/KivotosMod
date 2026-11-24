import json
from pathlib import Path
import UnityPy
from catalog import ROOT, dependencies

tools=Path(__file__).resolve().parent
names=[n for n in dependencies('Assets/_MX/AddressableAsset/UI/UILobby.prefab') if n.startswith(('ui-_mxload','uis-00_images-_mxload','uis-00_images-banner','prologdepengroup-assets-_mx-uis-atlas','prologdepengroup-assets-_mx-addressableasset-uis'))]
env=UnityPy.load(*(str(ROOT/'AssetBundles'/n) for n in names))
layout=json.loads((tools/'lobby_layout.json').read_text(encoding='utf8'))
report=[]
for c in layout:
    ref=c['component'].get('mTexture')
    if not ref or not ref['m_PathID']: continue
    assetfile=next((o.assets_file for o in env.objects if o.assets_file.name==c['file']),None)
    try:
        data=UnityPy.classes.PPtr(**ref,assetsfile=assetfile).read()
        if not hasattr(data,'image'): continue
        data.image.save(tools/'ExtractedUI'/(data.m_Name+'.png'))
        report.append(dict(path=c['path'],name=data.m_Name,width=data.m_Width,height=data.m_Height))
    except Exception as e:
        report.append(dict(path=c['path'],error=str(e)))
(tools/'direct_textures.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
for r in report:
    if 'error' not in r and ('/Quest/Able/Icon' in r['path'] or '/Right/Btn_Event/' in r['path']): print(r)
