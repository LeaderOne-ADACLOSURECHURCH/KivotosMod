"""Export 2D source layers, sampled AE properties and trimmed cues for Mika EX."""
from io import BytesIO
from pathlib import Path
import json
import math
import struct
import subprocess
import sys
import tempfile
import zipfile

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path.home() / ".codex/tmp/blackhole-aep-libs"))
import py_aep
import mapbox_earcut

ROOT = Path(__file__).resolve().parents[3]
OUTPUT = ROOT / "Assets/Skills/MikaDomain/Resonance"
DOWNLOADS = Path.home() / "Downloads"
FPS = 60
OUTPUT.mkdir(parents=True, exist_ok=True)


def archive_name(entry):
    name = entry.filename
    if not entry.flag_bits & 0x800:
        name = name.encode("cp437").decode("gbk")
    return name.replace("\\", "/").rsplit("/", 1)[-1]


with zipfile.ZipFile(DOWNLOADS / "图片 (4).zip") as archive:
    images = {archive_name(entry): archive.read(entry) for entry in archive.infolist()
              if archive_name(entry).endswith(".png")}

comps = {}
textures = {}
texture_bounds = {}
motion = bytearray()
mask_motion = bytearray()


def publish(image, key):
    if key not in textures:
        box = image.getchannel("A").getbbox() or (0, 0, 1, 1)
        left, top, right, bottom = box
        # Keep two transparent source texels for linear sampling at the trim edge.
        left, top = max(0, left-2), max(0, top-2)
        right, bottom = min(image.width, right+2), min(image.height, bottom+2)
        trimmed = image.crop((left, top, right, bottom))
        if trimmed.getextrema() == tuple((v, v) for v in trimmed.getpixel((0, 0))):
            trimmed = trimmed.resize((1, 1))
        else:
            # The strips retain their long dimension for the polar sampler.
            limit = (768, 1536) if image.height >= 5000 else (1280, 1280)
            trimmed.thumbnail(limit, Image.Resampling.LANCZOS)
        data = np.asarray(trimmed).copy()
        data[:, :, :3] = (data[:, :, :3].astype(np.uint16) * data[:, :, 3:] + 127) // 255
        name = f"R{len(textures):02}"
        Image.fromarray(data).save(OUTPUT / f"{name}.png")
        textures[key] = name
        texture_bounds[name] = [left, top, right-left, bottom-top]
    return textures[key]


def paint_variants(layer, image, key, effects):
    strokes = []
    for effect in effects:
        if effect.match_name != "ADBE Paint":
            continue
        for atom in effect.property("ADBE Paint Group"):
            raw = atom.property("ADBE Paint Duration").value
            start, end = raw[0] / image.width, raw[0] / image.width + raw[1] / image.height
            shape = atom.property("ADBE Paint Shape").value
            origin = atom.property("ADBE Paint Transform").property("ADBE Paint Position").value
            props = {q.match_name: q.value for q in atom.property("ADBE Paint Properties") if hasattr(q, "value")}
            diameter = props["ADBE Paint Diameter"]
            erase = Image.new("L", image.size)
            draw = ImageDraw.Draw(erase)
            points = [(origin[0] + x * image.width, origin[1] + y * image.height) for x, y in shape.vertices]
            if not points:
                continue
            draw.line(points, fill=255, width=max(1, round(diameter)), joint="curve")
            for x, y in (points[0], points[-1]):
                half = diameter / 2
                draw.ellipse((x-half, y-half, x+half, y+half), fill=255)
            softness = 1 - props.get("ADBE Paint Hardness", 0) / 100
            erase = erase.filter(ImageFilter.GaussianBlur(diameter * softness / 6))
            strokes.append((start, end, np.asarray(erase, dtype=np.uint16)))
    switches = []
    for moment in sorted({time for start, end, _ in strokes for time in (start, end)}):
        active = tuple(i for i, (start, end, _) in enumerate(strokes) if start <= moment < end)
        variant = image.copy()
        alpha = np.asarray(image.getchannel("A"), dtype=np.uint16)
        for i in active:
            alpha = alpha * (255 - strokes[i][2]) // 255
        variant.putalpha(Image.fromarray(alpha.astype(np.uint8)))
        texture = publish(variant, (key, active) if active else key)
        switches.append([moment, texture])
    return switches


