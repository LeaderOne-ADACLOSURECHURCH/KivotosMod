import json
from pathlib import Path
import UnityPy

ROOT = Path(r"Z:\YostarGames\BlueArchive_JP\BlueArchive_Data\StreamingAssets\AssetBundles")
OUT = Path(__file__).resolve().parent
records = []
for path in sorted(ROOT.glob("*.bundle")):
    if not (path.name.startswith("ui-_mxload") or path.name.startswith("ui-uiwork") or "spinecharacters-arona" in path.name or
            "spinebackground-arona_workpage_daytime" in path.name):
        continue
    try:
        env = UnityPy.load(str(path))
        entries = list(env.container.keys())
        print(path.name, len(entries), "entries", flush=True)
        named = []
        for obj in env.objects:
            if obj.type.name in ("TextAsset", "Texture2D", "Sprite"):
                data = obj.read()
                named.append(dict(type=obj.type.name, name=data.m_Name, path_id=obj.path_id))
        records.append(dict(bundle=path.name, entries=entries, assets=named))
    except Exception as exc:
        records.append(dict(bundle=path.name, error=str(exc)))
(OUT / "inventory.json").write_text(json.dumps(records, indent=2, ensure_ascii=False), encoding="utf-8")
for r in records:
    keys = [k for k in r.get("entries", []) if "lobby" in k.lower() or "work" in k.lower() or "arona" in k.lower()]
    if keys:
        print(json.dumps(dict(bundle=r["bundle"], entries=keys), ensure_ascii=False), flush=True)
print("Scanned", len(records), "bundles", flush=True)
