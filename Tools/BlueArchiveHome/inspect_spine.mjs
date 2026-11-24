import fs from 'node:fs';
import path from 'node:path';
import { TextureAtlas, AtlasAttachmentLoader, SkeletonBinary, Skeleton, AnimationState,
    AnimationStateData, Physics, RegionAttachment, MeshAttachment, FakeTexture } from './SpineJS/package/dist/index.js';

const root = path.resolve('Assets/Tablet/Home');
const report = [];
for (const [folder, stem] of [['Arona','arona_spr'], ['Classroom','arona_workpage_daytime']]) {
    const dir = path.join(root, folder);
    const atlas = new TextureAtlas(fs.readFileSync(path.join(dir, stem+'.atlas.bin'), 'utf8'));
    for (const page of atlas.pages) page.setTexture(new FakeTexture({width:page.width,height:page.height}));
    const reader = new SkeletonBinary(new AtlasAttachmentLoader(atlas));
    const data = reader.readSkeletonData(new Uint8Array(fs.readFileSync(path.join(dir, stem+'.skel.bin'))));
    const skeleton = new Skeleton(data);
    const state = new AnimationState(new AnimationStateData(data));
    const idle = data.animations.find(a=>a.name===(process.argv[2] ?? 'Idle_01')) ?? data.animations.find(a=>a.name==='Idle_01') ?? data.animations[0];
    if (idle) state.setAnimation(0,idle.name,true);
    state.update(0); state.apply(skeleton); skeleton.updateWorldTransform(Physics.update);
    const info={stem,version:data.version,bones:data.bones.length,slots:data.slots.length,
        animations:data.animations.map(a=>({name:a.name,duration:a.duration})),bounds:skeleton.getBoundsRect(),idle:idle?.name,
        slots:data.slots.map(s=>({name:s.name,attachment:s.attachmentName}))};
    report.push(info);
    console.log(JSON.stringify(info));
    const meshes=[];
    for (const slot of skeleton.drawOrder) {
        const a=slot.attachment;
        let vertices,uvs,triangles;
        if (a instanceof RegionAttachment) {
            vertices=new Float32Array(8); a.computeWorldVertices(slot,vertices,0,2);
            uvs=a.uvs; triangles=[0,1,2,2,3,0];
        } else if (a instanceof MeshAttachment) {
            vertices=new Float32Array(a.worldVerticesLength); a.computeWorldVertices(slot,0,a.worldVerticesLength,vertices,0,2);
            uvs=a.uvs; triangles=a.triangles;
        } else continue;
        meshes.push({texture:path.join(dir,a.region.page.name),vertices:Array.from(vertices),
            uvs:Array.from(uvs),triangles:Array.from(triangles),color:slot.color,attachment:a.name});
    }
    fs.writeFileSync(path.join('Tools/BlueArchiveHome',stem+'_pose.json'),JSON.stringify(meshes));
}
fs.writeFileSync('Tools/BlueArchiveHome/spine_inspection.json',JSON.stringify(report,null,2));