def value(prop, time):
    return prop.value_at_time(time) if prop.keyframes else prop.value


def contour(shape):
    points = np.asarray(shape.vertices)
    tin, tout = np.asarray(shape.in_tangents), np.asarray(shape.out_tangents)
    result = []
    for i, start in enumerate(points):
        j = (i + 1) % len(points)
        end = points[j]
        a, b = start + tout[i], end + tin[j]
        distance = np.linalg.norm(a-start)+np.linalg.norm(b-a)+np.linalg.norm(end-b)
        steps = 1 if not (tout[i].any() or tin[j].any()) else min(16,max(2,int(distance/12)))
        t = np.arange(steps)[:,None]/steps
        result.extend((1-t)**3*start+3*t*(1-t)**2*a+3*t*t*(1-t)*b+t**3*end)
    return np.asarray(result, dtype=np.float32)


def add_masks(layer, entry, image, key):
    group = layer.property("ADBE Mask Parade")
    if not group:
        return
    masks = [(m, m.property("ADBE Mask Shape"), m.property("ADBE Mask Opacity"),
              m.property("ADBE Mask Feather").value) for m in group]
    assert all(int(m.mask_mode)==6813 for m, *_ in masks)
    if any(any(f) or m.inverted for m, _, _, f in masks):
        assert len(masks)==1
        m, path, opacity, feather = masks[0]
        entry["maskInverted"]=m.inverted
        entry["maskFeather"]=list(feather)
    frames=[]
    previous=None
    end_frame=123 if "裂纹" in layer.name or "裂纹" in getattr(layer.source,"name","") else 301
    for frame in range(end_frame):
        time=frame/30
        batch=[]
        for _,path,opacity,_ in masks:
            alpha=round(max(0,min(100,value(opacity,time)))*255/100)
            if alpha==0:continue
            vertices=contour(value(path,time))
            if len(vertices)<3:continue
            indices=mapbox_earcut.triangulate_float32(vertices,np.array([len(vertices)],np.uint32))
            for x,y in vertices[indices]:
                batch.append(struct.pack("<hhB",round(float(x)*4),round(float(y)*4),alpha))
        payload=b"".join(batch)
        if payload!=previous:
            frames.append([frame,len(mask_motion),len(batch)])
            mask_motion.extend(payload)
            previous=payload
    entry["maskFrames"]=frames


def hue_samples(effect):
    """These projects store the master sliders as signed integers in aRbp."""
    samples = []
    for block in effect._tdgp.chunks:
        if getattr(block, "list_type", None) != "aRbs":
            continue
        for chunk in block.chunks:
            if chunk.chunk_type == "aRbp" and len(chunk.data) == 180:
                samples.append(struct.unpack_from(">3i", chunk.data))
    if not samples:
        return None
    keyframes = effect.property("ADBE HUE SATURATION-0003").keyframes
    times = [k.time for k in keyframes]
    if not times:
        times = [0.]
    assert len(times) == len(samples), effect.name
    return times, list(zip(*samples))


def effect_values(effects, time):
    adjust = [0.] * 4
    glow = [0.] * 3
    for effect, hue in effects:
        match = effect.match_name
        if match == "ADBE HUE SATURATION":
            if hue is None:
                master = [value(effect.property(match + f"-000{i}"), time) for i in (4, 5, 6)]
            else:
                times, channels = hue
                master = [float(np.interp(time, times, channel)) for channel in channels]
            adjust[0] = math.radians(master[0])
            adjust[1] = master[1] / 100
            adjust[2] += master[2] / 100
        elif match == "ADBE Brightness & Contrast 2":
            adjust[2:4] = [value(effect.property(i), time) / 100 for i in range(2)]
        elif match == "ADBE Glo2":
            glow = [value(effect.property(match + f"-000{i}"), time) for i in (2, 3, 4)]
            glow[0] /= 255
    return adjust + glow


