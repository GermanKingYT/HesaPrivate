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

    internal class Kalista : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 1150f);
            W = new Spell(SpellSlot.W, 5000f);
            E = new Spell(SpellSlot.E, 950f);
            R = new Spell(SpellSlot.R, 1500f);

            Q.SetSkillshot(0.35f, 40f, 2400f, true, SkillshotType.SkillshotLine);

            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddBool("ComboEUse", "Use E| if minion can kill and target have buff stack");
            ComboOption.AddBool("ComboMana", "Use E| Save Mana To Cast E");
            ComboOption.AddBool("ComboAttack", "Auto Attack Minion To Dash");

            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddBool("HarassESlow", "Use E| if minion can kill and target have buff stack");
            HarassOption.AddBool("HarassELeave", "Use E| if target leave E Range");
            HarassOption.AddSlider("HarassECount", "Use E| if target buff stack >= x", 3, 1, 10);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min Kill Count>= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove(false);

            KillStealOption.AddQ();
            KillStealOption.AddE();

            MiscOption.AddE();
            MiscOption.AddBool("AutoELast", "Auto E| LastHit Minion");
            MiscOption.AddBool("AutoSteal", "Auto E| Steal Mobs");
            MiscOption.AddSlider("EToler", "E Deviation", -100, 100, 0);
            MiscOption.AddR();
            MiscOption.AddBool("AutoSave", "Auto R| Save Ally");
            MiscOption.AddSlider("AutoSaveHp", "Auto R| Ally HealthPercent <= x%", 20);
            MiscOption.AddBool("Balista", "Auto R| Balista");
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcus", "Force Attack Have W Passive Target");

            DrawOption.AddQ();
            DrawOption.AddE();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Orbwalker.OnNonKillableMinion += OnNonKillableMinion;
            Orbwalker.BeforeAttack += BeforeAttack;
            Orbwalker.AfterAttack += AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }


        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
                return;

            if (isFleeMode && FleeOption.DisableMove)
            {
                Orbwalker.Move = false;
                return;
            }

            Orbwalker.Move = true;

            AutoR();
            StealMob();
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

        private static void AutoR()
        {
            if (R.Level > 0 && Me.Mana > R.ManaCost && R.IsReady())
            {
                var ally = ObjectManager.Heroes.Allies.FirstOrDefault(
                    x => !x.IsMe && !x.IsDead && !x.IsZombie && x.HasBuff("kalistacoopstrikeally"));

                if (ally != null && ally.IsVisible && ally.DistanceToPlayer() <= R.Range)
                {
                    if (MiscOption.GetBool("AutoSave") && Me.CountEnemiesInRange(R.Range) > 0 &&
                        ally.CountEnemiesInRange(R.Range) > 0 &&
                        ally.HealthPercent <= MiscOption.GetSlider("AutoSaveHp"))
                        R.Cast(true);

                    if (MiscOption.GetBool("Balista") && ally.ChampionName == "Blitzcrank")
                        if (ObjectManager.Heroes.Enemies.Any(
                            x => !x.IsDead && !x.IsZombie && x.IsValidTarget() && x.HasBuff("rocketgrab2")))
                            R.Cast(true);
                }
            }
        }

        private static void StealMob()
        {
            if (MiscOption.GetBool("AutoSteal") && E.IsReady() && !isJungleClearMode)
            {
                var canSteal = MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All, MinionTeam.Neutral,
                        MinionOrderTypes.MaxHealth)
                    .Where(x => !x.Name.ToLower().Contains("mini"))
                    .Any(
                        x =>
                            x.HasBuff("kalistaexpungemarker") && x.DistanceToPlayer() <= E.Range &&
                            x.Health < GetRealEDamage(x));

                if (canSteal)
                    E.Cast(true);
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.IsReady())
                foreach (
                    var target in
                    ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x)))
                    if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.VeryHigh)
                            Q.Cast(qPred.CastPosition, true);
                    }

            if (KillStealOption.UseE && E.IsReady())
                foreach (
                    var target in
                    ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(E.Range) && x.Health < GetRealEDamage(x)))
                    if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                    {
                        E.Cast(true);
                        return;
                    }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(Q.Range))
            {
                if (ComboOption.GetBool("ComboAttack") && target.DistanceToPlayer() > Me.AttackRange + Me.BoundingRadius)
                {
                    var minion =
                        MinionManager.GetMinions(Me.Position, Me.AttackRange + Me.BoundingRadius, MinionTypes.All,
                                MinionTeam.NotAlly)
                            .Where(Orbwalker.InAutoAttackRange)
                            .OrderBy(x => x.DistanceToPlayer())
                            .FirstOrDefault();

                    if (minion != null && !minion.IsDead)
                        Orbwalker.Orbwalk(minion, Game.CursorPosition);
                }

                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range) &&
                    !Orbwalker.InAutoAttackRange(target))
                    if (ComboOption.GetBool("ComboMana"))
                    {
                        if (Me.Mana > Q.ManaCost + E.ManaCost)
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.Hitchance >= HitChance.VeryHigh)
                                Q.Cast(qPred.CastPosition, true);
                        }
                    }
                    else
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.Hitchance >= HitChance.VeryHigh)
                            Q.Cast(qPred.CastPosition, true);
                    }

                if (ComboOption.UseW && W.IsReady() && Utils.TickCount - LastCastTickW > 2000)
                    if (NavMesh.IsGrass(target.ServerPosition) && !target.IsVisible)
                        if (ComboOption.GetBool("ComboMana"))
                        {
                            if (Me.Mana > Q.ManaCost + E.ManaCost * 2 + W.ManaCost + R.ManaCost)
                                W.Cast(target.ServerPosition, true);
                        }
                        else
                        {
                            W.Cast(target.ServerPosition, true);
                        }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(E.Range) &&
                    target.HasBuff("kalistaexpungemarker") && Utils.TickCount - lastETime >= 500)
                {
                    if (target.Health < GetRealEDamage(target))
                        E.Cast(true);

                    if (ComboOption.GetBool("ComboEUse") &&
                        target.DistanceToPlayer() > Orbwalker.GetRealAutoAttackRange(Me) + 100)
                    {
                        var EKillMinion = MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All,
                                MinionTeam.NotAlly)
                            .FirstOrDefault(x => x.HasBuff("kalistaexpungemarker") &&
                                                 x.DistanceToPlayer() <= E.Range && x.Health < GetRealEDamage(x));

                        if (EKillMinion != null && EKillMinion.DistanceToPlayer() <= E.Range &&
                            target.IsValidTarget(E.Range))
                            E.Cast(true);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                if (target.IsValidTarget(Q.Range))
                {
                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                    {
                        SpellManager.PredCast(Q, target);
                    }

                    if (HarassOption.UseE && E.IsReady() && target.IsValidTarget(E.Range) &&
                        target.HasBuff("kalistaexpungemarker"))
                    {
                        var buffcount = target.GetBuffCount("kalistaexpungemarker");

                        if (HarassOption.GetBool("HarassELeave") && target.DistanceToPlayer() >= 800 &&
                            target.IsValidTarget(E.Range) &&
                            buffcount >= HarassOption.GetSlider("HarassECount"))
                            E.Cast(true);

                        if (HarassOption.GetBool("HarassESlow"))
                        {
                            var EKillMinion = MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All,
                                    MinionTeam.NotAlly)
                                .FirstOrDefault(x => x.HasBuff("kalistaexpungemarker") &&
                                                     x.DistanceToPlayer() <= E.Range && x.Health < GetRealEDamage(x));

                            if (EKillMinion != null && EKillMinion.DistanceToPlayer() <= E.Range &&
                                target.IsValidTarget(E.Range))
                                E.Cast(true);
                        }
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
                var minions = MinionManager.GetMinions(Me.Position, Q.Range);

                if (minions.Any())
                {
                    if (LaneClearOption.UseE && E.IsReady())
                    {
                        var eMinionsCount =
                            MinionManager
                                .GetMinions(Me.Position, E.Range)
                                .Count(x => x.HasBuff("kalistaexpungemarker") && x.Health < GetRealEDamage(x));

                        if (eMinionsCount >= LaneClearOption.GetSlider("LaneClearECount"))
                            E.Cast(true);
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Me.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseQ && Q.IsReady() && mob.IsValidTarget(Q.Range))
                        Q.Cast(mob, true);

                    if (JungleClearOption.UseE && E.IsReady())
                        if (mobs.Any(x => x.HasBuff("kalistaexpungemarker") && x.DistanceToPlayer() <= E.Range
                                          && x.Health < GetRealEDamage(x)))
                            E.Cast(true);
                }
            }
        }

        private static void OnNonKillableMinion(AttackableUnit sender)
        {
            if (Me.IsDead || isComboMode)
                return;

            if (MiscOption.GetBool("AutoELast") || !E.IsReady())
                return;

            var minion = (Obj_AI_Minion)sender;

            if (minion != null && minion.IsValidTarget(E.Range) && minion.Health < GetRealEDamage(minion) &&
                Me.CountEnemiesInRange(600) == 0 && Me.ManaPercent >= 60)
                E.Cast(true);
        }

        private static void BeforeAttack(BeforeAttackEventArgs Args)
        {
            if (MiscOption.GetBool("Forcus"))
                if (isComboMode || isHarassMode)
                {
                    foreach (var target in ObjectManager.Heroes.Enemies.Where(x => !x.IsDead && !x.IsZombie &&
                                                                                   Orbwalker.InAutoAttackRange(x) &&
                                                                                   x.HasBuff("kalistacoopstrikemarkally"))
                    )
                        if (!target.IsDead && target.IsValidTarget(Orbwalker.GetRealAutoAttackRange(target)))
                            myOrbwalker.ForceTarget(target);
                }
                else if (isLaneClearMode || isJungleClearMode)
                {
                    var minion = MinionManager
                        .GetMinions(Me.Position, Orbwalker.GetRealAutoAttackRange(Me),
                            MinionTypes.All, MinionTeam.NotAlly)
                        .FirstOrDefault(x => Orbwalker.InAutoAttackRange(x) && x.HasBuff("kalistacoopstrikemarkally"));

                    if (minion != null && minion.IsValidTarget(Orbwalker.GetRealAutoAttackRange(minion)))
                        myOrbwalker.ForceTarget(minion);
                }
        }

        private static void AfterAttack(AttackableUnit sender, AttackableUnit ArgsTarget)
        {
            if (!sender.IsMe || Me.IsDead)
                return;

            myOrbwalker.ForceTarget(null);

            if (isComboMode)
            {
                var target = ArgsTarget as AIHeroClient;

                if (target != null && !target.IsDead && !target.IsZombie)
                    if (ComboOption.UseQ && Q.IsReady())
                        if (ComboOption.GetBool("ComboMana"))
                        {
                            if (Me.Mana > Q.ManaCost + E.ManaCost)
                            {
                                SpellManager.PredCast(Q, target);
                            }
                        }
                        else
                        {
                            SpellManager.PredCast(Q, target);
                        }
            }
            else if (isJungleClearMode && JungleClearOption.HasEnouguMana && JungleClearOption.UseQ && Q.IsReady())
            {
                var mob = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth).FirstOrDefault();

                if (mob != null && !mob.IsDead)
                    Q.Cast(mob, true);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (!sender.IsMe)
                return;

            if (Args.SData.Name == "KalistaW")
                LastCastTickW = Utils.TickCount;

            if (Args.SData.Name == "KalistaExpunge" || Args.SData.Name == "KalistaExpungeWrapper" || Args.SData.Name == "KalistaDummySpell")
                lastETime = Utils.TickCount;
        }

        private static double GetRealEDamage(Obj_AI_Base target)
        {
            if (target != null && !target.IsDead && !target.IsZombie && target.HasBuff("kalistaexpungemarker"))
            {
                if (target.HasBuff("KindredRNoDeathBuff"))
                    return 0;

                if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3)
                    return 0;

                if (target.HasBuff("JudicatorIntervention"))
                    return 0;

                if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3)
                    return 0;

                if (target.HasBuff("FioraW"))
                    return 0;

                if (target.HasBuff("ShroudofDarkness"))
                    return 0;

                if (target.HasBuff("SivirShield"))
                    return 0;

                var damage = 0d;

                damage += E.IsReady() ? E.GetDamage(target) : 0d + MiscOption.GetSlider("EToler") - target.HPRegenRate;

                if (target.CharData.BaseSkinName == "Moredkaiser")
                    damage -= target.Mana;

                if (Me.HasBuff("SummonerExhaust"))
                    damage = damage * 0.6f;

                if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
                    damage -= target.Mana / 2f;

                if (target.HasBuff("GarenW"))
                    damage = damage * 0.7f;

                if (target.HasBuff("ferocioushowl"))
                    damage = damage * 0.7f;

                return damage;
            }

            return 0d;
        }
    }
}
