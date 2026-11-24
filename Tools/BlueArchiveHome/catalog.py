import base64
import json
import struct
from pathlib import Path

ROOT = Path(r"Z:\YostarGames\BlueArchive_JP\BlueArchive_Data\StreamingAssets")
CATALOG = json.loads((ROOT / "catalog_Remote.json").read_text())
entries = base64.b64decode(CATALOG["m_EntryDataString"])
buckets = base64.b64decode(CATALOG["m_BucketDataString"])
keys = base64.b64decode(CATALOG["m_KeyDataString"])
rows = [struct.unpack_from("<7i", entries, offset) for offset in range(4, len(entries), 28)]
bucket_rows = []
pos = 4
for _ in range(struct.unpack_from("<i", buckets)[0]):
    offset, count = struct.unpack_from("<2i", buckets, pos)
    pos += 8
    ids = struct.unpack_from("<" + str(count) + "i", buckets, pos)
    pos += count * 4
    bucket_rows.append((offset, ids))

def dependencies(address):
    index = CATALOG["m_InternalIds"].index(address)
    row = next(r for r in rows if r[0] == index)
    visited = set()
    def walk(row):
        if row[2] < 0:
            return
        for entry in bucket_rows[row[2]][1]:
            if entry in visited:
                continue
            visited.add(entry)
            walk(rows[entry])
    walk(row)
    return [CATALOG["m_InternalIds"][rows[i][0]].split("\\")[-1] for i in sorted(visited)]

if __name__ == "__main__":
    for address in ["Assets/_MX/AddressableAsset/UI/UILobby.prefab", "Assets/_MX/AddressableAsset/UI/UIWork.prefab"]:
        print(address)
        print("\n".join(dependencies(address)))
