using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HesaEngine.SDK;
using HesaEngine.SDK.Args;

namespace Flowers_ADCSeries.MyCommon
{
    internal static class MyManaManager
    {
        internal static bool SpellFarm { get; set; } = true;
        internal static bool SpellHarass { get; set; } = true;

        internal static void AddSpellFarm(Menu mainMenu)
        {
            mainMenu.AddSeparator("Farm Logic Settings");
            mainMenu.Add(new MenuCheckbox("SpellFarm", "Use Spell Farm(Mouse Scroll)", true));
            mainMenu.Add(new MenuKeybind("SpellHarass", "Use Spell Harass(In LaneClear Mode)", new KeyBind(SharpDX.DirectInput.Key.H, MenuKeybindType.Toggle, true)));

            Game.OnWndProc += delegate(WndEventArgs Args)
            {
                if (Args.Msg == 0x20a)
                {
                    mainMenu.Get<MenuCheckbox>("SpellFarm").SetValue(!mainMenu.Get<MenuCheckbox>("SpellFarm").Checked);
                    SpellFarm = mainMenu.Get<MenuCheckbox>("SpellFarm").Checked;
                }
            };

            Game.OnTick += delegate
            {
                SpellFarm = mainMenu.Get<MenuCheckbox>("SpellFarm").Checked;
                SpellHarass = mainMenu.Get<MenuKeybind>("SpellHarass").Active;
            };
        }

        internal static void AddDrawFarm(Menu mainMenu)
        {
            mainMenu.Add(new MenuCheckbox("DrawFarm", "Draw Spell Farm Status", true));
            mainMenu.Add(new MenuCheckbox("DrawHarass", "Draw Spell Harass Status", true));

            Drawing.OnDraw += delegate
            {
                if (!ObjectManager.Player.IsDead && !Shop.IsShopOpen && !Chat.IsChatOpen)
                {
                    if (mainMenu.Get<MenuCheckbox>("DrawFarm").Checked)
                    {
                        var MePos = Drawing.WorldToScreen(ObjectManager.Player.Position);

                        Drawing.DrawText(MePos[0] - 57, MePos[1] + 48, System.Drawing.Color.FromArgb(242, 120, 34).ToSharpDX(),
                            "Spell Farm:" + (SpellFarm ? "On" : "Off"));
                    }

                    if (mainMenu.Get<MenuCheckbox>("DrawHarass").Checked)
                    {
                        var MePos = Drawing.WorldToScreen(ObjectManager.Player.Position);

                        Drawing.DrawText(MePos[0] - 57, MePos[1] + 68, System.Drawing.Color.FromArgb(242, 120, 34).ToSharpDX(),
                            "Spell Hars:" + (SpellHarass ? "On" : "Off"));
                    }
                }
            };
        }
    }
}