def export_layer(layer, comp, effects):
    source = layer.source
    size = [source.width, source.height]
    entry = dict(name=layer.name, enabled=bool(layer.enabled), index=layer.index,
                 inTime=layer.in_point, outTime=layer.out_point, sourceStart=layer.start_time,
                 stretch=layer.stretch/100, size=size, additive=int(layer.blending_mode) == 5223,
                 adjustment=bool(layer.adjustment_layer))
    matte = layer.track_matte_layer
    if matte is not None:
        entry.update(matte=matte.index, matteMode=int(layer.track_matte_type)-5012)
    if layer.parent is not None:
        entry["parent"] = layer.parent.index
    remap = layer.property("ADBE Time Remapping")
    polar = next((e for e in effects if e.match_name == "ADBE Polar Coordinates"), None)
    geometry = next((e for e in effects if e.match_name == "ADBE Geometry2"), None)
    if hasattr(source, "layers"):
        entry["comp"] = source.name
        entry["openScope"] = comp.name == "冲击后段" and source.name == "冲击前段"
    elif not layer.adjustment_layer and getattr(source, "file", None):
        key = Path(str(source.file)).name
        image = Image.open(BytesIO(images[key])).convert("RGBA")
        entry["texture"] = publish(image, key)
        entry["paint"] = paint_variants(layer, image, key, effects)
        add_masks(layer, entry, image, key)
    transform = layer.transform
    rotation = transform.property("ADBE Rotate Z")
    count = round(comp.duration * FPS) + 1
    entry["motion"] = len(motion) // 4
    entry["remap"] = bool(remap and remap.keyframes)
    entry["warp"] = bool(polar or geometry)
    sampled_effects = [(effect, hue_samples(effect) if effect.match_name == "ADBE HUE SATURATION" else None)
                       for effect in effects]
    for frame in range(count):
        t = frame / FPS
        x, y = value(transform.position, t)[:2]
        ax, ay = value(transform.anchor_point, t)[:2]
        sx, sy = value(transform.scale, t)[:2]
        angle = value(rotation, t)
        if int(layer.auto_orient) == 4213:
            before = value(transform.position, t - .001)
            after = value(transform.position, t + .001)
            angle += math.degrees(math.atan2(after[1]-before[1], after[0]-before[0]))
        opacity = value(transform.opacity, t)
        sample = [x, y, ax, ay, sx/100, sy/100, math.radians(angle), opacity/100,
                  value(remap, t) if entry["remap"] else 0.]
        sample += effect_values(sampled_effects, t)
        sample += [value(polar.property(0), t) if polar else 0.]
        if geometry:
            get = lambda index: value(geometry.property(f"ADBE Geometry2-{index:04}"), t)
            ax, ay = get(1)
            x, y = get(2)
            sy = get(3) / 100
            sx = sy if get(11) else get(4) / 100
            sample += [x, y, ax, ay, sx, sy, math.radians(get(7)), get(8)/100]
        else:
            sample += [0., 0., 0., 0., 1., 1., 0., 1.]
        assert len(sample) == 25 and all(math.isfinite(v) for v in sample), (comp.name, layer.name)
        motion.extend(struct.pack("<25f", *sample))
    return entry


