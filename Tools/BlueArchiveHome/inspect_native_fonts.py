from pathlib import Path
from collections import Counter
import UnityPy

root=Path(r'Z:\YostarGames\BlueArchive_JP\BlueArchive_Data')
env=UnityPy.load(str(root/'resources.assets'))
for obj in env.objects:
    if obj.type.name!='MonoBehaviour': continue
    try:
        d=obj.read_typetree(check_read=False)
        name=d.get('m_Name','')
        script=UnityPy.classes.PPtr(**d['m_Script'],assetsfile=obj.assets_file).read()
        cls=getattr(script,'m_ClassName','')
        if any(k in (name+' '+cls).lower() for k in ('font','localiz')):
            print(obj.path_id,name,cls,list(d),flush=True)
            if 'font' in cls.lower():print(str(d)[:10000],flush=True)
    except Exception: pass
