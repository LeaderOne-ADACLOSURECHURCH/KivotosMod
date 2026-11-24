"""Vendor a pinned official Spine 4.2 C# parser with an isolated namespace."""
import concurrent.futures
import json
from pathlib import Path
import urllib.request

def fetch(url):
    request = urllib.request.Request(url, headers={"User-Agent": "KivotosMod-local-tools"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read()

root = Path(__file__).resolve().parents[2]
repo = "https://api.github.com/repos/EsotericSoftware/spine-runtimes"
commit = json.loads(fetch(repo + "/commits/4.2"))["sha"]
tree = json.loads(fetch(repo + "/git/trees/" + commit + "?recursive=1"))["tree"]
paths = [r["path"] for r in tree if r["path"].startswith("spine-csharp/src/") and r["path"].endswith(".cs")]

def save(path):
    text = fetch("https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/" + commit + "/" + path).decode("utf-8-sig")
    text = text.replace("namespace Spine", "namespace KivotosMod.Runtime.Spine42")
    text = text.replace("namespace SharpJson", "namespace KivotosMod.Runtime.Spine42.SharpJson")
    text = text.replace("using Spine", "using KivotosMod.Runtime.Spine42")
    text = text.replace("global::Spine.", "global::KivotosMod.Runtime.Spine42.")
    dest = root / "ThirdParty/Spine42" / path.removeprefix("spine-csharp/src/")
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(text, encoding="utf-8")

with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
    list(pool.map(save, paths))
notice = root / "ThirdParty/Spine42/PROVENANCE.txt"
notice.write_text("Source: https://github.com/EsotericSoftware/spine-runtimes\nBranch: 4.2\nCommit: " + commit +
                  "\nThe namespace was changed to KivotosMod.Runtime.Spine42 to avoid collisions with other mods.\n"
                  "Copyright and license notices are preserved in every source file.\n", encoding="utf-8")
print("Vendored", len(paths), "files at", commit)
