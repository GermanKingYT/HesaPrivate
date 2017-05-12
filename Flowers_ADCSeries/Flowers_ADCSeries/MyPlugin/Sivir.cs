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

    internal class Sivir : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 1200f){MinHitChance = HitChance.VeryHigh};
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.SkillshotLine);

            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRCount", "Use R| Min Enemies Counts >= x", 3, 1, 5);

            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q| Min Hit Counts >= x", 3, 1, 5);
            LaneClearOption.AddW();
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove(false);

            KillStealOption.AddQ();

            MiscOption.AddQ();
            MiscOption.AddBool("AutoQ", "Auto Q| CC");
            MiscOption.AddE();
            MiscOption.AddBool("AutoE", "Auto E| Shield Spell");
            MiscOption.AddSlider("AutoEHp", "Auto E| Player HealthPercent <= x%", 80);
            MiscOption.AddR();
            MiscOption.AddBool("AutoR", "Auto R", false);

            DrawOption.AddQ();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Orbwalker.AfterAttack += AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (isFleeMode && FleeOption.DisableMove)
            {
                Orbwalker.Move = false;
                return;
            }

            Orbwalker.Move = true;

            Auto();
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

        private static void Auto()
        {
            if (Me.UnderTurret(true))
            {
                return;
            }

            if (MiscOption.GetBool("AutoQ") && Q.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(Q.Range) && !x.CanMoveMent()))
                {
                    if (target.IsValidTarget(Q.Range))
                    {
                        SpellManager.PredCast(Q, target, true);
                    }
                }
            }

            if (MiscOption.GetBool("AutoR") && R.IsReady() && Me.CountEnemiesInRange(850) >= 3)
            {
                R.Cast();
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.IsReady())
            {
                foreach (
                    var target in
                    ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x)))
                {
                    if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                    {
                        SpellManager.PredCast(Q, target, true);
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1500f, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(1500f))
            {
                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range) &&
                    !Me.IsDashing())
                {
                    SpellManager.PredCast(Q, target, true);
                }

                if (ComboOption.UseR && Me.CountEnemiesInRange(850) >= ComboOption.GetSlider("ComboRCount") &&
                    ((target.Health <= Me.GetAutoAttackDamage(target) * 3 && !Q.IsReady()) ||
                     (target.Health <= Me.GetAutoAttackDamage(target) * 3 + Q.GetDamage(target))))
                {
                    R.Cast();
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana && HarassOption.UseQ && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                if (target.IsValidTarget(Q.Range))
                {
                    SpellManager.PredCast(Q, target, true);
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
                if (LaneClearOption.UseQ && Q.IsReady())
                {
                    var Minions = MinionManager.GetMinions(Me.Position, Q.Range);

                    if (Minions.Any())
                    {
                        var QFarm =
                            MinionManager.GetBestLineFarmLocation(Minions.Select(x => x.Position.To2D()).ToList(),
                                Q.Width, Q.Range);

                        if (QFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearQCount"))
                        {
                            Q.Cast(QFarm.Position);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                if (JungleClearOption.UseQ && Q.IsReady())
                {
                    var mobs =
                        MinionManager.GetMinions(Me.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral,
                            MinionOrderTypes.MaxHealth);

                    if (mobs.Any())
                    {
                        var QFarm =
                            MinionManager.GetBestLineFarmLocation(mobs.Select(x => x.Position.To2D()).ToList(),
                                Q.Width, Q.Range);

                        if (QFarm.MinionsHit >= 1)
                        {
                            Q.Cast(QFarm.Position);
                        }
                    }
                }
            }
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit ArgsTarget)
        {
            if (!unit.IsMe || !W.IsReady() || ArgsTarget == null)
            {
                return;
            }

            if (isComboMode)
            {
                var hero = ArgsTarget as AIHeroClient;

                if (hero != null && ComboOption.UseW && W.IsReady())
                {
                    if (hero.IsValidTarget(Orbwalker.GetRealAutoAttackRange(Me)))
                    {
                        W.Cast();
                        Orbwalker.ResetAutoAttackTimer();
                    }
                }
            }
            else
            {
                if (isLaneClearMode && LaneClearOption.HasEnouguMana && LaneClearOption.UseW)
                {
                    if (ArgsTarget.ObjectType == GameObjectType.obj_AI_Turret ||
                        ArgsTarget.ObjectType == GameObjectType.obj_Barracks ||
                        ArgsTarget.ObjectType == GameObjectType.obj_HQ ||
                        ArgsTarget.ObjectType == GameObjectType.obj_Turret ||
                        ArgsTarget.ObjectType == GameObjectType.obj_BarracksDampener)
                    {
                        if (Me.CountEnemiesInRange(1000) == 0)
                        {
                            W.Cast();
                            Orbwalker.ResetAutoAttackTimer();
                        }
                    }
                    else if (ArgsTarget.ObjectType == GameObjectType.obj_AI_Minion &&
                             ArgsTarget.Team != GameObjectTeam.Neutral)
                    {
                        var minions = MinionManager.GetMinions(Me.Position, Orbwalker.GetRealAutoAttackRange(Me));

                        if (minions.Count >= 3)
                        {
                            W.Cast();
                            Orbwalker.ResetAutoAttackTimer();
                        }
                    }
                }

                if (isJungleClearMode && JungleClearOption.HasEnouguMana)
                {
                    if (ArgsTarget.ObjectType == GameObjectType.obj_AI_Minion && ArgsTarget.Team == GameObjectTeam.Neutral)
                    {
                        var Mobs = MinionManager.GetMinions(Me.Position, Orbwalker.GetRealAutoAttackRange(Me), MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                        if (Mobs.Any())
                        {
                            W.Cast();
                            Orbwalker.ResetAutoAttackTimer();
                        }
                    }
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (MiscOption.GetBool("AutoE") && E.IsReady() && Me.HealthPercent <= MiscOption.GetSlider("AutoEHp"))
            {
                if (sender != null && sender.IsEnemy && sender is AIHeroClient)
                {
                    var e = (AIHeroClient)sender;

                    if (Args.Target != null)
                    {
                        if (Args.Target.IsMe)
                        {
                            if (CanE(e, Args))
                            {
                                Core.DelayAction(() => E.Cast(), 120);
                            }
                        }
                    }
                }
            }
        }

        private static bool CanE(AIHeroClient target, GameObjectProcessSpellCastEventArgs Args)
        {
            if (Orbwalker.IsAutoAttack(Args.SData.Name))
            {
                switch (target.ChampionName)
                {
                    case "TwistedFate":
                        if (Args.SData.Name == "GoldCardLock" || Args.SData.Name == "RedCardLock" || Args.SData.Name == "BlueCardLock" || target.HasBuff("GoldCardLock") || target.HasBuff("RedCardLock") || target.HasBuff("BlueCardLock"))
                        {
                            return true;
                        }
                        break;
                    case "Leona":
                        if (target.HasBuff("LeonaQ"))
                        {
                            return true;
                        }
                        break;
                    default:
                        return false;
                }
            }
            else
            {
                return !Args.SData.Name.ToLower().Contains("summoner");
            }

            return false;
        }
    }
}
