"""Publish straight-alpha Unity exports as PMA textures for tML's rawimg reader.

Always start from original exports, never multiply an already published texture.
Opaque pixels and all alpha bytes remain unchanged. Source exports stay in Tools.
"""
import hashlib
import json
from pathlib import Path
import numpy as np
from PIL import Image
import UnityPy

TOOLS = Path(__file__).resolve().parent
MOD = TOOLS.parents[1]
BUNDLES = Path(r'Z:\YostarGames\BlueArchive_JP\BlueArchive_Data\StreamingAssets\AssetBundles')
TARGET = MOD / 'Assets/Tablet/Home'
SOURCES = TOOLS / 'SourceTextures'

def publish_pma(image, destination):
    rgba = np.array(image.convert('RGBA'), dtype=np.uint8)
    original_alpha = rgba[:, :, 3].copy()
    original_opaque = rgba[original_alpha == 255].copy()
    rgb = rgba[:, :, :3].astype(np.uint16)
    alpha = rgba[:, :, 3:4].astype(np.uint16)
    rgba[:, :, :3] = ((rgb * alpha + 127) // 255).astype(np.uint8)
    assert np.array_equal(rgba[:, :, 3], original_alpha)
    assert np.array_equal(rgba[original_alpha == 255], original_opaque)
    assert np.all(rgba[:, :, :3] <= rgba[:, :, 3:4])
    destination.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgba).save(destination)
    return dict(path=destination.relative_to(MOD).as_posix(),
                pixels=int(rgba.shape[0] * rgba.shape[1]),
                translucentPixels=int(np.count_nonzero((original_alpha > 0) & (original_alpha < 255))),
                sha256=hashlib.sha256(destination.read_bytes()).hexdigest())

def prepare():
    SOURCES.mkdir(exist_ok=True)
    records = json.loads((TOOLS / 'asset_manifest.json').read_text(encoding='utf8'))
    spine_names = {'arona_spr', 'arona_workpage_daytime', 'arona_workpage_daytime2'}
    spine_records = {r['name']: r for r in records if r['name'] in spine_names}
    for bundle in {r['bundle'] for r in spine_records.values()}:
        env = UnityPy.load(str(BUNDLES / bundle))
        for obj in env.objects:
            if obj.type.name != 'Texture2D': continue
            texture = obj.read()
            if texture.m_Name in spine_names:
                texture.image.save(SOURCES / (texture.m_Name + '.png'))

    # The default prefab's event images are supplied at runtime in Unity. Keep the
    # three selected local example banners as original, straight-alpha exports too.
    banners = {'Lobby_Gacha_Banner_210204_1', 'Event_Banner_841_Jp', 'Event_Banner_842_Jp'}
    env = UnityPy.load(str(BUNDLES / 'uis-00_images-banner-_mxload-textures-2025-07-02_assets_all_2445240650.bundle'))
    for obj in env.objects:
        if obj.type.name != 'Texture2D': continue
        texture = obj.read()
        if texture.m_Name in banners:
            texture.image.save(TOOLS / 'ExtractedUI' / (texture.m_Name + '.png'))

    result = []
    for destination in sorted(TARGET.rglob('*.png')):
        source = (SOURCES / (destination.parent.name.lower() + '.png')) if destination.parent.name in {'Arona38', 'Sora38', 'Plana38'} else (TOOLS / 'ExtractedUI' if destination.parent.name == 'UI' else SOURCES) / destination.name
        if not source.exists(): raise FileNotFoundError(f'Missing original export: {source}')
        result.append(publish_pma(Image.open(source), destination))
    report = {'format': 'premultiplied RGBA8 for tModLoader rawimg', 'textures': result,
              'alphaPreserved': True, 'opaquePixelsPreserved': True, 'passed': True}
    (TOOLS / 'alpha_validation.json').write_text(json.dumps(report, indent=2), encoding='utf8')
    print(f'Published and validated {len(result)} PMA textures; alpha and opaque pixels unchanged.')

if __name__ == '__main__': prepare()
