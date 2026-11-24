using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace KivotosMod.Content.Skills.BlackHole;

internal static class MikaDomainStars
{
    internal sealed class Star
    {
        internal bool Back,Active,Wide;
        internal string Texture,Glow;
        internal Vector2 Original,Scale,GlowOffset,GlowScale;
        internal float Angle,GlowAngle,Hue,Saturation,Intensity,Size,Period,RotationSpeed;
        internal int Born,Kind;
    }
    private sealed class Spark { internal Vector2 Position,Velocity;internal int Age;internal float Rotation; }
    private static readonly List<Star> Stars=new();
    private static readonly List<Spark> Sparks=new();
    private static Vector2 _velocity;
    private static string _mask,_sparkTexture;
    private static float _frontMax,_frontMin=float.MaxValue,_backMax;
    internal static Vector2 BackMaskScale=Vector2.One;
    private static int _expired,_scatter=-1;
    private static readonly int[] SharedExpirations=new int[60];
    private static int _sharedFlying;
    internal static int ActiveCount => Stars.FindAll(s=>s.Active).Count;
    private const int SparksPerStar=20;
    internal static string Status=>$"领域：{BlackHolePreviewSystem.Enabled}，命中星星：{Sparks.Count}，消失累计：{_expired}/{SparksPerStar}，星光：{Stars.FindAll(s=>s.Active).Count}/{Stars.Count}，扩散：{_scatter>=0}";
    internal static void Load()
    {
        if(Stars.Count>0)return;
        using var doc=JsonDocument.Parse(ModContent.GetInstance<KivotosMod>().GetFileBytes("Assets/Skills/MikaDomain/Stars.json"));
        var root=doc.RootElement;var v=root.GetProperty("velocity");_velocity=new(v[0].GetSingle(),v[1].GetSingle());_mask=root.GetProperty("mask").GetString();
        var ms=root.GetProperty("maskScale");BackMaskScale=new Vector2(ms[0].GetSingle(),ms[1].GetSingle())/100;
        int i=0;
        foreach(var row in root.GetProperty("stars").EnumerateArray())
        {
            Vector2 Pair(string key){var a=row.GetProperty(key);return new(a[0].GetSingle(),a[1].GetSingle());}
            var s=new Star{Back=row.GetProperty("back").GetBoolean(),Texture=row.GetProperty("texture").GetString(),Glow=row.GetProperty("glow").GetString(),
                Original=Pair("position"),Scale=Pair("scale")/100,GlowOffset=Pair("glowPosition")-Pair("position"),GlowScale=Pair("glowScale")/100,
                Angle=MathHelper.ToRadians(row.GetProperty("angle").GetSingle()),GlowAngle=MathHelper.ToRadians(row.GetProperty("glowAngle").GetSingle()),
                Hue=row.GetProperty("hue").GetSingle(),Saturation=row.GetProperty("saturation").GetSingle(),Kind=row.GetProperty("kind").GetInt32(),
                Intensity=row.GetProperty("intensity").GetSingle(),Wide=(i++%10)>=7};
            s.Size=Math.Max(Math.Abs(s.Scale.X),Math.Abs(s.Scale.Y));Stars.Add(s);
            if(s.Back)_backMax=Math.Max(_backMax,s.Size);else _frontMax=Math.Max(_frontMax,s.Size);
            if(!s.Back)_frontMin=Math.Min(_frontMin,s.Size);
        }
        _sparkTexture=Stars[0].Texture;
        // Back-to-front order: spheres, hearts, crosses; small before large.
        Stars.Sort((a,b)=>{if(a.Back || b.Back)return a.Back.CompareTo(b.Back);int kind=a.Kind.CompareTo(b.Kind);return kind!=0?kind:a.Size.CompareTo(b.Size);});
    }
    internal static void Reset()
    {
        foreach(var s in Stars)s.Active=false;
        Sparks.Clear();_expired=0;_scatter=-1;_sharedFlying=0;
        Array.Clear(SharedExpirations);
    }
    internal static void Preload(MikaDomainRenderer renderer){foreach(var s in Stars){renderer.Texture(s.Texture);renderer.Texture(s.Glow);}renderer.Texture(_mask);}
    internal static void PrepareBloom(MikaDomainRenderer renderer,bool warmUp)
    {
        foreach(var s in Stars)
            if(s.Active || warmUp)renderer.PrepareBloom(s.Texture,s.Back);
    }
    internal static void Hit(Vector2 worldPosition)
    {
        if(Main.dedServ || !BlackHolePreviewSystem.Enabled || BlackHolePreviewSystem.PaintDemo ||
            BlackHolePreviewSystem.CompositionDemo!=null || BlackHolePreviewSystem.ResonanceTick>=MikaExState.ScatterTick)return;
        AddSparks(worldPosition,Main.rand.Next(4,8),Main.rand.Next(),0);
    }
    internal static void AddSparks(Vector2 position,int count,int seed,int age)
    {
        if(Main.dedServ || age>=60)return;
        Load();
        var random=new Random(seed);
        for(int i=0;i<count && Sparks.Count<512;i++)
        {
            float angle=(float)random.NextDouble()*MathHelper.TwoPi;
            var velocity=new Vector2(MathF.Cos(angle),MathF.Sin(angle))*(float)(4*random.NextDouble());
            float damping=MathF.Pow(.96f,age);
            Sparks.Add(new Spark{Position=position+velocity*((1-damping)/.04f),Velocity=velocity*damping,
                Rotation=(float)random.NextDouble()*MathHelper.TwoPi,Age=age});
        }
    }
    internal static int QueueSharedHit(int tick,int count)
    {
        count=Math.Min(count,512-_sharedFlying);
        SharedExpirations[tick%60]+=count;
        _sharedFlying+=count;
        return count;
    }
    internal static bool ScheduleSharedScatter(int at,bool restart=true)
    {
        if(_scatter>=0 && !restart)return false;
        _scatter=at;
        return true;
    }
    internal static bool AdvanceShared(int now)
    {
        int slot=now%60;
        int expired=SharedExpirations[slot];
        _sharedFlying-=expired;SharedExpirations[slot]=0;
        if(_scatter<0 || now<_scatter)_expired+=expired;
        return AdvanceStars(now);
    }
    internal static void WriteShared(BinaryWriter writer)
    {
        Load();
        writer.Write(_scatter);
        writer.Write((byte)_expired);
        writer.Write((byte)Stars.Count);
        foreach(var star in Stars)writer.Write((short)(star.Active?star.Born:-1));
    }
    internal static void ReadShared(BinaryReader reader)
    {
        Load();
        _scatter=reader.ReadInt32();
        _expired=reader.ReadByte();
        int count=reader.ReadByte();
        for(int i=0;i<count;i++)
        {
            int born=reader.ReadInt16();
            if(i>=Stars.Count)continue;
            if(born<0)Stars[i].Active=false;
            else Activate(i,born);
        }
    }
    internal static void Scatter(bool restart=false)
    {
        if(!MikaExDomain.Active && BlackHolePreviewSystem.Enabled && (_scatter<0 || restart))
            _scatter=BlackHolePreviewSystem.Ticks+MikaExState.ScatterTick;
    }
    internal static void Update()
    {
        int now=BlackHolePreviewSystem.Ticks;
        for(int i=Sparks.Count-1;i>=0;i--)
        {
            var p=Sparks[i];p.Position+=p.Velocity;p.Velocity*=.96f;
            if(++p.Age<60)continue;
            Sparks.RemoveAt(i);
            if(!MikaExDomain.Active && (_scatter<0 || now<_scatter))_expired++;
        }
        if(!MikaExDomain.Active)AdvanceStars(now);
    }
    private static bool AdvanceStars(int now)
    {
        bool changed=false;
        if(_expired>=SparksPerStar)Load();
        while(_expired>=SparksPerStar && (_scatter<0 || now<_scatter))
        {
            int free=Stars.Count-ActiveCount;
            if(free==0){_expired=0;break;}
            _expired-=SparksPerStar;
            int pick=Main.rand.Next(free);
            for(int i=0;i<Stars.Count;i++)
                if(!Stars[i].Active && pick--==0){Activate(i,now);break;}
            changed=true;
        }
        if(now==_scatter)_expired=0;
        if(_scatter>=0 && now>=_scatter+60)
        {
            foreach(var s in Stars)s.Active=false;
            _expired=0;_scatter=-1;changed=true;
        }
        return changed;
    }
    private static void Activate(int index,int born)
    {
        var star=Stars[index];
        star.Active=true;star.Born=born;
        var random=new Random(unchecked(born*7919+index*2971));
        star.Period=16*(1+.5f*(float)random.NextDouble());
        star.RotationSpeed=star.Kind==0?0:MathHelper.ToRadians(15*(.6f+.9f*(float)random.NextDouble()));
    }
    private static float Left=>MikaExDomain.Active?779.5f-MikaExDomain.Visual.Apple[0]/2f:BlackHolePreviewSystem.Left;
    private static void Path(Star s,out Vector2 velocity,out float start,out float end)
    {
        float factor=s.Back ? -MathHelper.Clamp(.3f*s.Size/Math.Max(.001f,_backMax),.1f,.3f) : MathHelper.Lerp(.5f,1.2f,MathHelper.Clamp((s.Size-_frontMin)/Math.Max(.001f,_frontMax-_frontMin),0,1));
        // Entire body, bottom layer and padded bloom must clear the viewport
        // before wrapping. Include rotation and any AE-authored pair offset.
        float margin=Math.Max(s.Size*459,s.GlowScale.Length()*259)*.75f+s.GlowOffset.Length()+32;
        float left=Math.Min(Left-margin,s.Original.X-margin),right=Math.Max(1559-Left+margin,s.Original.X+margin);
        velocity=_velocity*factor;start=((s.Back?right:left)-s.Original.X)/velocity.X;
        end=((s.Back?left:right)-s.Original.X)/velocity.X;
    }
    private static float TravelAge(Star s,int tick)
    {
        Path(s,out _,out float start,out float end);
        return (float)(((tick-s.Born)/60.0)%Math.Max(.01f,end-start));
    }
    private static Vector2 Position(Star s,int tick)
    {
        Path(s,out var velocity,out float start,out _);
        return s.Original+velocity*(start+TravelAge(s,tick));
    }
    internal static void Draw(MikaDomainRenderer renderer,bool back)
    {
        Load();int now=BlackHolePreviewSystem.Ticks;
        foreach(var s in Stars)
        {
            if(!s.Active || s.Back!=back || now<s.Born)continue;
            float burst=_scatter>=0 && now>=_scatter?now-_scatter:0;
            if(burst>=60)continue;
            float span=1559-2*Left;
            float distance=MathF.Sqrt(span*span+1007*1007)+600;
            Vector2 origin=Position(s,burst>0?_scatter:now);
            Vector2 direction=(origin-new Vector2(779.5f,503.5f)).SafeNormalize(Vector2.UnitX);
            Vector2 pos=origin+direction*(distance*burst*burst/3600);
            float age=burst>0?TravelAge(s,_scatter)+burst/60:TravelAge(s,now);
            Path(s,out var motion,out float start,out _);
            float spawnX=s.Original.X+motion.X*start;
            float entry=((s.Back?1559-Left:Left)-spawnX)/motion.X;
            float visibleAge=Math.Max(0,age-Math.Max(0,entry));
            float range=s.Kind==1?15:s.Wide?50:30;
            float hue=s.Hue+range*MathF.Sin(visibleAge*MathHelper.TwoPi/s.Period);
            float rotation=s.RotationSpeed*visibleAge;
            float angle=s.Angle+rotation;
            float bloomMix=MathHelper.Clamp(burst/12,0,1);
            float flicker=.9f+.1f*MathF.Sin(now*.12f+s.Original.X*.03f);
            if(burst>0)for(int j=16;j>=1;j--)renderer.DrawBloom(s.Texture,back,true,pos-direction*j*(2+burst*.6f),s.Scale*(1-j/24f),angle,hue+j*20,1,back?_mask:null,.24f*(1-j/17f));
            renderer.Sprite(s.Glow,pos+s.GlowOffset.RotatedBy(rotation),s.GlowScale,s.GlowAngle+rotation,hue,s.Saturation,back?_mask:null,1,Vector3.Zero);
            renderer.Sprite(s.Texture,pos,s.Scale,angle,hue,s.Saturation,back?_mask:null,1,Vector3.Zero);
            // AE Composite Original=Behind puts the original behind its glow.
            if(bloomMix<1)renderer.DrawBloom(s.Texture,back,false,pos,s.Scale,angle,hue,s.Saturation,back?_mask:null,s.Intensity*(1-bloomMix)*flicker);
            if(bloomMix>0)renderer.DrawBloom(s.Texture,back,true,pos,s.Scale,angle,hue,s.Saturation,back?_mask:null,s.Intensity*bloomMix*flicker);
        }
    }
    internal static void DrawSparks()
    {
        if(_sparkTexture==null)return;
        var texture=ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>("KivotosMod/Assets/Skills/MikaDomain/"+_sparkTexture).Value;
        foreach(var p in Sparks)Main.spriteBatch.Draw(texture,p.Position-Main.screenPosition,null,Color.White*(1-p.Age/60f),p.Rotation,texture.Size()/2, .035f,0,0);
    }
}
