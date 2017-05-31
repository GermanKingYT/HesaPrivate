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

    internal class Jinx : MyLogic
    {
        private static float bigGunRange;
        private static float rCoolDown;

        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 525f);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 920f);
            R = new Spell(SpellSlot.R, 3000f);

            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.2f, 100f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.7f, 140f, 1500f, false, SkillshotType.SkillshotLine);

            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSolo", "Use R| Solo Mode");
            ComboOption.AddBool("ComboRTeam", "Use R| Team Fight");

            HarassOption.AddQ();
            HarassOption.AddW();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q| Min Hit Counts >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove(false);

            KillStealOption.AddW();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            MiscOption.AddW();
            MiscOption.AddBool("AutoW", "Auto W| CC");
            MiscOption.AddE();
            MiscOption.AddBool("AutoE", "Auto E| CC");
            MiscOption.AddBool("AutoETP", "Auto E| Teleport");
            MiscOption.AddBool("GapE", "Auto E| Anti GapCloser");
            MiscOption.AddR();
            MiscOption.AddKey("rMenuSemi", "Semi R Key", SharpDX.DirectInput.Key.T);
            MiscOption.AddSlider("rMenuMin", "Use R| Min Range >= x", 1000, 500, 2500);
            MiscOption.AddSlider("rMenuMax", "Use R| Man Range <= x", 3000, 1500, 3500);

            DrawOption.AddW();
            DrawOption.AddE();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Orbwalker.BeforeAttack += BeforeAttack;
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

            if (Q.Level > 0)
            {
                bigGunRange = Q.Range + new[] { 75, 100, 125, 150, 175 }[Q.Level - 1];
            }

            if (R.Level > 0)
            {
                R.Range = MiscOption.GetSlider("rMenuMax");
            }

            rCoolDown = R.Level > 0
                ? (R.Instance.CooldownExpires - Game.Time < 0 ? 0 : R.Instance.CooldownExpires - Game.Time)
                : -1;

            AutoLogic();
            SemiRLogic();
            KillSteal();

            if (isComboMode)
                Combo();

            if (isHarassMode)
                Harass();

            if (isFarmMode)
            {
                FarmHarass();

                if (isJungleClearMode)
                    JungleClear();
            }
        }

        private static void AutoLogic()
        {
            if (MiscOption.GetBool("AutoW") && W.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(W.Range) && !x.CanMoveMent()))
                {
                    if (target.IsValidTarget())
                        W.Cast(target);
                }
            }

            if (MiscOption.GetBool("AutoE") && E.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.CanMoveMent()))
                {
                    if (target.IsValidTarget())
                        E.Cast(target);
                }
            }

            if (MiscOption.GetBool("AutoETP") && E.IsReady())
            {
                foreach (
                    var obj in
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(
                            x =>
                                x.IsEnemy && x.DistanceToPlayer() < E.Range &&
                                (x.HasBuff("teleport_target") || x.HasBuff("Pantheon_GrandSkyfall_Jump"))))
                {
                    if (obj.IsValidTarget())
                        E.Cast(obj.Position);
                }
            }
        }

        private static void SemiRLogic()
        {
            if (MiscOption.GetKey("rMenuSemi") && R.IsReady())
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
            if (KillStealOption.UseW && W.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(W.Range) && x.Health < W.GetDamage(x)))
                {
                    if (Orbwalker.InAutoAttackRange(target) && target.Health <= Me.GetAutoAttackDamage(target, true) * 2)
                    {
                        continue;
                    }

                    if (!target.IsUnKillable())
                    {
                        SpellManager.PredCast(W, target);
                        return;
                    }
                }
            }

            if (KillStealOption.UseR && R.IsReady())
            {
                foreach (
                    var target in
                    ObjectManager.Heroes.Enemies.Where(
                        x =>
                            x.IsValidTarget(R.Range) && x.DistanceToPlayer() > MiscOption.GetSlider("rMenuMin") &&
                            KillStealOption.GetKillStealTarget(x.ChampionName) && x.Health < R.GetDamage(x)))
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
            if (ComboOption.UseW && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(W.Range) && target.DistanceToPlayer() > Q.Range
                    && Me.CountEnemiesInRange(W.Range - 300) <= 3)
                {
                    SpellManager.PredCast(W, target);
                }
            }

            if (ComboOption.UseE && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(E.Range))
                {
                    if (!target.CanMoveMent())
                    {
                        E.Cast(target);
                    }
                    else
                    {
                        if (E.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
                        {
                            SpellManager.PredCast(E, target);
                        }
                    }
                }
            }

            if (ComboOption.UseQ && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(bigGunRange, TargetSelector.DamageType.Physical);

                if (Me.HasBuff("JinxQ"))
                {
                    if (Me.Mana < (rCoolDown == -1 ? 100 : (rCoolDown > 10 ? 130 : 150)))
                    {
                        Q.Cast();
                    }

                    if (Me.CountEnemiesInRange(1500) == 0)
                    {
                        Q.Cast();
                    }

                    if (target == null)
                    {
                        Q.Cast();
                    }
                    else if (target.IsValidTarget(bigGunRange))
                    {
                        if (target.Health < Me.GetAutoAttackDamage(target) * 3 &&
                            target.DistanceToPlayer() <= Q.Range + 60)
                        {
                            Q.Cast();
                        }
                    }
                }
                else
                {
                    if (target.IsValidTarget(bigGunRange))
                    {
                        if (Me.CountEnemiesInRange(Q.Range) == 0 && Me.CountEnemiesInRange(bigGunRange) > 0 &&
                            Me.Mana > R.ManaCost + W.ManaCost + Q.ManaCost * 2)
                        {
                            Q.Cast();
                        }

                        if (target.CountEnemiesInRange(150) >= 2 &&
                            Me.Mana > R.ManaCost + Q.ManaCost * 2 + W.ManaCost && target.DistanceToPlayer() > Q.Range)
                        {
                            Q.Cast();
                        }
                    }
                }
            }

            if (ComboOption.UseR && R.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(1200)))
                {
                    if (ComboOption.GetBool("ComboRTeam") && target.IsValidTarget(600) && Me.CountEnemiesInRange(600) >= 2 &&
                        target.CountAlliesInRange(200) <= 3 && target.HealthPercent < 50)
                    {
                        SpellManager.PredCast(R, target, true);
                    }

                    if (ComboOption.GetBool("ComboRSolo") && Me.CountEnemiesInRange(1500) <= 2 && target.DistanceToPlayer() > Q.Range &&
                        target.DistanceToPlayer() < bigGunRange && target.Health > Me.GetAutoAttackDamage(target) &&
                        target.Health < R.GetDamage(target) + Me.GetAutoAttackDamage(target) * 3)
                    {
                        SpellManager.PredCast(R, target, true);
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
                    var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical, true,
                        ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                    if (target.IsValidTarget(W.Range) && target.DistanceToPlayer() > Q.Range)
                    {
                        SpellManager.PredCast(W, target);
                    }
                }

                if (HarassOption.UseQ && Q.IsReady())
                {
                    var target = TargetSelector.GetTarget(bigGunRange, TargetSelector.DamageType.Physical, true,
                        ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                    if (target.IsValidTarget(bigGunRange) && Orbwalker.CanAttack())
                    {
                        if (target.CountEnemiesInRange(150) >= 2 &&
                            Me.Mana > R.ManaCost + Q.ManaCost * 2 + W.ManaCost && target.DistanceToPlayer() > Q.Range)
                        {
                            Q.Cast();
                        }

                        if (target.DistanceToPlayer() > Q.Range && Me.Mana > R.ManaCost + Q.ManaCost * 2 + W.ManaCost)
                        {
                            Q.Cast();
                        }
                    }
                    else
                    {
                        if (Me.HasBuff("JinxQ") && Q.IsReady())
                        {
                            Q.Cast();
                        }
                    }
                }
                else if (Me.HasBuff("JinxQ") && Q.IsReady())
                {
                    Q.Cast();
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

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Me.Position, bigGunRange, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    if (JungleClearOption.UseW && W.IsReady() && mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini")) != null)
                    {
                        W.Cast(mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini")));
                    }

                    if (JungleClearOption.UseQ && Q.IsReady())
                    {
                        if (Me.HasBuff("JinxQ"))
                        {
                            foreach (var mob in mobs)
                            {
                                var count = ObjectManager.Get<Obj_AI_Minion>().Count(x => x.Distance(mob) <= 150);

                                if (mob.DistanceToPlayer() <= bigGunRange)
                                {
                                    if (count < 2)
                                    {
                                        Q.Cast();
                                    }
                                    else if (mob.Health > Me.GetAutoAttackDamage(mob) * 1.1f)
                                    {
                                        Q.Cast();
                                    }
                                }
                            }

                            if (mobs.Count < 2)
                            {
                                Q.Cast();
                            }
                        }
                        else
                        {
                            foreach (var mob in mobs)
                            {
                                var count = ObjectManager.Get<Obj_AI_Minion>().Count(x => x.Distance(mob) <= 150);

                                if (mob.DistanceToPlayer() <= bigGunRange)
                                {
                                    if (count >= 2)
                                    {
                                        Q.Cast();
                                    }
                                    else if (mob.Health < Me.GetAutoAttackDamage(mob) * 1.1f &&
                                             mob.DistanceToPlayer() > Q.Range)
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Me.HasBuff("JinxQ") && Q.IsReady())
                    {
                        Q.Cast();
                    }
                }
            }
            else
            {
                if (Me.HasBuff("JinxQ") && Q.IsReady())
                {
                    Q.Cast();
                }
            }
        }

        private static void BeforeAttack(BeforeAttackEventArgs Args)
        {
            if (!Args.Unit.IsMe || !Q.IsReady())
            {
                return;
            }

            if (isComboMode)
            {
                if (ComboOption.UseQ)
                {
                    var target = Args.Target as AIHeroClient;

                    if (target != null && !target.IsDead && !target.IsZombie)
                    {
                        if (Me.HasBuff("JinxQ"))
                        {
                            if (target.Health < Me.GetAutoAttackDamage(target) * 3 &&
                                target.DistanceToPlayer() <= Q.Range + 60)
                            {
                                Q.Cast();
                            }
                            else if (Me.Mana < (rCoolDown == -1 ? 100 : (rCoolDown > 10 ? 130 : 150)))
                            {
                                Q.Cast();
                            }
                            else if (target.IsValidTarget(Q.Range))
                            {
                                Q.Cast();
                            }
                        }
                        else
                        {
                            if (target.CountEnemiesInRange(150) >= 2 &&
                                Me.Mana > R.ManaCost + Q.ManaCost * 2 + W.ManaCost &&
                                target.DistanceToPlayer() > Q.Range)
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
            }
            else
            {
                if ((isFarmMode && MyManaManager.SpellHarass) || isHarassMode)
                {
                    if (HarassOption.HasEnouguMana)
                    {
                        if (HarassOption.UseQ)
                        {
                            var target = Args.Target as AIHeroClient;

                            if (target != null && !target.IsDead && !target.IsZombie)
                            {
                                if (Me.HasBuff("JinxQ"))
                                {
                                    if (target.DistanceToPlayer() >= bigGunRange)
                                    {
                                        Q.Cast();
                                    }
                                }
                                else
                                {
                                    if (target.CountEnemiesInRange(150) >= 2 &&
                                        Me.Mana > R.ManaCost + Q.ManaCost * 2 + W.ManaCost &&
                                        target.DistanceToPlayer() > Q.Range)
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Me.HasBuff("JinxQ") && Q.IsReady())
                        {
                            Q.Cast();
                        }
                    }
                }

                if (isLaneClearMode)
                {
                    if (LaneClearOption.HasEnouguMana)
                    {
                        if (LaneClearOption.UseQ)
                        {
                            var min = Args.Target as Obj_AI_Base;
                            var minions = MinionManager.GetMinions(Me.Position, bigGunRange);

                            if (minions.Any() && min != null)
                            {
                                foreach (var minion in minions.Where(x => x.NetworkId != min.NetworkId))
                                {
                                    var count = ObjectManager.Get<Obj_AI_Minion>().Count(x => x.Distance(minion) <= 150);

                                    if (minion.DistanceToPlayer() <= bigGunRange)
                                    {
                                        if (Me.HasBuff("JinxQ"))
                                        {
                                            if (LaneClearOption.GetSlider("LaneClearQCount") > count)
                                            {
                                                Q.Cast();
                                            }
                                            else if (min.Health > Me.GetAutoAttackDamage(min) * 1.1f)
                                            {
                                                Q.Cast();
                                            }
                                        }
                                        else if (!Me.HasBuff("JinxQ"))
                                        {
                                            if (LaneClearOption.GetSlider("LaneClearQCount") <= count)
                                            {
                                                Q.Cast();
                                            }
                                            else if (min.Health < Me.GetAutoAttackDamage(min) * 1.1f &&
                                                     min.DistanceToPlayer() > Q.Range)
                                            {
                                                Q.Cast();
                                            }
                                        }
                                    }
                                }

                                if (minions.Count <= 2 && Me.HasBuff("JinxQ"))
                                {
                                    Q.Cast();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Me.HasBuff("JinxQ") && Q.IsReady())
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser Args)
        {
            var target = Args.Sender;

            if (target.IsValidTarget(E.Range) && (Args.End.DistanceToPlayer() <= 300 || target.DistanceToPlayer() <= 300))
            {
                if (MiscOption.GetBool("GapE") && E.IsReady())
                {
                    SpellManager.PredCast(E, target, true);
                }
            }
        }
    }
}
