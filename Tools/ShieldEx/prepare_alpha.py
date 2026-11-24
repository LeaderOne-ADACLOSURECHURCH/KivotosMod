"""Publish PMA PNGs for tML's rawimg path, always from preserved originals."""
from pathlib import Path
import hashlib, json
import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]

def prepare():
    report = []
    for group, manifest_path in [('Shield', ROOT/'Tools/Skills7/assets.json'),
                                 ('BlackHole', HERE/'assets.json')]:
        rows = json.loads(manifest_path.read_text(encoding='utf-8'))
        entries = [(row, ROOT/row['output']) for row in rows]
        auxiliary = 'Icon.png' if group == 'Shield' else 'GlowMask.png'
        entries.append((None, ROOT/f'Assets/Skills/{group}'/auxiliary))
        for row, destination in entries:
            source = HERE/'SourceTextures'/group/destination.name
            if not source.exists():
                if row:
                    expected = row.get('source_sha256', row['sha256'])
                    assert hashlib.sha256(destination.read_bytes()).hexdigest() == expected
                source.parent.mkdir(parents=True, exist_ok=True)
                source.write_bytes(destination.read_bytes())
            source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
            if row:
                assert source_hash == row.get('source_sha256', row['sha256'])
            original = np.array(Image.open(source).convert('RGBA'))
            output = original.copy()
            output[:, :, :3] = ((original[:, :, :3].astype(np.uint16) *
                                 original[:, :, 3:4].astype(np.uint16) + 127)//255).astype(np.uint8)
            assert np.array_equal(output[:, :, 3], original[:, :, 3])
            assert np.all(output[:, :, :3] <= output[:, :, 3:4])
            Image.fromarray(output).save(destination)
            digest = hashlib.sha256(destination.read_bytes()).hexdigest()
            if row:
                row['source_sha256'] = source_hash
                row['sha256'] = digest
                row['alpha_format'] = 'premultiplied RGBA8 for tML rawimg'
            report.append(dict(output=destination.relative_to(ROOT).as_posix(),
                               source=source.relative_to(ROOT).as_posix(), sha256=digest,
                               source_sha256=source_hash,
                               original_rgb_above_alpha=int(np.any(original[:, :, :3] > original[:, :, 3:4], axis=2).sum())))
        manifest_path.write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding='utf-8')
    (HERE/'alpha_validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(f'Published {len(report)} PMA textures from originals; alpha unchanged, RGB<=alpha everywhere.')

if __name__ == '__main__':
    prepare()
