namespace Flowers_ADCSeries.MyPlugin
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Args;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyBase;
    using MyCommon;

    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class Twitch : MyLogic
    {
        private static bool PlayerIsKillTarget { get; set; }

        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 950f) {MinHitChance = HitChance.VeryHigh};
            E = new Spell(SpellSlot.E, 1200f);
            R = new Spell(SpellSlot.R, 975f);

            W.SetSkillshot(0.25f, 100f, 1410f, false, SkillshotType.SkillshotCircle);

            ComboOption.AddQ();
            ComboOption.AddSlider("ComboQCount", "Use Q| Min Enemies Count >= x", 3, 1, 5);
            ComboOption.AddSlider("ComboQRange", "Use Q| Min Search Range", 600, 0, 1800);
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddBool("ComboEKill", "Use E| Target Can KillSteal", false);
            ComboOption.AddBool("ComboEFull", "Use E| Target Have Full Stack", false);
            ComboOption.AddR();
            ComboOption.AddBool("ComboRKillSteal", "Use R| Target Can KS");
            ComboOption.AddSlider("ComboRCount", "Use R| Min Enemies Count >= x", 3, 1, 5);
            ComboOption.AddBool("ComboRYouMuu", "Use Youmuu| R Is Active");

            HarassOption.AddW();
            HarassOption.AddE();
            HarassOption.AddBool("HarassEStack", "Use E| Target Will Leave E Range");
            HarassOption.AddSlider("HarassEStackCount", "Use E| Target Min Stack Count >= x", 3, 1, 6);
            HarassOption.AddBool("HarassEFull", "Use E| Target Have Full Stack");
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min Kill Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove(false);

            KillStealOption.AddW();
            KillStealOption.AddE();

            MiscOption.AddQ();
            MiscOption.AddBool("AutoQ", "Auto Q| After KS Target and Have Enemies In Range");

            DrawOption.AddW();
            DrawOption.AddE();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            PlayerIsKillTarget = false;

            Game.OnUpdate += OnUpdate;
            Game.OnNotify += OnNotify;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (MiscOption.GetBool("AutoQ"))
            {
                if (PlayerIsKillTarget && Q.IsReady() && Me.CountEnemiesInRange(1000) >= 1)
                {
                    Q.Cast();
                }
            }

            KillSteal();

            if (isComboMode)
                Combo();

            if (isHarassMode)
                Harass();

            if (isFarmMode)
            {
                FarmHarass();

                if (isLaneClearMode)
                    LaneClear();

                if (isJungleClearMode)
                    JungleClear();
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseE && E.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(E.Range) && x.Health < E.GetDamage(x) - 5))
                {
                    if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                    {
                        E.Cast();
                    }
                }
            }

            if (KillStealOption.UseW && W.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(W.Range) && x.Health < W.GetDamage(x)))
                {
                    if (target.IsValidTarget(W.Range) && !Orbwalker.InAutoAttackRange(target) && !target.IsUnKillable())
                    {
                        SpellManager.PredCast(W, target, true);
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(E.Range))
            {
                if (ComboOption.GetBool("ComboRYouMuu") && myOrbwalker.GetTarget() != null && myOrbwalker.GetTarget() is AIHeroClient && Me.HasBuff("TwitchFullAutomatic"))
                {
                    if (Item.HasItem(3142))
                    {
                        Item.UseItem(3142);
                    }
                }

                if (ComboOption.UseR && R.IsReady())
                {
                    if (ComboOption.GetBool("ComboRKillSteal") &&
                        ObjectManager.Heroes.Enemies.Count(x => x.DistanceToPlayer() <= R.Range) <= 2 &&
                        target.Health <= Me.GetAutoAttackDamage(target, true) * 4 + GetRealEDamage(target) * 2)
                    {
                        R.Cast();
                    }

                    if (ObjectManager.Heroes.Enemies
                            .Count(x => x.DistanceToPlayer() <= R.Range) >= ComboOption.GetSlider("ComboRCount"))
                    {
                        R.Cast();
                    }
                }

                if (ComboOption.UseQ && Q.IsReady() &&
                    ObjectManager.Heroes.Enemies.Count(
                        x => x.DistanceToPlayer() <= ComboOption.GetSlider("ComboQRange")) >= ComboOption.GetSlider("ComboQCount"))
                {
                    Q.Cast();
                }

                if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range) &&
                    target.Health > W.GetDamage(target) && GetEStackCount(target) < 6 &&
                    Me.Mana >= Q.ManaCost + W.ManaCost + E.ManaCost + R.ManaCost)
                {
                    SpellManager.PredCast(W, target, true);
                }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range) &&
                    target.HasBuff("TwitchDeadlyVenom"))
                {
                    if (ComboOption.GetBool("ComboEFull") && GetEStackCount(target) >= 6)
                    {
                        E.Cast();
                    }

                    if (ComboOption.GetBool("ComboEKill") && target.Health <= E.GetDamage(target) && target.IsValidTarget(E.Range))
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                if (HarassOption.UseW && W.IsReady())
                {
                    var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                    if (wTarget.IsValidTarget(W.Range))
                    {
                        SpellManager.PredCast(W, wTarget, true);
                    }
                }

                if (HarassOption.UseE && E.IsReady())
                {
                    var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                    if (eTarget.IsValidTarget(E.Range))
                    {
                        if (HarassOption.GetBool("HarassEStack"))
                        {
                            if (eTarget.DistanceToPlayer() > E.Range * 0.8 && eTarget.IsValidTarget(E.Range) &&
                                GetEStackCount(eTarget) >= HarassOption.GetSlider("HarassEStackCount"))
                            {
                                E.Cast();
                            }
                        }

                        if (HarassOption.GetBool("HarassEFull") && GetEStackCount(eTarget) >= 6)
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void FarmHarass()
        {
            if (MyManaManager.SpellHarass)
            {
                Harass();
            }
        }

        private static void LaneClear()
        {
            if (LaneClearOption.HasEnouguMana)
            {
                if (LaneClearOption.UseE && E.IsReady())
                {
                    var eKillMinionsCount =
                        MinionManager.GetMinions(Me.Position, E.Range)
                            .Count(
                                x =>
                                    x.DistanceToPlayer() <= E.Range && x.HasBuff("TwitchDeadlyVenom") &&
                                    x.Health < E.GetDamage(x));

                    if (eKillMinionsCount >= LaneClearOption.GetSlider("LaneClearECount"))
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                if (JungleClearOption.UseE && E.IsReady())
                {
                    var mobs = MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                    foreach (
                        var mob in
                        mobs.Where(
                            x =>
                                !x.Name.ToLower().Contains("mini") && x.DistanceToPlayer() <= E.Range &&
                                x.HasBuff("TwitchDeadlyVenom")))
                    {
                        if (mob.Health < E.GetDamage(mob) && mob.IsValidTarget(E.Range))
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void OnNotify(GameNotifyEventArgs Args)
        {
            if (Me.IsDead)
            {
                PlayerIsKillTarget = false;
            }
            else if (!Me.IsDead)
            {
                if (Args.EventId == GameEventId.OnChampionDie && Args.SourceNetworkId == Me.NetworkId)
                {
                    PlayerIsKillTarget = true;

                    Core.DelayAction(() => {PlayerIsKillTarget = false;}, 5000);
                }
            }
        }

        private static int GetEStackCount(Obj_AI_Base target)
        {
            return target.HasBuff("TwitchDeadlyVenom") ? target.GetBuffCount("TwitchDeadlyVenom") : 0;
        }

        private static float GetRealEDamage(Obj_AI_Base target)
        {
            if (target != null && !target.IsDead && !target.IsZombie && target.HasBuff("TwitchDeadlyVenom"))
            {
                if (target.HasBuff("KindredRNoDeathBuff"))
                {
                    return 0;
                }

                if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("JudicatorIntervention"))
                {
                    return 0;
                }

                if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("FioraW"))
                {
                    return 0;
                }

                if (target.HasBuff("ShroudofDarkness"))
                {
                    return 0;
                }

                if (target.HasBuff("SivirShield"))
                {
                    return 0;
                }

                var damage = 0f;

                damage += E.IsReady() ? E.GetDamage(target) : 0f;

                if (target.CharData.BaseSkinName == "Moredkaiser")
                {
                    damage -= target.Mana;
                }

                if (Me.HasBuff("SummonerExhaust"))
                {
                    damage = damage * 0.6f;
                }

                if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
                {
                    damage -= target.Mana / 2f;
                }

                if (target.HasBuff("GarenW"))
                {
                    damage = damage * 0.7f;
                }

                if (target.HasBuff("ferocioushowl"))
                {
                    damage = damage * 0.7f;
                }

                return damage;
            }

            return 0f;
        }
    }
}
