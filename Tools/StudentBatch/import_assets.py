"""Import the seven supplied player outfits at native pixel size, without resampling."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib, io, json, zipfile

TOOLS = Path(__file__).resolve().parent
ROOT = TOOLS.parents[1]
DOWNLOADS = Path('C:/Users/Sunbinbin/Downloads')
SPECS = {
    'Haruka': ('春香 (1).zip', '光环.png', {'Head_Head':'头', 'Body_Body':'手', 'Torso':'身子', 'Boots_Legs':'腿', 'SideLocks':'鬓发（前）'}, []),
    'Kayoko': ('佳代子.zip', '光环佳代子.png', {'Head_Head':'头', 'Body_Body':'手', 'Torso':'身子', 'Boots_Legs':'腿', 'BackHair':'马尾', 'SideLocks':'鬓发'}, []),
    'Mutsuki': ('睦月.zip', '光环睦月.png', {'Head_Head':'头', 'Body_Body':'手', 'Torso':'身子', 'Boots_Legs':'腿', 'BackHair':'侧马尾'}, []),
    'Ibuki': ('伊吹 (1).zip', None, {'Head_Head':'头', 'Body_Body':'手', 'Torso':'身子', 'Boots_Legs':'腿', 'BackHair':'后发', 'Tail':'尾巴', 'Wings':'翅膀', 'Coat':'大衣', 'Halo':'光环'}, []),
    'Iroha': ('1伊吕波.zip', None, {'Head_Head':'头', 'Body_Body':'手', 'Torso':'身子', 'Boots_Legs':'腿', 'BackHair':'后发', 'Coat':'大衣', 'Halo':'光环'}, []),
    'Hikari': ('小光 (1).zip', '光环（小光）.png', {'Head_Head':'头', 'Body_Body':'身子', 'Boots_Legs':'脚', 'BackHair':'后发'}, []),
    'Nozomi': ('小望.zip', '小望光环.png', {'Head_Head':'头', 'Body_Body':'手', 'Boots_Legs':'腿', 'BackHair':'马尾', 'Tail':'尾巴', 'SideLocks':'鬓发'}, []),
}

def read_zip(path):
    result = {}
    with zipfile.ZipFile(path) as z:
        for f in z.infolist():
            if f.is_dir() or '__MACOSX' in f.filename:
                continue
            name = f.filename
            if not f.flag_bits & 0x800:
                try:
                    name = name.encode('cp437').decode('utf8')
                except UnicodeDecodeError:
                    name = name.encode('cp437').decode('gbk')
            result[Path(name).stem] = Image.open(io.BytesIO(z.read(f))).convert('RGBA')
    return result

def make_assets(name, source, hikari):
    _, halo_file, mapping, front = SPECS[name]
    images = {key: source[value].copy() for key, value in mapping.items()}
    if name == 'Haruka':
        images['Head_Head'] = Image.open(ROOT/'Tools/CostumeRevision/HarukaHead.png').convert('RGBA')
    if name == 'Hikari':
        images['Tail'] = read_zip(DOWNLOADS/'小望.zip')['尾巴']
        # The authored green strands are part of this head sheet. Move only the
        # lower hair-color pixels to a foreground layer, preserving their positions.
        head = images['Head_Head']
        locks = Image.new('RGBA', head.size)
        for y in range(head.height):
            if y % 56 < 25: continue
            for x in range(head.width):
                r,g,b,a = head.getpixel((x,y))
                if a and g > r and g > b:
                    locks.putpixel((x,y),(r,g,b,a))
                    head.putpixel((x,y),(0,0,0,0))
        images['SideLocks'] = locks
    for key in front:
        images['Head_Head'].alpha_composite(source[key])
    if halo_file:
        images['Halo'] = Image.open(DOWNLOADS/halo_file).convert('RGBA')
    atlas = images['Body_Body']
    assert atlas.size == (360,224)
    # The new Hikari atlas contains its torso in the female row only. Supply
    # that same uniform for both player genders. Nozomi borrows it as requested;
    # all of Nozomi's own front/rear arm cells remain untouched.
    if name in ('Hikari', 'Nozomi'):
        uniform = hikari['身子']
        for col in (0,1):
            torso = uniform.crop((col*40,112,(col+1)*40,168))
            assert torso.getbbox()
            atlas.paste(torso, (col*40,0))
            atlas.paste(torso, (col*40,112))
    for key, im in images.items():
        if key not in ('Halo','Body_Body'):
            assert im.height == 1120 and im.width in (40,56), (name,key,im.size)
    return images

def preview_character(images, frame=0, flip=False):
    canvas = Image.new('RGBA',(104,112))
    def strip(key):
        if key not in images: return
        im=images[key]
        canvas.alpha_composite(im.crop((0,frame*56,im.width,(frame+1)*56)), (32-(im.width-40)//2,32))
    if 'BackHair' in images:
        im=images['BackHair']
        canvas.alpha_composite(im.crop((0,frame*56,im.width,(frame+1)*56)),(16 if im.width==56 else 32,32))
    strip('Tail'); strip('Wings')
    atlas=images['Body_Body']
    # Material preview at zero arm rotation. Runtime uses native composite arms.
    canvas.alpha_composite(atlas.crop((80,112,120,168)),(32,32))
    strip('Boots_Legs')
    if 'Torso' in images: strip('Torso')
    else: canvas.alpha_composite(atlas.crop((0,0,40,56)),(32,32))
    strip('Head_Head')
    strip('Coat')
    canvas.alpha_composite(atlas.crop((80,0,120,56)),(32,32))
    strip('SideLocks')
    halo=images['Halo']; top=images['Head_Head'].crop((0,0,40,56)).getbbox()[1]
    canvas.alpha_composite(halo,(52-halo.width//2,32+top-3-halo.height))
    return canvas.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if flip else canvas

def main():
    sources={name:read_zip(DOWNLOADS/spec[0]) for name,spec in SPECS.items()}
    report={}
    preview=Image.new('RGBA',(7*208,3*224),(44,58,74,255))
    draw=ImageDraw.Draw(preview)
    for col,(name,spec) in enumerate(SPECS.items()):
        assets=make_assets(name,sources[name],sources['Hikari'])
        out=ROOT/'Content/Items/Vanity'/name;out.mkdir(exist_ok=True)
        for suffix,im in assets.items(): im.save(out/f'{name}{suffix}.png')
        for item,key in [('Head','Head_Head'),('Body','Torso'),('Boots','Boots_Legs')]:
            im=assets[key].crop((0,0,40,56)) if key in assets else assets['Body_Body'].crop((0,0,40,56))
            im.crop(im.getbbox()).save(out/f'{name}{item}.png')
        thumb=preview_character(assets)
        thumb_dir=ROOT/'Assets/Tablet/Students'/name;thumb_dir.mkdir(parents=True,exist_ok=True)
        thumb.crop(thumb.getbbox()).save(thumb_dir/'Thumbnail.png')
        for row,(frame,flip) in enumerate([(0,False),(0,True),(10,False)]):
            im=preview_character(assets,frame,flip).resize((208,224),Image.Resampling.NEAREST)
            preview.alpha_composite(im,(col*208,row*224))
            draw.text((col*208+4,row*224+4),f'{name} '+('flipped' if flip else f'frame {frame}'),fill='white')
        report[name]={'sourceZip':str(DOWNLOADS/spec[0]),'zipSha256':hashlib.sha256((DOWNLOADS/spec[0]).read_bytes()).hexdigest(),
            'haloSource':str(DOWNLOADS/spec[1]) if spec[1] else 'zip:光环.png',
            'headTop':assets['Head_Head'].crop((0,0,40,56)).getbbox()[1],
            'assets':{key:{'size':im.size,'sha256':hashlib.sha256((out/f'{name}{key}.png').read_bytes()).hexdigest()} for key,im in assets.items()}}
        if name=='Haruka':
            report[name]['headOverrideSource']='Tools/CostumeRevision/HarukaHead.png'
            report[name]['headOverrideSha256']=hashlib.sha256((ROOT/'Tools/CostumeRevision/HarukaHead.png').read_bytes()).hexdigest()
        if name=='Hikari': report[name]['tailSource']='小望.zip/尾巴.png (shared with Nozomi)'
    preview.save(TOOLS/'outfit_preview.png')
    (TOOLS/'import_manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
    print('Imported 7 outfits at source pixel scale; manifest and material previews saved.')

if __name__ == '__main__': main()
