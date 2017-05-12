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

    internal class Ezreal : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 1150f){MinHitChance = HitChance.VeryHigh};
            W = new Spell(SpellSlot.W, 950f);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 10000f);
            EQ = new Spell(SpellSlot.Q, 1150f + 475f);

            EQ.SetSkillshot(0.25f + 0.65f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddBool("ComboECheck", "Use E | Safe Check");
            ComboOption.AddBool("ComboEWall", "Use E | Wall Check");
            ComboOption.AddR();

            HarassOption.AddQ();
            HarassOption.AddW();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddBool("LaneClearQLH", "Use Q| Only LastHit", false);
            LaneClearOption.AddW();
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddMana();

            LastHitOption.AddQ();
            LastHitOption.AddMana();

            FleeOption.AddMove();

            KillStealOption.AddQ();
            KillStealOption.AddW();

            MiscOption.AddE();
            MiscOption.AddBool("Gapcloser", "Anti GapCloser");
            MiscOption.AddBool("AntiMelee", "Anti Melee");
            MiscOption.AddSlider("AntiMeleeHp", "Anti Melee|When Player HealthPercent <= x%", 50);
            MiscOption.AddR();
            MiscOption.AddBool("AutoR", "Auto R?");
            MiscOption.AddSlider("RRange", "Use R |Min Cast Range >= x", 800, 0, 1500);
            MiscOption.AddSlider("RMaxRange", "Use R |Max Cast Range >= x", 3000, 1500, 5000);
            MiscOption.AddSlider("RMinCast", "Use R| Min Hit Enemies >= x (6 = off)", 2, 1, 6);
            MiscOption.AddKey("SemiR", "Semi-manual R Key", SharpDX.DirectInput.Key.T);
            MiscOption.AddSetting("Mode");
            MiscOption.AddList("PlayMode", "Play Mode: ", new[] { "AD", "AP" });

            DrawOption.AddQ();
            DrawOption.AddW();
            DrawOption.AddE();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Orbwalker.BeforeAttack += BeforeAttack;
            Orbwalker.AfterAttack += AfterAttack;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (R.Level > 0)
            {
                R.Range = MiscOption.GetSlider("RMaxRange");
            }

            if (isFleeMode && FleeOption.DisableMove)
            {
                Orbwalker.Move = false;
                return;
            }

            Orbwalker.Move = true;

            if (MiscOption.GetKey("SemiR") && R.IsReady())
            {
                OneKeyCastR();
            }

            KillSteal();
            AutoRLogic();

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

            if (isLastHitMode)
                LastHit();
        }

        private static void AutoRLogic()
        {
            if (MiscOption.GetBool("AutoR") && R.IsReady() && Me.CountEnemiesInRange(1000) == 0)
            {
                foreach (
                    var target in
                    ObjectManager.Heroes.Enemies.Where(
                        x =>
                            x.IsValidTarget(R.Range) && x.DistanceToPlayer() >= MiscOption.GetSlider("RRange")))
                {
                    if (!target.CanMoveMent() && target.IsValidTarget(EQ.Range) &&
                        R.GetDamage(target) + Q.GetDamage(target) * 3 >= target.Health + target.HPRegenRate * 2)
                    {
                        R.Cast(target, true);
                    }

                    if (R.GetDamage(target) > target.Health + target.HPRegenRate * 2 && target.Path.Length < 2 &&
                        R.GetPrediction(target, true).Hitchance >= HitChance.High)
                    {
                        R.Cast(target, true);
                    }

                    if (isComboMode && Me.CountEnemiesInRange(800) == 0)
                    {
                        R.CastIfWillHit(target, MiscOption.GetSlider("RMinCast"), true);
                    }
                }
            }
        }

        private static void KillSteal()
        {
            foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(Q.Range)))
            {
                if (KillStealOption.UseQ && Q.GetDamage(target) > target.Health && target.IsValidTarget(Q.Range))
                {
                    if (!target.IsUnKillable())
                    {
                        SpellManager.PredCast(Q, target);
                        return;
                    }
                }

                if (KillStealOption.UseW && W.GetDamage(target) > target.Health && target.IsValidTarget(W.Range))
                {
                    if (!target.IsUnKillable())
                    {
                        SpellManager.PredCast(W, target, true);
                        return;
                    }
                }

                if (KillStealOption.UseQ && KillStealOption.UseW &&
                    target.Health < Q.GetDamage(target) + W.GetDamage(target) && target.IsValidTarget(W.Range))
                {
                    if (!target.IsUnKillable())
                    {
                        SpellManager.PredCast(W, target, true);
                        SpellManager.PredCast(Q, target);
                        return;
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MiscOption.GetList("PlayMode") == 0
                ? TargetSelector.GetTarget(EQ.Range, TargetSelector.DamageType.Physical)
                : TargetSelector.GetTarget(EQ.Range);

            if (target.IsValidTarget(EQ.Range))
            {
                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    SpellManager.PredCast(Q, target);
                }

                if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range))
                {
                    SpellManager.PredCast(W, target, true);
                }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(EQ.Range))
                {
                    if (ComboOption.GetBool("ComboECheck") && !Me.UnderTurret(true) &&
                        Me.CountEnemiesInRange(1200) <= 2)
                    {
                        var useECombo = false;

                        if (target.DistanceToPlayer() > Orbwalker.GetRealAutoAttackRange(Me) &&
                            target.IsValidTarget())
                        {
                            if (target.Health < E.GetDamage(target) + Me.GetAutoAttackDamage(target) &&
                                target.Distance(Game.CursorPosition) < Me.Distance(Game.CursorPosition))
                            {
                                useECombo = true;
                            }

                            if (target.Health < E.GetDamage(target) + W.GetDamage(target) && W.IsReady() &&
                                target.Distance(Game.CursorPosition) + 350 < Me.Distance(Game.CursorPosition))
                            {
                                useECombo = true;
                            }

                            if (target.Health < E.GetDamage(target) + Q.GetDamage(target) && Q.IsReady() &&
                                target.Distance(Game.CursorPosition) + 300 < Me.Distance(Game.CursorPosition))
                            {
                                useECombo = true;
                            }
                        }

                        if (useECombo)
                        {
                            var CastEPos = Me.Position.Extend(target.Position, 475f);

                            if (ComboOption.GetBool("ComboEWall"))
                            {
                                if (NavMesh.GetCollisionFlags(CastEPos) != CollisionFlags.Wall &&
                                    NavMesh.GetCollisionFlags(CastEPos) != CollisionFlags.Building &&
                                    NavMesh.GetCollisionFlags(CastEPos) != CollisionFlags.Prop)
                                {
                                    E.Cast(CastEPos);
                                    useECombo = false;
                                }
                            }
                            else
                            {
                                E.Cast(CastEPos);
                                useECombo = false;
                            }
                        }
                    }
                }

                if (ComboOption.UseR && R.IsReady())
                {
                    if (Me.UnderTurret(true) || Me.CountEnemiesInRange(800) > 1)
                    {
                        return;
                    }

                    foreach (var rTarget in ObjectManager.Heroes.Enemies.Where(
                            x =>
                                x.IsValidTarget(R.Range) &&
                                target.DistanceToPlayer() > Orbwalker.GetRealAutoAttackRange(Me)))
                    {
                        if (target.Health < R.GetDamage(rTarget) && R.GetPrediction(rTarget).Hitchance >= HitChance.VeryHigh &&
                            target.DistanceToPlayer() > Q.Range + E.Range / 2)
                        {
                            R.Cast(rTarget, true);
                        }

                        if (rTarget.IsValidTarget(Q.Range + E.Range) &&
                            R.GetDamage(rTarget) + (Q.IsReady() ? Q.GetDamage(rTarget) : 0) +
                            (W.IsReady() ? W.GetDamage(rTarget) : 0) > rTarget.Health + rTarget.HPRegenRate * 2)
                        {
                            R.Cast(rTarget, true);
                        }
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                if (HarassOption.UseQ && Q.IsReady())
                {
                    foreach (
                        var target in
                        ObjectManager.Heroes.Enemies.Where(
                            x =>
                                x.IsValidTarget(Q.Range) &&
                                HarassOption.GetHarassTarget(x.ChampionName)))
                    {
                        if (target.IsValidTarget(Q.Range))
                        {
                            SpellManager.PredCast(Q, target);
                        }
                    }
                }

                if (HarassOption.UseW && W.IsReady())
                {
                    foreach (
                        var target in
                        ObjectManager.Heroes.Enemies.Where(
                            x =>
                                x.IsValidTarget(W.Range) &&
                                HarassOption.GetHarassTarget(x.ChampionName)))
                    {
                        if (target.IsValidTarget(W.Range))
                        {
                            SpellManager.PredCast(W, target, true);
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
                if (LaneClearOption.UseQ && Q.IsReady())
                {
                    var minions = MinionManager.GetMinions(Me.Position, Q.Range);

                    if (minions.Any())
                    {
                        if (LaneClearOption.GetBool("LaneClearQLH"))
                        {
                            var min = minions.FirstOrDefault(x => x.Health < Q.GetDamage(x) && MinionHealthPrediction.GetHealthPrediction(x, 250) > 0);

                            if (min != null)
                            {
                                Q.Cast(min, true);
                            }                       
                        }
                        else
                        {
                            Q.Cast(minions.FirstOrDefault(), true);
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
                    var mobs = MinionManager.GetMinions(Me.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral);

                    if (mobs.Any())
                    {
                        Q.Cast(mobs.FirstOrDefault(), true);
                    }
                }
            }
        }

        private static void LastHit()
        {
            if (LastHitOption.HasEnouguMana)
            {
                if (LastHitOption.UseQ && Q.IsReady())
                {
                    var minions =
                        MinionManager.GetMinions(Me.Position, Q.Range)
                            .Where(
                                x =>
                                    x.DistanceToPlayer() <= Q.Range &&
                                    x.DistanceToPlayer() > Orbwalker.GetRealAutoAttackRange(Me) &&
                                    x.Health < Q.GetDamage(x)).ToArray();

                    if (minions.Any())
                    {
                        Q.Cast(minions.FirstOrDefault(), true);
                    }
                }
            }
        }

        private static void OneKeyCastR()
        {
            var target = TargetSelector.GetTarget(3000);

            if (target.IsValidTarget(3000f))
            {
                SpellManager.PredCast(R, target, true);
            }
        }

        private static void BeforeAttack(Orbwalker.BeforeAttackEventArgs Args)
        {
            if (!Args.Unit.IsMe || Me.IsDead || Args.Target == null || !Args.Target.IsValidTarget() || !isLaneClearMode)
                return;

            if (LaneClearOption.HasEnouguMana)
            {
                if (LaneClearOption.UseW && W.IsReady() && Me.CountEnemiesInRange(850) == 0)
                {
                    if (Args.Target.ObjectType == GameObjectType.obj_AI_Turret || Args.Target.ObjectType == GameObjectType.obj_Barracks || 
                        Args.Target.ObjectType == GameObjectType.obj_HQ || Args.Target.ObjectType == GameObjectType.obj_Turret ||
                        Args.Target.ObjectType == GameObjectType.obj_BarracksDampener)
                    {
                        if (W.IsReady() && Me.CountAlliesInRange(W.Range) >= 1)
                        {
                            W.Cast(ObjectManager.Heroes.Allies.Find(x => x.DistanceToPlayer() <= W.Range).Position, true);
                        }
                    }
                }
            }
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit ArgsTarget)
        {
            if (!unit.IsMe || Me.IsDead || ArgsTarget == null || ArgsTarget.IsDead || !ArgsTarget.IsValidTarget() || ArgsTarget.ObjectType != GameObjectType.AIHeroClient)
                return;

            if (isComboMode)
            {
                var target = (AIHeroClient)ArgsTarget;

                if (!target.IsDead && !target.IsZombie)
                {
                    if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                    {
                        SpellManager.PredCast(Q, target);
                    }
                    else if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range))
                    {
                        SpellManager.PredCast(W, target, true);
                    }
                }
            }
            else if (isHarassMode && HarassOption.HasEnouguMana)
            {
                var target = (AIHeroClient)ArgsTarget;

                if (!target.IsDead && !target.IsZombie && HarassOption.GetHarassTarget(target.ChampionName))
                {
                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                    {
                        SpellManager.PredCast(Q, target);
                    }

                    if (HarassOption.UseW && W.IsReady() && target.IsValidTarget(W.Range))
                    {
                        SpellManager.PredCast(W, target, true);
                    }
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser Args)
        {
            if (MiscOption.GetBool("Gapcloser") && E.IsReady())
            {
                if (Args.End.DistanceToPlayer() <= 200)
                {
                    E.Cast(Me.Position.Extend(Args.Sender.Position, -E.Range), true);
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (MiscOption.GetBool("AntiMelee") && E.IsReady() && Me.HealthPercent <= MiscOption.GetSlider("AntiMeleeHp"))
            {
                if (sender != null && sender.IsEnemy && Args.Target != null && Args.Target.IsMe)
                {
                    if (sender.ObjectType == Me.ObjectType && sender.IsMelee())
                    {
                        E.Cast(Me.Position.Extend(sender.Position, -E.Range), true);
                    }
                }
            }
        }
    }
}
