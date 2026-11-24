using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace KivotosMod.Content.Skills.BlackHole;

[Autoload(Side=ModSide.Client)]
public sealed class MikaDomainTuning : ModSystem
{
    internal static bool Open;
    private static readonly int[] Ids={76,106,107,108,160};
    private static readonly MikaVisualSettings Draft=new();
    internal static readonly MikaVisualSettings Committed=new();
    private static int[] Flower=>Draft.Flower;
    private static int[] Apple=>Draft.Apple;
    private static readonly string[] FlowerLabels={"第77层 高光全透明度","第107层 发光遮罩前透明度","第108层 发光遮罩后透明度",
        "第109层 光带下透明度","第161层 光带遮罩透明度","第76层 光谱3饱和度",
        "前方星光发光阈值 (%)","前方星光发光半径","后方星光发光阈值 (%)","后方星光发光半径"};
    private static readonly string[] AppleLabels={"显示范围 X","显示范围 Y","震动频率 (次/秒)","最大震幅 (像素)"};
    private static bool _apple,_locked=true;
    private static float _ratio=1850f/1195;
    private static int[] Values=>_apple?Apple:Flower;
    private static string[] Labels=>_apple?AppleLabels:FlowerLabels;
    private static MikaVisualSettings Effective=>MikaExDomain.Active?MikaExDomain.Visual:Committed;
    internal static int Saturation=>Effective.Flower[5];
    internal static float Opacity(int index,float original){int i=Array.IndexOf(Ids,index);return i<0?original:Effective.Flower[i]/100f;}
    internal static int ViewWidth=>Effective.Apple[0];
    internal static int ViewHeight=>Effective.Apple[1];
    internal static int ShakeFrequency=>Effective.Apple[2];
    internal static int ShakeAmplitude=>Effective.Apple[3];
    internal static Point StarBloom(bool back)
    {
        int i=back?8:6;
        return new Point(Effective.Flower[i],Effective.Flower[i+1]);
    }
    private static int _focus=-1;
    private static string _text="";
    private static bool _leftWasDown;
    private static KeyboardState _previousKeys;
    private static Vector2 _mouse;
    private static int Top=>Math.Max(8,Math.Min(115,(int)(Terraria.GameInput.PlayerInput.RealScreenHeight/Main.UIScale)-PanelHeight-8));
    private static int PanelHeight=>_apple?239:391;
    private static Rectangle Panel=>new(32,Top,515,PanelHeight);
    private static Rectangle CloseButton=>new(513,Top+5,27,27);
    private static Rectangle LockButton=>new(48,Top+170,250,26);
    private static Rectangle Field(int index)=>new(414,Top+38+index*32,112,27);
    internal static void Toggle(bool apple=false)
    {
        MikaShatterTuning.Close();
        bool open=!Open || _apple!=apple;
        EndInput();_apple=apple;Open=open;
        _leftWasDown=Mouse.GetState().LeftButton==ButtonState.Pressed;_previousKeys=Keyboard.GetState();
    }
    internal static void Close(){Open=false;EndInput();}
    private static void Commit()
    {
        if(_focus<0)return;
        Apple.CopyTo(Committed.Apple,0);
        Flower.CopyTo(Committed.Flower,0);
        Main.LocalPlayer.GetModPlayer<MikaExPlayer>().SendVisualSettings();
    }
    private static void EndInput(){Commit();if(_focus>=0)Main.blockInput=false;_focus=-1;}
    public override void OnWorldUnload(){Open=false;EndInput();}
    public override void UpdateUI(GameTime gameTime)
    {
        if(!Open)return;
        var keys=Keyboard.GetState();var previous=_previousKeys;_previousKeys=keys;
        bool startedFocus=false;
        // Mouse.GetState is in physical client pixels. Convert exactly once to
        // the UI coordinates used by InterfaceScaleType.UI, regardless of which
        // vanilla/mod interface has already consumed Main.mouseLeftRelease.
        var state=Mouse.GetState();
        _mouse=new Vector2(state.X,state.Y)/Main.UIScale;
        bool leftDown=state.LeftButton==ButtonState.Pressed;
        bool pressed=leftDown && !_leftWasDown;_leftWasDown=leftDown;
        if(Panel.Contains(_mouse.ToPoint()))Main.LocalPlayer.mouseInterface=true;
        if(pressed)
        {
            if(CloseButton.Contains(_mouse.ToPoint())){Open=false;EndInput();return;}
            if(_apple && LockButton.Contains(_mouse.ToPoint()))
            {
                EndInput();_locked=!_locked;
                if(_locked)_ratio=(float)Apple[0]/Apple[1];
                return;
            }
            int next=-1;
            for(int i=0;i<Values.Length;i++)if(Field(i).Contains(_mouse.ToPoint())){next=i;break;}
            if(next<0)EndInput();
            else if(next!=_focus){EndInput();_focus=next;_text="";startedFocus=true;}
        }
        if(keys.IsKeyDown(Keys.Escape)){Open=false;EndInput();return;}
        if(_focus<0)return;
        Main.blockInput=true;
        bool signed=!_apple && _focus==5;
        if(!startedFocus)_text=MikaNumericInput.Read(_text,keys,previous,signed,_apple && _focus<2?4:3);
        if(int.TryParse(_text,out int value))SetValue(value,signed);
        if(keys.IsKeyDown(Keys.Enter) && !previous.IsKeyDown(Keys.Enter))EndInput();
    }
    private static void SetValue(int value,bool signed)
    {
        if(!_apple || _focus>=2)
        {
            Values[_focus]=Math.Clamp(value,signed?-100:0,_apple && _focus==2?60:100);
            return;
        }
        const int min=256,max=3072;
        float ratio=_focus==0?_ratio:1/_ratio;
        int low=_locked?Math.Max(min,(int)MathF.Ceiling(min*ratio)):min;
        int high=_locked?Math.Min(max,(int)MathF.Floor(max*ratio)):max;
        Apple[_focus]=Math.Clamp(value,low,high);
        if(_locked)Apple[1-_focus]=Math.Clamp((int)MathF.Round(Apple[_focus]/ratio),min,max);
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if(Open)layers.Add(new LegacyGameInterfaceLayer("Kivotos:DomainTuning",Draw,InterfaceScaleType.UI));
    }
    private static bool Draw()
    {
        var sb=Main.spriteBatch;var pixel=Terraria.GameContent.TextureAssets.MagicPixel.Value;
        sb.Draw(pixel,Panel,new Color(24,20,42,240));
        Utils.DrawBorderString(sb,_apple?"未花黑洞视野苹果":"未花黑洞调色花",new Vector2(48,Top+8),Color.Plum,.85f);
        for(int i=0;i<Values.Length;i++)
        {
            var box=Field(i);int y=box.Y+2;
            Utils.DrawBorderString(sb,Labels[i],new Vector2(48,y),Color.White,.7f);
            sb.Draw(pixel,box,_focus==i?new Color(115,75,145):box.Contains(_mouse.ToPoint())?new Color(75,65,100):new Color(45,40,65));
            Utils.DrawBorderString(sb,_focus==i?_text+"|":Values[i].ToString(),new Vector2(box.X+8,y),Color.White,.8f);
        }
        if(_apple)
        {
            sb.Draw(pixel,LockButton,_locked?new Color(115,75,145):new Color(45,40,65));
            Utils.DrawBorderString(sb,_locked?"宽高比锁定：开":"宽高比锁定：关",new Vector2(56,LockButton.Y+1),Color.White,.7f);
        }
        Utils.DrawBorderString(sb,_apple?"尺寸 256–3072，频率 0–60，振幅 0–100":"透明度与发光 0–100，饱和度 -100–100",
            new Vector2(48,Top+PanelHeight-37),Color.LightGray,.6f);
        Utils.DrawBorderString(sb,"Enter 或点击空白应用，Esc 关闭",new Vector2(48,Top+PanelHeight-19),Color.LightGray,.6f);
        Utils.DrawBorderString(sb,"×",new Vector2(CloseButton.X,CloseButton.Y),Color.White);
        return true;
    }
}
