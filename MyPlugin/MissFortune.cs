namespace Flowers_ADCSeries.MyPlugin
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyBase;
    using MyCommon;

    using SharpDX;

    using System;
    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class MissFortune : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 700f);
            QExtend = new Spell(SpellSlot.Q, 1300f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1000f);
            R = new Spell(SpellSlot.R, 1350f);

            QExtend.SetSkillshot(0.25f, 70f, 1500f, true, SkillshotType.SkillshotLine);
            Q.SetTargetted(0.25f, 1400f);
            E.SetSkillshot(0.5f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 50f, 3000f, false, SkillshotType.SkillshotCircle);

            ComboOption.AddQ();
            ComboOption.AddBool("ComboQ1", "Use Q Extend");
            ComboOption.AddW();
            ComboOption.AddE();

            HarassOption.AddQ();
            HarassOption.AddBool("HarassQ1", "Use Q Extend");
            HarassOption.AddE();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min Hit Counts >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove(false);

            KillStealOption.AddQ();

            MiscOption.AddR();
            MiscOption.AddKey("SemiR", "Semi-manual R Key", SharpDX.DirectInput.Key.T);

            DrawOption.AddQ();
            DrawOption.AddE();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Orbwalker.AfterAttack += AfterAttack;
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

            if (Me.HasBuff("missfortunebulletsound"))
            {
                Orbwalker.Attack = false;
                Orbwalker.Move = false;
                return;
            }

            Orbwalker.Attack = true;
            Orbwalker.Move = true;

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
                foreach (
                    var target in
                    ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(QExtend.Range) && x.Health < Q.GetDamage(x)))
                {
                    if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                        QLogic(target, true);
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(QExtend.Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(QExtend.Range))
            {
                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(QExtend.Range))
                {
                    QLogic(target, ComboOption.GetBool("ComboQ1"));
                }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                {
                    E.Cast(target.Position, true);
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                var target = TargetSelector.GetTarget(QExtend.Range, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                if (target.IsValidTarget(QExtend.Range))
                {
                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(QExtend.Range))
                    {
                        QLogic(target, HarassOption.GetBool("HarassQ1"));
                    }

                    if (HarassOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                    {
                        E.Cast(target.Position, true);
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
                var minions = MinionManager.GetMinions(Me.Position, E.Range);

                if (minions.Any())
                {
                    if (LaneClearOption.UseE && E.IsReady())
                    {
                        var eFarm =
                            MinionManager.GetBestCircularFarmLocation(minions.Select(x => x.Position.To2D()).ToList(),
                                E.Width, E.Range);

                        if (eFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearECount"))
                        {
                            E.Cast(eFarm.Position);
                        }
                    }

                    if (LaneClearOption.UseQ && Q.IsReady() && minions.Count > 2)
                    {
                        Q.Cast(minions.FirstOrDefault());
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
                    var mobs = MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All, MinionTeam.Neutral,
                        MinionOrderTypes.MaxHealth);

                    if (mobs.Any())
                    {
                        var bigmobs = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

                        if (bigmobs != null)
                        {
                            E.Cast(bigmobs.Position);
                        }
                        else if (mobs.Count >= 2)
                        {
                            E.Cast(mobs.FirstOrDefault());
                        }
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
                var target = ArgsTarget as AIHeroClient;

                if (target != null)
                {
                    if (ComboOption.UseQ && Q.IsReady())
                    {
                        Q.Cast(target, true);
                    }
                    else if (ComboOption.UseW && W.IsReady())
                    {
                        W.Cast();
                    }
                }
            }
            else if (isJungleClearMode && JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (mob != null)
                    {
                        if (JungleClearOption.UseQ && Q.IsReady())
                        {
                            Q.Cast(mob, true);
                        }
                        else if (JungleClearOption.UseW && W.IsReady())
                        {
                            W.Cast();
                        }
                    }
                }
            }
        }

        private static void QLogic(AIHeroClient target, bool UseQ1 = false)// SFX Challenger MissFortune QLogic (im so lazy, kappa)
        {
            if (target != null)
            {
                if (target.IsValidTarget(Q.Range))
                {
                    Q.CastOnUnit(target);
                }
                else if (UseQ1 && target.IsValidTarget(QExtend.Range) && target.DistanceToPlayer() > Q.Range)
                {
                    var heroPositions = (from t in ObjectManager.Heroes.Enemies
                                         where t.IsValidTarget(QExtend.Range)
                                         let prediction = Q.GetPrediction(t)
                                         select new Position(t, prediction.UnitPosition)).Where(
                        t => t.UnitPosition.Distance(Me.Position) < QExtend.Range).ToList();

                    if (heroPositions.Any())
                    {
                        var minions = MinionManager.GetMinions(QExtend.Range, MinionTypes.All, MinionTeam.NotAlly);

                        if (minions.Any(m => m.IsMoving) &&
                            !heroPositions.Any(h => h.Hero.HasBuff("missfortunepassive")))
                        {
                            return;
                        }

                        var outerMinions = minions.Where(m => m.Distance(Me) > Q.Range).ToList();
                        var innerPositions = minions.Where(m => m.Distance(Me) < Q.Range).ToList();

                        foreach (var minion in innerPositions)
                        {
                            var lMinion = minion;
                            var coneBuff = new Geometry.Polygon.Sector(
                                minion.Position,
                                Me.Position.Extend(minion.Position, Me.Distance(minion) + Q.Range * 0.5f),
                                (float)(40 * Math.PI / 180), QExtend.Range - Q.Range);
                            var coneNormal = new Geometry.Polygon.Sector(
                                minion.Position,
                                Me.Position.Extend(minion.Position, Me.Distance(minion) + Q.Range * 0.5f),
                                (float)(60 * Math.PI / 180), QExtend.Range - Q.Range);

                            foreach (var enemy in
                                heroPositions.Where(
                                    m => m.UnitPosition.Distance(lMinion.Position) < QExtend.Range - Q.Range))
                            {
                                if (coneBuff.IsInside(enemy.Hero) && enemy.Hero.HasBuff("missfortunepassive"))
                                {
                                    Q.CastOnUnit(minion);
                                    return;
                                }
                                if (coneNormal.IsInside(enemy.UnitPosition))
                                {
                                    var insideCone =
                                        outerMinions.Where(m => coneNormal.IsInside(m.Position)).ToList();

                                    if (!insideCone.Any() ||
                                        enemy.UnitPosition.Distance(minion.Position) <
                                        insideCone.Select(
                                                m => m.Position.Distance(minion.Position) - m.BoundingRadius)
                                            .DefaultIfEmpty(float.MaxValue)
                                            .Min())
                                    {
                                        Q.CastOnUnit(minion);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal struct Position
        {
            public readonly AIHeroClient Hero;
            public readonly Vector3 UnitPosition;

            public Position(AIHeroClient hero, Vector3 unitPosition)
            {
                Hero = hero;
                UnitPosition = unitPosition;
            }
        }
    }
}
