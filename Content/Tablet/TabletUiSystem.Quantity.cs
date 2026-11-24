using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;

namespace KivotosMod.Content.Tablet;

public sealed partial class TabletUiSystem
{
    private static int _quantityItem = -1;
    private static string _quantityText = "";
    private static int _quantityOriginal;
    private static bool _quantityEscapeHeld;

    private static void BeginQuantityInput(int itemType)
    {
        EndQuantityInput(true);
        _quantityItem = itemType;
        _quantityOriginal = GetSelectedQuantity(itemType);
        _quantityText = "";
        Main.clrInput();
        Main.blockInput = true;
    }

    private static void EndQuantityInput(bool commit)
    {
        if (_quantityItem < 0) return;
        int value = int.TryParse(_quantityText, out int entered) ? Math.Clamp(entered, 1, 9999) : _quantityOriginal;
        ProductQuantities[_quantityItem] = commit ? value : _quantityOriginal;
        _quantityItem = -1;
        Main.blockInput = false;
    }

    private static void UpdateQuantityInput(bool enabled)
    {
        if (_quantityItem < 0) return;
        if (!enabled || _page != TabletPage.Shop || _showingCart || _checkoutOpen)
        {
            EndQuantityInput(true);
            return;
        }
        Main.blockInput = true;
        Terraria.GameInput.PlayerInput.WritingText = true;
        Main.instance.HandleIME();
        string input = Main.GetInputText(_quantityText);
        _quantityText = new string(input.Where(c => c >= '0' && c <= '9').Take(4).ToArray());
        if (int.TryParse(_quantityText, out int value)) ProductQuantities[_quantityItem] = Math.Clamp(value, 1, 9999);
        if (Main.keyState.IsKeyDown(Keys.Enter) && !Main.oldKeyState.IsKeyDown(Keys.Enter)) EndQuantityInput(true);
        if (Main.keyState.IsKeyDown(Keys.Escape))
        {
            EndQuantityInput(false);
            _quantityEscapeHeld = true;
            _escapeWasDown = true;
        }
    }
}
