namespace Flowers_ADCSeries.MyPlugin
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Args;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyBase;
    using MyCommon;

    using SharpDX;

    using System;
    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class Vayne : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 300f);
            E = new Spell(SpellSlot.E, 650f);
            R = new Spell(SpellSlot.R);

            E.SetTargetted(0.25f, 1600f);

            ComboOption.AddQ();
            ComboOption.AddBool("ComboAQA", "Use Q Reset Auto Attack");
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRCount", "Use R| Min Enemies Count >= x", 2, 1, 5);
            ComboOption.AddSlider("ComboRHp", "Use R| And Player HealthPercent <= x%", 40, 0, 100);

            HarassOption.AddQ();
            HarassOption.AddBool("HarassQ2Passive", "Use Q| Only Target Have 2 Passive");
            HarassOption.AddE();
            HarassOption.AddBool("HarassE2Passive", "Use E| Only Target Have 2 Passive");
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove(false);

            KillStealOption.AddE();

            MiscOption.AddQ();
            MiscOption.AddBool("QCheck", "Use Q| Check Safe");
            MiscOption.AddBool("QTurret", "Use Q| Dont Dash To Enemy Turret");
            MiscOption.AddBool("QMelee", "Use Q| Anti Melee");
            MiscOption.AddE();
            //MiscOption.AddBool("InterruptE", "Use E| Interrupt Spell");
            //MiscOption.AddSlider("EPush", "Use E| Push Deviation", 25, 0, 150);
            MiscOption.AddSlider("EPush", "Use E| Push Deviation", 0, -100);
            MiscOption.AddBool("AntiAlistar", "Use E| Anti Alistar");
            MiscOption.AddBool("AntiRengar", "Use E| Anti Rengar");
            MiscOption.AddBool("AntiKhazix", "Use E| Anti Khazix");
            MiscOption.AddBool("AntiGapcloserE", "Use E| Anti Gapcloser");
            MiscOption.AddGapcloserTargetList();
            MiscOption.AddR();
            MiscOption.AddBool("AutoR", "Auto R");
            MiscOption.AddSlider("AutoRCount", "Auto R| Min Enemies Count >= x", 3, 1, 5);
            MiscOption.AddSlider("AutoRRange", "Auto R| Min Search Enemy Range", 600, 500, 1200);
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("ForcusAttack", "Forcus Attack 2 Passive Target");

            DrawOption.AddE();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Orbwalker.BeforeAttack += BeforeAttack;
            Orbwalker.AfterAttack += AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            GameObject.OnCreate += OnCreate;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            //Interrupter
            SpellBook.OnCastSpell += OnCastSpell;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            KillSteal();

            if (isFleeMode && FleeOption.DisableMove)
            {
                Orbwalker.Move = false;
                return;
            }

            Orbwalker.Move = true;

            if (isComboMode)
            {
                Combo();
                return;
            }

            if (isHarassMode)
            {
                Harass();
                return;
            }

            if (isFarmMode)
            {
                if (isLaneClearMode)
                {
                    LaneClear();
                    return;
                }

                if (isJungleClearMode)
                {
                    JungleClear();
                    return;
                }
                Farm();
                return;
            }

            if (R.Level > 0 && R.IsReady())
            {
                RLogic();
            }
        }

        private static void RLogic()
        {
            if (!R.IsReady() || Me.Mana < R.ManaCost || R.Level == 0)
            {
                return;
            }

            if (MiscOption.GetBool("AutoR") && R.IsReady() &&
                ObjectManager.Heroes.Enemies.Count(x => x.IsValidTarget(MiscOption.GetSlider("AutoRRange"))) >= MiscOption.GetSlider("AutoRCount"))
            {
                R.Cast();
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseE && E.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x =>x.IsValidTarget(E.Range) && x.Health < E.GetDamage(x)))
                {
                    if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                    {
                        E.CastOnUnit(target, true);
                        return;
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.UseR && R.IsReady() && ObjectManager.Heroes.Enemies.Count(x => x.IsValidTarget(650)) >= ComboOption.GetSlider("ComboRCount") && Me.HealthPercent <= ComboOption.GetSlider("ComboRHp"))
            {
                R.Cast();
            }

            if (ComboOption.UseE && E.IsReady())
            {
                ELogic();
            }
            
            if (ComboOption.UseQ && Q.IsReady() && !Me.IsWindingUp)
            {
                if (Me.HasBuff("vayneinquisition") && Me.CountEnemiesInRange(1200) > 0 && Me.CountEnemiesInRange(700) >= 2)
                {
                    var dashPos = GetDashQPos();

                    if (dashPos != Vector3.Zero)
                        if (Me.CanMoveMent())
                            Q.Cast(dashPos, true);
                }
                /*
                if (!ObjectManager.Heroes.Enemies.Exists(x => !x.IsDead && !x.IsZombie && x.IsValidTarget(Me.AttackRange)))
                {
                    var target = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);
                    if (target.IsValidTarget())
                    {
                        if (!Orbwalker.InAutoAttackRange(target) && target.Position.DistanceToMouse() < target.Position.DistanceToPlayer())
                        {
                            var dashPos = GetDashQPos();
                            if (dashPos != Vector3.Zero)
                                if (Me.CanMoveMent())
                                    Q.Cast(dashPos, true);
                        }

                        if (E.IsReady())
                        {
                            var dashPos = GetDashQPos();
                            if (dashPos != Vector3.Zero && CondemnCheck(dashPos, target))
                                if (Me.CanMoveMent())
                                    Q.Cast(dashPos, true);
                        }
                    }
                }*/
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                if (HarassOption.UseE && E.IsReady())
                {
                    var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical, true,
                        ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                    if (target.IsValidTarget(E.Range))
                    {
                        if (HarassOption.GetBool("HarassE2Passive"))
                        {
                            if (target.IsValidTarget(E.Range) && Has2WStacks(target))
                            {
                                E.CastOnUnit(target, true);
                            }
                        }
                        else
                        {
                            if (CondemnCheck(Me.ServerPosition, target))
                            {
                                E.CastOnUnit(target, true);
                            }
                        }
                    }
                }
            }
        }

        private static void Farm()
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
                    var minions =
                        MinionManager.GetMinions(Me.Position, Me.AttackRange + Me.BoundingRadius)
                            .Where(m => m.Health <= Me.GetAutoAttackDamage(m) + Q.GetDamage(m))
                            .ToArray();

                    if (minions.Any() && minions.Length > 1)
                    {
                        var minion = minions.OrderBy(m => m.Health).FirstOrDefault();
                        var afterQPosition = Me.ServerPosition.Extend(Game.CursorPosition, Q.Range);

                        if (minion != null && afterQPosition.Distance(minion.ServerPosition) <= Me.AttackRange + Me.BoundingRadius)
                        {
                            if (Q.Cast(Game.CursorPosition, true))
                            {
                                myOrbwalker.ForceTarget(minion);
                            }
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        var mob = mobs.FirstOrDefault(
                                    x =>
                                        !x.Name.ToLower().Contains("mini") && !x.Name.ToLower().Contains("baron") &&
                                        !x.Name.ToLower().Contains("dragon") && !x.Name.ToLower().Contains("crab") &&
                                        !x.Name.ToLower().Contains("herald"));

                        if (mob != null && mob.IsValidTarget(E.Range))
                        {
                            if (CondemnCheck(Me.ServerPosition, mob))
                            {
                                E.CastOnUnit(mob, true);
                            }
                        }
                    }
                }
            }
        }

        private static void BeforeAttack(BeforeAttackEventArgs Args)
        {
            if (!Args.Unit.IsMe)
                return;

            if (isComboMode || isHarassMode)
            {
                var ForcusTarget = ObjectManager.Heroes.Enemies.FirstOrDefault(x => x.IsValidTarget(Me.AttackRange + Me.BoundingRadius) && Has2WStacks(x));

                if (MiscOption.GetBool("ForcusAttack") && ForcusTarget != null && ForcusTarget.IsValidTarget(Me.AttackRange + Me.BoundingRadius - ForcusTarget.BoundingRadius + 15))
                {
                    myOrbwalker.ForceTarget(ForcusTarget);
                }
                else
                {
                    myOrbwalker.ForceTarget(null);
                }
            }
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit tar)
        {
            if (!unit.IsMe || Me.IsDead)
                return;

            myOrbwalker.ForceTarget(null);

            if (tar == null || tar.IsDead || !tar.IsVisible)
            {
                return;
            }

            if (isComboMode)
            {
                if (ComboOption.GetBool("ComboAQA"))
                {
                    var target = tar as AIHeroClient;

                    if (target != null && !target.IsDead && !target.IsZombie && Q.IsReady())
                    {
                        AfterQLogic(target);
                        return;
                    }
                }
            }

            if (isHarassMode || (isFarmMode && MyManaManager.SpellHarass))
            {
                if (HarassOption.HasEnouguMana && HarassOption.UseQ)
                {
                    var target = tar as AIHeroClient;

                    if (target != null && !target.IsDead && !target.IsZombie && Q.IsReady() && HarassOption.GetHarassTarget(target.ChampionName))
                    {
                        if (HarassOption.GetBool("HarassQ2Passive") && !Has2WStacks(target))
                        {
                            return;
                        }

                        AfterQLogic(target);
                        return;
                    }
                }
            }

            if (isLaneClearMode && LaneClearOption.HasEnouguMana && LaneClearOption.UseQ)
            {
                if (tar.ObjectType == GameObjectType.obj_AI_Turret || tar.ObjectType == GameObjectType.obj_Turret ||
                    tar.ObjectType == GameObjectType.obj_HQ || tar.ObjectType == GameObjectType.obj_Barracks ||
                    tar.ObjectType == GameObjectType.obj_BarracksDampener)
                {
                    if (Me.CountEnemiesInRange(850) == 0)
                        if (Me.CanMoveMent())
                        {
                            Q.Cast(Game.CursorPosition, true);
                            return;
                        }
                }
                else if (tar.ObjectType == GameObjectType.obj_AI_Minion && tar.Team != GameObjectTeam.Neutral)
                {
                    var minions =
                        MinionManager.GetMinions(Me.Position, Me.AttackRange + Me.BoundingRadius)
                            .Where(m => m.Health <= Me.GetAutoAttackDamage(m) + Q.GetDamage(m))
                            .ToArray();

                    if (minions.Any() && minions.Length >= 1)
                    {
                        var minion = minions.OrderBy(m => m.Health).FirstOrDefault();
                        var afterQPosition = Me.ServerPosition.Extend(Game.CursorPosition, Q.Range);

                        if (minion != null && afterQPosition.Distance(minion.ServerPosition) <= Me.AttackRange + Me.BoundingRadius)
                        {
                            if (Q.Cast(Game.CursorPosition, true))
                            {
                                myOrbwalker.ForceTarget(minion);
                                return;
                            }
                        }
                    }
                }
            }

            if (isJungleClearMode && tar.ObjectType == GameObjectType.obj_AI_Minion && tar.Team == GameObjectTeam.Neutral && JungleClearOption.HasEnouguMana && JungleClearOption.UseQ)
            {
                var mobs = MinionManager.GetMinions(Me.Position, 800, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                    if (Me.CanMoveMent())
                        Q.Cast(Game.CursorPosition, true);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (sender == null || !sender.IsEnemy || !sender.IsMelee() || sender.ObjectType != GameObjectType.AIHeroClient || Args.Target == null || !Args.Target.IsMe)
            {
                return;
            }

            if (MiscOption.GetBool("QMelee") && Q.IsReady())
            {
                if (sender.DistanceToPlayer() <= 300 && Me.HealthPercent <= 40)
                {
                    if (sender.Health < Me.GetAutoAttackDamage(sender) * 2)
                        return;

                    if (Me.CanMoveMent())
                        Q.Cast(Me.Position.Extend(sender.Position, -300), true);
                }
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            var Rengar = ObjectManager.Heroes.Enemies.Find(heros => heros.ChampionName.Equals("Rengar"));
            var Khazix = ObjectManager.Heroes.Enemies.Find(heros => heros.ChampionName.Equals("Khazix"));

            if (Rengar != null && MiscOption.GetBool("AntiRengar"))
            {
                if (sender.Name == "Rengar_LeapSound.troy" && sender.Position.DistanceToPlayer() < E.Range && Rengar.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(Rengar, true);
                }
            }

            if (Khazix != null && MiscOption.GetBool("AntiKhazix"))
            {
                if (sender.Name == "Khazix_Base_E_Tar.troy" && sender.Position.DistanceToPlayer() <= 300 && Khazix.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(Khazix, true);
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser Args)
        {
            if (Args.Sender == null || !Args.Sender.IsValid() || !Args.Sender.IsEnemy || !E.IsReady())
                return;

            if (MiscOption.GetBool("AntiAlistar") && Args.Sender.ChampionName == "Alistar" && Args.SkillType == GapcloserType.Targeted)
            {
                E.CastOnUnit(Args.Sender, true);
            }

            if (MiscOption.GetBool("AntiGapcloserE") && MiscOption.GetGapcloserTarget(Args.Sender.ChampionName))
            {
                if (Args.Sender.DistanceToPlayer() <= 200 && Args.Sender.IsValid())
                {
                    E.CastOnUnit(Args.Sender, true);
                }
            }
        }

        private static void OnCastSpell(SpellBook sender, SpellbookCastSpellEventArgs Args)
        {
            if (sender.Owner.IsMe && Args.Slot == SpellSlot.Q)
            {
                Orbwalker.ResetAutoAttackTimer();
            }
        }

        private static Vector3 GetDashQPos()
        {
            var firstQPos = Me.ServerPosition.Extend(Game.CursorPosition, Q.Range);
            var allPoint = MyGeometry.GetCirclrPos(Me.ServerPosition, Q.Range);

            foreach (var point in allPoint)
            {
                var mousecount = firstQPos.CountEnemiesInRange(300);
                var count = point.CountEnemiesInRange(300);

                if (!HaveEnemiesInRange(point))
                    continue;

                if (mousecount == count)
                {
                    if (point.DistanceToMouse() < firstQPos.DistanceToMouse())
                    {
                        firstQPos = point;
                    }
                }

                if (count < mousecount)
                {
                    firstQPos = point;
                }
            }

            for (var i = 1; i <= 5; i++)
            {
                if (NavMesh.IsWall(Me.ServerPosition.Extend(firstQPos, i * 20)))
                {
                    return Vector3.Zero;
                }
            }

            if (MiscOption.GetBool("QTurret") && firstQPos.UnderTurret(true))
            {
                return Vector3.Zero;
            }

            if (MiscOption.GetBool("QCheck"))
            {
                if (Me.CountEnemiesInRange(Q.Range + Me.BoundingRadius - 30) < firstQPos.CountEnemiesInRange(Q.Range * 2 - Me.BoundingRadius))
                {
                    return Vector3.Zero;
                }

                if (firstQPos.CountEnemiesInRange(Q.Range * 2 - Me.BoundingRadius) > 3)
                {
                    return Vector3.Zero;
                }
            }

            return !HaveEnemiesInRange(firstQPos) ? Vector3.Zero : firstQPos;
        }

        private static void AfterQLogic(Obj_AI_Base target)
        {
            if (!Q.IsReady())
            {
                return;
            }

            var qPosition = Me.ServerPosition.Extend(Game.CursorPosition, Q.Range);
            var targetDisQ = target.ServerPosition.Distance(qPosition);

            if (MiscOption.GetBool("QTurret") && qPosition.UnderTurret(true))
            {
                return;
            }

            if (MiscOption.GetBool("QCheck"))
            {
                if (ObjectManager.Heroes.Enemies.Count(x => x.IsValidTarget(300f, true, qPosition)) >= 3)
                {
                    return;
                }

                //Catilyn W
                if (ObjectManager
                        .Get<Obj_GeneralParticleEmitter>()
                        .FirstOrDefault(
                            x =>
                                x != null && x.IsValid() &&
                                x.Name.ToLower().Contains("yordletrap_idle_red.troy") &&
                                x.Position.Distance(qPosition) <= 100) != null)
                {
                    return;
                }

                //Jinx E
                if (ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(x => x.IsValid() && x.IsEnemy && x.Name == "k" &&
                                             x.Position.Distance(qPosition) <= 100) != null)
                {
                    return;
                }

                //Teemo R
                if (ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(x => x.IsValid() && x.IsEnemy && x.Name == "Noxious Trap" &&
                                             x.Position.Distance(qPosition) <= 100) != null)
                {
                    return;
                }
            }

            if (targetDisQ >= 300 && targetDisQ <= 600)
                if (Me.CanMoveMent())
                    Q.Cast(Game.CursorPosition, true);
        }

        private static void ELogic()
        {
            foreach (var target in ObjectManager.Heroes.Enemies.Where(h => h.IsValidTarget(E.Range) && !h.HasBuffOfType(BuffType.SpellShield) && !h.HasBuffOfType(BuffType.SpellImmunity)))
            {
                if (CondemnCheck(Me.ServerPosition, target))
                {
                    E.Cast(target, true);
                    return;
                }
            }
        }

        private static bool CondemnCheck(Vector3 serverPosition, Obj_AI_Base target)
        {
            var EPred = E.GetPrediction(target);
            var PD = serverPosition == Me.ServerPosition ? 425 : 410 + MiscOption.GetSlider("EPush");
            var PP = EPred.UnitPosition.Extend(serverPosition, -PD);

            for (var i = 1; i < PD; i += (int)target.BoundingRadius)
            {
                var VL = EPred.UnitPosition.Extend(serverPosition, -i);
                var J4 = ObjectManager.Get<Obj_AI_Base>().Any(f => f.Distance(PP) <= target.BoundingRadius && f.Name.ToLower() == "beacon");
                //var CF = NavMesh.GetCollisionFlags(VL);
                if (VL.IsWall() || J4)
                {
                    return true;
                }
            }
            return false;
            /*
            var pushDistance = 350 + MiscOption.GetSlider("EPush");
            var targetPosition = E.GetPrediction(target).UnitPosition;
            var finalPosition = targetPosition.Extend(serverPosition, -pushDistance);
            var numberOfChecks = (float)Math.Ceiling(pushDistance / 30f);
            for (var i = 1; i <= 30; i++)
            {
                var v3 = (targetPosition - serverPosition).Normalized();
                var extendedPosition = targetPosition + v3 * (numberOfChecks * i);
                var j4Flag = ObjectManager.Get<Obj_AI_Base>().Any(m => m.Distance(extendedPosition) <= target.BoundingRadius && m.Name == "Beacon");
                if ((extendedPosition.IsWall() || j4Flag) && (target.Path.Count() < 2) && !target.IsDashing())
                {
                    return true;
                }
            }
            return false;*/
        }

        private static bool HaveEnemiesInRange(Vector3 position)
        {
            if (myOrbwalker.GetTarget() != null && !myOrbwalker.GetTarget().IsDead && myOrbwalker.GetTarget().ObjectType == GameObjectType.AIHeroClient)
            {
                return position.Distance(myOrbwalker.GetTarget().Position) <= Me.AttackRange + Me.BoundingRadius;
            }

            return position.CountEnemiesInRange(Me.AttackRange + Me.BoundingRadius) > 0;
        }

        private static bool Has2WStacks(AIHeroClient target)
        {
            return target.Buffs.Any(x => x.Name.ToLower() == "vaynesilvereddebuff" && x.Count == 2);
        }
    }
}
