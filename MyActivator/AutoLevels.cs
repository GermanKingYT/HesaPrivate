namespace Flowers_ADCSeries.MyActivator
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyCommon;

    internal class AutoLevels
    {
        private static Menu levelMenu;

        internal static void AddToMenu(Menu mainMenu)
        {
            levelMenu = mainMenu.AddSubMenu("Auto Levels");

            mainMenu.Add(new MenuCheckbox("LevelsEnable", "Enabled", false));
            mainMenu.Add(new MenuCheckbox("LevelsAutoR", "Auto Level R", true));
            mainMenu.Add(new MenuSlider("LevelsDelay", "Auto Level Delays", new Slider(0, 2000, 700)));
            mainMenu.Add(new MenuSlider("LevelsLevels", "When Player Level >= Enable!", new Slider(1, 18, 3)));
            mainMenu.Add(new MenuCombo("LevelsMode", "Mode: ", new StringList(new[] {"Q -> W -> E", "Q -> E -> W", "W -> Q -> E", "W -> E -> Q", "E -> Q -> W", "E -> W -> Q"}, 0)));

            AIHeroClient.OnLevelUp += OnLevelUp;
        }

        private static void OnLevelUp(AIHeroClient sender, int level)
        {
            if (!sender.IsMe || !levelMenu.GetBool("LevelsEnable"))
            {
                return;
            }

            if (levelMenu.GetBool("LevelsAutoR") && (ObjectManager.Player.Level == 6 || ObjectManager.Player.Level == 11 || ObjectManager.Player.Level == 16))
            {
                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);
            }

            if (ObjectManager.Player.Level >= levelMenu.GetSlider("LevelsLevels"))
            {
                int Delay = levelMenu.GetSlider("LevelsDelay");

                if (ObjectManager.Player.Level < 3)
                {
                    switch (levelMenu.GetList("LevelsMode"))
                    {
                        case 0:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                            break;
                        case 1:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                            break;
                        case 2:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                            break;
                        case 3:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                            break;
                        case 4:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                            break;
                        case 5:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                            break;
                    }
                }
                else if (ObjectManager.Player.Level > 3)
                {
                    switch (levelMenu.GetList("LevelsMode"))
                    {
                        case 0:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);

                            //Q -> W -> E
                            DelayLevels(Delay, SpellSlot.Q);
                            DelayLevels(Delay + 50, SpellSlot.W);
                            DelayLevels(Delay + 100, SpellSlot.E);
                            break;
                        case 1:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);

                            //Q -> E -> W
                            DelayLevels(Delay, SpellSlot.Q);
                            DelayLevels(Delay + 50, SpellSlot.E);
                            DelayLevels(Delay + 100, SpellSlot.W);
                            break;
                        case 2:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);

                            //W -> Q -> E
                            DelayLevels(Delay, SpellSlot.W);
                            DelayLevels(Delay + 50, SpellSlot.Q);
                            DelayLevels(Delay + 100, SpellSlot.E);
                            break;
                        case 3:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);

                            //W -> E -> Q
                            DelayLevels(Delay, SpellSlot.W);
                            DelayLevels(Delay + 50, SpellSlot.E);
                            DelayLevels(Delay + 100, SpellSlot.Q);
                            break;
                        case 4:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);

                            //E -> Q -> W
                            DelayLevels(Delay, SpellSlot.E);
                            DelayLevels(Delay + 50, SpellSlot.Q);
                            DelayLevels(Delay + 100, SpellSlot.W);
                            break;
                        case 5:
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                            else if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0)
                                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);

                            //E -> W -> Q
                            DelayLevels(Delay, SpellSlot.E);
                            DelayLevels(Delay + 50, SpellSlot.W);
                            DelayLevels(Delay + 100, SpellSlot.Q);
                            break;
                    }
                }
            }
        }

        private static void DelayLevels(int time, SpellSlot slot)
        {
            Core.DelayAction(() => ObjectManager.Player.Spellbook.LevelSpell(slot), time);
        }
    }
}
