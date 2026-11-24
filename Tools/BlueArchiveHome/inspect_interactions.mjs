import fs from 'node:fs';
import path from 'node:path';
import { TextureAtlas, AtlasAttachmentLoader, SkeletonBinary, Skeleton, AnimationState,
    AnimationStateData, Physics, FakeTexture } from './SpineJS/package/dist/index.js';

export function load(folder, stem) {
    const dir=path.resolve('Assets/Tablet/Home',folder);
    const atlas=new TextureAtlas(fs.readFileSync(path.join(dir,stem+'.atlas.bin'),'utf8'));
    for(const page of atlas.pages) page.setTexture(new FakeTexture({width:page.width,height:page.height}));
    return new SkeletonBinary(new AtlasAttachmentLoader(atlas)).readSkeletonData(new Uint8Array(fs.readFileSync(path.join(dir,stem+'.skel.bin'))));
}
export function snapshot(skeleton) {
    return {visible:skeleton.slots.filter(s=>s.attachment&&s.color.a>0.01).map(s=>s.data.name),
        attachments:skeleton.slots.filter(s=>s.attachment).map(s=>s.data.name+'='+s.attachment.name),
        bones:skeleton.bones.map(b=>[b.worldX,b.worldY,b.a,b.b,b.c,b.d])};
}
if(process.argv[1]?.endsWith('inspect_interactions.mjs')) {
    const reports=[];
    for(const [folder,stem,names] of [['Arona','arona_spr',['02','04','06','08','Idle_01','Eye_Close_01','Pat_01_A','Look_01_A']],
        ['Classroom','arona_workpage_daytime',['Idle_01','Idle_01_Touch_A','Idle_01_Touch_M']]]) {
        const data=load(folder,stem);
        const report={stem,animations:[]};
        for(const name of names) {
            const a=data.findAnimation(name); if(!a)continue;
            const summary={name,duration:a.duration,timelines:{},attachmentTimeline:[]};
            for(const timeline of a.timelines) {
                const type=timeline.constructor.name;
                summary.timelines[type]=(summary.timelines[type]??0)+1;
                if(type==='AttachmentTimeline') summary.attachmentTimeline.push({slot:data.slots[timeline.slotIndex].name,
                    names:timeline.attachmentNames,frames:Array.from(timeline.frames)});
            }
            report.animations.push(summary);
        }
        for(const track of [0,2]) {
            const skeleton=new Skeleton(data);const stateData=new AnimationStateData(data);stateData.defaultMix=.15;
            const state=new AnimationState(stateData);state.setAnimation(0,'Idle_01',true);
            state.update(1);state.apply(skeleton);skeleton.updateWorldTransform(Physics.update);
            const start=snapshot(skeleton);
            const touch=stem==='arona_spr'?'02':'Idle_01_Touch_A';state.setAnimation(track,touch,false);
            const samples=[];
            for(let i=0;i<90;i++) {
                state.update(1/60);state.apply(skeleton);skeleton.updateWorldTransform(Physics.update);
                if([0,9,29,59,89].includes(i)) {
                    const snap=snapshot(skeleton);
                    samples.push({time:(i+1)/60,visible:snap.visible.length,lost:start.visible.filter(n=>!snap.visible.includes(n)),
                        changedAttachments:snap.attachments.filter(n=>!start.attachments.includes(n))});
                }
            }
            report['track'+track]=samples;
        }
        reports.push(report);
    }
    fs.writeFileSync('Tools/BlueArchiveHome/interaction_inspection.json',JSON.stringify(reports,null,2));
    for(const report of reports) {
        console.log(report.stem);
        for(const a of report.animations)console.log(JSON.stringify({name:a.name,duration:a.duration,timelines:a.timelines}));
        console.log('Replace idle',JSON.stringify(report.track0));
        console.log('Overlay idle',JSON.stringify(report.track2));
    }
}
