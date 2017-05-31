namespace Flowers_ADCSeries.MyActivator
{
    using HesaEngine.SDK;

    using System.Linq;

    internal class Potions
    {
        private static readonly Item HealthPotion = new Item(2003);
        private static readonly Item TotalBiscuitofRejuvenation = new Item(2010);
        private static readonly Item RefillablePotion = new Item(2031);
        private static readonly Item HuntersPotion = new Item(2032);
        private static readonly Item CorruptingPotion = new Item(2033);

        private static readonly string[] buffName = { "RegenerationPotion", "ItemMiniRegenPotion", "ItemCrystalFlask", "ItemCrystalFlaskJungle", "ItemDarkCrystalFlask" };

        private static Menu potionsMenu;

        internal static void AddToMenu(Menu mainMenu)
        {
            potionsMenu = mainMenu.AddSubMenu("Potions");

            potionsMenu.Add(new MenuCheckbox("Enabled", "Enabled"));
            potionsMenu.Add(new MenuSlider("HealthPercent", "When Player HealthPercent <= x%", new Slider(0, 100, 35)));

            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (!potionsMenu.Get<MenuCheckbox>("Enabled").Checked || ObjectManager.Player.IsDead ||
                ObjectManager.Player.InFountain() || ObjectManager.Player.IsRecalling() ||
                ObjectManager.Player.Buffs.Any(b => b.Name.ToLower().Contains("recall") || b.Name.ToLower().Contains("teleport")))
                return;

            if (ObjectManager.Player.Buffs.Any(x => buffName.Contains(x.Name)))
                return;

            if (ObjectManager.Player.HealthPercent <= potionsMenu.Get<MenuSlider>("HealthPercent").CurrentValue)
            {
                if (HealthPotion.IsOwned() && HealthPotion.IsReady())
                {
                    HealthPotion.Cast();
                }
                else if (TotalBiscuitofRejuvenation.IsOwned() && TotalBiscuitofRejuvenation.IsReady())
                {
                    TotalBiscuitofRejuvenation.Cast();
                }
                else if (RefillablePotion.IsOwned() && RefillablePotion.IsReady())
                {
                    RefillablePotion.Cast();
                }
                else if (HuntersPotion.IsOwned() && HuntersPotion.IsReady())
                {
                    HuntersPotion.Cast();
                }
                else if (CorruptingPotion.IsOwned() && CorruptingPotion.IsReady())
                {
                    CorruptingPotion.Cast();
                }
            }
        }
    }
}