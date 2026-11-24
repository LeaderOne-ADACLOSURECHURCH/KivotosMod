"""Channel-wise threshold extraction plus padded separable Gaussian bloom.
No animation is baked: two threshold endpoints are shared by all stars of a style.
"""
from pathlib import Path
import json,math
import numpy as np
from PIL import Image,ImageFilter
R=Path(__file__).resolve().parents[3];A=R/'Assets/Skills/MikaDomain'
s=json.loads((A/'Stars.json').read_text(encoding='utf-8'));cache={}
def glow(texture,threshold,radius,name):
 im=np.array(Image.open(A/(texture+'.png')).convert('RGBA')).astype(np.float32)/255
 alpha=im[:,:,3:4];rgb=im[:,:,:3]/np.maximum(alpha,1e-6)
 bright=np.maximum(rgb-threshold,0)*alpha*8
 pad=math.ceil(radius*2);channels=[]
 for k in range(3):
  channel=np.pad(bright[:,:,k],pad)
  # Float channels preserve the extracted color ratios before quantization.
  sigma=max(.01,radius/3);half=math.ceil(3*sigma);x=np.arange(-half,half+1);kernel=np.exp(-x*x/(2*sigma*sigma));kernel/=kernel.sum()
  blurred=np.apply_along_axis(lambda v:np.convolve(v,kernel,mode='same'),0,channel)
  channels.append(np.apply_along_axis(lambda v:np.convolve(v,kernel,mode='same'),1,blurred))
 color=np.stack(channels,axis=2);a=np.max(color,axis=2,keepdims=True)
 packed=np.concatenate([color,a],axis=2).clip(0,1)
 Image.fromarray(np.rint(packed*255).astype('uint8')).save(A/(name+'.png'))
 return pad
def highlight(texture,radius,pad,name):
 a=np.array(Image.open(A/(texture+'.png')).convert('RGBA'))[:,:,3].astype(float)/255
 # A separate neutral, soft core: colored bloom remains a separate pass.
 channel=np.pad(a**3,pad);sigma=max(1,radius/12);half=math.ceil(3*sigma)
 x=np.arange(-half,half+1);kernel=np.exp(-x*x/(2*sigma*sigma));kernel/=kernel.sum()
 channel=np.apply_along_axis(lambda v:np.convolve(v,kernel,mode='same'),0,channel)
 channel=np.apply_along_axis(lambda v:np.convolve(v,kernel,mode='same'),1,channel)*.6
 image=np.repeat(channel[:,:,None],4,axis=2)
 Image.fromarray(np.rint(image.clip(0,1)*255).astype('uint8')).save(A/(name+'.png'))
for star in s['stars']:
 key=(star['texture'],star['threshold'],star['radius'])
 if key not in cache:
  name='Bloom'+str(len(cache)).zfill(2);pad=glow(key[0],key[1],key[2],name+'N');glow(key[0],key[1]*.7,key[2],name+'B');cache[key]=(name,pad)
 name,pad=cache[key]
 highlight(star['texture'],star['radius'],pad,name+'H')
 star.update(bloom=name+'N',bloomBurst=name+'B',highlight=name+'H',padding=pad)
(A/'Stars.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf-8')
print('Shared bloom styles:',len(cache))
