namespace Flowers_ADCSeries.MyActivator
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyBase;
    using MyCommon;

    using System.Collections.Generic;

    internal class Cleanese
    {
        private static int useCleanTime;
        private static Menu cleanMenu;
        private static readonly List<BuffType> debuffTypes = new List<BuffType>();

        internal static void AddToMenu(Menu mainMenu)
        {
            cleanMenu = mainMenu.AddSubMenu("Cleanese");

            var Debuff = cleanMenu.AddSubMenu("Debuffs");
            {
                Debuff.Add(new MenuCheckbox("Cleanblind", "Blind", true));
                Debuff.Add(new MenuCheckbox("Cleancharm", "Charm", true));
                Debuff.Add(new MenuCheckbox("Cleanfear", "Fear", true));
                Debuff.Add(new MenuCheckbox("Cleanflee", "Flee", true));
                Debuff.Add(new MenuCheckbox("Cleanstun", "Stun", true));
                Debuff.Add(new MenuCheckbox("Cleansnare", "Snare", true));
                Debuff.Add(new MenuCheckbox("Cleantaunt", "Taunt", true));
                Debuff.Add(new MenuCheckbox("Cleansuppression", "Suppression", true));
                Debuff.Add(new MenuCheckbox("Cleanpolymorph", "Polymorph", false));
                Debuff.Add(new MenuCheckbox("Cleansilence", "Silence", false));
                Debuff.Add(new MenuCheckbox("Cleanexhaust", "Exhaust", true));
            }

            cleanMenu.Add(new MenuCheckbox("CleanEnable", "Enabled", true));
            cleanMenu.Add(new MenuSlider("CleanDelay", "Clean Delay(ms)", new Slider(0, 2000, 0)));
            cleanMenu.Add(new MenuSlider("CleanBuffTime", "Debuff Less End Times(ms)", new Slider(0, 1000, 800)));
            cleanMenu.Add(new MenuCheckbox("CleanOnlyKey", "Only Combo Mode Active?", true));

            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            if (cleanMenu.GetBool("CleanEnable"))
            {
                if (cleanMenu.GetBool("CleanOnlyKey") && !MyLogic.isComboMode)
                {
                    return;
                }
                
                if (CanClean(ObjectManager.Player) && Utils.TickCount > useCleanTime)
                {
                    if (Item.HasItem(ItemId.Quicksilver_Sash, ObjectManager.Player) && Item.CanUseItem(ItemId.Quicksilver_Sash))
                    {
                        Item.UseItem(ItemId.Quicksilver_Sash, ObjectManager.Player);
                        useCleanTime = Utils.TickCount + 3000;
                    }
                    else if (Item.HasItem(ItemId.Mercurial_Scimitar, ObjectManager.Player) && Item.CanUseItem(ItemId.Mercurial_Scimitar))
                    {
                        Item.UseItem(ItemId.Mercurial_Scimitar, ObjectManager.Player);
                        useCleanTime = Utils.TickCount + 3000;
                    }
                    else if (Item.HasItem(ItemId.Mikaels_Crucible, ObjectManager.Player) && Item.CanUseItem(ItemId.Mikaels_Crucible))
                    {
                        Item.UseItem(ItemId.Mikaels_Crucible, ObjectManager.Player);
                        useCleanTime = Utils.TickCount + 3000;
                    }
                    else if (Item.HasItem(ItemId.Dervish_Blade, ObjectManager.Player) && Item.CanUseItem(ItemId.Dervish_Blade))
                    {
                        Item.UseItem(ItemId.Dervish_Blade, ObjectManager.Player);
                        useCleanTime = Utils.TickCount + 3000;
                    }
                }
            }
        }

        private static bool CanClean(AIHeroClient hero)
        {
            var CanUse = false;

            if (useCleanTime > Utils.TickCount)
            {
                return false;
            }

            if (cleanMenu.GetBool("Cleanblind"))
            {
                debuffTypes.Add(BuffType.Blind);
            }

            if (cleanMenu.GetBool("Cleancharm"))
            {
                debuffTypes.Add(BuffType.Charm);
            }

            if (cleanMenu.GetBool("Cleanfear"))
            {
                debuffTypes.Add(BuffType.Fear);
            }

            if (cleanMenu.GetBool("Cleanflee"))
            {
                debuffTypes.Add(BuffType.Flee);
            }

            if (cleanMenu.GetBool("Cleanstun"))
            {
                debuffTypes.Add(BuffType.Stun);
            }

            if (cleanMenu.GetBool("Cleansnare"))
            {
                debuffTypes.Add(BuffType.Snare);
            }

            if (cleanMenu.GetBool("Cleantaunt"))
            {
                debuffTypes.Add(BuffType.Taunt);
            }

            if (cleanMenu.GetBool("Cleansuppression"))
            {
                debuffTypes.Add(BuffType.Suppression);
            }

            if (cleanMenu.GetBool("Cleanpolymorph"))
            {
                debuffTypes.Add(BuffType.Polymorph);
            }

            if (cleanMenu.GetBool("Cleansilence"))
            {
                debuffTypes.Add(BuffType.Blind);
            }

            if (cleanMenu.GetBool("Cleansilence"))
            {
                debuffTypes.Add(BuffType.Silence);
            }

            foreach (var buff in hero.Buffs)
            {
                if (debuffTypes.Contains(buff.Type) &&
                    (buff.EndTime - Game.Time) * 1000 >= cleanMenu.GetSlider("CleanBuffTime") &&
                    buff.IsActive)
                {
                    CanUse = true;
                }
            }

            if (cleanMenu.GetBool("Cleanexhaust") && hero.HasBuff("CleanSummonerExhaust"))
            {
                CanUse = true;
            }

            useCleanTime = Utils.TickCount + cleanMenu.GetSlider("CleanDelay");

            return CanUse;
        }
    }
}
