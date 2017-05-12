namespace Flowers_ADCSeries.MyPlugin
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyBase;
    using MyCommon;

    using System;
    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class Corki : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 825f);
            W = new Spell(SpellSlot.W, 800f);
            E = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 1300f);

            Q.SetSkillshot(0.3f, 200f, 1000f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.1f, (float)(45 * Math.PI / 180), 1500f, false, SkillshotType.SkillshotCone);
            R.SetSkillshot(0.2f, 40f, 2000f, true, SkillshotType.SkillshotLine);

            ComboOption.AddQ();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRLimit", "Use R| Limit Stack >= x", 0, 0, 7);

            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddR();
            HarassOption.AddSlider("HarassRLimit", "Use R| Limit Stack >= x", 4, 0, 7);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddR();
            LaneClearOption.AddSlider("LaneClearRCount", "Use R| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddSlider("LaneClearRLimit", "Use Q| Limit Stack >= x", 4, 0, 7);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddR();
            JungleClearOption.AddSlider("JungleClearRLimit", "Use R| Limit Stack >= x", 0, 0, 7);
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddW();
            FleeOption.AddMove(false);

            KillStealOption.AddQ();
            KillStealOption.AddR();

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
        }


        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (R.Level > 0)
            {
                R.Range = Me.HasBuff("CorkiMissileBarrageCounterBig") ? 1500f : 1300f;
            }

            if (isFleeMode)
            {
                Flee();

                if (FleeOption.DisableMove)
                {
                    Orbwalker.Move = false;
                }

                return;
            }

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

                if(isJungleClearMode)
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
                        SpellManager.PredCast(R, target, true);
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

            if (target.IsValidTarget(R.Range) && (!Orbwalker.InAutoAttackRange(target) || !Me.CanAttack))
            {
                if (ComboOption.UseR && R.IsReady() && R.Instance.CurrentCharge >= ComboOption.GetSlider("ComboRLimit") &&
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
                    E.Cast();
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(R.Range))
                {
                    if (HarassOption.UseR && R.IsReady() && R.Instance.CurrentCharge >= HarassOption.GetSlider("HarassRLimit") &&
                        target.IsValidTarget(R.Range))
                    {
                        SpellManager.PredCast(R, target, true);
                    }

                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                    {
                        SpellManager.PredCast(Q, target, true);
                    }

                    if (HarassOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                    {
                        E.Cast();
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
                if (LaneClearOption.UseQ && Q.IsReady())
                {
                    var minions = MinionManager.GetMinions(Me.Position, Q.Range);

                    if (minions.Any())
                    {
                        var QFram = MinionManager.GetBestCircularFarmLocation(minions.Select(x => x.Position.To2D()).ToList(), Q.Width, Q.Range);

                        if (QFram.MinionsHit >= LaneClearOption.GetSlider("LaneClearQCount"))
                        {
                            Q.Cast(QFram.Position, true);
                        }
                    }
                }

                if (LaneClearOption.UseE && E.IsReady())
                {
                    var eMinions = MinionManager.GetMinions(Me.Position, E.Range);

                    if (eMinions.Any())
                    {
                        if (eMinions.Count >= LaneClearOption.GetSlider("LaneClearECount"))
                        {
                            E.Cast();
                        }
                    }
                }

                if (LaneClearOption.UseR && R.IsReady() && R.Instance.CurrentCharge >= LaneClearOption.GetSlider("LaneClearRLimit"))
                {
                    var rMinions = MinionManager.GetMinions(Me.Position, R.Range);

                    if (rMinions.Any())
                    {
                        var RFarm = MinionManager.GetBestLineFarmLocation(rMinions.Select(x => x.Position.To2D()).ToList(), R.Width, R.Range);

                        if (RFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearRCount"))
                        {
                            R.Cast(RFarm.Position, true);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(R.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseR && R.IsReady() && R.Instance.CurrentCharge >= JungleClearOption.GetSlider("JungleClearRLimit"))
                    {
                        R.Cast(mob, true);
                    }

                    if (JungleClearOption.UseQ && Q.IsReady())
                    {
                        Q.Cast(mob, true);
                    }
                }
            }
        }

        private static void Flee()
        {
            if (FleeOption.UseW && W.IsReady())
            {
                W.Cast(Me.Position.Extend(Game.CursorPosition, W.Range), true);
            }
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit ArgsTarget)
        {
            if (!unit.IsMe || Me.IsDead || ArgsTarget == null || ArgsTarget.IsDead || !ArgsTarget.IsValidTarget())
                return;

            if (isComboMode && ArgsTarget.ObjectType == GameObjectType.AIHeroClient)
            {
                var target = ArgsTarget as AIHeroClient;

                if (target != null)
                {
                    if (ComboOption.UseR && R.IsReady() && R.Instance.CurrentCharge >= ComboOption.GetSlider("ComboRLimit"))
                    {
                        SpellManager.PredCast(R, target, true);
                    }
                    else if (ComboOption.UseQ && Q.IsReady())
                    {
                        SpellManager.PredCast(Q, target, true);
                    }
                    else if (ComboOption.UseE && E.IsReady())
                    {
                        E.Cast();
                    }
                }
            }
            else if (isJungleClearMode && ArgsTarget.ObjectType == GameObjectType.obj_AI_Minion)
            {
                if (JungleClearOption.HasEnouguMana)
                {
                    var mobs = MinionManager.GetMinions(R.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                    if (mobs.Any())
                    {
                        var mob = mobs.FirstOrDefault();

                        if (JungleClearOption.UseR && R.IsReady() && R.Instance.CurrentCharge >= JungleClearOption.GetSlider("JungleClearRLimit"))
                        {
                            R.Cast(mob, true);
                        }
                        else if (JungleClearOption.UseQ && Q.IsReady())
                        {
                            Q.Cast(mob, true);
                        }
                        else if (JungleClearOption.UseE && E.IsReady())
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }
    }
}
