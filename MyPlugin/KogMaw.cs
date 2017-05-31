namespace Flowers_ADCSeries.MyPlugin
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyBase;
    using MyCommon;

    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class KogMaw : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 980f);
            W = new Spell(SpellSlot.W, Me.AttackRange);
            E = new Spell(SpellSlot.E, 1200f);
            R = new Spell(SpellSlot.R, 1800f);

            Q.SetSkillshot(0.25f, 50f, 2000f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 120f, 1400f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.2f, 120f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRLimit", "Use R| Limit Stack >= x", 3, 0, 10);

            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddR();
            HarassOption.AddSlider("HarassRLimit", "Use R| Limit Stack >= x", 5, 0, 10);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min Hit Counts >= x", 3, 1, 5);
            LaneClearOption.AddR();
            LaneClearOption.AddSlider("LaneClearRLimit", "Use R| Limit Stack >= x", 4, 0, 10);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddR();
            JungleClearOption.AddSlider("JungleClearRLimit", "Use R| Limit Stack >= x", 5, 0, 10);
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove(false);

            KillStealOption.AddQ();
            KillStealOption.AddE();
            KillStealOption.AddR();

            MiscOption.AddE();
            MiscOption.AddBool("GapE", "Use E| Anti GapCloser");
            MiscOption.AddR();
            MiscOption.AddKey("SemiR", "Semi-manual R Key", SharpDX.DirectInput.Key.T);

            DrawOption.AddQ();
            DrawOption.AddW();
            DrawOption.AddE();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Orbwalker.AfterAttack += AfterAttack;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
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

            if (W.Level > 0)
            {
                W.Range = Me.AttackRange + new[] { 130, 150, 170, 190, 210 }[W.Level - 1];
            }

            if (R.Level > 0)
            {
                R.Range = 1200 + 300 * R.Level - 1;
            }

            SemiRLogic();
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

        private static void SemiRLogic()
        {
            if (MiscOption.GetKey("SemiR") && R.IsReady())
            {
                var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(R.Range))
                {
                    SpellManager.PredCast(R, target, true);
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x)))
                {
                    if (!target.IsUnKillable())
                    {
                        SpellManager.PredCast(Q, target);
                        return;
                    }
                }
            }

            if (KillStealOption.UseE && E.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(E.Range) && x.Health < E.GetDamage(x)))
                {
                    if (!target.IsUnKillable())
                    {
                        SpellManager.PredCast(E, target, true);
                        return;
                    }
                }
            }

            if (KillStealOption.UseR && R.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(R.Range) && x.Health < R.GetDamage(x)))
                {
                    if (!target.IsUnKillable())
                    {
                        SpellManager.PredCast(R, target, true);
                        return;
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(R.Range))
            {
                if (ComboOption.UseR && R.IsReady() &&
                    ComboOption.GetSlider("ComboRLimit") >= GetRCount &&
                    target.IsValidTarget(R.Range))
                {
                    SpellManager.PredCast(R, target, true);
                }

                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    SpellManager.PredCast(Q, target, true);
                }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                {
                    SpellManager.PredCast(E, target);
                }

                if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range) &&
                    target.DistanceToPlayer() > Orbwalker.GetRealAutoAttackRange(Me) && Orbwalker.CanAttack())
                {
                    W.Cast();
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                if (target.IsValidTarget(R.Range))
                {
                    if (HarassOption.UseR && R.IsReady() && HarassOption.GetSlider("HarassRLimit") >= GetRCount &&
                        target.IsValidTarget(R.Range))
                    {
                        SpellManager.PredCast(R, target, true);
                    }

                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                    {
                        SpellManager.PredCast(Q, target);
                    }

                    if (HarassOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                    {
                        SpellManager.PredCast(E, target, true);
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
                var minions = MinionManager.GetMinions(Me.Position, R.Range);

                if (minions.Any())
                {
                    if (LaneClearOption.UseR && R.IsReady() && LaneClearOption.GetSlider("LaneClearRLimit") >= GetRCount)
                    {
                        var rMinion =
                            minions.FirstOrDefault(x => x.DistanceToPlayer() > Orbwalker.GetRealAutoAttackRange(Me));

                        if (rMinion != null && MinionHealthPrediction.GetHealthPrediction(rMinion, 250) > 0)
                        {
                            R.Cast(rMinion);
                        }
                    }

                    if (LaneClearOption.UseE && E.IsReady())
                    {
                        var eMinions = MinionManager.GetMinions(Me.Position, E.Range);
                        var eFarm =
                            MinionManager.GetBestLineFarmLocation(eMinions.Select(x => x.Position.To2D()).ToList(),
                                E.Width, E.Range);

                        if (eFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearECount"))
                        {
                            E.Cast(eFarm.Position);
                        }
                    }

                    if (LaneClearOption.UseQ && Q.IsReady())
                    {
                        var qMinion =
                            MinionManager
                                .GetMinions(
                                    Me.Position, Q.Range)
                                .FirstOrDefault(
                                    x =>
                                        x.Health < Q.GetDamage(x) &&
                                        MinionHealthPrediction.GetHealthPrediction(x, 250) > 0 &&
                                        x.Health > Me.GetAutoAttackDamage(x));

                        if (qMinion != null)
                        {
                            Q.Cast(qMinion);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Me.Position, R.Range, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();
                    var bigmob = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

                    if (JungleClearOption.UseW && W.IsReady() && bigmob != null && bigmob.IsValidTarget(W.Range))
                    {
                        W.Cast();
                    }

                    if (JungleClearOption.UseR && R.IsReady() && JungleClearOption.GetSlider("JungleClearRLimit") >= GetRCount &&
                        bigmob != null)
                    {
                        R.Cast(bigmob);
                    }

                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        if (bigmob != null && bigmob.IsValidTarget(E.Range))
                        {
                            E.Cast(bigmob);
                        }
                        else
                        {
                            var eMobs = MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All, MinionTeam.Neutral,
                                MinionOrderTypes.MaxHealth);
                            var eFarm =
                                MinionManager.GetBestLineFarmLocation(eMobs.Select(x => x.Position.To2D()).ToList(),
                                    E.Width, E.Range);

                            if (eFarm.MinionsHit >= 2)
                            {
                                E.Cast(eFarm.Position);
                            }
                        }
                    }

                    if (JungleClearOption.UseQ && Q.IsReady() && mob != null && mob.IsValidTarget(Q.Range))
                    {
                        Q.Cast(mob);
                    }
                }
            }
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit ArgsTarget)
        {
            if (!unit.IsMe)
                return;

            if (isComboMode)
            {
                var target = (AIHeroClient)ArgsTarget;

                if (target != null && !target.IsDead && !target.IsZombie)
                {
                    if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range))
                    {
                        W.Cast();
                    }
                    else if (ComboOption.UseR && R.IsReady() && ComboOption.GetSlider("ComboRLimit") >= GetRCount &&
                        target.IsValidTarget(R.Range))
                    {
                        SpellManager.PredCast(R, target, true);
                    }
                    else if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                    {
                        SpellManager.PredCast(Q, target);
                    }
                    else if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                    {
                        SpellManager.PredCast(E, target, true);
                    }
                }
            }
            else if (isJungleClearMode && JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Me.Position, R.Range, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();
                    var bigmob = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

                    if (JungleClearOption.UseW && W.IsReady() && bigmob != null && bigmob.IsValidTarget(W.Range))
                    {
                        W.Cast();
                    }
                    else if (JungleClearOption.UseR && R.IsReady() && JungleClearOption.GetSlider("JungleClearRLimit") >= GetRCount &&
                        bigmob != null)
                    {
                        R.Cast(bigmob);
                    }
                    else if (JungleClearOption.UseE && E.IsReady())
                    {
                        if (bigmob != null && bigmob.IsValidTarget(E.Range))
                        {
                            E.Cast(bigmob);
                        }
                        else
                        {
                            var eMobs = MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All, MinionTeam.Neutral,
                                MinionOrderTypes.MaxHealth);
                            var eFarm = E.GetLineFarmLocation(eMobs, E.Width);

                            if (eFarm.MinionsHit >= 2)
                            {
                                E.Cast(eFarm.Position);
                            }
                        }
                    }
                    else if (JungleClearOption.UseQ && Q.IsReady() && mob != null && mob.IsValidTarget(Q.Range))
                    {
                        Q.Cast(mob);
                    }
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser Args)
        {
            if (MiscOption.GetBool("GapE") && E.IsReady() && Args.Sender.IsValidTarget(E.Range))
            {
                SpellManager.PredCast(E, Args.Sender, true);
            }
        }

        private static int GetRCount => Me.HasBuff("kogmawlivingartillerycost") ? Me.GetBuffCount("kogmawlivingartillerycost") : 0;
    }
}
