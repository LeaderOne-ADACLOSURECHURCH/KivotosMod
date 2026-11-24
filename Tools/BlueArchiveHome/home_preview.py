"""Offline composition preview; does not start Terraria or compile the mod."""
import json, math, argparse
from datetime import datetime
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps

tools=Path(__file__).resolve().parent
root=tools.parents[1]
ui=tools/'ExtractedUI'  # Straight-alpha originals; packaged Home assets use PMA.
factor=2
parser=argparse.ArgumentParser()
parser.add_argument('--clock',choices=['real','day','night'],default='real')
parser.add_argument('--arona-version',choices=['38','42'],default='38')
parser.add_argument('--scene',choices=['day','night'],default='day')
args=parser.parse_args()
canvas=Image.new('RGBA',(610*factor,450*factor))
ink=(21,55,87)
def box(rect): return tuple(round(v*factor) for v in rect)
def solid(rect,color):
    x,y,w,h=box(rect); layer=Image.new('RGBA',(w,h),color); canvas.alpha_composite(layer,(x,y))
def fit(name,rect,tint=None,flip=False):
    x,y,w,h=box(rect); im=Image.open(ui/(name+'.png')).convert('RGBA'); im.thumbnail((w,h),Image.Resampling.LANCZOS)
    if flip: im=ImageOps.mirror(im)
    if tint:
        import numpy as np
        rgba=np.array(im);rgba[:,:,:3]=(rgba[:,:,:3].astype('uint16')*np.array(tint,dtype='uint16')//255).astype('uint8');im=Image.fromarray(rgba)
    canvas.alpha_composite(im,(x+(w-im.width)//2,y+(h-im.height)//2))
def text(s,x,y,size,color=ink,width=0,outline=False):
    font_path='Z:/ui素材/碧蓝档案主界面UI/字体/原始字体/SourceHanSansSC-Bold.otf'
    font=ImageFont.truetype(font_path,round(size*factor))
    if width and font.getlength(s)>width*factor:
        font=ImageFont.truetype(font_path,max(1,round(size*factor*width*factor/font.getlength(s))))
    ImageDraw.Draw(canvas).text((x*factor,y*factor),s,font=font,fill=color,anchor='mt',stroke_width=1 if outline else 0,stroke_fill=ink)
def panel(rect,color=(255,255,255,255),incline=0):
    tex=Image.open(ui/'Common_Bg_Raius5px_Shadow.png').convert('RGBA')
    x,y,w,h=rect
    sx=[0,20,28,48]; sy=[0,15,18,48]; dx=[0,5,w-5,w]; dy=[0,4,h-8,h]
    if h<16:dy=[0,2,h-3,h]
    layer=Image.new('RGBA',(round(w*factor),round(h*factor)))
    for a in range(3):
        for b in range(3):
            part=tex.crop((sx[b],sy[a],sx[b+1],sy[a+1])).resize((round((dx[b+1]-dx[b])*factor),round((dy[a+1]-dy[a])*factor)))
            layer.alpha_composite(part,(round(dx[b]*factor),round(dy[a]*factor)))
    import numpy as np
    rgba=np.array(layer);rgba=(rgba.astype('uint16')*np.array(color,dtype='uint16')//255).astype('uint8');layer=Image.fromarray(rgba)
    shear=math.tan(math.radians(incline));margin=math.ceil(h*factor*abs(shear)/2)
    if incline:
        layer=layer.transform((layer.width+2*margin,layer.height),Image.Transform.AFFINE,(1,shear,-margin-layer.height*shear/2,0,1,0),Image.Resampling.BICUBIC)
    canvas.alpha_composite(layer,(round(x*factor)-margin,round(y*factor)))
# Legacy Shittim Chest classroom; match ClassroomSourceRect and runtime layer order.
# The software preview omits the GPU water-caustic pass.
bg=Image.new('RGBA',(880,466))
for name in ['Sea','Sky','Ground','Wall','Lockers','Board','Light','ChairBack','TableBack','ChairMid','TableMid','ChairFront','TableFront']:
    bg.alpha_composite(Image.open(tools/('NightTabletSources' if args.scene=='night' else 'LegacyTabletSources')/(name+'.png')).convert('RGBA'))
bg=bg.crop((210,20,804,466)).resize((514*factor,386*factor),Image.Resampling.NEAREST)

glass=Image.new('RGBA',canvas.size); glass.alpha_composite(bg,(32*factor,32*factor))
if args.arona_version=='38' or args.scene=='night':
    import numpy as np
    model='plana38' if args.scene=='night' else 'arona38'
    meshes=json.loads((tools/(model+'_idle_pose.json')).read_text('utf8'))
    vertices=np.concatenate([np.asarray(m['vertices']).reshape(-1,2) for m in meshes])
    lo=vertices.min(axis=0);hi=vertices.max(axis=0);w,h=hi-lo
    bound_x,bound_y=math.floor(lo[0]),math.floor(lo[1]);bound_w,bound_h=math.ceil(w),math.ceil(h)
    scale=620/bound_h
    anchor_x=287-(bound_x+bound_w/2)*scale;anchor_y=82+(bound_y+bound_h)*scale
    arona=Image.open(tools/(model+'_idle_preview.png')).convert('RGBA').resize((round(w*scale*factor),round(h*scale*factor)),Image.Resampling.LANCZOS)
    glass.alpha_composite(arona,(round((anchor_x+lo[0]*scale)*factor),round((anchor_y-hi[1]*scale)*factor)))
else:
    arona=Image.open(tools/'arona_spr_preview.png').convert('RGBA').resize((round(1077.489*.30*factor),round(2129.779*.30*factor)),Image.Resampling.LANCZOS)
    glass.alpha_composite(arona,(round((287-544.908*.30)*factor),round((480-1333.277*.30)*factor)))
canvas.alpha_composite(glass.crop((32*factor,32*factor,546*factor,418*factor)),(32*factor,32*factor))
panel((39,40,125,46),(*ink,255),10)
text('HP',56,43,9,(255,222,45));text('400',57,56,17,'white',34);text('先生',115,45,12,'white',75)
solid((77,63,77,2),(81,211,240,255));text('400/400',116,69,8,(103,218,246))
for icon,value,r,plus in [('Stamina','200/200',(181,40,77,25),True),('Gold','12,345',(265,40,102,25),False),('Diamond','123',(374,40,82,25),True)]:
    panel(r,(255,255,255,235),10);x,y,w,h=r;fit('Common_Icon_'+icon,(x+3,y+3,17,18));tw=w-26-(12 if plus else 0);text(value,x+23+tw/2,y+7,10,width=tw)
    if plus:fit('Common_Plus_Btn',(x+w-13,y+8,8,8),(39,175,225))
panel((464,40,74,25),(255,255,255,235),10)
fit('Common_Icon_SpecialLobby',(468,46,17,15),ink);fit('Common_Icon_Mail',(493,46,17,15),ink);fit('Common_Icon_Menu_Small',(519,46,15,15),ink)
shortcuts=['Common_Icon_Notice','Common_Icon_MoMoTalk','School_Icon_Schedule','Common_Icon_Gembag']
for i,(name,label) in enumerate(zip(shortcuts,['公告','MomoTalk','任务','青辉石购买'])):
    x=43+i%2*52;y=108+i//2*58
    fit(name,(x+6,y+1,37,34));text(label,x+24,y+37,9,'white',49,True)
fit('Event_Banner_841_Jp',(458,118,77,49));text('活动',496,108,9,'white')
fit('Lobby_Gacha_Banner_210204_1',(43,294,118,59))
panel((480,170,32,7),(255,255,255,235))
for i in range(2):solid((491+i*8,172,2,2),(72,187,236,255) if i==0 else (*ink,255))
panel((39,392,407,23),(255,255,255,235),10)
for i,(icon,label) in enumerate(zip(['Cafe','SchaleBilding','Student','Edit','Social','Craft','Shop','Gacha'],['咖啡厅','日程','学生','编队','社交','制造','商店','招募'])):
    x=39+i*44;fit('Common_Icon_'+icon,(x+6,364,32,39));text(label,x+22,403,10,width=42)
if args.clock=='real':
    fit('UIWorkAronaSceneBG_DecoColor',(403,391,36,7));clock=datetime.now().strftime('%H:%M')
else:
    fit('UIWorkAronaSceneBG_DecoColor',(398,392,24,5));text('白天' if args.clock=='day' else '夜晚',434,391,7,width=20)
    clock='04:30' if args.clock=='day' else '19:30'
text(clock,420,400,11,(45,70,100),46)
fit('Lobby_Icon_Work',(451,329,87,66));panel((453,387,82,27),(*ink,255),10);text('工作',494,393,13,'white')
for name,rect,inner in [('Common_UIHide_Icon2',(503,77,33,25),(510,81,19,17)),('Common_Icon_Change_Count',(465,77,34,25),(470,81,24,17))]:
    panel(rect,(255,255,255,235),10);fit(name,inner,ink)
fit('Cafe_Icon_Arrow',(34,225,19,32));fit('Cafe_Icon_Arrow',(524,225,20,32),flip=True)
frame=Image.open(root/'Assets/Tablet/UI/TabletFrameOpen.png').convert('RGBA').resize(canvas.size)
canvas.alpha_composite(frame)
background=Image.new('RGBA',canvas.size,(38,55,72,255)); background.alpha_composite(canvas)
filename='home_preview.jpg' if args.clock=='real' else f'home_preview_clock_{args.clock}.jpg'
if args.scene=='night':filename=filename.replace('.jpg','_night.jpg')
if args.arona_version=='42':filename=filename.replace('.jpg','_spine42.jpg')
background.convert('RGB').save(tools/filename,quality=94)
print('Saved offline tablet Home layout preview')
