"""Keep the user's PCM voices byte-identical and record volume-based heavy groups."""
from pathlib import Path
import zipfile,io,wave,json,re,hashlib
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
GROUPS={'诺亚':'Noa','日奈':'Hina','圣亚':'Seia','优香':'Yuuka','渚':'Nagisa'}
report={}
with zipfile.ZipFile('C:/Users/Sunbinbin/Downloads/受伤语音.zip') as z:
 for f in z.infolist():
  if f.is_dir() or '__MACOSX' in f.filename:continue
  name=f.filename
  if not f.flag_bits&0x800:
   for enc in ['utf8','gbk']:
    try:name=f.filename.encode('cp437').decode(enc);break
    except UnicodeError:pass
  path=Path(name);student=GROUPS[path.parent.name];number=int(re.search(r'\d+',path.stem).group())
  data=z.read(f);out=ROOT/'Assets/Sounds/Students'/student;out.mkdir(parents=True,exist_ok=True)
  (out/f'Hurt{number}.wav').write_bytes(data)
  with wave.open(io.BytesIO(data)) as w:
   assert (w.getnchannels(),w.getsampwidth(),w.getframerate())==(2,2,44100)
   samples=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
   active=samples[abs(samples)>.005]
   report.setdefault(student,[]).append({'source':name,'variant':number,'seconds':w.getnframes()/w.getframerate(),
     'activeRmsDb':float(20*np.log10(np.sqrt(np.mean(active**2)))),'sha256':hashlib.sha256(data).hexdigest()})
for student,voices in report.items():
 # Reserve the loudest voice for larger hits; quieter voices remain available normally.
 loud=max(voices,key=lambda v:v['activeRmsDb'])['variant']
 for voice in voices:voice['heavy']=voice['variant']==loud
(ROOT/'Tools/CostumeRevision/voice_manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
print('Imported 20 original WAVs for Noa, Hina, Seia, Yuuka and Nagisa.')
