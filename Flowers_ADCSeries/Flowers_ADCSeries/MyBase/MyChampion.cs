namespace Flowers_ADCSeries.MyBase
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Enums;

    using MyCommon;
    using MyPlugin;

    using System.Linq;
    using System;

    internal class MyChampion : MyLogic
    {
        private static readonly string[] SuppoetChampions =
        {
            "Ashe", "Caityln", "Corki", "Draven", "Ezreal", "Graves", "Jhin",
            "Jinx", "Kalista", "Kindred", "KogMaw", "Lucian", "MissFortune", "Quinn", "Sivir", "Teemo", "Tristana",
            "TwistedFate", "Twitch", "Urgot", "Varus", "Vayne", "Xayah"
        };

        internal static void Init()
        {
            Chat.Print("Flowers' ADC Series: Main Init!");

            InitActivator();
            InitChampions();
        }

        private static void InitActivator()
        {
            MyMenuExtensions.ActivatorMenu = Menu.AddMenu(new Menu("Flowers' Activator"));

            Chat.Print("Flowers' ADC Series: Activator Init Successful! Made by NightMoon");
        }

        private static void InitChampions()
        {
            if (!SuppoetChampions.Contains(herosName))
            {
                Chat.Print("Flowers' ADC Series" + "[" + herosName + "]: Not Support!");
                return;
            }

            MyMenuExtensions.myMenu = Menu.AddMenu(new Menu("Flowers' ADC Series: " + herosName));

            myOrbwalker = new Orbwalker.OrbwalkerInstance(MyMenuExtensions.myMenu.AddSubMenu(":: Orbwalker Settings"));

            MyMenuExtensions.ComboMenu = MyMenuExtensions.myMenu.AddSubMenu(":: Combo Settings");

            MyMenuExtensions.HarassMenu = MyMenuExtensions.myMenu.AddSubMenu(":: Harass Settings");

            MyMenuExtensions.LaneClearMenu = MyMenuExtensions.myMenu.AddSubMenu(":: LaneClear Settings");

            MyMenuExtensions.JungleClearMenu = MyMenuExtensions.myMenu.AddSubMenu(":: JungleClear Settings");

            MyMenuExtensions.LastHitMenu = MyMenuExtensions.myMenu.AddSubMenu(":: LastHit Settings");

            MyMenuExtensions.FleeMenu = MyMenuExtensions.myMenu.AddSubMenu(":: Flee Settings");

            MyMenuExtensions.KillStealMenu = MyMenuExtensions.myMenu.AddSubMenu(":: KillSteal Settings");

            MyMenuExtensions.MiscMenu = MyMenuExtensions.myMenu.AddSubMenu(":: Misc Settings");
            MyManaManager.AddSpellFarm(MyMenuExtensions.MiscMenu);

            MyMenuExtensions.DrawMenu = MyMenuExtensions.myMenu.AddSubMenu(":: Drawings Settings");

            switch (ObjectManager.Player.ChampionName)
            {
                case "Ashe":
                    Ashe.Init();
                    break;
                case "Caitlyn":
                    Caitlyn.Init();
                    break;
                case "Corki":
                    Corki.Init();
                    break;
                case "Draven":
                    Draven.Init();
                    break;
                case "Ezreal":
                    Ezreal.Init();
                    break;
                case "Graves":
                    Graves.Init();
                    break;
                case "Jayce":
                    Jayce.Init();
                    break;
                case "Jhin":
                    Jhin.Init();
                    break;
                case "Jinx":
                    Jinx.Init();
                    break;
                case "Kalista":
                    Kalista.Init();
                    break;
                case "Kindred":
                    Kindred.Init();
                    break;
                case "KogMaw":
                    KogMaw.Init();
                    break;
                case "Lucian":
                    Lucian.Init();
                    break;
                case "MissFortune":
                    MissFortune.Init();
                    break;
                case "Quinn":
                    Quinn.Init();
                    break;
                case "Sivir":
                    Sivir.Init();
                    break;
                case "Teemo":
                    Teemo.Init();
                    break;
                case "Tristana":
                    Tristana.Init();
                    break;
                case "TwistedFate":
                    TwistedFate.Init();
                    break;
                case "Twitch":
                    Twitch.Init();
                    break;
                case "Urgot":
                    Urgot.Init();
                    break;
                case "Varus":
                    Varus.Init();
                    break;
                case "Vayne":
                    Vayne.Init();
                    break;
                case "Xayah":
                    Xayah.Init();
                    break;
            }

            Chat.Print("Flowers' ADC Series" + "[" + herosName + "]: Init Successful! Made by NightMoon");
        }

    }
}