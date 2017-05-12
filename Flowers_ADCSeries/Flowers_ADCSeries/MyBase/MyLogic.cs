namespace Flowers_ADCSeries.MyBase
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.GameObjects;

    using System.Linq;

    internal class MyLogic
    {
        internal static Orbwalker.OrbwalkerInstance myOrbwalker { get; set; }

        internal static Spell Q { get; set; }
        internal static Spell QExtend { get; set; }
        internal static Spell Q2 { get; set; }
        internal static Spell W { get; set; }
        internal static Spell W1 { get; set; }
        internal static Spell W2 { get; set; }
        internal static Spell E { get; set; }
        internal static Spell E2 { get; set; }
        internal static Spell R { get; set; }
        internal static Spell R1 { get; set; }
        internal static Spell EQ { get; set; }

        internal static int lastQTime { get; set; } = 0;
        internal static int lastWTime { get; set; } = 0;
        internal static int lastETime { get; set; } = 0;
        internal static int lastRTime { get; set; } = 0;
        internal static int lastCastTime { get; set; } = 0;
        internal static int lastCatchTime { get; set; } = 0;

        internal static bool havePassive { get; set; }
        internal static bool havePassiveBuff => Me.Buffs.Any(x => x.Name.ToLower() == "lucianpassivebuff");

        internal static AIHeroClient Me => ObjectManager.Player;

        internal static string herosName => ObjectManager.Player.ChampionName;

        internal static bool isComboMode => myOrbwalker.ActiveMode == Orbwalker.OrbwalkingMode.Combo;
        internal static bool isHarassMode => myOrbwalker.ActiveMode == Orbwalker.OrbwalkingMode.Harass;
        internal static bool isLaneClearMode => myOrbwalker.ActiveMode == Orbwalker.OrbwalkingMode.LaneClear;
        internal static bool isJungleClearMode => myOrbwalker.ActiveMode == Orbwalker.OrbwalkingMode.JungleClear;
        internal static bool isLastHitMode => myOrbwalker.ActiveMode == Orbwalker.OrbwalkingMode.LastHit;
        internal static bool isFleeMode => myOrbwalker.ActiveMode == Orbwalker.OrbwalkingMode.Flee;
        internal static bool isNoneMode => myOrbwalker.ActiveMode == Orbwalker.OrbwalkingMode.None;
        internal static bool isFarmMode => myOrbwalker.ActiveMode == Orbwalker.OrbwalkingMode.LaneClear || myOrbwalker.ActiveMode == Orbwalker.OrbwalkingMode.JungleClear;
    }
}