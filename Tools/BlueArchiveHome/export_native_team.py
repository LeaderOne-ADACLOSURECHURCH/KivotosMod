"""Export native Blue Archive lobby reference assets directly from game files.

No KivotosMod textures, adapted coordinates, or fonts are used as input.
"""
import csv
import hashlib
import io
import json
import math
import shutil
from pathlib import Path
import numpy as np
import UnityPy
from PIL import Image, ImageDraw, ImageFont
from fontTools.ttLib import TTFont
from catalog import dependencies, ROOT

GAME = ROOT.parent
OUT = Path(r'Z:\ui素材\碧蓝档案主界面UI')
ADDRESS = 'Assets/_MX/AddressableAsset/UI/UILobby.prefab'
MANIFEST = []

def safe(name):
    return ''.join('_' if c in '<>:"/\\|?*' or ord(c)<32 else c for c in name).strip('. ') or 'unnamed'

def write_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False), encoding='utf8')

def write_csv(path, rows):
    path.parent.mkdir(parents=True, exist_ok=True)
    keys = list(dict.fromkeys(k for row in rows for k in row))
    with path.open('w', encoding='utf-8-sig', newline='') as stream:
        writer=csv.DictWriter(stream, fieldnames=keys); writer.writeheader(); writer.writerows(rows)

def record(path, kind, **source):
    MANIFEST.append(dict(path=path.relative_to(OUT).as_posix(), kind=kind,
        bytes=path.stat().st_size, sha256=hashlib.sha256(path.read_bytes()).hexdigest(), **source))

def export_fonts():
    env=UnityPy.load(str(GAME/'resources.assets'))
    report=[]
    for obj in env.objects:
        if obj.type.name!='Font': continue
        font=obj.read()
        if not font.m_FontData: continue
        raw=bytes(font.m_FontData)
        extension='.otf' if raw[:4]==b'OTTO' else '.ttf'
        path=OUT/'字体/原始字体'/(safe(font.m_Name)+extension)
        path.parent.mkdir(parents=True,exist_ok=True); path.write_bytes(raw)
        face=TTFont(io.BytesIO(raw))
        def name(index):
            value=face['name'].getDebugName(index); return value or ''
        cmap=face.getBestCmap()
        info=dict(unityName=font.m_Name, unityPathId=str(obj.path_id), source='BlueArchive_Data/resources.assets',
            file=path.relative_to(OUT).as_posix(), family=name(1), style=name(2), fullName=name(4),
            postScriptName=name(6), weight=face['OS/2'].usWeightClass if 'OS/2' in face else None,
            glyphCount=len(face.getGlyphOrder()), characterCount=len(cmap), fontSize=font.m_FontSize,
            lineSpacing=font.m_LineSpacing, originalSha256=hashlib.sha256(raw).hexdigest())
        report.append(info); record(path,'original font',source=info['source'],unityPathId=str(obj.path_id))
        metadata=obj.read_typetree()
        metadata.pop('m_FontData',None)
        metadata['fontDataExternalFile']=path.relative_to(OUT).as_posix()
        write_json(OUT/'字体/原始元数据'/(safe(font.m_Name)+'.json'), metadata)
        (OUT/'字体/字符集').mkdir(exist_ok=True)
        (OUT/'字体/字符集'/(safe(font.m_Name)+'.txt')).write_text(''.join(chr(c) for c in sorted(cmap) if c>=32 and not 0xD800<=c<=0xDFFF), encoding='utf8')
        print('Native font',font.m_Name,name(2),len(cmap),flush=True)
    write_json(OUT/'字体/原生字体清单.json',report)
    write_csv(OUT/'字体/原生字体清单.csv',report)
    sheet=Image.new('RGB',(1500,len(report)*190),'#eef4f8');draw=ImageDraw.Draw(sheet)
    caption=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',24)
    for i,info in enumerate(report):
        y=i*190
        draw.text((24,y+12),f"{info['unityName']}  /  {info['style']}  /  weight {info['weight']}",font=caption,fill='#274762')
        sample='こんにちは、先生。シャーレへようこそ。' if 'ShinM' in info['unityName'] else '老师，欢迎来到夏莱。咖啡厅 学生 编队 商店' if 'SourceHan' in info['unityName'] else 'Blue Archive / SCHALE / 0123456789'
        font=ImageFont.truetype(str(OUT/info['file']),40)
        draw.text((24,y+63),sample,font=font,fill='#183a58')
        draw.text((24,y+125),'ABCDEFGHIJKLMNOPQRSTUVWXYZ  0123456789',font=font,fill='#183a58')
    sheet.save(OUT/'字体/字体预览.png')
    return report

