namespace Flowers_ADCSeries.MyPlugin
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Args;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyBase;
    using MyCommon;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class Draven : MyLogic
    {
        private static readonly List<AllAxe> AxeList = new List<AllAxe>();

        private static int AxeCount => (Me.HasBuff("dravenspinning") ? 1 : 0) + (Me.HasBuff("dravenspinningleft") ? 1 : 0) + AxeList.Count;

        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 950f);
            R = new Spell(SpellSlot.R, 3000f);

            E.SetSkillshot(0.25f, 100f, 1400f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.4f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddBool("ComboWLogic", "Use W| If Target Not In Attack Range");
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSolo", "Use R| Solo Mode");
            ComboOption.AddBool("ComboRTeam", "Use R| TeamFight");

            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddMana();

            LaneClearOption.AddQ();
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddW();
            FleeOption.AddMove(false);

            KillStealOption.AddE();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            MiscOption.AddQ();
            MiscOption.AddList("CatchMode", "Catch Axe Mode: ", new[] { "All Mode", "Only Combo", "Off" });
            MiscOption.AddSlider("CatchRange", "Max Catch Range(Mouse is Center)", 600, 150, 1500);
            MiscOption.AddBool("UnderTurret", "Dont Cast In Under Turret");
            MiscOption.AddBool("CheckSafe", "Check Axe Position is Safe");
            MiscOption.AddSlider("MaxAxeCount", "Max Axe Count <= x", 2, 1, 3);
            MiscOption.AddBool("EnableControl", "Enable Cancel Catch Axe Key?", false);
            MiscOption.AddKey("ControlKey", "Cancel Key", SharpDX.DirectInput.Key.G);
            MiscOption.AddBool("ControlKey2", "Or Right Click?");
            MiscOption.AddBool("ControlKey3", "Or Mouse Scroll?", false);
            MiscOption.AddW();
            MiscOption.AddList("WCatchAxe", "If Axe too Far Auto Use", new[] { "Combo/Harass Mode", "Only Combo", "Off" });
            MiscOption.AddBool("AutoWSlow", "Auto W|If Player Have Slow Debuff");
            MiscOption.AddE();
            //MiscOption.AddBool("Interrupt", "Interrupt Spell");
            MiscOption.AddBool("Anti", "Anti Gapcloser", false);
            MiscOption.AddBool("AntiRengar", "Anti Rengar");
            MiscOption.AddBool("AntiKhazix", "Anti Khazix");
            MiscOption.AddBool("AntiMelee", "Anti Melee");
            MiscOption.AddR();
            MiscOption.AddKey("rMenuSemi", "Semi R Key", SharpDX.DirectInput.Key.T);
            MiscOption.AddSlider("rMenuMin", "Use R| Min Range >= x", 1000, 500, 2500);
            MiscOption.AddSlider("rMenuMax", "Use R| Man Range <= x", 3000, 1500, 3500);


            DrawOption.AddBool("DrawCatchAxe", "Draw Catch Axe Range");
            DrawOption.AddBool("DrawAxe", "Draw Axe Position");
            DrawOption.AddSlider("DrawThinkness", "Draw Circle Thinkness", 3, 1, 10);
            DrawOption.AddE();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Game.OnWndProc += OnWndProc;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            //Interrupter
            Orbwalker.BeforeAttack += BeforeAttack;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnUpdate()
        {
            AxeList.RemoveAll(x => x.Axe.IsDead || !x.Axe.IsValid());

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

            AutoCatchLogic();
            SemiRLogic();
            AutoUseLogic();
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

        private static void AutoCatchLogic()
        {
            if ((MiscOption.GetList("CatchMode") == 2) || (MiscOption.GetList("CatchMode") == 1 && !isComboMode))
            {
                myOrbwalker.SetOrbwalkingPoint(Game.CursorPosition);
                return;
            }

            var catchRange = MiscOption.GetSlider("CatchRange");

            var bestAxe =
                AxeList.Where(x => !x.Axe.IsDead && x.Axe.IsValid() && x.Axe.Position.DistanceToMouse() <= catchRange)
                    .OrderBy(x => x.AxeTime)
                    .ThenBy(x => x.Axe.Position.DistanceToPlayer())
                    .ThenBy(x => x.Axe.Position.DistanceToMouse())
                    .FirstOrDefault();

            if (bestAxe != null)
            {
                if (MiscOption.GetBool("UnderTurret") &&
                    ((Me.UnderTurret(true) && bestAxe.Axe.Position.UnderTurret(true)) || (bestAxe.Axe.Position.
                                                                                              UnderTurret(true) &&
                                                                                          !Me.UnderTurret(true))))
                {
                    return;
                }

                if (MiscOption.GetBool("CheckSafe") &&
                    (ObjectManager.Heroes.Enemies.Count(x => x.Distance(bestAxe.Axe.Position) < 350) > 3 ||
                     ObjectManager.Heroes.Enemies.Count(x => x.Distance(bestAxe.Axe.Position) < 350 && x.IsMelee()) > 1))
                {
                    return;
                }

                if (((MiscOption.GetList("WCatchAxe") == 0 && (isComboMode || isHarassMode)) ||
                    (MiscOption.GetList("WCatchAxe") == 1 && isComboMode)) && W.IsReady() &&
                    (bestAxe.Axe.Position.DistanceToPlayer() / Me.MovementSpeed * 1000 >= bestAxe.AxeTime - Utils.TickCount))
                {
                    W.Cast();
                }

                if (bestAxe.Axe.Position.DistanceToPlayer() > 100)
                {
                    if (Utils.TickCount - lastCatchTime > 1800)
                    {
                        if (!isNoneMode)
                        {
                            myOrbwalker.SetOrbwalkingPoint(bestAxe.Axe.Position);
                        }
                        else
                        {
                            Me.IssueOrder(GameObjectOrder.MoveTo, bestAxe.Axe.Position);
                        }
                    }
                    else
                    {
                        if (!isNoneMode)
                        {
                            myOrbwalker.SetOrbwalkingPoint(Game.CursorPosition);
                        }
                    }
                }
                else
                {
                    if (!isNoneMode)
                    {
                        myOrbwalker.SetOrbwalkingPoint(Game.CursorPosition);
                    }
                }
            }
            else
            {
                if (!isNoneMode)
                {
                    myOrbwalker.SetOrbwalkingPoint(Game.CursorPosition);
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

        private static void AutoUseLogic()
        {
            if (MiscOption.GetBool("AutoWSlow") && W.IsReady() && Me.HasBuffOfType(BuffType.Slow))
            {
                W.Cast();
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseE && E.IsReady())
            {
                foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(E.Range) && x.Health < E.GetDamage(x)))
                {
                    if (!target.IsUnKillable())
                    {
                        SpellManager.PredCast(E, target);
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
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(E.Range))
            {
                if (ComboOption.UseW && W.IsReady() && !Me.HasBuff("dravenfurybuff"))
                {
                    if (ComboOption.GetBool("ComboWLogic"))
                    {
                        if (target.DistanceToPlayer() >= 600)
                        {
                            W.Cast();
                        }
                        else
                        {
                            if (target.Health <
                                (AxeCount > 0 ? Q.GetDamage(target) * 5 : Me.GetAutoAttackDamage(target) * 5))
                            {
                                W.Cast();
                            }
                        }
                    }
                    else
                    {
                        W.Cast();
                    }
                }

                if (ComboOption.UseE && E.IsReady())
                {
                    if (!Orbwalker.InAutoAttackRange(target) ||
                        target.Health < (AxeCount > 0 ? Q.GetDamage(target) * 3 : Me.GetAutoAttackDamage(target) * 3) ||
                        Me.HealthPercent < 40)
                    {
                        SpellManager.PredCast(E, target);
                    }
                }

                if (ComboOption.UseR && R.IsReady())
                {
                    if (ComboOption.GetBool("ComboRSolo"))
                    {
                        if ((target.Health <
                             R.GetDamage(target) +
                             (AxeCount > 0 ? Q.GetDamage(target) * 2 : Me.GetAutoAttackDamage(target) * 2) +
                             (E.IsReady() ? E.GetDamage(target) : 0)) &&
                            target.Health > (AxeCount > 0 ? Q.GetDamage(target) * 3 : Me.GetAutoAttackDamage(target) * 3) &&
                            (Me.CountEnemiesInRange(1000) == 1 ||
                             (Me.CountEnemiesInRange(1000) == 2 && Me.HealthPercent >= 60)))
                        {
                            SpellManager.PredCast(R, target, true);
                        }
                    }

                    if (ComboOption.GetBool("ComboRTeam"))
                    {
                        if (Me.CountAlliesInRange(1000) <= 3 && Me.CountEnemiesInRange(1000) <= 3)
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.AoeTargetsHitCount >= 3)
                            {
                                R.Cast(rPred.CastPosition);
                            }
                            else if (rPred.AoeTargetsHitCount >= 2)
                            {
                                R.Cast(rPred.CastPosition);
                            }
                        }
                        else if (Me.CountAlliesInRange(1000) <= 2 && Me.CountEnemiesInRange(1000) <= 4)
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.AoeTargetsHitCount >= 3)
                            {
                                R.Cast(rPred.CastPosition);
                            }
                        }
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(E.Range))
                {
                    if (HarassOption.UseE && E.IsReady())
                    {
                        E.CastIfWillHit(target, 2);
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
                if (LaneClearOption.UseQ && Q.IsReady() && AxeCount < 2 && !Orbwalker.CanAttack())// TODO !Me.Spellbook.IsAutoAttacking)
                {
                    var minions = MinionManager.GetMinions(Me.Position, 600);

                    if (minions.Any() && minions.Count >= 2)
                    {
                        Q.Cast();
                    }
                }

                if (LaneClearOption.UseE && E.IsReady())
                {
                    var minions = MinionManager.GetMinions(Me.Position, E.Range);

                    if (minions.Any())
                    {
                        var eFarm = E.GetLineFarmLocation(minions, E.Width);

                        if (eFarm.MinionsHit >= LaneClearOption.GetSlider("LaneClearECount"))
                        {
                            E.Cast(eFarm.Position);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseE && E.IsReady())
                    {
                        E.Cast(mob, true);
                    }

                    if (JungleClearOption.UseW && W.IsReady() && !Me.HasBuff("dravenfurybuff") &&
                        AxeCount > 0)
                    {
                        foreach (
                            var m in
                            mobs.Where(
                                x =>
                                    x.DistanceToPlayer() <= 600 && !x.Name.ToLower().Contains("mini") &&
                                    !x.Name.ToLower().Contains("crab") && x.MaxHealth > 1500 &&
                                    x.Health > Me.GetAutoAttackDamage(x) * 2))
                        {
                            if (m.IsValidTarget(600))
                                W.Cast();
                        }
                    }

                    if (JungleClearOption.UseQ && Q.IsReady() && AxeCount < 2 && !Orbwalker.CanAttack())//TODO IsAutoAttacking
                    {
                        var qmobs = MinionManager.GetMinions(600f, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                        if (qmobs.Any())
                        {
                            if (qmobs.Count >= 2)
                            {
                                Q.Cast();
                            }

                            var qmob = qmobs.FirstOrDefault();
                            if (qmob != null && qmobs.Count == 1 && qmob.Health > Me.GetAutoAttackDamage(qmob) * 5)
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
            }
        }

        private static void Flee()
        {
            if (FleeOption.GetBool("FleeW") && W.IsReady())
            {
                W.Cast(true);
            }
        }

        private static void OnWndProc(WndEventArgs Args)
        {
            if (MiscOption.GetBool("EnableControl"))
            {
                if (MiscOption.GetKey("ControlKey"))
                {
                    if (Utils.TickCount - lastCastTime > 1800)
                    {
                        lastCastTime = Utils.TickCount;
                    }
                }

                if (MiscOption.GetBool("ControlKey2") && (Args.Msg == 516 || Args.Msg == 517))
                {
                    if (Utils.TickCount - lastCastTime > 1800)
                    {
                        lastCastTime = Utils.TickCount;
                    }
                }

                if (MiscOption.GetBool("ControlKey3") && Args.Msg == 0x20a)
                {
                    if (Utils.TickCount - lastCastTime > 1800)
                    {
                        lastCastTime = Utils.TickCount;
                    }
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (MiscOption.GetBool("AntiMelee") && E.IsReady())
            {
                if (sender != null && sender.IsEnemy && Args.Target != null && Args.Target.IsMe)
                {
                    if (sender.ObjectType == Me.ObjectType && sender.IsMelee())
                    {
                        E.Cast(sender.Position);
                    }
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser Args)
        {
            if (MiscOption.GetBool("Anti") && E.IsReady())
            {
                if (Args.End.Distance(Me.Position) <= 200 && Args.Sender.IsValidTarget(E.Range))
                {
                    E.Cast(Args.Sender.Position, true);
                }
            }
        }

        private static void BeforeAttack(Orbwalker.BeforeAttackEventArgs Args)
        {
            if (!Args.Unit.IsMe || Me.IsDead || Args.Target == null || Args.Target.ObjectType != GameObjectType.AIHeroClient)
                return;

            if (isComboMode)
            {
                if (ComboOption.UseQ && Q.IsReady() && AxeCount < ComboOption.GetSlider("MaxAxeCount"))
                {
                    var target = Args.Target as AIHeroClient;

                    if (target.IsValidTarget())
                    {
                        Q.Cast();
                    }
                }
            }
            else if (isHarassMode || isFarmMode && MyManaManager.SpellHarass)
            {
                if (HarassOption.HasEnouguMana)
                {
                    if (HarassOption.UseQ && Q.IsReady() && AxeCount < 2)
                    {
                        var target = Args.Target as AIHeroClient;

                        if (target.IsValidTarget())
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        private static void OnCreate(GameObject sender, EventArgs Args)
        {
            if (sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                AxeList.Add(new AllAxe(sender, Utils.TickCount + 1800));
            }

            if (E.IsReady())
            {
                var Rengar = ObjectManager.Heroes.Enemies.Find(heros => heros.ChampionName.Equals("Rengar"));
                var Khazix = ObjectManager.Heroes.Enemies.Find(heros => heros.ChampionName.Equals("Khazix"));

                if (MiscOption.GetBool("AntiRengar") && Rengar != null)
                {
                    if (sender.Name == "Rengar_LeapSound.troy" && sender.Position.Distance(Me.Position) < E.Range)
                    {
                        E.Cast(Rengar.Position, true);
                    }
                }

                if (MiscOption.GetBool("AntiKhazix") && Khazix != null)
                {
                    if (sender.Name == "Khazix_Base_E_Tar.troy" && sender.Position.Distance(Me.Position) <= 300)
                    {
                        E.Cast(Khazix.Position, true);
                    }
                }
            }
        }

        private static void OnDelete(GameObject sender, EventArgs Args)
        {
            if (sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                AxeList.RemoveAll(x => x.Axe.NetworkId == sender.NetworkId);
            }
        }

        private static void OnDraw(EventArgs Args)
        {
            if (ObjectManager.Player.IsDead || Shop.IsShopOpen || Chat.IsChatOpen)
                return;

            if (DrawOption.GetBool("DrawCatchAxe"))
                Drawing.DrawCircle(Game.CursorPosition, MiscOption.GetSlider("CatchRange"), System.Drawing.Color.FromArgb(251, 0, 255).ToSharpDX(), DrawOption.GetSlider("DrawThinkness"));

            if (DrawOption.GetBool("DrawAxe"))
            {
                foreach (var Axe in AxeList.Where(x => !x.Axe.IsDead && x.Axe.IsValid()))
                {
                    Drawing.DrawCircle(Axe.Axe.Position, 120, System.Drawing.Color.FromArgb(45, 255, 0).ToSharpDX(), DrawOption.GetSlider("DrawThinkness"));
                }
            }       
        }

        internal class AllAxe
        {
            public int AxeTime;
            public GameObject Axe;

            public AllAxe(GameObject axe, int time)
            {
                Axe = axe;
                AxeTime = time;
            }
        }
    }
}