def export_comp(comp):
    if comp.name in comps:
        return
    info = dict(width=comp.width, height=comp.height, duration=comp.duration, layers=[])
    comps[comp.name] = info
    needed = set()

    def require(layer):
        if layer is None or layer.index in needed:
            return
        needed.add(layer.index)
        require(layer.track_matte_layer)
        require(layer.parent)

    for layer in comp.layers:
        source = getattr(layer, "source", None)
        if (layer.enabled and source is not None and not layer.name.lower().endswith((".mp3",".wav"))
            and layer.name!="黑洞底图"):
            require(layer)
    for layer in comp.layers:
        if layer.index not in needed:
            continue
        effects = [e for e in layer.property("ADBE Effect Parade") if e.enabled]
        entry = export_layer(layer, comp, effects)
        info["layers"].append(entry)
        if "comp" in entry:
            export_comp(layer.source)


main = py_aep.parse(str(DOWNLOADS / "未花EX (5).aep")).project
icon = Image.open(BytesIO(images["黑洞.png"])).convert("RGBA")
icon = icon.crop(icon.getchannel("A").getbbox())
icon.thumbnail((64, 64), Image.Resampling.LANCZOS)
icon.save(OUTPUT / "DomainIcon.png")
flight = Image.new("RGBA", (120, 72))
curve = [(60 + 46 * math.sin(t), 36 + 23 * math.sin(2*t))
         for t in np.linspace(0, 2*math.pi, 200)]
ImageDraw.Draw(flight).line(curve, fill="white", width=10, joint="curve")
flight.resize((40, 24), Image.Resampling.LANCZOS).save(OUTPUT / "FlightIcon.png")
demo = py_aep.parse(str(DOWNLOADS / "实现一些功能.aep")).project
resonance = next(c for c in main.compositions if c.name == "冲击后段")
export_comp(resonance)
opening = next(c for c in main.compositions if c.name == "裂纹动画")
export_comp(opening)
for comp in demo.compositions:
    if comp.name in ("时间重映射", "实现极坐标", "预合成控制功能"):
        export_comp(comp)

audio = []
with zipfile.ZipFile(DOWNLOADS / "音效 (1).zip") as archive, tempfile.TemporaryDirectory() as temp:
    sources = {archive_name(entry): archive.read(entry) for entry in archive.infolist()}
    for layer in list(resonance.layers)+list(opening.layers):
        if layer.name not in sources:
            continue
        file = Path(temp) / ("input" + Path(layer.name).suffix)
        file.write_bytes(sources[layer.name])
        name = f"Cue{len(audio)}"
        offset = (layer.in_point - layer.start_time) * 100 / layer.stretch
        length = layer.out_point - layer.in_point
        level = layer.property("ADBE Audio Group").property("ADBE Audio Levels").value[0]
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-ss", str(max(0, offset)),
                        "-i", str(file), "-map", "0:a:0", "-t", str(length), "-af", f"volume={level}dB",
                        "-c:a", "libvorbis", "-q:a", "5", str(OUTPUT / f"{name}.ogg")], check=True)
        audio.append(dict(tick=round(layer.in_point * FPS), name=name, opening=layer.name.lower().endswith(".wav")))

with zipfile.ZipFile(DOWNLOADS / "音乐.zip") as archive, tempfile.TemporaryDirectory() as temp:
    source=Path(temp)/"track.mp3"
    source.write_bytes(next(archive.read(e) for e in archive.infolist() if e.filename.lower().endswith(".mp3")))
    # tModLoader's NVorbis decoder rejects video streams, including attached cover art.
    subprocess.run(["ffmpeg","-y","-loglevel","error","-i",str(source),"-map","0:a:0","-c:a","libvorbis","-q:a","5",
                    str(ROOT/"Assets/Music/MikaElizaP2.ogg")],check=True)

(OUTPUT / "Scene.json").write_text(json.dumps(dict(comps=comps, audio=audio, textures=texture_bounds),
                                             ensure_ascii=False, indent=2),
                                  encoding="utf-8")
(OUTPUT / "Motion.bin").write_bytes(motion)
(OUTPUT / "MaskMotion.bin").write_bytes(mask_motion)
print(f"{len(comps)} compositions, {sum(len(c['layers']) for c in comps.values())} layers, "
      f"{len(textures)} textures, {len(audio)} audio cues")
