namespace Flowers_ADCSeries.MyPlugin
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyBase;
    using MyCommon;

    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class Ashe : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1255f);
            E = new Spell(SpellSlot.E, 5000f);
            R = new Spell(SpellSlot.R, 2000f);

            W.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotCone);
            E.SetSkillshot(0.25f, 300f, 1400f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.25f, 130f, 1600f, true, SkillshotType.SkillshotLine);

            ComboOption.AddQ();
            ComboOption.AddBool("ComboSaveMana", "Save Mana to Cast Q");
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddR();

            HarassOption.AddW();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddW();
            LaneClearOption.AddSlider("LaneClearWCount", "Use W| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove();

            KillStealOption.AddW();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            MiscOption.AddR();
            MiscOption.AddBool("AutoR", "Auto Cast Ult");
            //MiscOption.AddBool("Interrupt", "Interrupt Danger Spells");
            MiscOption.AddKey("SemiR", "Semi-manual R Key", SharpDX.DirectInput.Key.T);
            MiscOption.AddBool("AntiGapCloser", "Anti GapCloser");
            MiscOption.AddSlider("AntiGapCloserHp", "AntiGapCloser |When Player HealthPercent <= x%", 30);
            MiscOption.AddGapcloserTargetList();

            DrawOption.AddW();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Orbwalker.AfterAttack += AfterAttack;
            //Interrupt.
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (FleeOption.DisableMove && isFleeMode)
            {
                Orbwalker.Move = false;
                return;
            }

            Orbwalker.Move = true;

            if (MiscOption.GetKey("SemiR"))
            {
                OneKeyR();
            }

            AutoRLogic();
            KillSteal();

            if (isComboMode)
            {
                Combo();
            }

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

        private static void OneKeyR()
        {
            Orbwalker.MoveTo(Game.CursorPosition);

            if (R.IsReady())
            {
                var select = TargetSelector.GetSelectedTarget();
                var target = TargetSelector.GetTarget(R.Range);

                if (select != null && !target.HasBuffOfType(BuffType.SpellShield) && target.IsValidTarget(R.Range))
                {
                    SpellManager.PredCast(R, target);
                }
                else if (select == null && target != null && !target.HasBuffOfType(BuffType.SpellShield) && target.IsValidTarget(R.Range))
                {
                    SpellManager.PredCast(R, target);
                }
            }
        }

        private static void AutoRLogic()
        {
            if (MiscOption.GetBool("AutoR") && R.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(R.Range)))
                {
                    if (!(target.DistanceToPlayer() > Orbwalker.GetRealAutoAttackRange(Me)) ||
                        !(target.DistanceToPlayer() <= 700) ||
                        !(target.Health > Me.GetAutoAttackDamage(target)) ||
                        !(target.Health < R.GetDamage(target) + Me.GetAutoAttackDamage(target) * 3) ||
                        target.HasBuffOfType(BuffType.SpellShield))
                    {
                        continue;
                    }

                    SpellManager.PredCast(R, target);
                    return;
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseW && W.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(W.Range)))
                {
                    if (!target.IsValidTarget(W.Range) || !(target.Health < W.GetDamage(target)))
                        continue;

                    if (target.DistanceToPlayer() <= Orbwalker.GetRealAutoAttackRange(Me) && Me.HasBuff("AsheQAttack"))
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
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(2000) && KillStealOption.GetKillStealTarget(x.ChampionName)))
                {
                    if (!(target.DistanceToPlayer() > 800) || !(target.Health < R.GetDamage(target)) || target.HasBuffOfType(BuffType.SpellShield))
                        continue;

                    if (!target.IsUnKillable())
                    {
                        SpellManager.PredCast(R, target);
                        return;
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.UseR && R.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(1200)))
                {
                    if (target.IsValidTarget(600) && Me.CountEnemiesInRange(600) >= 3 && target.CountAlliesInRange(200) <= 2)
                    {
                        SpellManager.PredCast(R, target);
                    }

                    if (Me.CountEnemiesInRange(800) == 1 &&
                        target.DistanceToPlayer() > Orbwalker.GetRealAutoAttackRange(Me) &&
                        target.DistanceToPlayer() <= 700 &&
                        target.Health > Me.GetAutoAttackDamage(target) &&
                        target.Health < R.GetDamage(target) + Me.GetAutoAttackDamage(target) * 3 &&
                        !target.HasBuffOfType(BuffType.SpellShield))
                    {
                        SpellManager.PredCast(R, target);
                    }

                    if (target.DistanceToPlayer() <= 1000 &&
                        (!target.CanMove || target.HasBuffOfType(BuffType.Stun) ||
                        R.GetPrediction(target).Hitchance == HitChance.Immobile))
                    {
                        SpellManager.PredCast(R, target);
                    }
                }
            }

            if (ComboOption.UseW && W.IsReady() && !Me.HasBuff("AsheQAttack"))
            {
                if ((ComboOption.GetBool("ComboSaveMana") &&
                     Me.Mana > (R.IsReady() ? R.ManaCost : 0) + W.ManaCost + Q.ManaCost) ||
                    !ComboOption.GetBool("ComboSaveMana"))
                {
                    var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);

                    if (target.IsValidTarget(W.Range))
                    {
                        SpellManager.PredCast(W, target);
                    }
                }
            }

            if (ComboOption.UseE && E.IsReady())
            {
                var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(1000))
                {
                    var EPred = E.GetPrediction(target);

                    if ((NavMesh.GetCollisionFlags(EPred.CastPosition) == CollisionFlags.Grass ||
                         NavMesh.IsGrass(target.ServerPosition)) && !target.IsVisible)
                    {
                        E.Cast(EPred.CastPosition);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                if (HarassOption.UseW && W.IsReady() && !Me.HasBuff("AsheQAttack"))
                {
                    var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                    if (target.IsValidTarget(W.Range))
                    {
                        SpellManager.PredCast(W, target);
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
                if (LaneClearOption.UseW && W.IsReady())
                {
                    var minions = MinionManager.GetMinions(Me.Position, W.Range);

                    if (minions.Any())
                    {
                        var wFarm = MinionManager.GetBestCircularFarmLocation(minions.Select(x => x.Position.To2D()).ToList(), W.Width, W.Range);

                        if (wFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearWCount"))
                        {
                            W.Cast(wFarm.Position, true);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                if (JungleClearOption.UseW && !Me.HasBuff("AsheQAttack"))
                {
                    var mobs = MinionManager.GetMinions(Me.Position, W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                    if (mobs.Any())
                    {
                        var mob = mobs.FirstOrDefault();

                        if (mob != null)
                        {
                            W.Cast(mob.Position, true);
                        }
                    }
                }
            }
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit ArgsTarget)
        {
            if (!unit.IsMe || Me.IsDead || ArgsTarget == null || ArgsTarget.IsDead || !ArgsTarget.IsValidTarget())
                return;

            if (isComboMode && ArgsTarget.ObjectType == GameObjectType.AIHeroClient)
            {
                if (ComboOption.UseQ && Q.IsReady())
                {
                    var target = (AIHeroClient)ArgsTarget;

                    if (!target.IsDead && !target.IsZombie)
                    {
                        if (Me.HasBuff("asheqcastready"))
                        {
                            Q.Cast(true);
                            Orbwalker.ResetAutoAttackTimer();
                        }
                    }
                }
            }
            else if (isJungleClearMode && ArgsTarget.ObjectType == GameObjectType.obj_AI_Minion)
            {
                if (JungleClearOption.HasEnouguMana && JungleClearOption.UseQ && Q.IsReady())
                {
                    var mobs = MinionManager.GetMinions(Me.Position, Orbwalker.GetRealAutoAttackRange(Me), MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                    if (mobs.Any())
                    {
                        foreach (var mob in mobs)
                        {
                            if (!mob.IsValidTarget(Orbwalker.GetRealAutoAttackRange(Me)) || !(mob.Health > Me.GetAutoAttackDamage(mob) * 2))
                            {
                                continue;
                            }

                            if (Me.HasBuff("asheqcastready"))
                            {
                                Q.Cast(true);
                                Orbwalker.ResetAutoAttackTimer();
                            }
                        }
                    }
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser Args)
        {
            if (!Args.Sender.IsEnemy || !R.IsReady() || MiscOption.GetBool("AntiGapCloser") ||
                Me.HealthPercent > MiscOption.GetSlider("AntiGapCloserHp"))
            {
                return;
            }

            if (MiscOption.GetGapcloserTarget(Args.Sender.ChampionName) && Args.End.DistanceToPlayer() <= 300)
            {
                SpellManager.PredCast(R, Args.Sender);
            }
        }
    }
}
