using System;
using System.Collections.Generic;
namespace Microsoft.Xna.Framework {}
namespace Microsoft.Xna.Framework.Input {
 public enum Keys {Enter,Escape}
 public struct KeyboardState {public Keys? Key;public bool IsKeyDown(Keys key)=>Key==key;}
}
namespace Terraria {
 public class InputHost {public void HandleIME(){}}
 public static class Main {
  public static bool blockInput,writingText;public static InputHost instance=new();
  public static Microsoft.Xna.Framework.Input.KeyboardState keyState,oldKeyState;
  public static string NextInput;
  public static void clrInput(){}
  public static string GetInputText(string text){var value=NextInput??text;NextInput=null;return value;}
 }
}
namespace KivotosMod.Content.Tablet {
 public sealed partial class TabletUiSystem {
  private enum TabletPage {Home,Shop,Formation}
  private static TabletPage _page=TabletPage.Shop;private static bool _showingCart,_checkoutOpen,_escapeWasDown;
  private static readonly Dictionary<int,int> ProductQuantities=new();
  private static int GetSelectedQuantity(int type)=>ProductQuantities.TryGetValue(type,out int q)?q:1;
  private static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  public static void ValidateQuantity(){
   ProductQuantities[10]=7;BeginQuantityInput(10);Check(Terraria.Main.blockInput,"Input does not block game keys");
   Terraria.Main.NextInput="123";UpdateQuantityInput(true);Check(GetSelectedQuantity(10)==123,"Typing quantity failed");
   Terraria.Main.keyState=new(){Key=Microsoft.Xna.Framework.Input.Keys.Enter};UpdateQuantityInput(true);
   Check(_quantityItem<0&&!Terraria.Main.blockInput&&GetSelectedQuantity(10)==123,"Enter did not commit/release input");
   Terraria.Main.keyState=new();BeginQuantityInput(10);Terraria.Main.NextInput="abc999999xyz";UpdateQuantityInput(true);
   Check(_quantityText=="9999"&&GetSelectedQuantity(10)==9999,"Paste filtering/upper bound");
   Terraria.Main.keyState=new(){Key=Microsoft.Xna.Framework.Input.Keys.Escape};UpdateQuantityInput(true);
   Check(GetSelectedQuantity(10)==123&&!Terraria.Main.blockInput&&_quantityEscapeHeld,"Escape did not restore prior quantity");
   Terraria.Main.keyState=new();BeginQuantityInput(10);Terraria.Main.NextInput="0";UpdateQuantityInput(true);EndQuantityInput(true);
   Check(GetSelectedQuantity(10)==1,"Minimum quantity is not one");
   BeginQuantityInput(10);UpdateQuantityInput(false);Check(_quantityItem<0&&!Terraria.Main.blockInput,"Closing tablet left keyboard locked");
   BeginQuantityInput(10);_page=TabletPage.Formation;UpdateQuantityInput(true);Check(_quantityItem<0,"Changing page left quantity input active");
  }
 }
}

namespace Terraria.GameInput { public static class PlayerInput { public static bool WritingText; } }