def export_ui():
    deps=dependencies(ADDRESS)
    names=[n for n in deps if n.startswith(('ui-_mxload','uis-00_images-_mxload',
        'prologdepengroup-assets-_mx-uis-atlas','prologdepengroup-assets-_mx-addressableasset-uis',
        'prologdepengroup-assets-_mx-uis-_mxcommon','prologdepengroup-assets-ngui-resources-materials',
        'MX_monoscripts')) or ('textures' in n and not n.startswith('packages-'))
        or n.startswith('prologgroup-assets-_mx-addressableasset-uis-00_images-imagefont')
        or n.startswith(('effect-ui-location_unlock_fx-', 'assets-_mx-uis-_mxcommon-',
                         'prologdepengroup-assets-_mx-etceffects-_mxcommon-uifx-',
                         'prologdepengroup-assets-_mx-effectsfreezed-texture-'))]
    print('Loading native lobby dependencies:',len(names),flush=True)
    env=UnityPy.load(*(str(ROOT/'AssetBundles'/n) for n in names))
    objects={(o.assets_file.name,o.path_id):o for o in env.objects}
    native_file=next(o.assets_file.name for o in env.objects if o.type.name=='GameObject' and o.read().m_Name=='UILobby')
    nodes={}; transforms={}; by_go={}; paths={}; all_raw=[]
    for obj in env.objects:
        if obj.assets_file.name!=native_file: continue
        if obj.type.name=='GameObject': nodes[obj.path_id]=obj.read_typetree()
        if obj.type.name=='Transform':
            d=obj.read_typetree();transforms[obj.path_id]=d;by_go[d['m_GameObject']['m_PathID']]=obj.path_id
    def full_path(go):
        if go in paths:return paths[go]
        node=nodes[go];transform=transforms.get(by_go.get(go),{})
        parent=transforms.get(transform.get('m_Father',{}).get('m_PathID'))
        paths[go]=(full_path(parent['m_GameObject']['m_PathID'])+'/' if parent else '')+node['m_Name']
        return paths[go]
    def ref_path(ref,assetsfile):
        if not isinstance(ref,dict) or not ref.get('m_PathID'):return ''
        if ref.get('m_FileID',0)==0:
            id=ref['m_PathID'];d=transforms.get(id)
            if d:return full_path(d['m_GameObject']['m_PathID'])
            if id in nodes:return full_path(id)
            o=objects.get((assetsfile.name,id))
            if o:
                try:
                    go=o.read_typetree().get('m_GameObject',{}).get('m_PathID')
                    if go in nodes:return full_path(go)+' ['+o.type.name+']'
                except Exception:pass
        return f"fileID={ref.get('m_FileID')}, pathID={ref.get('m_PathID')}"
    matrices={}
    def world(transform_id):
        if transform_id in matrices:return matrices[transform_id]
        t=transforms[transform_id];p=t['m_LocalPosition'];s=t['m_LocalScale'];q=t['m_LocalRotation'];x,y,z,w=(q[k] for k in ('x','y','z','w'))
        m=np.eye(4);m[:3,:3]=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
            [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])@np.diag([s['x'],s['y'],s['z']])
        m[:3,3]=[p['x'],p['y'],p['z']]
        parent=t.get('m_Father',{}).get('m_PathID')
        matrices[transform_id]=world(parent)@m if parent in transforms else m
        return matrices[transform_id]
    def script_name(obj,data):
        if 'm_Script' not in data:return obj.type.name
        try:
            s=UnityPy.classes.PPtr(**data['m_Script'],assetsfile=obj.assets_file).read()
            return '.'.join(x for x in (s.m_Namespace,s.m_ClassName) if x)
        except Exception:return 'MonoBehaviour'
    rows=[];widgets=[];labels=[];atlas_ids=set();texture_refs=[];unparsed=[]
    for obj in env.objects:
        if obj.assets_file.name!=native_file or obj.type.name=='AssetBundle':continue
        try:data=obj.read_typetree()
        except Exception as error:
            unparsed.append(dict(type=obj.type.name,pathId=str(obj.path_id),error=str(error)));continue
        go=obj.path_id if obj.type.name=='GameObject' else data.get('m_GameObject',{}).get('m_PathID')
        path=full_path(go) if go in nodes else ''
        cls=script_name(obj,data)
        all_raw.append(dict(pathId=str(obj.path_id),unityType=obj.type.name,scriptType=cls,path=path,data=data))
        if obj.type.name=='GameObject':continue
        if path!='UILobby' and not path.startswith('UILobby/'):continue
        t=transforms.get(by_go.get(go),{});w=world(by_go[go])[:3,3].tolist() if go in by_go else [0,0,0]
        widget=dict(path=path,pathId=str(obj.path_id),scriptType=cls,transform=t,
            nominalWorldPosition=w,gameObject=nodes[go],parameters=data)
        widgets.append(widget)
        row=dict(path=path,scriptType=cls,pathId=str(obj.path_id),activeSelf=nodes[go].get('m_IsActive'),
            localX=t.get('m_LocalPosition',{}).get('x'),localY=t.get('m_LocalPosition',{}).get('y'),localZ=t.get('m_LocalPosition',{}).get('z'),
            scaleX=t.get('m_LocalScale',{}).get('x'),scaleY=t.get('m_LocalScale',{}).get('y'),
            nominalWorldX=w[0],nominalWorldY=w[1],width=data.get('mWidth'),height=data.get('mHeight'),pivot=data.get('mPivot'),depth=data.get('mDepth'),
            spriteName=data.get('mSpriteName'),imageName=data.get('ImageName'),spriteType=data.get('mType'),
            fontSize=data.get('mFontSize'),fontStyle=data.get('mFontStyle'),isBold=data.get('IsBold'),useLocalizeFont=data.get('UseLocalizeFont'),
            localizationKey=data.get('LocalizeKey'),serializedText=data.get('mText'))
        for parameter in ('mAlignment','mSpacingX','mSpacingY','mUseFloatSpacing','mFloatSpacingX','mFloatSpacingY',
                          'mEffectStyle','mOverflow','mMaxLineCount','mEncoding','mApplyGradient',
                          'arrangement','sorting','cellWidth','cellHeight','maxPerLine','hideInactive'):
            row[parameter]=data.get(parameter)
        row.update({'color'+c.upper():data.get('mColor',{}).get(c) for c in ('r','g','b','a')})
        for name in ('leftAnchor','rightAnchor','bottomAnchor','topAnchor'):
            a=data.get(name,{})
            row[name+'Target']=ref_path(a.get('target'),obj.assets_file);row[name+'Relative']=a.get('relative');row[name+'Absolute']=a.get('absolute')
        rows.append(row)
        if 'mText' in data:labels.append(row)
        if data.get('mAtlas',{}).get('m_PathID'):
            try:
                a=UnityPy.classes.PPtr(**data['mAtlas'],assetsfile=obj.assets_file).deref()
                atlas_ids.add((a.assets_file.name,a.path_id))
            except Exception:pass
        if data.get('mTexture',{}).get('m_PathID'):texture_refs.append((obj,data['mTexture'],path))

    hierarchy=[dict(path=full_path(go),pathId=str(go),gameObject=d,transform=transforms.get(by_go.get(go)),
        nominalWorldMatrix=world(by_go[go]).tolist() if go in by_go else None) for go,d in nodes.items()]
    write_json(OUT/'布局/UILobby_完整组件.json',widgets);write_csv(OUT/'布局/UILobby_布局参数.csv',rows)
    write_csv(OUT/'布局/UILobby_文字与字体样式.csv',labels)
    write_json(OUT/'布局/原生预制体全部层级.json',hierarchy)
    write_json(OUT/'原始数据/预制体全部对象.typetree.json',all_raw)
    write_json(OUT/'原始数据/未解析对象.json',unparsed)
    write_json(OUT/'原始数据/Addressables依赖.json',dict(address=ADDRESS,allDependencies=deps,loadedBundles=names))
    textures=[];atlas_report=[];sprite_records=[];seen={};path_owners={}
    def unique_path(folder,name,owner):
        path=OUT/folder/(name+'.png')
        key=str(path).casefold()
        if key in path_owners and path_owners[key]!=owner:
            suffix=hashlib.sha256(repr(owner).encode()).hexdigest()[:12]
            path=OUT/folder/(name+'__'+suffix+'.png');key=str(path).casefold()
        path_owners[key]=owner
        return path
    def export_texture(texture_obj,folder='UI/原始纹理',label=None):
        key=(texture_obj.assets_file.name,texture_obj.path_id)
        d=texture_obj.read();name=safe(label or d.m_Name)
        cache_key=(key,folder,label)
        if cache_key in seen:return seen[cache_key],d.image
        path=unique_path(folder,name,key)
        if cache_key not in seen:
            path.parent.mkdir(parents=True,exist_ok=True);d.image.save(path);seen[cache_key]=path
            textures.append(dict(name=d.m_Name,width=d.m_Width,height=d.m_Height,file=path.relative_to(OUT).as_posix(),
                assetFile=texture_obj.assets_file.name,pathId=str(texture_obj.path_id),alpha='original straight alpha; no PMA conversion'))
            record(path,'native texture',assetFile=texture_obj.assets_file.name,pathId=str(texture_obj.path_id))
        return path,d.image
    for obj in env.objects:
        if obj.type.name=='Texture2D':export_texture(obj)
        if obj.type.name!='MonoBehaviour':continue
        try:d=obj.read_typetree()
        except Exception:continue
        if 'mSprites' not in d:continue
        name=safe(d.get('m_Name') or 'Atlas');is_lobby=(obj.assets_file.name,obj.path_id) in atlas_ids
        material_ref=UnityPy.classes.PPtr(**d['material'],assetsfile=obj.assets_file);material=material_ref.read_typetree()
        for key,value in material['m_SavedProperties']['m_TexEnvs']:
            if key!='_MainTex' or not value['m_Texture']['m_PathID']:continue
            tex=UnityPy.classes.PPtr(**value['m_Texture'],assetsfile=material_ref.deref().assets_file).deref()
            atlas_path,image=export_texture(tex,'UI/图集',name)
            for sprite in d['mSprites']:
                x,y,width,height=(sprite[k] for k in ('x','y','width','height'))
                target=unique_path('UI/切图/'+name,safe(sprite['name']),(name,sprite['name'],x,y,width,height));target.parent.mkdir(parents=True,exist_ok=True)
                crop=image.crop((x,y,x+width,y+height));crop.save(target)
                sprite_records.append(dict(atlas=name,lobbyAtlas=is_lobby,file=target.relative_to(OUT).as_posix(),**sprite))
                record(target,'native atlas sprite',atlas=name,assetFile=tex.assets_file.name,pathId=str(tex.path_id),rectangle=[x,y,width,height])
            write_json(OUT/'UI/图集'/(name+'.sprites.json'),d)
            atlas_report.append(dict(name=name,lobbyReferenced=is_lobby,texture=atlas_path.relative_to(OUT).as_posix(),
                width=image.width,height=image.height,sprites=len(d['mSprites']),assetFile=obj.assets_file.name,pathId=str(obj.path_id)))
    direct=[]
    for obj,ref,path in texture_refs:
        try:
            texture=UnityPy.classes.PPtr(**ref,assetsfile=obj.assets_file).deref()
            if texture.type.name=='Texture2D':
                target,_=export_texture(texture)
                direct.append(dict(path=path,file=target.relative_to(OUT).as_posix(),unityType=texture.type.name))
            else:
                target=OUT/'UI/材质与其他引用'/(safe(texture.type.name+'_'+str(texture.path_id))+'.json')
                write_json(target,texture.read_typetree())
                direct.append(dict(path=path,file=target.relative_to(OUT).as_posix(),unityType=texture.type.name))
        except Exception as e:direct.append(dict(path=path,unresolved=str(e),reference=ref))
    materials=[]
    for obj in env.objects:
        if obj.type.name!='Material':continue
        data=obj.read_typetree();target=OUT/'UI/材质与其他引用'/(safe(data.get('m_Name','Material'))+'__'+str(obj.path_id)+'.json')
        write_json(target,data)
        materials.append(dict(name=data.get('m_Name'),assetFile=obj.assets_file.name,pathId=str(obj.path_id),file=target.relative_to(OUT).as_posix()))
    write_json(OUT/'UI/原生材质清单.json',materials)
    write_json(OUT/'UI/原始纹理清单.json',textures);write_json(OUT/'UI/图集清单.json',atlas_report)
    write_json(OUT/'UI/直接纹理引用.json',direct);write_csv(OUT/'UI/切图与九宫格参数.csv',sprite_records)
    used={r['spriteName'] for r in rows if r.get('spriteName')}
    used_sprites=[r for r in sprite_records if r['name'] in used and r['lobbyAtlas']]
    columns=8;cell_w=190;cell_h=165
    sheet=Image.new('RGB',(columns*cell_w,math.ceil(len(used_sprites)/columns)*cell_h),'#c5d2df');draw=ImageDraw.Draw(sheet)
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',13)
    for i,r in enumerate(used_sprites):
        im=Image.open(OUT/r['file']).convert('RGBA');im.thumbnail((150,120),Image.Resampling.LANCZOS)
        x=i%columns*cell_w;y=i//columns*cell_h;sheet.paste(im,(x+(cell_w-im.width)//2,y+4),im)
        draw.text((x+5,y+128),r['name'][:27],font=font,fill='#183a58')
        draw.text((x+5,y+145),f"{r['width']}x{r['height']} / {r['atlas']}",font=font,fill='#183a58')
    sheet.save(OUT/'UI/大厅使用素材索引.jpg',quality=94)
    # Native bundles are retained exactly, so the team can re-extract any object.
    for name in names:
        if name.startswith('MX_monoscripts'):continue
        target=OUT/'原始数据/AssetBundles'/name;target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(ROOT/'AssetBundles'/name,target);record(target,'original game bundle',source=name)
    print('Native UI',len(widgets),'components;',len(nodes),'nodes;',len(sprite_records),'sprites;',len(labels),'labels',flush=True)
    return dict(mainLobbyComponents=len(widgets),hierarchyNodes=len(nodes),sprites=len(sprite_records),
        atlases=atlas_report,labels=len(labels),unparsedObjects=len(unparsed),usedSprites=len(used_sprites),directTextureUnresolved=sum('unresolved' in r for r in direct))

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    fonts=export_fonts();ui=export_ui()
    summary=dict(sourceGame=str(GAME.parent),address=ADDRESS,extraction='native game assets; no KivotosMod input',
        ui=ui,fonts=fonts,files=MANIFEST)
    write_json(OUT/'来源与校验/导出清单.json',summary)
    print('Exported native reference package to',OUT,flush=True)

if __name__=='__main__':main()
