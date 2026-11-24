import json
from pathlib import Path
import UnityPy
from prepare_textures import publish_pma, SOURCES

ROOT = Path(r"Z:\YostarGames\BlueArchive_JP\BlueArchive_Data\StreamingAssets\AssetBundles")
TOOLS = Path(__file__).resolve().parent
MOD = TOOLS.parents[1]
UI = TOOLS / "ExtractedUI"
UI.mkdir(parents=True, exist_ok=True)
records = []
patterns = ["uis-00_images-lobby-*.bundle", "uis-00_images-bigbtn-*.bundle",
            "uis-01_common-00*.bundle", "uis-01_common-02*.bundle"]
paths = sorted({p for pattern in patterns for p in ROOT.glob(pattern)})
for path in paths:
    env = UnityPy.load(str(path))
    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        data = obj.read()
        dest = UI / (data.m_Name + ".png")
        data.image.save(dest)
        records.append(dict(name=data.m_Name, width=data.m_Width, height=data.m_Height,
                            bundle=path.name, path_id=obj.path_id, file=str(dest)))
    print("UI", path.name, flush=True)

for pattern, folder in [
    ("prologdepengroup-assets-_mx-spinecharacters-arona*.bundle", "Arona"),
    ("assets-_mx-spinebackground-arona_workpage_daytime-_mxdependency-text*.bundle", "Classroom")]:
    out = MOD / "Assets/Tablet/Home" / folder
    out.mkdir(parents=True, exist_ok=True)
    for path in ROOT.glob(pattern):
        env = UnityPy.load(str(path))
        for obj in env.objects:
            if obj.type.name == "Texture2D":
                data = obj.read()
                image = data.image
                SOURCES.mkdir(exist_ok=True)
                image.save(SOURCES / (data.m_Name + ".png"))
                publish_pma(image, out / (data.m_Name + ".png"))
                records.append(dict(name=data.m_Name, width=data.m_Width, height=data.m_Height,
                                    bundle=path.name, file=str(out / (data.m_Name + ".png"))))
            elif obj.type.name == "TextAsset":
                data = obj.read()
                raw = data.m_Script
                if isinstance(raw, str):
                    raw = raw.encode("utf-8", "surrogateescape")
                suffix = ".bin" if data.m_Name.endswith((".skel", ".atlas")) else ".txt"
                (out / (data.m_Name + suffix)).write_bytes(raw)
                print("Spine", data.m_Name, len(raw), repr(raw[:70]), flush=True)

(TOOLS / "asset_manifest.json").write_text(json.dumps(records, indent=2, ensure_ascii=False), encoding="utf-8")
print("Extracted", len(records), "textures/assets", flush=True)
