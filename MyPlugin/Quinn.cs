namespace Flowers_ADCSeries.MyPlugin
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Args;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyBase;
    using MyCommon;

    using System;
    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class Quinn : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 1000f);
            W = new Spell(SpellSlot.W, 2000f);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, 550f);

            Q.SetSkillshot(0.25f, 90f, 1550f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 2000f, 1400f, false, SkillshotType.SkillshotCircle);
            E.SetTargetted(0.25f, 2000f);

            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();

            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q| Min Hit Counts >= ", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove(false);

            KillStealOption.AddQ();

            MiscOption.AddE();
            MiscOption.AddBool("Interrupt", "Use E| Interrupt Danger Spells");
            MiscOption.AddBool("Gapcloser", "Use E| Anti Gapcloser");
            MiscOption.AddBool("AntiAlistar", "Use E| Anti Alistar");
            MiscOption.AddBool("AntiRengar", "Use E| Anti Rengar");
            MiscOption.AddBool("AntiKhazix", "Use E| Anti Khazix");
            MiscOption.AddR();
            MiscOption.AddBool("AutoR", "Auto R");
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcus", "Forcus Attack Passive Target");

            DrawOption.AddQ();
            DrawOption.AddE();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            GameObject.OnCreate += OnCreate;
            Orbwalker.BeforeAttack += BeforeAttack;
            Orbwalker.AfterAttack += AfterAttack;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            //Interrupter
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

            KillSteal();

            if (isNoneMode)
                AutoR();

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
                    if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                    {
                        SpellManager.PredCast(Q, target, true);
                        return;
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(Q.Range))
            {
                if (ComboOption.UseE && E.IsReady() && Me.HasBuff("QuinnR"))
                {
                    E.CastOnUnit(target);
                }

                if (ComboOption.UseQ && Q.IsReady() && !Me.HasBuff("QuinnR"))
                {
                    if (target.DistanceToPlayer() <= Orbwalker.GetRealAutoAttackRange(Me) && HavePassive(target))
                    {
                        return;
                    }

                    SpellManager.PredCast(Q, target, true);
                }

                if (ComboOption.UseW && W.IsReady())
                {
                    var WPred = W.GetPrediction(target);

                    if ((NavMesh.GetCollisionFlags(WPred.CastPosition) == CollisionFlags.Grass ||
                         NavMesh.IsGrass(target.ServerPosition)) && !target.IsVisible)
                    {
                        W.Cast();
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                if (HarassOption.UseQ)
                {
                    var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                    if (target.IsValidTarget(Q.Range))
                    {
                        SpellManager.PredCast(Q, target, true);
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
                        var QFarm =
                            MinionManager.GetBestCircularFarmLocation(minions.Select(x => x.Position.To2D()).ToList(),
                                Q.Width, Q.Range);

                        if (QFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearQCount"))
                        {
                            Q.Cast(QFarm.Position);
                        }
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
                    if (JungleClearOption.UseQ && Q.IsReady())
                    {
                        var QFarm =
                            MinionManager.GetBestCircularFarmLocation(mobs.Select(x => x.Position.To2D()).ToList(),
                                Q.Width, Q.Range);

                        if (QFarm.MinionsHit >= 1)
                        {
                            Q.Cast(QFarm.Position);
                        }

                    }

                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        var mob =
                            mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini") && x.Health >= E.GetDamage(x));

                        if (mob != null)
                        {
                            E.CastOnUnit(mob);
                        }
                    }
                }
            }
        }

        private static void AutoR()
        {
            if (MiscOption.GetBool("AutoR") && R.IsReady() && R.Instance.SpellData.Name == "QuinnR")
            {
                if (!Me.IsDead && Me.InFountain())
                {
                    R.Cast();
                }
            }
        }

        private static void OnCreate(GameObject sender, EventArgs Args)
        {
            var Rengar = ObjectManager.Heroes.Enemies.Find(heros => heros.ChampionName.Equals("Rengar"));
            var Khazix = ObjectManager.Heroes.Enemies.Find(heros => heros.ChampionName.Equals("Khazix"));

            if (Rengar != null && MiscOption.GetBool("AntiRengar"))
            {
                if (sender.Name == "Rengar_LeapSound.troy" && sender.Position.Distance(Me.Position) < E.Range)
                {
                    if (Rengar.IsValidTarget(E.Range))
                        E.CastOnUnit(Rengar);
                }
            }

            if (Khazix != null && MiscOption.GetBool("AntiKhazix"))
            {
                if (sender.Name == "Khazix_Base_E_Tar.troy" && sender.Position.Distance(Me.Position) <= 300)
                {
                    if (Khazix.IsValidTarget(E.Range))
                        E.CastOnUnit(Khazix);
                }
            }
        }

        private static void BeforeAttack(BeforeAttackEventArgs Args)
        {
            if (!Args.Unit.IsMe)
                return;

            if (MiscOption.GetBool("Forcus"))
            {
                if (isComboMode || isHarassMode)
                {
                    foreach (var enemy in ObjectManager.Heroes.Enemies.Where(x => !x.IsDead && !x.IsZombie && HavePassive(x)))
                    {
                        myOrbwalker.ForceTarget(enemy);
                    }
                }

                if (isLaneClearMode || isJungleClearMode)
                {
                    var all = MinionManager.GetMinions(Me.Position, Orbwalker.GetRealAutoAttackRange(Me),
                        MinionTypes.All, MinionTeam.NotAlly).Where(HavePassive).ToArray();

                    if (all.Any())
                    {
                        myOrbwalker.ForceTarget(all.FirstOrDefault());
                    }
                }
            }
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit ArgsTarget)
        {
            if (!unit.IsMe)
                return;

            myOrbwalker.ForceTarget(null);

            if (isComboMode)
            {
                var target = ArgsTarget as AIHeroClient;

                if (target != null && target.IsValidTarget())
                {
                    if (ComboOption.UseE && E.IsReady())
                    {
                        E.CastOnUnit(target, true);
                    }
                    else if (ComboOption.UseQ && Q.IsReady() && !Me.HasBuff("QuinnR"))
                    {
                        SpellManager.PredCast(Q, target, true);
                    }
                }
            }
            else if (isJungleClearMode && JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseE && E.IsReady() && mob.IsValidTarget(E.Range))
                    {
                        E.CastOnUnit(mob, true);
                    }
                    else if (JungleClearOption.UseQ && Q.IsReady() && mob.IsValidTarget(Q.Range) && !Me.HasBuff("QuinnR"))
                    {
                        Q.Cast(mob, true);
                    }
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser Args)
        {
            if (E.IsReady())
            {
                if (MiscOption.GetBool("AntiAlistar") && Args.Sender.ChampionName == "Alistar" && Args.SkillType == GapcloserType.Targeted)
                {
                    E.CastOnUnit(Args.Sender, true);
                }

                if (MiscOption.GetBool("Gapcloser"))
                {
                    if (Args.End.DistanceToPlayer() <= 250 && Args.Sender.IsValid())
                    {
                        E.CastOnUnit(Args.Sender, true);
                    }
                }
            }
        }

        private static bool HavePassive(Obj_AI_Base target)
        {
            return target.HasBuff("quinnw");
        }
    }
}
