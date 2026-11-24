import json
from pathlib import Path
import UnityPy
from catalog import dependencies, ROOT

TOOLS = Path(__file__).resolve().parent
OUT = TOOLS / "ExtractedUI"
OUT.mkdir(exist_ok=True)
bundle_names = dependencies("Assets/_MX/AddressableAsset/UI/UILobby.prefab")
bundle_names = [n for n in bundle_names if n.startswith(("ui-_mxload", "uis-00_images-_mxload",
               "prologdepengroup-assets-_mx-uis-atlas", "prologdepengroup-assets-_mx-addressableasset-uis"))]
env = UnityPy.load(*(str(ROOT / "AssetBundles" / n) for n in bundle_names))
nodes = {}
transforms = {}
transform_by_go = {}
prefab = []
atlases = []
for obj in env.objects:
    if obj.type.name == "GameObject":
        d = obj.read()
        nodes[(obj.assets_file.name, obj.path_id)] = d.m_Name
    elif obj.type.name == "Transform":
        d = obj.read_typetree()
        transforms[(obj.assets_file.name, obj.path_id)] = d
        transform_by_go[(obj.assets_file.name, d["m_GameObject"]["m_PathID"])] = d

for obj in env.objects:
    if obj.type.name == "Texture2D":
        d = obj.read()
        d.image.save(OUT / (d.m_Name + ".png"))
    elif obj.type.name == "MonoBehaviour":
        d = obj.read_typetree()
        if "mSprites" in d:
            atlases.append(dict(name=d.get("m_Name"), data=d, bundle=obj.assets_file.name))
            material_ref = UnityPy.classes.PPtr(**d["material"], assetsfile=obj.assets_file)
            material = material_ref.read_typetree()
            for key, value in material["m_SavedProperties"]["m_TexEnvs"]:
                if key != "_MainTex":
                    continue
                texture = UnityPy.classes.PPtr(**value["m_Texture"], assetsfile=material_ref.deref().assets_file).read()
                image = texture.image
                for sprite in d["mSprites"]:
                    x, y, w, h = (sprite[k] for k in ("x", "y", "width", "height"))
                    image.crop((x, y, x + w, y + h)).save(OUT / (sprite["name"] + ".png"))
        if "mSpriteName" in d or "mText" in d or "mWidth" in d:
            key = (obj.assets_file.name, d["m_GameObject"]["m_PathID"])
            name = nodes.get(key, str(key))
            transform = transform_by_go.get(key, {})
            prefab.append(dict(name=name, component=d, transform=transform, file=obj.assets_file.name))
(TOOLS / "lobby_hierarchy.json").write_text(json.dumps([
    dict(file=f, id=i, data=d, name=nodes.get((f, d["m_GameObject"]["m_PathID"])))
    for (f, i), d in transforms.items()], indent=2, ensure_ascii=False), encoding="utf-8")
(TOOLS / "lobby_components.json").write_text(json.dumps(prefab, indent=2, ensure_ascii=False), encoding="utf-8")
(TOOLS / "lobby_atlases.json").write_text(json.dumps(atlases, indent=2, ensure_ascii=False), encoding="utf-8")
print("Loaded", len(bundle_names), "bundles;", len(prefab), "UI widgets;", len(atlases), "NGUI atlases")
for a in atlases:
    print("Atlas", a["name"], list(a["data"]))
for p in prefab:
    d = p["component"]
    if p["name"].startswith("Btn_") or any(k in d.get("mSpriteName", "").lower() for k in ("lobby", "mail", "menu", "mission")):
        print(p["name"], d.get("mSpriteName"), d.get("mWidth"), d.get("mHeight"), p["transform"].get("m_LocalPosition"))
