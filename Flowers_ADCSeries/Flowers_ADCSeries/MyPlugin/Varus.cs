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

    internal class Varus : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 925f);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 975f);
            R = new Spell(SpellSlot.R, 1050f);

            Q.SetSkillshot(0.25f, 70f, 1650f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.35f, 120f, 1500f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 120f, 1950f, false, SkillshotType.SkillshotLine);

            Q.SetCharged("VarusQ", "VarusQ", 925, 1600, 1.5f);

            ComboOption.AddQ();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSolo", "Use R| Solo Mode");
            ComboOption.AddSlider("ComboRCount", "Use R| Min Enemies Count >= x", 3, 1, 5);
            ComboOption.AddSlider("ComboPassive", "Use Spell| Min Buff Count >= x", 3, 0, 3);

            HarassOption.AddQ();
            HarassOption.AddE(false);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove(false);

            KillStealOption.AddQ();
            KillStealOption.AddE();

            MiscOption.AddR();
            MiscOption.AddKey("SemiR", "Semi-manual R Key", SharpDX.DirectInput.Key.T);

            DrawOption.AddQ();
            DrawOption.AddE();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Orbwalker.BeforeAttack += BeforeAttack;
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

            SemiRLogic();
            KillSteal();

            if (isComboMode)
                Combo();

            if (isHarassMode)
            {
                Harass();
            }

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
                if (Q.IsCharging)
                {
                    return;
                }

                var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(R.Range))
                {
                    SpellManager.PredCast(R, target, false);
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(1600f) && x.Health < Q.GetDamage(x) + GetWDmg(x)))
                {
                    if (target.IsUnKillable())
                        return;

                    if (Q.IsCharging)
                    {
                        if (target.IsValidTarget(Q.ChargedMaxRange))
                        {
                            SpellManager.PredCast(Q, target);
                        }
                        else
                        {
                            foreach (
                                var t in
                                ObjectManager.Heroes.Enemies.Where(x => !x.IsDead && !x.IsZombie && x.IsValidTarget(Q.Range))
                                    .OrderBy(x => x.Health))
                            {
                                if (t.IsValidTarget(Q.ChargedMaxRange))
                                    SpellManager.PredCast(Q, target);
                            }
                        }
                    }
                    else
                    {
                        if (target.IsValidTarget(Q.Range))
                            SpellManager.PredCast(Q, target);
                        else
                            Q.StartCharging();
                    }
                    return;
                }
            }

            if (Q.IsCharging)
            {
                return;
            }

            if (KillStealOption.UseE && E.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(E.Range) && x.Health < E.GetDamage(x) + GetWDmg(x)))
                {
                    if (target.IsUnKillable())
                        return;

                    SpellManager.PredCast(E, target, true);
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetSelectedTarget() ?? myOrbwalker.GetTarget() as AIHeroClient;

            if (ComboOption.UseE && E.IsReady() && !Q.IsCharging)
            {
                if (target == null || !target.IsValidTarget())
                {
                    target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                }

                if (target.IsValidTarget(E.Range) && GetBuffCount(target) >= ComboOption.GetSlider("ComboPassive") || W.Level == 0 || target.Health < E.GetDamage(target) + GetWDmg(target) || !Orbwalker.InAutoAttackRange(target))
                {
                    SpellManager.PredCast(E, target, true);
                    return;
                }
            }

            if (ComboOption.UseQ && Q.IsReady())
            {
                if (target == null || !target.IsValidTarget())
                {
                    target = TargetSelector.GetTarget(1600f, TargetSelector.DamageType.Physical);
                }

                if (target.IsValidTarget(1600f))
                {
                    if (Q.IsCharging)
                    {
                        if (target.IsValidTarget(Q.ChargedMaxRange))
                        {
                            SpellManager.PredCast(Q, target);
                            return;
                        }
                    }
                    else
                    {
                        if (GetBuffCount(target) >= ComboOption.GetSlider("ComboPassive") || W.Level == 0 || target.Health < Q.GetDamage(target) + GetWDmg(target))
                        {
                            if (target.IsValidTarget(Q.Range))
                                SpellManager.PredCast(Q, target);
                            else
                                Q.StartCharging();
                            return;
                        }
                    }
                }
                else
                {
                    foreach (var t in ObjectManager.Heroes.Enemies.Where(x => !x.IsDead && !x.IsZombie && x.IsValidTarget(1600f)))
                    {
                        if (Q.IsCharging)
                        {
                            if (t.IsValidTarget(Q.ChargedMaxRange))
                            {
                                SpellManager.PredCast(Q, target);
                                return;
                            }
                        }
                        else
                        {
                            if (GetBuffCount(t) >= ComboOption.GetSlider("ComboPassive") || W.Level == 0 || t.Health < Q.GetDamage(t) + GetWDmg(t))
                            {
                                if (t.IsValidTarget(Q.Range))
                                    SpellManager.PredCast(Q, t);
                                else
                                    Q.StartCharging();
                                return;
                            }
                        }
                    }
                }
            }

            if (ComboOption.UseR && R.IsReady())
            {
                if (target == null || !target.IsValidTarget())
                {
                    target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                }

                if (target.IsValidTarget(R.Range) && ComboOption.GetBool("ComboRSolo") && Me.CountEnemiesInRange(1000) <= 2)
                {
                    if (target.Health + target.HPRegenRate * 2 <
                        R.GetDamage(target) + W.GetDamage(target) + (E.IsReady() ? E.GetDamage(target) : 0) +
                        (Q.IsReady() ? Q.GetDamage(target) : 0) + Me.GetAutoAttackDamage(target) * 3)
                    {
                        SpellManager.PredCast(R, target);
                        return;
                    }
                }

                var rPred = R.GetPrediction(target, true);

                if (rPred.AoeTargetsHitCount >= ComboOption.GetSlider("ComboRCount") ||
                    Me.CountEnemiesInRange(R.Range) >= ComboOption.GetSlider("ComboRCount"))
                {
                    SpellManager.PredCast(R, target);
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                var target = TargetSelector.GetTarget(1600f, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(1600f))
                {
                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(1600f))
                    {
                        if (Q.IsCharging)
                        {
                            if (target != null && target.IsValidTarget(Q.ChargedMaxRange))
                            {
                                SpellManager.PredCast(Q, target);
                            }
                            else
                            {
                                foreach (
                                    var t in
                                    ObjectManager.Heroes.Enemies.Where(x => !x.IsDead && !x.IsZombie && x.IsValidTarget(Q.ChargedMaxRange))
                                        .OrderBy(x => x.Health))
                                {
                                    if (t.IsValidTarget(Q.Range))
                                        SpellManager.PredCast(Q, target);
                                }
                            }
                        }
                        else
                        {
                            if (target.IsValidTarget(Q.Range))
                                SpellManager.PredCast(Q, target);
                            else
                                Q.StartCharging();
                        }
                        return;
                    }

                    if (Q.IsCharging)
                    {
                        return;
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
                Harass();
        }

        private static void LaneClear()
        {
            if (LaneClearOption.HasEnouguMana)
            {
                if (LaneClearOption.UseQ && Q.IsReady())
                {
                    var qMinions = MinionManager.GetMinions(Me.Position, 1600f);

                    if (qMinions.Any())
                    {
                        var qFarm =
                            MinionManager.GetBestLineFarmLocation(qMinions.Select(x => x.Position.To2D()).ToList(),
                                Q.Width, 1600f);

                        if (qFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearQCount"))
                        {
                            if (Q.IsCharging && qFarm.Position.DistanceToPlayer() <= Q.ChargedMaxRange)
                            {
                                Q.Cast(qFarm.Position, true);
                            }
                            else
                            {
                                if (qFarm.Position.DistanceToPlayer() <= Q.Range)
                                    Q.Cast(qFarm.Position, true);
                                else
                                    Q.StartCharging();
                            }
                        }
                    }
                }

                if (Q.IsCharging)
                {
                    return;
                }

                if (LaneClearOption.UseE && E.IsReady())
                {
                    var eMinions = MinionManager.GetMinions(Me.Position, E.Range);

                    if (eMinions.Any())
                    {
                        var eFarm =
                            MinionManager.GetBestCircularFarmLocation(eMinions.Select(x => x.Position.To2D()).ToList(),
                                E.Width, E.Range);

                        if (eFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearECount"))
                        {
                            E.Cast(eFarm.Position);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Me.Position, 1600f, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault(x => !x.Name.Contains("mini"));

                    if (mob != null)
                    {
                        if (JungleClearOption.UseQ && Q.IsReady() && mob.IsValidTarget(1600f))
                        {
                            if (Q.IsCharging)
                            {
                                if (mob.IsValidTarget(Q.Range))
                                {
                                    Q.Cast(mob.Position, true);
                                }
                            }
                            else
                            {
                                if (mob.IsValidTarget(Q.Range))
                                {
                                    Q.Cast(mob.Position, true);
                                }
                                else
                                    Q.StartCharging();
                            }
                        }

                        if (Q.IsCharging)
                        {
                            return;
                        }

                        if (JungleClearOption.UseE && E.IsReady() && mob.IsValidTarget(E.Range))
                        {
                            E.Cast(mob.Position);
                        }
                    }
                }
            }
        }

        private static void BeforeAttack(Orbwalker.BeforeAttackEventArgs Args)
        {
            if (Me.IsDead)
            {
                return;
            }

            Args.Process = !Q.IsCharging;
        }

        private static int GetBuffCount(Obj_AI_Base target)
        {
            return
                target.Buffs.Where(
                        buff => string.Equals(buff.Name, "varuswdebuff", StringComparison.CurrentCultureIgnoreCase))
                    .Select(buff => buff.Count == 0 ? 1 : buff.Count)
                    .FirstOrDefault();
        }

        private static float GetWDmg(Obj_AI_Base target)
        {
            return GetBuffCount(target) * W.GetDamage(target, 1);
        }
    }
}
