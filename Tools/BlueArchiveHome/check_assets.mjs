import fs from 'node:fs';
import path from 'node:path';
import { TextureAtlas, AtlasAttachmentLoader, SkeletonBinary, Skeleton, AnimationState,
    AnimationStateData, Physics, RegionAttachment, MeshAttachment, FakeTexture } from './SpineJS/package/dist/index.js';

const checks=[];
for (const [folder,stem,animations] of [
    ['Arona','arona_spr',['Idle_01','Eye_Close_01','02']],
    ['Classroom','arona_workpage_daytime',['Idle_background_00','Idle_01','Idle_01_Touch_A','Idle_01_Touch_M']]]) {
    const dir=path.resolve('Assets/Tablet/Home',folder);
    const atlas=new TextureAtlas(fs.readFileSync(path.join(dir,stem+'.atlas.bin'),'utf8'));
    for (const page of atlas.pages) {
        if (!fs.existsSync(path.join(dir,page.name))) throw Error('Missing atlas page '+page.name);
        page.setTexture(new FakeTexture({width:page.width,height:page.height}));
    }
    const data=new SkeletonBinary(new AtlasAttachmentLoader(atlas)).readSkeletonData(new Uint8Array(fs.readFileSync(path.join(dir,stem+'.skel.bin'))));
    let poses=0, meshes=0;
    for(const name of animations) {
        const animation=data.findAnimation(name);
        if(!animation) throw Error('Missing animation '+name);
        for(const time of [0,animation.duration/2,Math.max(0,animation.duration-0.01)]) {
            const skeleton=new Skeleton(data);
            const state=new AnimationState(new AnimationStateData(data));
            state.setAnimation(0,name,true); state.update(time); state.apply(skeleton); skeleton.updateWorldTransform(Physics.update);
            for(const slot of skeleton.drawOrder) {
                const a=slot.attachment;
                let vertices;
                if(a instanceof RegionAttachment) { vertices=new Float32Array(8); a.computeWorldVertices(slot,vertices,0,2); }
                else if(a instanceof MeshAttachment) {
                    vertices=new Float32Array(a.worldVerticesLength); a.computeWorldVertices(slot,0,a.worldVerticesLength,vertices,0,2);
                    if(a.triangles.some(i=>i<0||i>=vertices.length/2)) throw Error('Bad mesh indices '+a.name);
                } else continue;
                if([...vertices,...a.uvs].some(v=>!Number.isFinite(v))) throw Error('Bad vertex '+a.name);
                meshes++;
            }
            poses++;
        }
    }
    checks.push({stem,version:data.version,atlasPages:atlas.pages.length,animations,poses,meshes});
}
const icons=['Cafe','SchaleBilding','Student','Edit','Social','Craft','Shop','Gacha','Notice','MoMoTalk','DailyReward','Gembag','Stamina','Gold','Diamond','Mail','Menu_Small','SpecialLobby','Change_Count'].map(n=>'Common_Icon_'+n);
icons.push('School_Icon_Schedule','Common_Bg_Raius5px_Shadow','Common_UIHide_Icon2','Lobby_Icon_Work','Cafe_Icon_Arrow','Common_Plus_Btn',
    'Lobby_Gacha_Banner_210204_1','Event_Banner_841_Jp','Event_Banner_842_Jp','UIWorkAronaSceneBG_DecoColor');
for(const name of icons) if(!fs.existsSync(path.join('Assets/Tablet/Home/UI',name+'.png'))) throw Error('Missing UI '+name);
const result={spine:checks,uiTextures:icons.length,passed:true};
fs.writeFileSync('Tools/BlueArchiveHome/validation.json',JSON.stringify(result,null,2));
console.log(JSON.stringify(result));
