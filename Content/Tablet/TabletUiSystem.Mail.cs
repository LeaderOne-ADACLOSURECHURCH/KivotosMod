using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using KivotosMod.Content.Tablet.Mail;

namespace KivotosMod.Content.Tablet;

public sealed partial class TabletUiSystem
{
    private static readonly Rectangle MailListClip = new(46,82,190,324);
    private static readonly Rectangle MailBodyClip = new(248,140,280,174);
    private static readonly Rectangle MailClaimRect = new(434,378,94,26);
    private static int _mailScroll, _mailBodyScroll;
    private static string _selectedMail;
    private static readonly Dictionary<string,Asset<Texture2D>> MailTextures = new();
    private static TabletMailPlayer Mailbox => Main.LocalPlayer.GetModPlayer<TabletMailPlayer>();
    private static Texture2D MailTexture(string name)
    {
        if (!MailTextures.TryGetValue(name,out var asset))
            MailTextures[name]=asset=ModContent.Request<Texture2D>("KivotosMod/Assets/Tablet/Mail/"+name,AssetRequestMode.ImmediateLoad);
        return asset.Value;
    }
    private static Rectangle MailRow(int index)=>new(48,84+(index-_mailScroll)*54,184,50);
    private static Rectangle MailStar(Rectangle row)=>new(row.X-1,row.Y-1,12,12);
    private static void DrawUnreadDot(SpriteBatch batch, int x, int y, Layout layout)
    {
        var color = new Color(242, 65, 79);
        DrawSolid(batch, new Rectangle(x - 2, y - 3, 5, 7), color, layout);
        DrawSolid(batch, new Rectangle(x - 3, y - 2, 7, 5), color, layout);
    }
    private static TabletMailEntry SelectedMail()=>Mailbox.Letters.FirstOrDefault(e=>e.Id==_selectedMail);
    private static void HandleMailInput(Point point)
    {
        int wheel=PlayerInput.ScrollWheelDeltaForUI;
        if(wheel==0)return;
        if(MailListClip.Contains(point))_mailScroll=Math.Clamp(_mailScroll+(wheel>0?-1:1),0,Math.Max(0,Mailbox.Letters.Count-6));
        else if(MailBodyClip.Contains(point))_mailBodyScroll=Math.Max(0,_mailBodyScroll+(wheel>0?-2:2));
        else return;
        PlayerInput.LockVanillaMouseScroll("KivotosMod:Mail");
    }
    private static void HandleMailClick(Point point)
    {
        var entries=Mailbox.Sorted.ToList();
        if(MailListClip.Contains(point))
            for(int i=_mailScroll;i<Math.Min(entries.Count,_mailScroll+6);i++)
            {
                var row=MailRow(i);if(!row.Contains(point)&&!MailStar(row).Contains(point))continue;
                var entry=entries[i];
                if(MailStar(row).Contains(point))TabletMailPlayer.Request(entry.Id,2);
                else { _selectedMail=entry.Id;_mailBodyScroll=0;TabletMailPlayer.Request(entry.Id,1); }
                SoundEngine.PlaySound(SoundID.MenuTick);return;
            }
        var selected=SelectedMail();
        if(MailClaimRect.Contains(point)&&selected!=null&&!selected.Claimed)
        {TabletMailPlayer.Request(selected.Id,3);SoundEngine.PlaySound(SoundID.MenuTick);}
    }
    private static void MailPanel(SpriteBatch sb,Rectangle rect,Layout layout,Color color)
    {
        // Reuse the exact shop currency-field frame from the reference.
        DrawNineSlice(sb,_currencyPanel.Value,rect,layout,color,6);
    }
    private static int MailLineHeight => Math.Max(12,(int)MathF.Ceiling(_uiFont.Value.LineSpacing*.30f));
    private static int MailVisibleLines => Math.Max(1,(MailBodyClip.Height-8)/MailLineHeight);
    private static List<string> MailLines(string text)
    {
        var font=_uiFont.Value;const float scale=.30f;
        var lines=new List<string>();string line="";
        foreach(char c in text)
        {
            if(c=='\n'){lines.Add(line);line="";continue;}
            if(font.MeasureString(line+c).X*scale>274&&line.Length>0){lines.Add(line);line="";}
            line+=c;
        }
        if(line.Length>0)lines.Add(line);return lines;
    }
    private static void DrawMailbox(SpriteBatch sb,Layout layout,Vector2 mouse)
    {
        DrawSolid(sb,new Rectangle(40,76,500,338),new Color(30,43,55)*.65f,layout);
        var entries=Mailbox.Sorted.ToList();
        _mailScroll=Math.Clamp(_mailScroll,0,Math.Max(0,entries.Count-6));
        if(SelectedMail()==null&&entries.Count>0)_selectedMail=entries[0].Id;
        DrawClippedList(sb,layout,MailListClip,()=>
        {
            for(int i=_mailScroll;i<Math.Min(entries.Count,_mailScroll+6);i++)
            {
                var entry=entries[i];var definition=TabletMailCatalog.Definition(entry.Template);var row=MailRow(i);
                var color=entry.Claimed?new Color(145,160,172):new Color(246,252,255);
                MailPanel(sb,row,layout,color);
                if(_selectedMail==entry.Id)DrawSolid(sb,new Rectangle(row.X,row.Bottom-2,row.Width,2),new Color(80,204,238),layout);
                DrawTextureFit(sb,MailTexture(definition.Portrait),new Rectangle(row.X+7,row.Y+7,34,36),layout);
                // Short portrait divider deliberately stops inside the row.
                DrawSolid(sb,new Rectangle(row.X+46,row.Y+8,1,row.Height-16),new Color(148,170,188),layout);
                DrawUpgradeLabel(sb,definition.Title,new Rectangle(row.X+52,row.Y+5,126,20),new Color(38,56,77),layout,.31f);
                DrawUpgradeLabel(sb,definition.Sender,new Rectangle(row.X+52,row.Y+29,104,15),new Color(70,89,104),layout,.26f);
                string envelope=entry.Claimed&&!entry.Pending?"EnvelopeClaimed":entry.Read?"EnvelopeOpen":"EnvelopeClosed";
                DrawTextureFit(sb,MailTexture(envelope),new Rectangle(row.Right-18,row.Bottom-17,14,12),layout);
                if (!entry.Read) DrawUnreadDot(sb, row.Right - 7, row.Y + 8, layout);
                DrawTextureFit(sb,MailTexture(entry.Favorite?"Favorite":"NotFavorite"),MailStar(row),layout);
                if(MailStar(row).Contains(mouse.ToPoint()))Main.instance.MouseText(entry.Favorite?"取消收藏":"收藏邮件");
            }
        });
        if(entries.Count==0)DrawUpgradeLabel(sb,"暂无邮件",new Rectangle(62,190,154,24),Color.White,layout,.35f,true);
        var selected=SelectedMail();
        MailPanel(sb,new Rectangle(242,84,292,46),layout,new Color(246,252,255));
        MailPanel(sb,new Rectangle(242,136,292,272),layout,new Color(246,252,255));
        if(selected==null)return;
        var mail=TabletMailCatalog.Definition(selected.Template);
        DrawTextureFit(sb,MailTexture(mail.Portrait),new Rectangle(248,90,32,32),layout);
        DrawSolid(sb,new Rectangle(285,91,1,32),new Color(148,170,188),layout);
        DrawUpgradeLabel(sb,mail.Title,new Rectangle(292,89,234,26),new Color(38,56,77),layout,.42f);
        DrawUpgradeLabel(sb,mail.Sender,new Rectangle(292,113,234,13),new Color(70,89,104),layout,.23f);
        DrawSolid(sb,new Rectangle(246,132,284,1),Color.White,layout);
        var lines=MailLines(mail.Body);
        _mailBodyScroll=Math.Clamp(_mailBodyScroll,0,Math.Max(0,lines.Count-MailVisibleLines));
        DrawClippedList(sb,layout,MailBodyClip,()=>
        {
            for(int i=_mailBodyScroll;i<Math.Min(lines.Count,_mailBodyScroll+MailVisibleLines);i++)
                DrawText(sb,lines[i],new Vector2(250,144+(i-_mailBodyScroll)*MailLineHeight),new Color(38,56,77),.30f,layout,bold:false);
        });
        DrawSolid(sb,new Rectangle(248,319,280,1),new Color(148,170,188),layout);
        DrawUpgradeLabel(sb,"学员赠礼",new Rectangle(250,321,114,13),new Color(70,89,104),layout,.23f);
        for(int i=0;i<selected.Gifts.Count;i++)
        {
            var gift=selected.Gifts[i];var cell=new Rectangle(250+i%9*30,337+i/9*30,27,27);
            DrawNineSlice(sb,_currencyPanel.Value,cell,layout,Color.White,6);
            DrawItemIcon(sb,gift.Type,new Rectangle(cell.X+2,cell.Y+1,23,19),layout);
            DrawUpgradeLabel(sb,gift.Stack.ToString(),new Rectangle(cell.X,cell.Bottom-10,25,9),new Color(48,68,93),layout,.21f);
            if(cell.Contains(mouse.ToPoint()))ShowItemTooltip(gift.Type);
        }
        int buttonWidth=_formationSelectButton.Value.Width/3;
        int buttonFrame=!selected.Claimed&&MailClaimRect.Contains(mouse.ToPoint())?1:0;
        DrawNineSlice(sb,_formationSelectButton.Value,MailClaimRect,layout,Color.White,8,
            new Rectangle(buttonFrame*buttonWidth,0,buttonWidth,_formationSelectButton.Value.Height));
        if(selected.Claimed)DrawSolid(sb,MailClaimRect,Color.Black*.25f,layout);
        DrawUpgradeLabel(sb,selected.Pending?"投递中":selected.Claimed?"已收取":"收取",MailClaimRect,Color.White,layout,.33f,true);
    }
}
