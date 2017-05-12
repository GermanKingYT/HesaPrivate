namespace Flowers_ADCSeries.MyPlugin
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyBase;
    using MyCommon;

    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class Urgot : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 1000f){MinHitChance = HitChance.VeryHigh};
            QExtend = new Spell(SpellSlot.Q, 1200f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 550f);

            Q.SetSkillshot(0.25f, 60f, 1600f, true, SkillshotType.SkillshotLine);
            QExtend.SetSkillshot(0.25f, 60f, 1600f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 120f, 1500f, false, SkillshotType.SkillshotCircle);

            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddBool("ComboWAlways", "Use W| Always Use it", false);
            ComboOption.AddBool("ComboWBuff", "Use W| If target have E buff");
            ComboOption.AddSlider("ComboWLowHp", "Use W| Player HealthPercent <= x%", 50, 0, 100);
            ComboOption.AddE();
            ComboOption.AddBool("ComboFirstE", "Use E| First Cast To Target");
            ComboOption.AddR();

            HarassOption.AddQ();
            HarassOption.AddW();
            HarassOption.AddE();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove(false);

            KillStealOption.AddQ();
            KillStealOption.AddE();

            MiscOption.AddR();
            MiscOption.AddSlider("RSwap", "If After Swap Enemies Count <= x", 3, 1, 5);
            MiscOption.AddBool("RAlly", "If Target Under Ally Turret");
            MiscOption.AddBool("RSafe", "Dont Cast In Enemy Turret");
            MiscOption.AddBool("RKill", "If Target Can Kill");
            MiscOption.AddSetting("Dont Cast Ult List");
            foreach (var target in ObjectManager.Heroes.Enemies)
                if (target != null)
                    MiscOption.AddBool("Dontr" + target.ChampionName.ToLower() + ObjectManager.Player.ChampionName, target.ChampionName);

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

            if (R.Level > 0)
            {
                R.Range = new[] { 550f, 700f, 850f }[R.Level - 1];
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
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(QExtend.Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(QExtend.Range))
            {
                if (ComboOption.UseR && R.IsReady() && target.IsValidTarget(R.Range))
                {
                    if (MiscOption.GetBool("RSafe") && !Me.UnderTurret(true))
                    {
                        foreach (
                            var rTarget in
                            ObjectManager.Heroes.Enemies.Where(
                                    x =>
                                        x.IsValidTarget(R.Range) &&
                                        !MiscOption.GetBool("Dontr" + target.ChampionName.ToLower()))
                                .OrderByDescending(x => E.IsReady() ? E.GetDamage(x) : 0 + Q.GetDamage(x) * 2))
                        {
                            if (rTarget.CountEnemiesInRange(R.Range) <= MiscOption.GetSlider("RSwap"))
                            {
                                if (MiscOption.GetBool("RAlly") && Me.UnderAllyTurret() && rTarget.DistanceToPlayer() <= 350)
                                {
                                    R.CastOnUnit(rTarget);
                                }

                                if (MiscOption.GetBool("RKill") && target.Health < MyDamageCalculate.GetComboDamage(target))
                                {
                                    R.CastOnUnit(rTarget);
                                }
                            }
                        }
                    }
                }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range) &&
                    (Q.IsReady() || target.Health < E.GetDamage(target)))
                {
                    SpellManager.PredCast(E, target, true);
                }

                if (ComboOption.UseW && W.IsReady())
                {
                    if (target.DistanceToPlayer() <= Me.AttackRange + Me.BoundingRadius)
                    {
                        if (ComboOption.GetBool("ComboWAlways"))
                        {
                            W.Cast();
                        }

                        if (Me.HealthPercent <= ComboOption.GetSlider("ComboWLowHp"))
                        {
                            W.Cast();
                        }
                    }

                    if (ComboOption.GetBool("ComboWBuff") && HaveEBuff(target) && Q.IsReady())
                    {
                        W.Cast();
                    }
                }

                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(QExtend.Range))
                {
                    if (!HaveEBuff(target) && target.IsValidTarget(Q.Range))
                    {
                        if (ComboOption.GetBool("ComboFirstE") && E.IsReady() && ComboOption.UseE && target.IsValidTarget(E.Range))
                        {
                            SpellManager.PredCast(E, target, true);
                        }
                        else
                        {
                            SpellManager.PredCast(Q, target);
                        }
                    }
                    else if (target.IsValidTarget(QExtend.Range) && HaveEBuff(target))
                    {
                        QExtend.Cast(target);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                var target = TargetSelector.GetTarget(QExtend.Range, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(QExtend.Range))
                {
                    if (HarassOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                    {
                        SpellManager.PredCast(E, target, true);
                    }

                    if (HarassOption.UseW && W.IsReady())
                    {
                        if (HaveEBuff(target) && Q.IsReady())
                        {
                            W.Cast();
                        }
                    }

                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(QExtend.Range))
                    {
                        if (!HaveEBuff(target) && target.IsValidTarget(Q.Range))
                        {
                            SpellManager.PredCast(Q, target);
                        }
                        else if (target.IsValidTarget(QExtend.Range) && HaveEBuff(target))
                        {
                            QExtend.Cast(target, true);
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
                var minions = MinionManager.GetMinions(Me.Position, R.Range);

                if (minions.Any())
                {
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
                            Q.Cast(qMinion, true);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();
                    var bigmob = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        if (bigmob != null && bigmob.IsValidTarget(E.Range))
                        {
                            E.Cast(bigmob, true);
                        }
                        else
                        {
                            var eMobs = MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All, MinionTeam.Neutral,
                                MinionOrderTypes.MaxHealth);

                            var eFarm =
                                MinionManager.GetBestLineFarmLocation(eMobs.Select(x => x.Position.To2D()).ToList(),
                                    E.Width, E.Range);

                            if (eFarm.MinionsHit >= 1)
                            {
                                E.Cast(eFarm.Position, true);
                            }
                        }
                    }

                    if (JungleClearOption.UseQ && Q.IsReady() && mob != null && mob.IsValidTarget(Q.Range))
                    {
                        Q.Cast(mob, true);
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
                    if (ComboOption.UseE && E.IsReady() && (Q.IsReady() || target.Health < E.GetDamage(target)))
                    {
                        SpellManager.PredCast(E, target, true);
                    }

                    if (ComboOption.UseW && W.IsReady())
                    {
                        if (target.DistanceToPlayer() <= Me.AttackRange + Me.BoundingRadius)
                        {
                            if (ComboOption.GetBool("ComboWAlways"))
                            {
                                W.Cast();
                            }

                            if (Me.HealthPercent <= ComboOption.GetSlider("ComboWLowHp"))
                            {
                                W.Cast();
                            }
                        }

                        if (ComboOption.GetBool("ComboWBuff") && HaveEBuff(target) && Q.IsReady())
                        {
                            W.Cast();
                        }
                    }
                    else if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(QExtend.Range))
                    {
                        if (!HaveEBuff(target) && target.IsValidTarget(Q.Range))
                        {
                            if (ComboOption.GetBool("ComboFirstE") && E.IsReady() && ComboOption.UseE && target.IsValidTarget(E.Range))
                            {
                                SpellManager.PredCast(E, target, true);
                            }
                            else
                            {
                                SpellManager.PredCast(Q, target);
                            }
                        }
                        else if (target.IsValidTarget(QExtend.Range) && HaveEBuff(target))
                        {
                            QExtend.Cast(target);
                        }
                    }

                }
            }

            if (isJungleClearMode && JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (mob != null)
                    {
                        if (JungleClearOption.UseW && W.IsReady())
                        {
                            W.Cast();
                        }

                        if (JungleClearOption.UseQ && Q.IsReady() && mob.IsValidTarget(Q.Range))
                        {
                            Q.Cast(mob);
                        }
                    }
                }
            }
        }

        private static bool HaveEBuff(Obj_AI_Base target)
        {
            return target.HasBuff("urgotcorrosivedebuff");
        }
    }
}
