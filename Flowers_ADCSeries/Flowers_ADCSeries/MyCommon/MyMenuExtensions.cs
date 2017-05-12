namespace Flowers_ADCSeries.MyCommon
{
    using HesaEngine.SDK;

    using MyBase;

    using System;

    using Color = System.Drawing.Color;
    using Key = SharpDX.DirectInput.Key;

    internal static class MyMenuExtensions
    {
        internal static Menu myMenu { get; set; }
        internal static Menu ActivatorMenu { get; set; }
        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu LaneClearMenu { get; set; }
        internal static Menu JungleClearMenu { get; set; }
        internal static Menu LastHitMenu { get; set; }
        internal static Menu FleeMenu { get; set; }
        internal static Menu KillStealMenu { get; set; }

        internal static Menu MiscMenu { get; set; }
        internal static Menu DrawMenu { get; set; }

        private static string heroName => ObjectManager.Player.ChampionName;

        internal static bool GetBool(this Menu mainMenu, string name)
        {
            return mainMenu.Get<MenuCheckbox>(name) != null && mainMenu.Get<MenuCheckbox>(name).Checked;
        }

        internal static bool GetKey(this Menu mainMenu, string name)
        {
            return mainMenu.Get<MenuKeybind>(name) != null && mainMenu.Get<MenuKeybind>(name).Active;
        }

        internal static int GetSlider(this Menu mainMenu, string name)
        {
            return mainMenu.Get<MenuSlider>(name) != null ? mainMenu.Get<MenuSlider>(name).CurrentValue : Int32.MaxValue;
        }

        internal static int GetList(this Menu mainMenu, string name)
        {
            return mainMenu.Get<MenuCombo>(name) != null ? mainMenu.Get<MenuCombo>(name).CurrentValue : Int32.MaxValue;
        }

        internal class ComboOption
        {
            private static Menu comboMenu => ComboMenu;

            internal static void AddQ(bool enabled = true)
            {
                comboMenu.Add(new MenuCheckbox("ComboQ" + heroName, "Use Q", enabled));
            }

            internal static void AddW(bool enabled = true)
            {
                comboMenu.Add(new MenuCheckbox("ComboW" + heroName, "Use W", enabled));
            }

            internal static void AddE(bool enabled = true)
            {
                comboMenu.Add(new MenuCheckbox("ComboE" + heroName, "Use E", enabled));
            }

            internal static void AddR(bool enabled = true)
            {
                comboMenu.Add(new MenuCheckbox("ComboR" + heroName, "Use R", enabled));
            }

            internal static void AddBool(string name, string defaultName, bool enabled = true)
            {
                comboMenu.Add(new MenuCheckbox(name + heroName, defaultName, enabled));
            }

            internal static void AddSlider(string name, string defaultName, int defaultValue, int minValue, int maxValue)
            {
                comboMenu.Add(new MenuSlider(name + heroName, defaultName, new Slider(defaultValue, minValue, maxValue)));
            }

            internal static void AddKey(string name, string defaultName, Key defaultKey, MenuKeybindType type = MenuKeybindType.Hold, bool enabled = false)
            {
                comboMenu.Add(new MenuKeybind(name + heroName, defaultName, new KeyBind(defaultKey, type, enabled)));
            }

            internal static void AddList(string name, string defaultName, string[] values, int defaultValue = 0)
            {
                comboMenu.Add(new MenuCombo(name + heroName, defaultName, new StringList(values, defaultValue)));
            }

            internal static bool GetBool(string name)
            {
                return comboMenu.Get<MenuCheckbox>(name + heroName) != null && comboMenu.Get<MenuCheckbox>(name + heroName).Checked;
            }

            internal static int GetSlider(string name)
            {
                return comboMenu.Get<MenuSlider>(name + heroName) != null ? comboMenu.Get<MenuSlider>(name + heroName).CurrentValue : Int32.MaxValue;
            }

            internal static bool GetKey(string name)
            {
                return comboMenu.Get<MenuKeybind>(name + heroName) != null && comboMenu.Get<MenuKeybind>(name + heroName).Active;
            }

            internal static int GetList(string name)
            {
                return comboMenu.Get<MenuCombo>(name + heroName) != null ? comboMenu.Get<MenuCombo>(name + heroName).CurrentValue : Int32.MaxValue;
            }

            internal static bool UseQ => comboMenu.Get<MenuCheckbox>("ComboQ" + heroName) != null && comboMenu.Get<MenuCheckbox>("ComboQ" + heroName).Checked;

            internal static bool UseW => comboMenu.Get<MenuCheckbox>("ComboW" + heroName) != null && comboMenu.Get<MenuCheckbox>("ComboW" + heroName).Checked;

            internal static bool UseE => comboMenu.Get<MenuCheckbox>("ComboE" + heroName) != null && comboMenu.Get<MenuCheckbox>("ComboE" + heroName).Checked;

            internal static bool UseR => comboMenu.Get<MenuCheckbox>("ComboR" + heroName) != null && comboMenu.Get<MenuCheckbox>("ComboR" + heroName).Checked;
        }

        internal class HarassOption
        {
            private static Menu harassMenu => HarassMenu;

            internal static void AddQ(bool enabled = true)
            {
                harassMenu.Add(new MenuCheckbox("HarassQ" + heroName, "Use Q", enabled));
            }

            internal static void AddW(bool enabled = true)
            {
                harassMenu.Add(new MenuCheckbox("HarassW" + heroName, "Use W", enabled));
            }

            internal static void AddE(bool enabled = true)
            {
                harassMenu.Add(new MenuCheckbox("HarassE" + heroName, "Use E", enabled));
            }

            internal static void AddR(bool enabled = true)
            {
                harassMenu.Add(new MenuCheckbox("HarassR" + heroName, "Use R", enabled));
            }

            internal static void AddTargetList()
            {
                harassMenu.AddSeparator("Harass Target Settings");

                foreach (var target in ObjectManager.Heroes.Enemies)
                {
                    if (target != null)
                    {
                        harassMenu.Add(new MenuCheckbox("HarassList" + target.ChampionName.ToLower(),target.ChampionName, true));
                    }
                }
            }

            internal static bool GetHarassTarget(string name)
            {
                return harassMenu.Get<MenuCheckbox>("HarassList" + name.ToLower()) != null && harassMenu.Get<MenuCheckbox>("HarassList" + name.ToLower()).Checked;
            }

            internal static void AddMana(int defalutValue = 30)
            {
                harassMenu.Add(new MenuSlider("HarassMana" + heroName, "When Player ManaPercent >= x%", new Slider(0, 100, defalutValue)));
            }

            internal static bool HasEnouguMana => ObjectManager.Player.ManaPercent >= GetSlider("HarassMana");

            internal static void AddBool(string name, string defaultName, bool enabled = true)
            {
                harassMenu.Add(new MenuCheckbox(name + heroName, defaultName, enabled));
            }

            internal static void AddSlider(string name, string defaultName, int defaultValue, int minValue, int maxValue)
            {
                harassMenu.Add(new MenuSlider(name + heroName, defaultName, new Slider(defaultValue, minValue, maxValue)));
            }

            internal static void AddKey(string name, string defaultName, Key defaultKey, MenuKeybindType type = MenuKeybindType.Hold, bool enabled = false)
            {
                harassMenu.Add(new MenuKeybind(name + heroName, defaultName, new KeyBind(defaultKey, type, enabled)));
            }

            internal static void AddList(string name, string defaultName, string[] values, int defaultValue = 0)
            {
                harassMenu.Add(new MenuCombo(name + heroName, defaultName, new StringList(values, defaultValue)));
            }

            internal static bool GetBool(string name)
            {
                return harassMenu.Get<MenuCheckbox>(name + heroName) != null && harassMenu.Get<MenuCheckbox>(name + heroName).Checked;
            }

            internal static int GetSlider(string name)
            {
                return harassMenu.Get<MenuSlider>(name + heroName) != null ? harassMenu.Get<MenuSlider>(name + heroName).CurrentValue : Int32.MaxValue;
            }

            internal static bool GetKey(string name)
            {
                return harassMenu.Get<MenuKeybind>(name + heroName) != null && harassMenu.Get<MenuKeybind>(name + heroName).Active;
            }

            internal static int GetList(string name)
            {
                return harassMenu.Get<MenuCombo>(name + heroName) != null ? harassMenu.Get<MenuCombo>(name + heroName).CurrentValue : Int32.MaxValue;
            }

            internal static bool UseQ => harassMenu.Get<MenuCheckbox>("HarassQ" + heroName) != null && harassMenu.Get<MenuCheckbox>("HarassQ" + heroName).Checked;

            internal static bool UseW => harassMenu.Get<MenuCheckbox>("HarassW" + heroName) != null && harassMenu.Get<MenuCheckbox>("HarassW" + heroName).Checked;

            internal static bool UseE => harassMenu.Get<MenuCheckbox>("HarassE" + heroName) != null && harassMenu.Get<MenuCheckbox>("HarassE" + heroName).Checked;

            internal static bool UseR => harassMenu.Get<MenuCheckbox>("HarassR" + heroName) != null && harassMenu.Get<MenuCheckbox>("HarassR" + heroName).Checked;
        }

        internal class LaneClearOption
        {
            private static Menu laneClearMenu => LaneClearMenu;

            internal static void AddQ(bool enabled = true)
            {
                laneClearMenu.Add(new MenuCheckbox("LaneClearQ" + heroName, "Use Q", enabled));
            }

            internal static void AddW(bool enabled = true)
            {
                laneClearMenu.Add(new MenuCheckbox("LaneClearW" + heroName, "Use W", enabled));
            }

            internal static void AddE(bool enabled = true)
            {
                laneClearMenu.Add(new MenuCheckbox("LaneClearE" + heroName, "Use E", enabled));
            }

            internal static void AddR(bool enabled = true)
            {
                laneClearMenu.Add(new MenuCheckbox("LaneClearR" + heroName, "Use R", enabled));
            }

            internal static void AddMana(int defalutValue = 60)
            {
                laneClearMenu.Add(new MenuSlider("LaneClearMana" + heroName, "When Player ManaPercent >= x%", new Slider(0, 100, defalutValue)));
            }

            internal static bool HasEnouguMana => ObjectManager.Player.ManaPercent >= GetSlider("LaneClearMana") && MyManaManager.SpellFarm;

            internal static void AddBool(string name, string defaultName, bool enabled = true)
            {
                laneClearMenu.Add(new MenuCheckbox(name + heroName, defaultName, enabled));
            }

            internal static void AddSlider(string name, string defaultName, int defaultValue, int minValue, int maxValue)
            {
                laneClearMenu.Add(new MenuSlider(name + heroName, defaultName, new Slider(defaultValue, minValue, maxValue)));
            }

            internal static bool GetBool(string name)
            {
                return laneClearMenu.Get<MenuCheckbox>(name + heroName) != null && laneClearMenu.Get<MenuCheckbox>(name + heroName).Checked;
            }

            internal static int GetSlider(string name)
            {
                return laneClearMenu.Get<MenuSlider>(name + heroName) != null ? laneClearMenu.Get<MenuSlider>(name + heroName).CurrentValue : Int32.MaxValue;
            }

            internal static bool UseQ => laneClearMenu.Get<MenuCheckbox>("LaneClearQ" + heroName) != null && laneClearMenu.Get<MenuCheckbox>("LaneClearQ" + heroName).Checked;

            internal static bool UseW => laneClearMenu.Get<MenuCheckbox>("LaneClearW" + heroName) != null && laneClearMenu.Get<MenuCheckbox>("LaneClearW" + heroName).Checked;

            internal static bool UseE => laneClearMenu.Get<MenuCheckbox>("LaneClearE" + heroName) != null && laneClearMenu.Get<MenuCheckbox>("LaneClearE" + heroName).Checked;

            internal static bool UseR => laneClearMenu.Get<MenuCheckbox>("LaneClearR" + heroName) != null && laneClearMenu.Get<MenuCheckbox>("LaneClearR" + heroName).Checked;
        }

        internal class JungleClearOption
        {
            private static Menu jungleClearMenu => JungleClearMenu;

            internal static void AddQ(bool enabled = true)
            {
                jungleClearMenu.Add(new MenuCheckbox("JungleClearQ" + heroName, "Use Q", enabled));
            }

            internal static void AddW(bool enabled = true)
            {
                jungleClearMenu.Add(new MenuCheckbox("JungleClearW" + heroName, "Use W", enabled));
            }

            internal static void AddE(bool enabled = true)
            {
                jungleClearMenu.Add(new MenuCheckbox("JungleClearE" + heroName, "Use E", enabled));
            }

            internal static void AddR(bool enabled = true)
            {
                jungleClearMenu.Add(new MenuCheckbox("JungleClearR" + heroName, "Use R", enabled));
            }

            internal static void AddBool(string name, string defaultName, bool enabled = true)
            {
                jungleClearMenu.Add(new MenuCheckbox(name + heroName, defaultName, enabled));
            }

            internal static void AddMana(int defalutValue = 30)
            {
                jungleClearMenu.Add(new MenuSlider("JungleClearMana" + heroName, "When Player ManaPercent >= x%", new Slider(0, 100, defalutValue)));
            }

            internal static void AddSlider(string name, string defaultName, int defaultValue, int minValue, int maxValue)
            {
                jungleClearMenu.Add(new MenuSlider(name + heroName, defaultName, new Slider(defaultValue, minValue, maxValue)));
            }

            internal static bool HasEnouguMana => ObjectManager.Player.ManaPercent >= GetSlider("JungleClearMana") && MyManaManager.SpellFarm;

            internal static bool GetBool(string name)
            {
                return jungleClearMenu.Get<MenuCheckbox>(name + heroName) != null && jungleClearMenu.Get<MenuCheckbox>(name + heroName).Checked;
            }

            internal static int GetSlider(string name)
            {
                return jungleClearMenu.Get<MenuSlider>(name + heroName) != null ? jungleClearMenu.Get<MenuSlider>(name + heroName).CurrentValue : Int32.MaxValue;
            }

            internal static bool UseQ => jungleClearMenu.Get<MenuCheckbox>("JungleClearQ" + heroName) != null && jungleClearMenu.Get<MenuCheckbox>("JungleClearQ" + heroName).Checked;

            internal static bool UseW => jungleClearMenu.Get<MenuCheckbox>("JungleClearW" + heroName) != null && jungleClearMenu.Get<MenuCheckbox>("JungleClearW" + heroName).Checked;

            internal static bool UseE => jungleClearMenu.Get<MenuCheckbox>("JungleClearE" + heroName) != null && jungleClearMenu.Get<MenuCheckbox>("JungleClearE" + heroName).Checked;

            internal static bool UseR => jungleClearMenu.Get<MenuCheckbox>("JungleClearR" + heroName) != null && jungleClearMenu.Get<MenuCheckbox>("JungleClearR" + heroName).Checked;
        }

        internal class LastHitOption
        {
            private static Menu lastHitMenu => LastHitMenu;

            internal static void AddNothing()
            {
                lastHitMenu.AddSeparator("Nothing in here");
            }

            internal static void AddQ(bool enabled = true)
            {
                lastHitMenu.Add(new MenuCheckbox("LastHitQ" + heroName, "Use Q", enabled));
            }

            internal static void AddW(bool enabled = true)
            {
                lastHitMenu.Add(new MenuCheckbox("LastHitW" + heroName, "Use W", enabled));
            }

            internal static void AddE(bool enabled = true)
            {
                lastHitMenu.Add(new MenuCheckbox("LastHitE" + heroName, "Use E", enabled));
            }

            internal static void AddR(bool enabled = true)
            {
                lastHitMenu.Add(new MenuCheckbox("LastHitR" + heroName, "Use R", enabled));
            }

            internal static void AddBool(string name, string defaultName, bool enabled = true)
            {
                lastHitMenu.Add(new MenuCheckbox(name + heroName, defaultName, enabled));
            }

            internal static void AddMana(int defalutValue = 30)
            {
                lastHitMenu.Add(new MenuSlider("LastHitMana" + heroName, "When Player ManaPercent >= x%", new Slider(0, 100, defalutValue)));
            }

            internal static void AddSlider(string name, string defaultName, int defaultValue, int minValue, int maxValue)
            {
                lastHitMenu.Add(new MenuSlider(name + heroName, defaultName, new Slider(defaultValue, minValue, maxValue)));
            }

            internal static bool HasEnouguMana => ObjectManager.Player.ManaPercent >= GetSlider("LastHitMana") && MyManaManager.SpellFarm;

            internal static bool GetBool(string name)
            {
                return lastHitMenu.Get<MenuCheckbox>(name + heroName) != null && lastHitMenu.Get<MenuCheckbox>(name + heroName).Checked;
            }

            internal static int GetSlider(string name)
            {
                return lastHitMenu.Get<MenuSlider>(name + heroName) != null ? lastHitMenu.Get<MenuSlider>(name + heroName).CurrentValue : Int32.MaxValue;
            }

            internal static bool UseQ => lastHitMenu.Get<MenuCheckbox>("LastHitQ" + heroName) != null && lastHitMenu.Get<MenuCheckbox>("LastHitQ" + heroName).Checked;

            internal static bool UseW => lastHitMenu.Get<MenuCheckbox>("LastHitW" + heroName) != null && lastHitMenu.Get<MenuCheckbox>("LastHitW" + heroName).Checked;

            internal static bool UseE => lastHitMenu.Get<MenuCheckbox>("LastHitE" + heroName) != null && lastHitMenu.Get<MenuCheckbox>("LastHitE" + heroName).Checked;

            internal static bool UseR => lastHitMenu.Get<MenuCheckbox>("LastHitR" + heroName) != null && lastHitMenu.Get<MenuCheckbox>("LastHitR" + heroName).Checked;
        }

        internal class FleeOption
        {
            private static Menu fleeMenu => FleeMenu;

            internal static void AddMove(bool enabled = true)
            {
                fleeMenu.Add(new MenuCheckbox("FleeMove" + heroName, "Disable Flee Mode MoveMent", enabled));
            }

            internal static void AddQ(bool enabled = true)
            {
                fleeMenu.Add(new MenuCheckbox("FleeQ" + heroName, "Use Q", enabled));
            }

            internal static void AddW(bool enabled = true)
            {
                fleeMenu.Add(new MenuCheckbox("FleeW" + heroName, "Use W", enabled));
            }

            internal static void AddE(bool enabled = true)
            {
                fleeMenu.Add(new MenuCheckbox("FleeE" + heroName, "Use E", enabled));
            }

            internal static void AddR(bool enabled = true)
            {
                fleeMenu.Add(new MenuCheckbox("FleeR" + heroName, "Use R", enabled));
            }

            internal static void AddBool(string name, string defaultName, bool enabled = true)
            {
                fleeMenu.Add(new MenuCheckbox(name + heroName, defaultName, enabled));
            }

            internal static bool GetBool(string name)
            {
                return fleeMenu.Get<MenuCheckbox>(name + heroName) != null && fleeMenu.Get<MenuCheckbox>(name + heroName).Checked;
            }

            internal static bool DisableMove => fleeMenu.Get<MenuCheckbox>("FleeMove" + heroName) != null && fleeMenu.Get<MenuCheckbox>("FleeMove" + heroName).Checked;

            internal static bool UseQ => fleeMenu.Get<MenuCheckbox>("FleeQ" + heroName) != null && fleeMenu.Get<MenuCheckbox>("FleeQ" + heroName).Checked;

            internal static bool UseW => fleeMenu.Get<MenuCheckbox>("FleeW" + heroName) != null && fleeMenu.Get<MenuCheckbox>("FleeW" + heroName).Checked;

            internal static bool UseE => fleeMenu.Get<MenuCheckbox>("FleeE" + heroName) != null && fleeMenu.Get<MenuCheckbox>("FleeE" + heroName).Checked;

            internal static bool UseR => fleeMenu.Get<MenuCheckbox>("FleeR" + heroName) != null && fleeMenu.Get<MenuCheckbox>("FleeR" + heroName).Checked;
        }

        internal class KillStealOption
        {
            private static Menu killStealMenu => KillStealMenu;

            internal static void AddQ(bool enabled = true)
            {
                killStealMenu.Add(new MenuCheckbox("KillStealQ" + heroName, "Use Q", enabled));
            }

            internal static void AddW(bool enabled = true)
            {
                killStealMenu.Add(new MenuCheckbox("KillStealW" + heroName, "Use W", enabled));
            }

            internal static void AddE(bool enabled = true)
            {
                killStealMenu.Add(new MenuCheckbox("KillStealE" + heroName, "Use E", enabled));
            }

            internal static void AddR(bool enabled = true)
            {
                killStealMenu.Add(new MenuCheckbox("KillStealR" + heroName, "Use R", enabled));
            }

            internal static void AddTargetList()
            {
                killStealMenu.AddSeparator("KillSteal List Settings");

                foreach (var target in ObjectManager.Heroes.Enemies)
                {
                    if (target != null)
                    {
                        killStealMenu.Add(new MenuCheckbox("KillStealList" + target.ChampionName.ToLower(), target.ChampionName, true));
                    }
                }
            }

            internal static bool GetKillStealTarget(string name)
            {
                return killStealMenu.Get<MenuCheckbox>("KillStealList" + name.ToLower()) != null && killStealMenu.Get<MenuCheckbox>("KillStealList" + name.ToLower()).Checked;
            }

            internal static void AddBool(string name, string defaultName, bool enabled = true)
            {
                killStealMenu.Add(new MenuCheckbox(name + heroName, defaultName, enabled));
            }

            internal static bool GetBool(string name)
            {
                return killStealMenu.Get<MenuCheckbox>(name + heroName) != null && killStealMenu.Get<MenuCheckbox>(name + heroName).Checked;
            }

            internal static bool UseQ => killStealMenu.Get<MenuCheckbox>("KillStealQ" + heroName) != null && killStealMenu.Get<MenuCheckbox>("KillStealQ" + heroName).Checked;

            internal static bool UseW => killStealMenu.Get<MenuCheckbox>("KillStealW" + heroName) != null && killStealMenu.Get<MenuCheckbox>("KillStealW" + heroName).Checked;

            internal static bool UseE => killStealMenu.Get<MenuCheckbox>("KillStealE" + heroName) != null && killStealMenu.Get<MenuCheckbox>("KillStealE" + heroName).Checked;

            internal static bool UseR => killStealMenu.Get<MenuCheckbox>("KillStealR" + heroName) != null && killStealMenu.Get<MenuCheckbox>("KillStealR" + heroName).Checked;
        }

        internal class MiscOption
        {
            private static Menu miscMenu => MiscMenu;

            internal static void AddNothing()
            {
                miscMenu.AddSeparator("Nothing in here");
            }

            internal static void AddBool(string name, string defaultName, bool enabled = true)
            {
                miscMenu.Add(new MenuCheckbox(name + heroName, defaultName, enabled));
            }

            internal static void AddSlider(string name, string defaultName, int defalueValue, int minValue = 0, int maxValue = 100)
            {
                miscMenu.Add(new MenuSlider(name + heroName, defaultName, new Slider(minValue, maxValue, defalueValue)));
            }

            internal static void AddKey(string name, string defaultName, Key defaultKey, MenuKeybindType type = MenuKeybindType.Hold, bool enabled = false)
            {
                miscMenu.Add(new MenuKeybind(name + heroName, defaultName, new KeyBind(defaultKey, type, enabled)));
            }

            internal static void AddList(string name, string defaultName, string[] values, int defaultValue = 0)
            {
                miscMenu.Add(new MenuCombo(name + heroName, defaultName, new StringList(values, defaultValue)));
            }

            internal static void AddSetting(string name)
            {
                miscMenu.AddSeparator(name + " Settings");
            }

            internal static void AddQ()
            {
                miscMenu.AddSeparator("Q Settings");
            }

            internal static void AddW()
            {
                miscMenu.AddSeparator("W Settings");
            }

            internal static void AddE()
            {
                miscMenu.AddSeparator("E Settings");
            }

            internal static void AddR()
            {
                miscMenu.AddSeparator("R Settings");
            }

            internal static void AddGapcloser()
            {
                miscMenu.AddSeparator("Anti Gapcloser Settings");
            }

            internal static void AddInterrupt()
            {
                miscMenu.AddSeparator("Interrupt Spell Settings");
            }

            internal static void AddMelee()
            {
                miscMenu.AddSeparator("Anti Melee Settings");
            }

            internal static void AddGapcloserTargetList()
            {
                miscMenu.AddSeparator("AntiGapcloser List Settings");

                foreach (var target in ObjectManager.Heroes.Enemies)
                {
                    if (target != null)
                    {
                        miscMenu.Add(new MenuCheckbox("AntiGapcloserList" + target.ChampionName.ToLower(), target.ChampionName, true));
                    }
                }
            }

            internal static bool GetGapcloserTarget(string name)
            {
                return miscMenu.Get<MenuCheckbox>("AntiGapcloserList" + name.ToLower()) != null && miscMenu.Get<MenuCheckbox>("AntiGapcloserList" + name.ToLower()).Checked;
            }

            internal static bool GetBool(string name)
            {
                return miscMenu.Get<MenuCheckbox>(name + heroName) != null && miscMenu.Get<MenuCheckbox>(name + heroName).Checked;
            }

            internal static bool GetKey(string name)
            {
                return miscMenu.Get<MenuKeybind>(name + heroName) != null && miscMenu.Get<MenuKeybind>(name + heroName).Active;
            }

            internal static int GetSlider(string name)
            {
                return miscMenu.Get<MenuSlider>(name + heroName) != null ? miscMenu.Get<MenuSlider>(name + heroName).CurrentValue : Int32.MaxValue;
            }

            internal static int GetList(string name)
            {
                return miscMenu.Get<MenuCombo>(name + heroName) != null ? miscMenu.Get<MenuCombo>(name + heroName).CurrentValue : Int32.MaxValue;
            }
        }

        internal class DrawOption
        {
            private static Menu drawMenu => DrawMenu;

            internal static void AddBool(string name, string defaultName, bool enabled = true)
            {
                drawMenu.Add(new MenuCheckbox(name + heroName, defaultName, enabled));
            }

            internal static void AddSlider(string name, string defaultName, int defalueValue, int minValue = 0, int maxValue = 100)
            {
                drawMenu.Add(new MenuSlider(name + heroName, defaultName, new Slider(minValue, maxValue, defalueValue)));
            }

            internal static bool GetBool(string name)
            {
                return drawMenu.Get<MenuCheckbox>(name + heroName) != null && drawMenu.Get<MenuCheckbox>(name + heroName).Checked;
            }

            internal static int GetSlider(string name)
            {
                return drawMenu.Get<MenuSlider>(name + heroName) != null ? drawMenu.Get<MenuSlider>(name + heroName).CurrentValue : Int32.MaxValue;
            }

            internal static void AddQ(bool enabled = false)
            {
                drawMenu.Add(new MenuCheckbox("DrawQ" + heroName, "Draw Q Range", enabled));
            }

            internal static void AddW(bool enabled = false)
            {
                drawMenu.Add(new MenuCheckbox("DrawW" + heroName, "Draw W Range", enabled));
            }

            internal static void AddE(bool enabled = false)
            {
                drawMenu.Add(new MenuCheckbox("DrawE" + heroName, "Draw E Range", enabled));
            }

            internal static void AddR(bool enabled = false)
            {
                drawMenu.Add(new MenuCheckbox("DrawR" + heroName, "Draw R Range", enabled));
            }

            internal static void AddRMinMap(bool enabled = false)
            {
                drawMenu.Add(new MenuCheckbox("DrawRMin" + heroName, "Draw R MinMap Range", enabled));
            }

            internal static void AddQExtend(bool enabled = false)
            {
                drawMenu.Add(new MenuCheckbox("DrawQExtend" + heroName, "Draw R Range", enabled));
            }

            internal static void AddFarm()
            {
                MyManaManager.AddDrawFarm(drawMenu);
            }

            internal static void AddEvent()
            {
                Drawing.OnDraw += delegate
                {
                    if (ObjectManager.Player.IsDead || Shop.IsShopOpen || Chat.IsChatOpen)
                        return;

                    if (drawMenu.Get<MenuCheckbox>("DrawQ" + heroName) != null && drawMenu.Get<MenuCheckbox>("DrawQ" + heroName).Checked && MyLogic.Q.IsReady())
                        Drawing.DrawCircle(ObjectManager.Player.Position, MyLogic.Q.Range, Color.FromArgb(19, 130, 234).ToSharpDX(), 1);

                    if (drawMenu.Get<MenuCheckbox>("DrawW" + heroName) != null && drawMenu.Get<MenuCheckbox>("DrawW" + heroName).Checked && MyLogic.W.IsReady())
                        Drawing.DrawCircle(ObjectManager.Player.Position, MyLogic.W.Range, Color.FromArgb(248, 246, 6).ToSharpDX(), 1);

                    if (drawMenu.Get<MenuCheckbox>("DrawE" + heroName) != null && drawMenu.Get<MenuCheckbox>("DrawE" + heroName).Checked && MyLogic.E.IsReady())
                        Drawing.DrawCircle(ObjectManager.Player.Position, MyLogic.E.Range, Color.FromArgb(188, 6, 248).ToSharpDX(), 1);

                    if (drawMenu.Get<MenuCheckbox>("DrawR" + heroName) != null && drawMenu.Get<MenuCheckbox>("DrawR" + heroName).Checked && MyLogic.R.IsReady())
                        Drawing.DrawCircle(ObjectManager.Player.Position, MyLogic.R.Range, Color.Red.ToSharpDX(), 1);

                    if (drawMenu.Get<MenuCheckbox>("DrawQExtend" + heroName) != null && drawMenu.Get<MenuCheckbox>("DrawQExtend" + heroName).Checked && MyLogic.QExtend.IsReady())
                        Drawing.DrawCircle(ObjectManager.Player.Position, MyLogic.QExtend.Range, Color.FromArgb(0, 255, 161).ToSharpDX(), 1);
                };

                //Drawing.OnEndScene += delegate
                //{
                //    if (ObjectManager.Player.IsDead || Shop.IsShopOpen || Chat.IsChatOpen)
                //        return;
                //};
            }
        }
    }
}
