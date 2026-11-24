import json
from pathlib import Path
root = Path(__file__).resolve().parent
hierarchy = json.loads((root / "lobby_hierarchy.json").read_text(encoding="utf-8"))
transforms = {(x["file"], x["id"]): x for x in hierarchy}
def chain(file, data):
    parent = transforms.get((file, data.get("m_Father", {}).get("m_PathID")))
    if parent:
        return chain(file, parent["data"]) + [parent["name"] or ""]
    return []
widgets = json.loads((root / "lobby_components.json").read_text(encoding="utf-8"))
for w in widgets:
    w["path"] = "/".join(chain(w["file"], w["transform"]) + [w["name"]])
(root / "lobby_layout.json").write_text(json.dumps(widgets, indent=2, ensure_ascii=False), encoding="utf-8")
for w in widgets:
    d = w["component"]
    if not d.get("mSpriteName"):
        continue
    p = w["path"]
    if any(t in p.lower() for t in ("bottom", "top", "left", "right", "menu")) and not any(t in p.lower() for t in ("eventelement", "tutorial", "particle", "popup")):
        print(p, ":", d["mSpriteName"], d.get("mWidth"), d.get("mHeight"), w["transform"].get("m_LocalPosition"))
