using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace KivotosMod.Content.Skills.BlackHole;

[Autoload(Side=ModSide.Client)]
public sealed class MikaShatterTuning : ModSystem
{
    private sealed record Field(string Label,float Min,float Max,Func<MikaShatter.Physics,float> Read,
        Action<MikaShatter.Physics,float> Write);
    private static readonly Field[] Fields=
    {
        new("凸出深度",0,2,p=>p.Extrusion,(p,v)=>p.Extrusion=v),
        new("作用力深度",-3,3,p=>p.ForceDepth,(p,v)=>p.ForceDepth=v),
        new("作用力强度",-40,40,p=>p.Strength,(p,v)=>p.Strength=v),
        new("旋转速度",0,3,p=>p.Spin,(p,v)=>p.Spin=v),
        new("随机性",0,1,p=>p.Randomness,(p,v)=>p.Randomness=v),
        new("重力",0,20,p=>p.Gravity,(p,v)=>p.Gravity=v),
        new("重力 Z 方向",-1,1,p=>p.GravityZ,(p,v)=>p.GravityZ=v),
        new("灯光深度",-5,5,p=>p.LightDepth,(p,v)=>p.LightDepth=v),
        new("镜面反射",0,1,p=>p.Specular,(p,v)=>p.Specular=v),
        new("裂纹第16层 色相",-180,180,_=>CrackGrade.X,(_,v)=>OpeningGrade=CrackGrade with { X=v }),
        new("裂纹第16层 饱和度",-100,100,_=>CrackGrade.Y,(_,v)=>OpeningGrade=CrackGrade with { Y=v })
    };
    private static MikaShatter.Physics _settings;
    internal static MikaShatter.Physics Current=>_settings??MikaShatter.Defaults;
    internal static Vector2 DefaultOpeningGrade { get; set; }
    internal static Vector2? OpeningGrade { get; private set; }
    private static Vector2 CrackGrade => OpeningGrade??DefaultOpeningGrade;
    private static bool _open,_pressed;
    private static int _focus=-1;
    private static KeyboardState _prior;
    private static string _text="";
    private static int ViewHeight => (int)(Terraria.GameInput.PlayerInput.RealScreenHeight/Main.UIScale);
    private static int RowHeight => Math.Clamp((ViewHeight-92)/Fields.Length,20,28);
    private static int PanelHeight => 76+Fields.Length*RowHeight;
    private static int Top=>Math.Clamp(ViewHeight-PanelHeight-8,8,90);
    private static Rectangle Panel=>new(34,Top,430,PanelHeight);
    private static Rectangle ResetButton=>new(48,Top+40+Fields.Length*RowHeight,120,28);
    private static Rectangle CloseButton=>new(429,Top+2,30,30);
    private static Rectangle Input(int i)=>new(340,Top+37+i*RowHeight,105,RowHeight-4);

    internal static void Toggle()
    {
        if(MikaShatter.Defaults==null)return;
        if(_open){Close();return;}
        if(MikaDomainTuning.Open)MikaDomainTuning.Close();
        _open=true;_prior=Keyboard.GetState();_pressed=Mouse.GetState().LeftButton==ButtonState.Pressed;
    }
    internal static void Close()
    {
        if(_focus>=0)Main.blockInput=false;
        _focus=-1;_open=false;
    }
    public override void OnWorldUnload()=>Close();
    public override void Unload(){Close();_settings=null;OpeningGrade=null;}

    public override void UpdateUI(GameTime time)
    {
        if(!_open)return;
        var keys=Keyboard.GetState();
        var mouse=Mouse.GetState();
        Point point=(new Vector2(mouse.X,mouse.Y)/Main.UIScale).ToPoint();
        bool down=mouse.LeftButton==ButtonState.Pressed;
        bool clicked=down&&!_pressed;
        _pressed=down;
        if(Panel.Contains(point))Main.LocalPlayer.mouseInterface=true;
        if(keys.IsKeyDown(Keys.Escape) || (clicked && CloseButton.Contains(point))){Close();return;}
        if(clicked)
        {
            if(_focus>=0)Main.blockInput=false;
            _focus=-1;
            if(ResetButton.Contains(point)){_settings=null;OpeningGrade=null;}
            for(int i=0;i<Fields.Length;i++)
                if(Input(i).Contains(point)){_focus=i;_text="";break;}
        }
        if(_focus>=0)
        {
            Main.blockInput=true;
            var field=Fields[_focus];
            if(!clicked) _text=MikaNumericInput.Read(_text,keys,_prior,field.Min<0,8,decimalPoint:true);
            if(float.TryParse(_text,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,out float value))
            {
                _settings??=MikaShatter.Defaults.Copy();
                field.Write(_settings,Math.Clamp(value,field.Min,field.Max));
            }
            if(keys.IsKeyDown(Keys.Enter)&&!_prior.IsKeyDown(Keys.Enter))
            {
                _focus=-1;Main.blockInput=false;
            }
        }
        _prior=keys;
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if(_open)layers.Add(new LegacyGameInterfaceLayer("Kivotos: Shatter Tuning",Draw,InterfaceScaleType.UI));
    }
    private static bool Draw()
    {
        var batch=Main.spriteBatch;
        var pixel=Terraria.GameContent.TextureAssets.MagicPixel.Value;
        batch.Draw(pixel,Panel,new Color(24,20,42,242));
        Utils.DrawBorderString(batch,"未花碎片调试桃",new Vector2(48,Top+9),Color.Plum,.85f);
        for(int i=0;i<Fields.Length;i++)
        {
            var field=Fields[i];var box=Input(i);
            Utils.DrawBorderString(batch,$"{field.Label}  [{field.Min}, {field.Max}]",new Vector2(48,box.Y),Color.White,.68f);
            batch.Draw(pixel,box,_focus==i?new Color(115,75,145):new Color(52,44,74));
            Utils.DrawBorderString(batch,_focus==i?_text+"|":field.Read(Current).ToString("0.###",CultureInfo.InvariantCulture),
                new Vector2(box.X+7,box.Y),Color.White,.70f);
        }
        batch.Draw(pixel,ResetButton,new Color(85,56,111));
        Utils.DrawBorderString(batch,"重置默认值",new Vector2(56,ResetButton.Y+3),Color.White,.68f);
        Utils.DrawBorderString(batch,"×",new Vector2(CloseButton.X+7,CloseButton.Y),Color.White);
        return true;
    }
}
