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

    internal class Kindred : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 340f);
            W = new Spell(SpellSlot.W, 800f);
            E = new Spell(SpellSlot.E, 550f);
            R = new Spell(SpellSlot.R, 550f);

            ComboOption.AddQ();
            ComboOption.AddBool("ComboAQA", "Use Q| Reset Auto Attack");
            ComboOption.AddW();
            ComboOption.AddE();

            HarassOption.AddQ();
            HarassOption.AddW();
            HarassOption.AddE();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q| Min Hit Counts >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddQ();
            FleeOption.AddMove();

            KillStealOption.AddQ();

            MiscOption.AddQ();
            MiscOption.AddBool("QCheck", "Use Q| Safe Check?");
            MiscOption.AddBool("QTurret", "Use Q| Dont Cast To Turret");
            MiscOption.AddBool("QMelee", "Use Q| Anti Melee");
            MiscOption.AddR();
            MiscOption.AddBool("AutoR", "Auto R| Save Myself?");
            MiscOption.AddSlider("AutoRHp", "Auto R| Player Health Percent <= x%", 15);
            MiscOption.AddBool("AutoSave", "Auto R| Save Ally?");
            MiscOption.AddSlider("AutoSaveHp", "Auto R| Ally Health Percent <= x%", 20);
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcus", "Forcus Attack Passive Target");
            MiscOption.AddBool("ForcusE", "Forcus Attack E Mark Target");

            DrawOption.AddW();
            DrawOption.AddE();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Orbwalker.BeforeAttack += BeforeAttack;
            Orbwalker.AfterAttack += AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }


        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (isFleeMode)
            {
                Flee();

                if (FleeOption.DisableMove)
                    Orbwalker.Move = false;

                return;
            }

            Orbwalker.Move = true;

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
                        QLogic(target);
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(E.Range) && !target.IsDead && !target.IsZombie)
            {
                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                {
                    E.Cast(target);
                }

                if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range))
                {
                    W.Cast();
                }

                if (ComboOption.UseQ && Q.IsReady())
                {
                    QLogic(target);
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                if (target.IsValidTarget(E.Range) && !target.IsDead && !target.IsZombie)
                {
                    if (HarassOption.UseE && E.IsReady() && target.IsValidTarget(E.Range))
                    {
                        E.Cast(target);
                    }

                    if (HarassOption.UseW && W.IsReady() && target.IsValidTarget(W.Range))
                    {
                        W.Cast();
                    }

                    if (HarassOption.UseQ && Q.IsReady())
                    {
                        QLogic(target);
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
                        if (minions.Count >= LaneClearOption.GetSlider("LaneClearQCount"))
                        {
                            Q.Cast(Game.CursorPosition);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        E.Cast(mob, true);
                    }

                    if (JungleClearOption.UseW && W.IsReady())
                    {
                        W.Cast(mob, true);
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
            if (FleeOption.UseQ && Q.IsReady())
            {
                Q.Cast(Me.ServerPosition.Extend(Game.CursorPosition, Q.Range), true);
            }
        }

        private static void BeforeAttack(Orbwalker.BeforeAttackEventArgs Args)
        {
            if (!Args.Target.IsMe)
                return;

            if (isComboMode || isHarassMode)
            {
                var ForcusETarget =
                    ObjectManager.Heroes.Enemies.FirstOrDefault(
                        x =>
                            x.IsValidTarget(Orbwalker.GetRealAutoAttackRange(Me)) &&
                            x.HasBuff("kindredecharge"));

                var ForcusTarget =
                    ObjectManager.Heroes.Enemies.FirstOrDefault(
                        x =>
                            x.IsValidTarget(Orbwalker.GetRealAutoAttackRange(Me)) &&
                            x.HasBuff("kindredhittracker"));

                if (ForcusETarget.IsValidTarget(Orbwalker.GetRealAutoAttackRange(Me)) &&
                    MiscOption.GetBool("ForcusE"))
                {
                    myOrbwalker.ForceTarget(ForcusETarget);
                }
                else if (MiscOption.GetBool("Forcus") &&
                         ForcusTarget.IsValidTarget(Orbwalker.GetRealAutoAttackRange(Me)))
                {
                    myOrbwalker.ForceTarget(ForcusTarget);
                }
                else
                {
                    myOrbwalker.ForceTarget(null);
                }
            }
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit ArgsTarget)
        {
            if (!unit.IsMe)
            {
                return;
            }

            myOrbwalker.ForceTarget(null);

            if (isComboMode)
            {
                if (ComboOption.GetBool("ComboAQA"))
                {
                    var target = ArgsTarget as AIHeroClient;

                    if (target != null && !target.IsDead && !target.IsZombie && Q.IsReady())
                    {
                        QLogic(target);
                    }
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (sender == null || !sender.IsEnemy || Args.Target == null || Me.IsDead || Me.InFountain())
            {
                return;
            }

            switch (sender.ObjectType)
            {
                case GameObjectType.AIHeroClient:
                    if (Args.Target.IsMe)
                    {
                        if (sender.IsMelee() && Q.IsReady() && MiscOption.GetBool("QMelee"))
                        {
                            Q.Cast(Me.Position.Extend(sender.Position, -Q.Range));
                        }

                        if (R.IsReady() && MiscOption.GetBool("AutoR"))
                        {
                            if (Me.HealthPercent <= MiscOption.GetSlider("AutoRHp"))
                            {
                                R.Cast();
                            }

                            if (Orbwalker.IsAutoAttack(Args.SData.Name))
                            {
                                if (sender.GetAutoAttackDamage(Me, true) >= Me.Health)
                                {
                                    R.Cast();
                                }
                            }
                            else
                            {
                                var target = (AIHeroClient)Args.Target;

                                if (target.GetSpellSlot(Args.SData.Name) != SpellSlot.Unknown)
                                {
                                    if (target.GetSpellDamage(Me, Args.SData.Name) > Me.Health)
                                    {
                                        if (Args.End.DistanceToPlayer() < 150 + Me.BoundingRadius)
                                        {
                                            R.Cast();
                                        }

                                        if (target.DistanceToPlayer() < 150 + Me.BoundingRadius)
                                        {
                                            R.Cast();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (Args.Target.IsAlly && Args.Target.ObjectType == GameObjectType.AIHeroClient && !Args.Target.IsDead)
                    {
                        var ally = (AIHeroClient)Args.Target;

                        if (R.IsReady() && MiscOption.GetBool("AutoSave") && ally.DistanceToPlayer() <= R.Range)
                        {
                            if (ally.HealthPercent <= MiscOption.GetSlider("AutoSaveHp"))
                            {
                                R.Cast();
                            }

                            if (Orbwalker.IsAutoAttack(Args.SData.Name))
                            {
                                if (sender.GetAutoAttackDamage(ally, true) >= ally.Health)
                                {
                                    R.Cast();
                                }
                            }
                            else
                            {
                                var target = (AIHeroClient)Args.Target;

                                if (target.GetSpellSlot(Args.SData.Name) != SpellSlot.Unknown)
                                {
                                    if (target.GetSpellDamage(Me, Args.SData.Name) > Me.Health)
                                    {
                                        if (Args.End.DistanceToPlayer() < 150 + ally.BoundingRadius)
                                        {
                                            R.Cast();
                                        }

                                        if (target.DistanceToPlayer() < 150 + ally.BoundingRadius)
                                        {
                                            R.Cast();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.obj_AI_Turret:
                    if (Args.Target.IsMe)
                    {
                        if (sender.IsMelee() && Q.IsReady() && MiscOption.GetBool("QMelee"))
                        {
                            Q.Cast(Me.Position.Extend(sender.Position, -Q.Range));
                        }

                        if (R.IsReady() && MiscOption.GetBool("AutoR"))
                        {
                            if (sender.TotalAttackDamage > Me.Health)
                            {
                                R.Cast();
                            }
                        }
                    }
                    else if (Args.Target.IsAlly && Args.Target.ObjectType == GameObjectType.AIHeroClient && !Args.Target.IsDead)
                    {
                        var ally = (AIHeroClient)Args.Target;

                        if (R.IsReady() && MiscOption.GetBool("AutoSave") && ally.DistanceToPlayer() <= R.Range)
                        {
                            if (sender.TotalAttackDamage > ally.Health)
                            {
                                R.Cast();
                            }
                        }
                    }
                    break;
            }
        }

        private static void QLogic(Obj_AI_Base target)
        {
            if (!Q.IsReady())
            {
                return;
            }

            var qPosition = Me.ServerPosition.Extend(Game.CursorPosition, Q.Range);
            var targetDisQ = target.ServerPosition.Distance(qPosition);
            var canQ = false;

            if (MiscOption.GetBool("QTurret") && qPosition.UnderTurret(true))
            {
                canQ = false;
            }

            if (MiscOption.GetBool("QCheck"))
            {
                if (qPosition.CountEnemiesInRange(300f) >= 3)
                {
                    canQ = false;
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
                    canQ = false;
                }

                //Jinx E
                if (ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(x => x.IsValid() && x.IsEnemy && x.Name == "k" &&
                                             x.Position.Distance(qPosition) <= 100) != null)
                {
                    canQ = false;
                }

                //Teemo R
                if (ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(x => x.IsValid() && x.IsEnemy && x.Name == "Noxious Trap" &&
                                             x.Position.Distance(qPosition) <= 100) != null)
                {
                    canQ = false;
                }
            }

            if (targetDisQ >= Q.Range && targetDisQ <= Q.Range * 2)
            {
                canQ = true;
            }

            if (canQ)
            {
                Q.Cast(Game.CursorPosition, true);
                canQ = false;
            }
        }
    }
}
