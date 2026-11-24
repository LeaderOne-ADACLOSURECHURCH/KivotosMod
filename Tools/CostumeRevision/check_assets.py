"""Check variant source pixels, 60px special frames, shared tail, and original voices."""
from pathlib import Path
from PIL import Image
import sys,json,hashlib,zipfile,io,wave
TOOLS=Path(__file__).resolve().parent;ROOT=TOOLS.parents[1]
sys.path.insert(0,str(ROOT/'Tools/StudentBatch'))
from import_assets import read_zip,DOWNLOADS
from import_variants import SPECS
checks=0
for name,(filename,mapping) in SPECS.items():
    source=read_zip(DOWNLOADS/filename);folder=ROOT/'Content/Items/Vanity'/name.replace('2x2','')
    for key,original in mapping.items():
        expected=source[original].copy()
        if key=='Head_Head' and name=='Seia':expected.alpha_composite(source['鬓发'])
        if key=='Body_Body':
            for x in (0,1):expected.paste(expected.crop((x*40,112,(x+1)*40,168)),(x*40,0))
        actual=Image.open(folder/f'{name}{key}.png').convert('RGBA')
        assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),(name,key)
        if key!='Body_Body':
            h=actual.height//20
            assert h==(60 if name=='Seia' and key in ('Ears','Tail') else 56)
        checks+=1
    for part,equip in [('Head','Head'),('Body','Body'),('Boots','Legs')]:
        assert Image.open(folder/f'{name}{part}.png').getbbox()
        assert (folder/f'{name}{part}_{equip}.png').is_file()
assert Image.open(ROOT/'Content/Items/Vanity/Haruka/HarukaHead_Head.png').tobytes()==Image.open(TOOLS/'HarukaHead.png').tobytes()
assert Image.open(ROOT/'Content/Items/Vanity/Hikari/HikariTail.png').tobytes()==Image.open(ROOT/'Content/Items/Vanity/Nozomi/NozomiTail.png').tobytes()
for student in ('Mutsuki','Nozomi'):
    layers=(ROOT/f'Content/Items/Vanity/{student}/{student}DrawLayers.cs').read_text('utf8')
    assert 'offsetX: -8f' in layers
    assert 'new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc)' in layers
for student in ('Ibuki','Iroha'):
    layers=(ROOT/f'Content/Items/Vanity/{student}/{student}DrawLayers.cs').read_text('utf8')
    assert 'new Between(PlayerDrawLayers.Head, PlayerDrawLayers.ArmOverItem)' in layers
assert 'class HarukaBackHairLayer' not in (ROOT/'Content/Items/Vanity/Haruka/HarukaDrawLayers.cs').read_text('utf8')
assert 'class MikaIdleSideLocksLayer' in (ROOT/'Content/Items/Vanity/Mika/MikaSideLocksLayers.cs').read_text('utf8')
for name in ('Nagisa','Seia'):
    for locale in ('zh-Hans','en-US'):
        items=(ROOT/f'Localization/{locale}/Mods.KivotosMod.Items.hjson').read_text('utf8')
        for part in ('Head','Body','Boots'):assert name+part+':' in items
config=(ROOT/'Globals/Configs/ClientsSide.cs').read_text('utf8')
assert 'UseMika2x2' not in config and 'UseYuuka2x2' not in config
for student in ('Mika','Yuuka','Ibuki','Iroha','Hikari'):
    assert (ROOT/f'Content/Items/Vanity/{student}/{student}2x2Set.cs').is_file()
voices=json.loads((TOOLS/'voice_manifest.json').read_text('utf8'));voice_count=0
with zipfile.ZipFile(DOWNLOADS/'受伤语音.zip') as z:
    raw={}
    for f in z.infolist():
        if f.is_dir() or '__MACOSX' in f.filename:continue
        name=f.filename
        if not f.flag_bits&0x800:
            for enc in ('utf8','gbk'):
                try:name=f.filename.encode('cp437').decode(enc);break
                except UnicodeError:pass
        raw[name]=z.read(f)
for student,entries in voices.items():
    for entry in entries:
        data=(ROOT/f'Assets/Sounds/Students/{student}/Hurt{entry["variant"]}.wav').read_bytes()
        assert data==raw[entry['source']] and hashlib.sha256(data).hexdigest()==entry['sha256']
        with wave.open(io.BytesIO(data)) as w:assert w.getnframes()>0
        voice_count+=1
assert voice_count==20
report=dict(passed=True,variantPixelComparisons=checks,nativePixelScale=True,specialSixtyPixelFrames=True,
    newHarukaHeadAndSingleRearLocks=True,sharedHikariNozomiTail=True,referencePonytailOffset=[-16,0],
    coatAboveHeadAndBelowFrontArm=True,perPlayerWardrobeInsteadOfSwitches=True,originalPcmVoices=20,voiceStudents=5)
(TOOLS/'asset_validation.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report,indent=2))
