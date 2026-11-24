import fs from 'node:fs';
import assert from 'node:assert/strict';
import { Skeleton, AnimationState, AnimationStateData, Physics } from './SpineJS/package/dist/index.js';
import { load, snapshot } from './inspect_interactions.mjs';

const results=[];
for(const [folder,stem] of [['Arona','arona_spr'],['Classroom','arona_workpage_daytime']]) {
    const data=load(folder,stem);
    const standing=folder==='Arona';
    const skeleton=new Skeleton(data);
    const stateData=new AnimationStateData(data);stateData.defaultMix=.15;
    const state=new AnimationState(stateData);state.setAnimation(0,'Idle_01',true);
    const essential=standing?['Head_01','Torso_Original']:
        ['A_01_Head_01','A_01_B_Torso_01','A_01_F_Torso','A_01_F_Skirt_01','A_01_lag'];
    // Match the C# controller: idle on 0; paired motion/attachments on 1/2;
    // blink on 3, suppressed during interaction; reject overlapping touches.
    let touchRemaining=0,blinkTimer=2.7,played=0;
    function overlay(track,name) {
        assert(data.findAnimation(name),`Missing ${name}`);
        state.setAnimation(track,name,false);
        state.addEmptyAnimation(track,.15,0);
        return data.findAnimation(name).duration;
    }
    function touch() {
        if(touchRemaining>0)return;
        state.clearTrack(3);
        let duration;
        if(standing)duration=overlay(2,'02');
        else duration=Math.max(overlay(1,'Idle_01_Touch_M'),overlay(2,'Idle_01_Touch_A'));
        touchRemaining=duration+.15;
        blinkTimer=1;
        played++;
    }
    const idleRotations=data.findAnimation('Idle_01').timelines.filter(t=>t.constructor.name==='RotateTimeline');
    const motionRanges=idleRotations.map(t=>({bone:skeleton.bones[t.boneIndex],min:Infinity,max:-Infinity}));
    let minVisible=Infinity;
    for(let frame=0;frame<780;frame++) {
        if([60,70,75,360,370].includes(frame))touch();
        const dt=1/60;touchRemaining=Math.max(0,touchRemaining-dt);
        state.update(dt);
        if(standing&&touchRemaining<=0&&(blinkTimer-=dt)<=0) {
            state.setAnimation(3,'Eye_Close_01',false);state.addEmptyAnimation(3,.08,.14);blinkTimer=3.6;
        }
        state.apply(skeleton);skeleton.updateWorldTransform(Physics.update);
        assert.equal(state.getCurrent(0).animation.name,'Idle_01');
        for(const name of essential) {
            const slot=skeleton.findSlot(name);
            assert(slot,`Missing required slot ${stem}/${name}`);
            assert(slot.attachment,`${stem}: ${name} disappeared at frame ${frame}`);
            assert(slot.color.a>.01,`${stem}: ${name} became invisible at frame ${frame}`);
        }
        minVisible=Math.min(minVisible,snapshot(skeleton).visible.length);
        if(frame>=85&&frame<110) for(const range of motionRanges) {
            range.min=Math.min(range.min,range.bone.rotation);range.max=Math.max(range.max,range.bone.rotation);
        }
    }
    const motion=motionRanges.sort((a,b)=>(b.max-b.min)-(a.max-a.min))[0];
    assert(motion.max-motion.min>.01,`${stem}: idle froze during touch`);
    assert.equal(played,2,'Rapid repeated clicks must not restart the interaction');
    for(const track of [1,2])assert(!state.getCurrent(track),`${stem}: interaction did not clear`);
    const control=new Skeleton(data);
    const idleState=new AnimationState(stateData);idleState.setAnimation(0,'Idle_01',true);
    idleState.update(state.getCurrent(0).trackTime);idleState.apply(control);
    assert.deepEqual(snapshot(skeleton).attachments,snapshot(control).attachments,`${stem}: attachments did not restore to idle`);
    results.push({stem,frames:780,touches:played,minVisible,idleMotionBone:motion.bone.data.name,
        idleRotationRange:motion.max-motion.min,bodyPreserved:true,overlaysCleared:true});
}
fs.writeFileSync('Tools/BlueArchiveHome/interaction_validation.json',JSON.stringify({passed:true,results},null,2));
console.log(JSON.stringify(results));
