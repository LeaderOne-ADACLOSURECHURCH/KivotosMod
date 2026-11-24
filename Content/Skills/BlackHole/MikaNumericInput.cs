using Microsoft.Xna.Framework.Input;

namespace KivotosMod.Content.Skills.BlackHole;

// Numeric controls need physical number keys, not Terraria's shared chat/IME buffer.
internal static class MikaNumericInput
{
    internal static string Read(string text,KeyboardState now,KeyboardState previous,bool signed,int digits=3,bool decimalPoint=false)
    {
        bool Pressed(Keys key)=>now.IsKeyDown(key) && !previous.IsKeyDown(key);
        if((now.IsKeyDown(Keys.LeftControl) || now.IsKeyDown(Keys.RightControl)) && Pressed(Keys.A))return "";
        if(Pressed(Keys.Delete))return "";
        if(Pressed(Keys.Back) && text.Length>0)text=text[..^1];
        if(signed && (Pressed(Keys.OemMinus) || Pressed(Keys.Subtract)))
            text=text.StartsWith("-")?text[1..]:"-"+text;
        if(decimalPoint && !text.Contains('.') && (Pressed(Keys.OemPeriod)||Pressed(Keys.Decimal)))text+=".";
        for(int i=0;i<10;i++)
            if(Pressed((Keys)((int)Keys.D0+i)) || Pressed((Keys)((int)Keys.NumPad0+i)))
                if(text.Length<digits+(text.StartsWith("-")?1:0))text+=(char)('0'+i);
        return text;
    }
}
