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

    internal class Graves : MyLogic
    {
        private static Menu BurstMenu;

        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 800f) { MinHitChance = HitChance.VeryHigh };
            W = new Spell(SpellSlot.W, 900f) { MinHitChance = HitChance.VeryHigh };
            E = new Spell(SpellSlot.E, 425f);
            R = new Spell(SpellSlot.R, 1050f){MinHitChance = HitChance.VeryHigh};

            Q.SetSkillshot(0.25f, 40f, 3000f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 250f, 1000f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 100f, 2100f, false, SkillshotType.SkillshotLine);

            BurstMenu = myMenu.AddSubMenu("Burst Settings");
            {
                BurstMenu.AddSeparator("Burst Target -> Left Click to Lock!");
                BurstMenu.AddSeparator("How to Burst -> Lock the target and then just press Burst Key!");
                BurstMenu.Add(new MenuKeybind("BurstKeys", "Burst Key", new KeyBind(SharpDX.DirectInput.Key.T)));
                BurstMenu.Add(new MenuCheckbox("BurstER", "Burst Mode -> Enabled E->R?", false)).SetTooltip("if you dont enabled is RE Burst Mode");
            }

            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddBool("ComboEAA", "Use E| Reset Attack");
            ComboOption.AddBool("ComboECheck", "Use E| Safe Check");
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRCount", "Use R| Min Hit Count >= x(6 = off)", 4, 1, 6);

            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove();

            KillStealOption.AddQ();
            KillStealOption.AddW();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            MiscOption.AddW();
            MiscOption.AddBool("GapW", "Anti GapCloser");

            DrawOption.AddQ();
            DrawOption.AddW();
            DrawOption.AddE();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnDoCast += OnDoCast;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
            Obj_AI_Base.OnIssueOrder += OnIssueOrder;
            SpellBook.OnCastSpell += OnCastSpell;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            KillSteal();

            if (BurstMenu.Get<MenuKeybind>("BurstKeys").Active)
                Burst();

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

            if (KillStealOption.UseW && W.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(W.Range) && x.Health < W.GetDamage(x)))
                {
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
                        x => x.IsValidTarget(R.Range) &&
                        KillStealOption.GetKillStealTarget(x.ChampionName.ToLower()) && x.Health < R.GetDamage(x) &&
                        x.DistanceToPlayer() > Me.AttackRange + Me.BoundingRadius - x.BoundingRadius + 15 + E.Range - 100))
                {
                    if (!target.IsUnKillable())
                    {
                        SpellManager.PredCast(R, target);
                        return;
                    }
                }
            }
        }

        private static void Burst()
        {
            var target = TargetSelector.GetSelectedTarget();

            Orbwalker.Orbwalk(target, Game.CursorPosition);

            if (target.IsValidTarget(800f))
            {
                if (R.IsReady())
                {
                    if (!BurstMenu.Get<MenuCheckbox>("BurstER").Checked)
                    {
                        if (E.IsReady() && target.IsValidTarget(600f))
                        {
                            if (R.CanCast(target))
                            {
                                R.Cast(target, true);
                            }
                        }
                    }
                    else
                    {
                        if (E.IsReady() && target.IsValidTarget(600f))
                        {
                            E.Cast(target.Position, true);
                        }
                    }
                }
                else
                {
                    if (Q.IsReady() && target.IsValidTarget(Q.Range))
                    {
                        SpellManager.PredCast(Q, target);
                    }

                    if (W.IsReady() && target.IsValidTarget(W.Range) &&
                             (target.DistanceToPlayer() <= target.AttackRange + 70 ||
                              (target.DistanceToPlayer() >= Me.AttackRange + Me.BoundingRadius - target.BoundingRadius + 15 + 80)))
                    {
                        SpellManager.PredCast(W, target);
                    }

                    if (E.IsReady() && !R.IsReady())
                    {
                        ELogic(target);
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(R.Range))
            {
                if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    SpellManager.PredCast(Q, target);
                }

                if (ComboOption.UseE && E.IsReady() && target.IsValidTarget(800f))
                {
                    ELogic(target);
                }

                if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range) &&
                    (target.DistanceToPlayer() <= target.AttackRange + 70 ||
                     (target.DistanceToPlayer() >= Me.AttackRange + Me.BoundingRadius - target.BoundingRadius + 15 + 80)))
                {
                    SpellManager.PredCast(W, target);
                }

                if (ComboOption.UseR && R.IsReady() && target.IsValidTarget(R.Range))
                {
                    R.CastIfWillHit(target, ComboOption.GetSlider("ComboRCount"));
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true,
                    ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    SpellManager.PredCast(Q, target);
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
                    var Minions = MinionManager.GetMinions(Me.Position, Q.Range);

                    if (Minions.Any())
                    {
                        var QFarm =
                            MinionManager.GetBestLineFarmLocation(Minions.Select(x => x.Position.To2D()).ToList(),
                                Q.Width, Q.Range);

                        if (QFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearQCount"))
                        {
                            Q.Cast(QFarm.Position, true);
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
                    var mobs = MinionManager.GetMinions(Me.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral,
                        MinionOrderTypes.MaxHealth);

                    if (mobs.Any())
                    {
                        var QFarm =
                            MinionManager.GetBestLineFarmLocation(mobs.Select(x => x.Position.To2D()).ToList(),
                                Q.Width, Q.Range);

                        if (QFarm.MinionsHit >= 1)
                        {
                            Q.Cast(QFarm.Position, true);
                        }
                    }
                }
            }
        }

        private static void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (Args.SData.Name.Contains("GravesChargeShot") && BurstMenu.Get<MenuKeybind>("BurstKeys").Active && TargetSelector.GetSelectedTarget() != null && E.IsReady())
            {
                E.Cast(Me.Position.Extend(TargetSelector.GetSelectedTarget().Position, E.Range), true);
            }
            else if (Orbwalker.IsAutoAttack(Args.SData.Name) && E.IsReady())
            {
                if (isComboMode && ComboOption.GetBool("ComboEAA"))
                {
                    var target = Args.Target as AIHeroClient;

                    if (target != null && !target.IsDead && !target.IsZombie)
                    {
                        ELogic(target);
                    }
                }
                else if (isJungleClearMode && JungleClearOption.HasEnouguMana && Args.Target.ObjectType == GameObjectType.obj_AI_Minion && Args.Target.Team == GameObjectTeam.Neutral)
                {
                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        var mobs =
                            MinionManager.GetMinions(Me.Position, W.Range, MinionTypes.All, MinionTeam.Neutral,
                            MinionOrderTypes.MaxHealth).Where(x => !x.Name.ToLower().Contains("mini")).ToArray();

                        if (mobs.Any() && mobs.FirstOrDefault() != null)
                        {
                            ELogic(mobs.FirstOrDefault());
                        }
                    }
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser Args)
        {
            if (MiscOption.GetBool("GapW") && W.IsReady() && Args.Sender.IsValidTarget(W.Range) && Args.End.DistanceToPlayer() <= 200)
            {
                W.Cast(Args.End, true);
            }
        }

        private static void OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs Args)
        {
            if (!sender.IsMe || !BurstMenu.Get<MenuKeybind>("BurstKeys").Active)
            {
                return;
            }

            if (Args.Animation == "Spell3")
            {
                if (BurstMenu.Get<MenuCheckbox>("BurstER").Checked && TargetSelector.GetSelectedTarget() != null && R.IsReady())
                {
                    var target = TargetSelector.GetSelectedTarget();

                    if (target != null)
                    {
                        Core.DelayAction(() => { Me.Spellbook.CastSpell(SpellSlot.R, target.ServerPosition); }, Game.Ping);
                    }
                }
            }
            else if (Args.Animation == "Spell4" && TargetSelector.GetSelectedTarget() != null &&
                !BurstMenu.Get<MenuCheckbox>("BurstER").Checked && E.IsReady())
            {
                E.Cast(Me.ServerPosition.Extend(TargetSelector.GetSelectedTarget().Position, E.Range), true);
            }
        }

        private static void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs Args)
        {
            if (!sender.IsMe || Args.Order != GameObjectOrder.AttackUnit || E.IsReady())
                return;

            var target = (Obj_AI_Base) Args.Target;

            if (target == null)
                return;

            if (!Orbwalker.CanAttack() || target.DistanceToPlayer() > Me.AttackRange + Me.BoundingRadius - target.BoundingRadius + 15)
            {
                Args.Process = false;
                return;
            }

            if (BurstMenu.Get<MenuKeybind>("BurstKeys").Active && target.ObjectType == GameObjectType.AIHeroClient)
            {
                if (!R.IsReady())
                {
                    Core.DelayAction(() =>
                    {
                        if (ELogic(target))
                            Orbwalker.ResetAutoAttackTimer();
                    }, Game.Ping);
                }
            }
            else if (isComboMode && ComboOption.UseE && ComboOption.GetBool("ComboEAA"))
            {
                Core.DelayAction(() =>
                {
                    if (ELogic(target))
                        Orbwalker.ResetAutoAttackTimer();
                }, Game.Ping);
            }
            else if (isJungleClearMode && JungleClearOption.HasEnouguMana && JungleClearOption.UseE)
            {
                Core.DelayAction(() =>
                {
                    if (ELogic(target))
                        Orbwalker.ResetAutoAttackTimer();
                }, Game.Ping);
            }
        }

        private static void OnCastSpell(SpellBook sender, SpellbookCastSpellEventArgs Args)
        {
            if (sender.Owner.IsMe && Args.Slot == SpellSlot.E)
            {
                Orbwalker.ResetAutoAttackTimer();
            }
        }

        private static bool ELogic(Obj_AI_Base target)
        {
            if (!E.IsReady())
            {
                return false;
            }

            var ePosition = Me.Position.Extend(Game.CursorPosition, E.Range);

            if (ePosition.UnderTurret(true) && Me.HealthPercent <= 50)
            {
                return false;
            }

            if (ComboOption.GetBool("ComboECheck") && isComboMode)
            {
                if (ObjectManager.Heroes.Enemies.Count(x => !x.IsDead && x.Distance(ePosition) <= 550) >= 3)
                {
                    return false;
                }

                //Catilyn W
                if (ObjectManager
                        .Get<Obj_GeneralParticleEmitter>()
                        .FirstOrDefault(
                            x =>
                                x != null && x.IsValid() &&
                                x.Name.ToLower().Contains("yordletrap_idle_red.troy") &&
                                x.Position.Distance(ePosition) <= 100) != null)
                {
                    return false;
                }

                //Jinx E
                if (ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(x => x.IsValid() && x.IsEnemy && x.Name == "k" &&
                                             x.Position.Distance(ePosition) <= 100) != null)
                {
                    return false;
                }

                //Teemo R
                if (ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(x => x.IsValid() && x.IsEnemy && x.Name == "Noxious Trap" &&
                                             x.Position.Distance(ePosition) <= 100) != null)
                {
                    return false;
                }
            }

            if (target.Distance(ePosition) > Me.AttackRange + Me.BoundingRadius - target.BoundingRadius + 15)
            {
                return false;
            }

            if (target.Health < Me.GetAutoAttackDamage(target, true) * 2 &&
                target.Distance(ePosition) <= Me.AttackRange + Me.BoundingRadius - target.BoundingRadius + 15)
            {
                if (E.Cast(ePosition, true))
                    return true;
            }
            else if (!Me.HasBuff("GravesBasicAttackAmmo2") && Me.HasBuff("GravesBasicAttackAmmo1") &&
                target.Distance(ePosition) <= Me.AttackRange + Me.BoundingRadius - target.BoundingRadius + 15)
            {
                if (E.Cast(ePosition, true))
                    return true;
            }
            else if (!Me.HasBuff("GravesBasicAttackAmmo2") && !Me.HasBuff("GravesBasicAttackAmmo1") &&
                target.IsValidTarget(Me.AttackRange + Me.BoundingRadius - target.BoundingRadius + 15))
            {
                if (E.Cast(ePosition, true))
                    return true;
            }

            return false;
        }
    }
}
