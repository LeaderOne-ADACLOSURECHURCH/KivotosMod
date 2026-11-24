"""Full-canvas matte at AE position (618,408.5), using original straight alpha."""
from pathlib import Path
import numpy as np
from PIL import Image

def make_mask():
    here = Path(__file__).resolve().parent
    source = here/'SourceTextures/BlackHole'
    alpha = np.array(Image.open(source/'Glow.png'))[:, :, 3].astype(np.uint16)
    # Glow top-left=(-8,145.5). Average the two adjacent integer placements.
    a = np.zeros((817, 1280), dtype=np.uint16)
    b = a.copy()
    a[145:145+526, :1244] = alpha[:, 8:]
    b[146:146+526, :1244] = alpha[:, 8:]
    mask = np.zeros((817, 1280, 4), dtype=np.uint8)
    mask[:, :, :3] = 255
    mask[:, :, 3] = ((a+b+1)//2).astype(np.uint8)
    Image.fromarray(mask).save(source/'GlowMask.png')

if __name__ == '__main__':
    make_mask()
