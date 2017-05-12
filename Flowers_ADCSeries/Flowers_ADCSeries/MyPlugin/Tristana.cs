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

    internal class Tristana : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, 700f);

            W.SetSkillshot(0.50f, 250f, 1400f, false, SkillshotType.SkillshotCircle);

            ComboOption.AddQ();
            ComboOption.AddBool("ComboQAlways", "Use Q| Always Cast it(Off = Logic Cast)");
            ComboOption.AddE();
            ComboOption.AddBool("ComboEOnlyAfterAA", "Use E| Only After Attack Cast it");
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRHp", "Use R| Player HealthPercent <= x%(Save mySelf)", 25, 0, 100);

            HarassOption.AddE(false);
            HarassOption.AddBool("HarassEToMinion", "Use E| Cast Low Hp Minion");
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddE();
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddW();
            FleeOption.AddMove();

            KillStealOption.AddE();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            MiscOption.AddE();
            MiscOption.AddKey("SemiE", "Semi-manual E Key", SharpDX.DirectInput.Key.T);
            MiscOption.AddR();
            //MiscOption.AddBool("InterruptR", "Use R| Interrupt Spell");
            MiscOption.AddBool("AntiRengar", "Use R| Anti Rengar");
            MiscOption.AddBool("AntiKhazix", "Use R| Anti Khazix");
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcustarget", "Forcus Attack Passive Target");

            DrawOption.AddW();
            DrawOption.AddE();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            GameObject.OnCreate += OnCreate;
            //Interrupter
            Orbwalker.BeforeAttack += BeforeAttack;
            Orbwalker.AfterAttack += AfterAttack;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (E.Level > 0)
            {
                E.Range = 630 + 7 * (Me.Level - 1);
            }

            if (R.Level > 0)
            {
                R.Range = 630 + 7 * (Me.Level - 1);
            }

            if (isFleeMode)
            {
                Flee();

                if (FleeOption.DisableMove)
                    Orbwalker.Move = false;

                return;
            }

            Orbwalker.Move = true;

            if (MiscOption.GetKey("SemiE") && E.IsReady())
            {
                OneKeyCastE();
            }

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

        private static void KillSteal()
        {
            if (KillStealOption.UseE && E.IsReady())
            {

                foreach (
                    var target in
                    ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(E.Range) && x.Health <
                                                   Me.GetSpellDamage(x, SpellSlot.E) *
                                                   (x.GetBuffCount("TristanaECharge") * 0.30) +
                                                   Me.GetSpellDamage(x, SpellSlot.E)))
                {
                    if (target.IsValidTarget(E.Range))
                    {
                        E.CastOnUnit(target, true);
                    }
                }
            }

            if (KillStealOption.UseR && R.IsReady())
            {
                if (KillStealOption.UseE && E.IsReady())
                {
                    foreach (
                        var target in
                        from x in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(E.Range) && KillStealOption.GetKillStealTarget(x.ChampionName))
                        let etargetstacks = x.Buffs.Find(buff => buff.Name == "TristanaECharge")
                        where
                        R.GetDamage(x) + E.GetDamage(x) + etargetstacks?.Count * 0.30 * E.GetDamage(x) >=
                        x.Health
                        select x)
                    {
                        if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                        {
                            R.CastOnUnit(target);
                            return;
                        }
                    }
                }
                else
                {
                    var target = ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(R.Range) && KillStealOption.GetKillStealTarget(x.ChampionName))
                        .OrderByDescending(x => x.Health).FirstOrDefault(x => x.Health < R.GetDamage(x));

                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                    {
                        R.CastOnUnit(target);
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(E.Range))
            {
                if (ComboOption.UseQ && Q.IsReady())
                {
                    if (ComboOption.GetBool("ComboQOnlyPassive"))
                    {
                        if (!E.IsReady() && target.HasBuff("TristanaECharge"))
                        {
                            Q.Cast();
                        }
                        else if (!E.IsReady() && !target.HasBuff("TristanaECharge") && E.Cooldown > 4)
                        {
                            Q.Cast();
                        }
                    }
                    else
                    {
                        Q.Cast();
                    }
                }

                if (ComboOption.UseE && E.IsReady() && !ComboOption.GetBool("ComboEOnlyAfterAA") && target.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(target, true);
                }

                if (ComboOption.UseR && R.IsReady() && Me.HealthPercent <= ComboOption.GetSlider("ComboRHp"))
                {
                    var dangerenemy = ObjectManager.Heroes.Enemies.Where(e => e.IsValidTarget(R.Range)).
                        OrderBy(enemy => enemy.Distance(Me)).FirstOrDefault();

                    if (dangerenemy != null)
                    {
                        R.CastOnUnit(dangerenemy, true);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                if (E.IsReady())
                {
                    if (HarassOption.UseE)
                    {
                        var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                        if (target.IsValidTarget(E.Range))
                            E.CastOnUnit(target, true);
                    }

                    if (HarassOption.GetBool("HarassEToMinion"))
                    {
                        foreach (var minion in MinionManager.GetMinions(E.Range).Where(m =>
                        m.Health < Me.GetAutoAttackDamage(m) && m.CountEnemiesInRange(m.BoundingRadius + 150) >= 1))
                        {
                            var etarget = E.GetTarget();

                            if (etarget != null)
                            {
                                return;
                            }

                            E.CastOnUnit(minion, true);
                            myOrbwalker.ForceTarget(minion);
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

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                var mobs =
                    MinionManager.GetMinions(Me.Position, E.Range, MinionTypes.All, MinionTeam.Neutral,
                        MinionOrderTypes.MaxHealth).Where(x => !x.Name.ToLower().Contains("mini")).ToArray();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        E.CastOnUnit(mob, true);
                    }

                    if (JungleClearOption.UseQ && Q.IsReady() && !E.IsReady())
                    {
                        Q.Cast();
                    }
                }
            }
        }

        private static void Flee()
        {
            if (FleeOption.UseW && W.IsReady())
            {
                W.Cast(Game.CursorPosition);
            }
        }

        private static void OneKeyCastE()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(E.Range))
            {
                if (target.Health <
                    Me.GetSpellDamage(target, SpellSlot.E) * (target.GetBuffCount("TristanaECharge") * 0.30) +
                    Me.GetSpellDamage(target, SpellSlot.E))
                {
                    E.CastOnUnit(target, true);
                }

                if (Me.CountEnemiesInRange(1200) == 1)
                {
                    if (Me.HealthPercent >= target.HealthPercent && Me.Level + 1 >= target.Level)
                    {
                        E.CastOnUnit(target);
                    }
                    else if (Me.HealthPercent + 20 >= target.HealthPercent &&
                        Me.HealthPercent >= 40 && Me.Level + 2 >= target.Level)
                    {
                        E.CastOnUnit(target);
                    }
                }
            }
        }

        private static void OnCreate(GameObject sender, EventArgs Args)
        {
            if (R.IsReady())
            {
                var Rengar = ObjectManager.Heroes.Enemies.Find(heros => heros.ChampionName.Equals("Rengar"));
                var Khazix = ObjectManager.Heroes.Enemies.Find(heros => heros.ChampionName.Equals("Khazix"));

                if (MiscOption.GetBool("AntiRengar") && Rengar != null)
                {
                    if (sender.Name == "Rengar_LeapSound.troy" && sender.Position.Distance(Me.Position) < R.Range)
                    {
                        if (Rengar.IsValidTarget(R.Range))
                            R.CastOnUnit(Rengar, true);
                    }
                }

                if (MiscOption.GetBool("AntiKhazix") && Khazix != null)
                {
                    if (sender.Name == "Khazix_Base_E_Tar.troy" && sender.Position.Distance(Me.Position) <= 300)
                    {
                        if (Khazix.IsValidTarget(R.Range))
                            R.CastOnUnit(Khazix, true);
                    }
                }
            }
        }

        private static void BeforeAttack(Orbwalker.BeforeAttackEventArgs Args)
        {
            if (!Args.Unit.IsMe)
                return;

            if (isComboMode)
            {
                if (MiscOption.GetBool("Forcustarget"))
                {
                    foreach (
                        var enemy in
                        ObjectManager.Heroes.Enemies.Where(
                            enemy => Orbwalker.InAutoAttackRange(enemy) && enemy.HasBuff("TristanaEChargeSound")))
                    {
                        myOrbwalker.ForceTarget(enemy);
                    }
                }

                if (ComboOption.UseQ && Q.IsReady())
                {
                    if (ComboOption.GetBool("ComboQAlways"))
                    {
                        var Target = Args.Target.ObjectType == GameObjectType.AIHeroClient
                            ? (AIHeroClient)Args.Target
                            : null;

                        if (Target != null &&
                            (Target.HasBuff("TristanaEChargeSound") || Target.HasBuff("TristanaECharge")))
                        {
                            Q.Cast();
                        }
                    }
                    else
                    {
                        Q.Cast();
                    }
                }
            }
            else if (isHarassMode)
            {
                if (MiscOption.GetBool("Forcustarget"))
                {
                    foreach (
                        var enemy in
                        ObjectManager.Heroes.Enemies.Where(
                            enemy => Orbwalker.InAutoAttackRange(enemy) && enemy.HasBuff("TristanaEChargeSound")))
                    {
                        myOrbwalker.ForceTarget(enemy);
                    }
                }
            }
            else if (isJungleClearMode && JungleClearOption.HasEnouguMana)
            {
                if (JungleClearOption.UseQ && Q.IsReady())
                {
                    var minion =
                        MinionManager.GetMinions(Orbwalker.GetRealAutoAttackRange(ObjectManager.Player),
                                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                    if (minion.Any(x => x.NetworkId == Args.Target.NetworkId))
                    {
                        Q.Cast();
                    }
                }
            }
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
            {
                return;
            }

            myOrbwalker.ForceTarget(null);

            if (isComboMode)
            {
                if (ComboOption.UseE && E.IsReady() && ComboOption.GetBool("ComboEOnlyAfterAA"))
                {
                    var t = target as AIHeroClient;

                    if (t != null && t.IsValidTarget(E.Range))
                    {
                        E.CastOnUnit(t, true);
                    }
                }
            }
            else if (isLaneClearMode && LaneClearOption.HasEnouguMana)
            {
                if (target != null)
                {
                    if (target.ObjectType == GameObjectType.obj_AI_Turret ||
                        target.ObjectType == GameObjectType.obj_Barracks ||
                        target.ObjectType == GameObjectType.obj_HQ ||
                        target.ObjectType == GameObjectType.obj_Turret ||
                        target.ObjectType == GameObjectType.obj_BarracksDampener)
                    {
                        if (LaneClearOption.UseE && E.IsReady())
                        {
                            E.CastOnUnit(target as Obj_AI_Base, true);

                            if (!Me.IsWindingUp && Me.CountEnemiesInRange(1000) == 0 && LaneClearOption.UseQ)
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
            }
        }
    }
}
