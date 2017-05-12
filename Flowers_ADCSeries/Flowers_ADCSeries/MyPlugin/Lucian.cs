namespace Flowers_ADCSeries.MyPlugin
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Args;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using MyBase;
    using MyCommon;

    using SharpDX.DirectInput;

    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class Lucian : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 650f);
            QExtend = new Spell(SpellSlot.Q, 900f);
            W = new Spell(SpellSlot.W, 1000f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.VeryHigh };
            W1 = new Spell(SpellSlot.W, 1000f, TargetSelector.DamageType.Magical) { MinHitChance = HitChance.VeryHigh };
            E = new Spell(SpellSlot.E, 425f);
            R = new Spell(SpellSlot.R, 1200f) { MinHitChance = HitChance.High };
            R1 = new Spell(SpellSlot.R, 1200f) { MinHitChance = HitChance.High };

            Q.SetTargetted(0.25f, float.MaxValue);
            QExtend.SetSkillshot(0.35f, 25f, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.3f, 80f, 1600f, true, SkillshotType.SkillshotLine);
            W1.SetSkillshot(0.3f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.1f, 110f, 2800f, true, SkillshotType.SkillshotLine);
            R1.SetSkillshot(0.1f, 110f, 2800f, false, SkillshotType.SkillshotLine);

            ComboOption.AddQ();
            ComboOption.AddBool("ComboQExtend", "Use Q Extend");
            ComboOption.AddW();
            ComboOption.AddBool("ComboWFast", "Use W| Fast Reset the Passive");
            ComboOption.AddE();
            ComboOption.AddBool("ComboEDash", "Use E| Dash to Target");
            ComboOption.AddBool("ComboEReset", "Use E| Reset Auto Attack");
            ComboOption.AddBool("ComboEShort", "Use E| Short E Reset Auto Attack");
            ComboOption.AddBool("ComboESafe", "Use E| Safe Check");
            ComboOption.AddR();

            HarassOption.AddQ();
            HarassOption.AddBool("HarassQExtend", "Use Q Extend");
            HarassOption.AddW(false);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddW();
            LaneClearOption.AddE();
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddMove(false);

            KillStealOption.AddQ();
            KillStealOption.AddW();

            MiscOption.AddE();
            MiscOption.AddBool("EnabledAnti", "Use E| Anti Gapcloser");
            MiscOption.AddSlider("AntiGapCloserHp", "Use E| Player HealthPercent <= x%", 45);
            MiscOption.AddBool("EnabledAntiMelee", "Use E| Anti Melee");
            MiscOption.AddSlider("AntiMeleeHp", "Use E| Player HealthPercent <= x%", 35);
            MiscOption.AddR();
            MiscOption.AddKey("SemiR", "Semi Cast R Key", Key.T);

            DrawOption.AddQ();
            DrawOption.AddQExtend();
            DrawOption.AddW();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
            Orbwalker.AfterAttack += AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            SpellBook.OnCastSpell += OnCastSpell;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                havePassive = false;
                return;
            }

            if (Utils.TickCount - lastCastTime >= 3100)
                havePassive = false;

            if (isFleeMode && FleeOption.DisableMove)
            {
                Orbwalker.Move = false;
                return;
            }

            Orbwalker.Move = true;

            if (Me.HasBuff("LucianR"))
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosition);
                return;
            }

            if (Me.IsDashing())
                return;

            if (R.Level > 0 && R.IsReady() && Me.Mana > R.ManaCost + Q.ManaCost + W.ManaCost + E.ManaCost &&
                MiscOption.GetKey("SemiR"))
                SemiRLogic();

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
            }
        }

        private static void SemiRLogic()
        {
            var select = TargetSelector.SelectedTarget;
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

            if (select != null && select.IsValidTarget(R.Range))
                R1.Cast(select, true);
            else if (select == null && target != null && target.IsValidTarget(R.Range))
                R1.Cast(target, true);
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.IsReady() && Me.Mana > Q.ManaCost + E.ManaCost)
                foreach (var target in ObjectManager.Heroes.Enemies
                    .Where(
                        x =>
                            x.IsValidTarget(QExtend.Range) && !x.IsUnKillable() &&
                            x.Health < MyDamageCalculate.GetQDamage(x)))
                {
                    QLogic(target);
                    return;
                }

            if (KillStealOption.UseW && W.IsReady() && Me.Mana > W.ManaCost + Q.ManaCost + E.ManaCost)
                foreach (var target in ObjectManager.Heroes.Enemies
                    .Where(
                        x => x.IsValidTarget(W.Range) && !x.IsUnKillable() && x.Health < MyDamageCalculate.GetWDamage(x))
                )
                    if (target.IsValidTarget(W.Range))
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.Hitchance >= HitChance.VeryHigh)
                        {
                            W.Cast(wPred.CastPosition, true);
                            return;
                        }
                    }
        }

        private static void Combo()
        {
            if (ComboOption.GetBool("ComboEDash") && E.IsReady())
            {
                var target = TargetSelector.GetTarget(Me.AttackRange + Me.BoundingRadius,
                    TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(Me.AttackRange + Me.BoundingRadius + E.Range) &&
                    !target.IsValidTarget(Me.AttackRange + Me.BoundingRadius))
                    ELogic(target, 0);
            }

            if (ComboOption.GetBool("ComboQExtend") && QExtend.IsReady() && !Me.IsDashing() && !havePassive &&
                !havePassiveBuff)
            {
                var target = TargetSelector.GetTarget(QExtend.Range, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(QExtend.Range) && !target.IsValidTarget(Q.Range))
                    QLogic(target);
            }

            if (ComboOption.UseR && R.IsReady())
            {
                var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget(R.Range) && !target.IsUnKillable() && !Me.IsUnderEnemyTurret() &&
                    !target.IsValidTarget(Me.AttackRange + Me.BoundingRadius))
                {
                    if (
                        ObjectManager.Heroes.Enemies.Any(
                            x => x.NetworkId != target.NetworkId && x.Distance(target) <= 550))
                        return;

                    var rDMG = MyDamageCalculate.GetRDamage(target);

                    if (target.Health + target.HPRegenRate * 3 < rDMG)
                        if (target.DistanceToPlayer() <= 800 && target.Health < rDMG * 0.6)
                            R.Cast(target, true);
                        else if (target.DistanceToPlayer() <= 1000 && target.Health < rDMG * 0.4)
                            R.Cast(target, true);
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                if (HarassOption.UseQ && Q.IsReady())
                    foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(QExtend.Range) && HarassOption.GetHarassTarget(x.ChampionName)))
                        if (target.IsValidTarget(QExtend.Range))
                        {
                            QLogic(target, HarassOption.GetBool("HarassQExtend"));
                            return;
                        }

                if (HarassOption.UseW && W.IsReady())
                    foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(W.Range) && HarassOption.GetHarassTarget(x.ChampionName)))
                        if (target.IsValidTarget(W.Range))
                        {
                            var wPred = W.GetPrediction(target);

                            if (wPred.Hitchance >= HitChance.VeryHigh)
                            {
                                W.Cast(wPred.CastPosition, true);
                                return;
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
            if (Utils.TickCount - lastCastTime < 600 || havePassiveBuff)
                return;

            if (LaneClearOption.HasEnouguMana)
            {
                if (LaneClearOption.UseQ && Q.IsReady())
                {
                    var minions = MinionManager.GetMinions(Me.Position, Q.Range);

                    if (minions.Any())
                        foreach (var minion in minions)
                        {
                            var qExminions = MinionManager.GetMinions(Me.Position, QExtend.Range);

                            if (minion != null &&
                                QExtend.CountHits(qExminions, Me.Position.Extend(minion.Position, 900)) >= 2)
                            {
                                Q.CastOnUnit(minion, true);
                                return;
                            }
                        }
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser Args)
        {
            if (!MiscOption.GetBool("EnabledAnti") || !E.IsReady() || Me.Mana < E.ManaCost ||
                Me.HealthPercent > MiscOption.GetSlider("AntiGapCloserHp") || Args.Sender == null || !Args.Sender.IsEnemy)
                return;

            if (Args.Target.IsMe || Args.Sender.DistanceToPlayer() <= 300 || Args.End.DistanceToPlayer() <= 250)
                E.Cast(Me.Position.Extend(Args.Sender.Position, -E.Range), true);
        }

        private static void OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs Args)
        {
            if (!sender.IsMe || isNoneMode)
                return;

            if (Args.Animation == "Spell1" || Args.Animation == "Spell2")
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosition);
        }

        private static void AfterAttack(AttackableUnit sender, AttackableUnit ArgsTarget)
        {
            if (!sender.IsMe || Me.IsDead)
                return;

            havePassive = false;

            if (ArgsTarget == null || ArgsTarget.IsDead || ArgsTarget.Health <= 0)
                return;

            if (isComboMode)
            {
                var target = ArgsTarget as AIHeroClient;

                if (target != null && !target.IsDead)
                    if (ComboOption.GetBool("ComboEReset") && E.IsReady() &&
                        target.IsValidTarget(Me.AttackRange + Me.BoundingRadius))
                        ELogic(target, 1);
                    else if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                        Q.CastOnUnit(target, true);
                    else if (ComboOption.UseW && W.IsReady() && target.IsValidTarget(W.Range))
                        if (ComboOption.GetBool("ComboWFast"))
                        {
                            W1.Cast(target.Position, true);
                        }
                        else
                        {
                            var wPred = W.GetPrediction(target);

                            if (wPred.Hitchance >= HitChance.VeryHigh)
                                W.Cast(wPred.CastPosition, true);
                        }
            }
            else
            {
                if (isLaneClearMode && LaneClearOption.HasEnouguMana &&
                    (ArgsTarget.ObjectType == GameObjectType.obj_AI_Turret ||
                     ArgsTarget.ObjectType == GameObjectType.obj_Turret ||
                     ArgsTarget.ObjectType == GameObjectType.obj_HQ ||
                     ArgsTarget.ObjectType == GameObjectType.obj_BarracksDampener))
                    if (LaneClearOption.UseE && E.IsReady())
                        E.Cast(Me.Position.Extend(Game.CursorPosition, 130), true);
                    else if (LaneClearOption.UseW && W.IsReady())
                        W1.Cast(Game.CursorPosition, true);

                if (isJungleClearMode && JungleClearOption.HasEnouguMana)
                {
                    var mobs = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral,
                        MinionOrderTypes.MaxHealth);

                    if (mobs.Any())
                        if (JungleClearOption.UseE && E.IsReady())
                            E.Cast(Me.Position.Extend(Game.CursorPosition, 130), true);
                        else if (JungleClearOption.UseQ && Q.IsReady())
                            Q.CastOnUnit(mobs.FirstOrDefault(), true);
                        else if (JungleClearOption.UseW && W.IsReady())
                            W1.Cast(mobs.FirstOrDefault(), true);
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (sender.IsEnemy && sender.ObjectType == GameObjectType.AIHeroClient && sender.IsMelee() &&
                     Args.Target != null && Args.Target.IsMe)
            {
                if (MiscOption.GetBool("EnabledAntiMelee") && Me.HealthPercent <= MiscOption.GetSlider("AntiMeleeHp"))
                    E.Cast(Me.Position.Extend(sender.Position, -E.Range), true);
            }
        }

        private static void OnCastSpell(SpellBook sender, SpellbookCastSpellEventArgs Args)
        {
            if (sender.Owner.IsMe)
            {
                if (Args.Slot == SpellSlot.Q)
                {
                    havePassive = true;
                    lastCastTime = Utils.TickCount;
                    Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosition);
                }
                if (Args.Slot == SpellSlot.W)
                {
                    havePassive = true;
                    lastCastTime = Utils.TickCount;
                    Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosition);
                }
                if (Args.Slot == SpellSlot.E)
                {
                    havePassive = true;
                    lastCastTime = Utils.TickCount;
                    Orbwalker.ResetAutoAttackTimer();
                }
            }
        }

        private static void QLogic(AIHeroClient target, bool useExtendQ = true)
        {
            if (!Q.IsReady() || target == null || target.IsDead || target.IsUnKillable())
                return;

            if (target.IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(target, true);
            }
            else if (target.IsValidTarget(QExtend.Range) && useExtendQ)
            {
                var collisions = MinionManager.GetMinions(Me.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly);

                if (!collisions.Any())
                    return;

                foreach (var minion in collisions)
                {
                    var qPred = QExtend.GetPrediction(target);
                    var qPloygon = new MyGeometry.Polygon.Rectangle(Me.Position,
                        Me.Position.Extend(minion.Position, QExtend.Range), QExtend.Width);

                    if (qPloygon.IsInside(qPred.UnitPosition.To2D()))
                    {
                        Q.CastOnUnit(minion, true);
                        break;
                    }
                }
            }
        }

        private static void ELogic(AIHeroClient target, int count)
        {
            if (!E.IsReady() || target == null || target.IsDead || target.IsUnKillable())
                return;

            switch (count)
            {
                case 0:
                    {
                        if (target.IsValidTarget(Me.AttackRange + Me.BoundingRadius) ||
                            !target.IsValidTarget(Me.AttackRange + Me.BoundingRadius + E.Range))
                            return;

                        var dashPos = Me.ServerPosition.Extend(Game.CursorPosition, E.Range);

                        if (dashPos.CountEnemiesInRange(500) >= 3 && dashPos.CountAlliesInRange(400) < 3 &&
                            ComboOption.GetBool("ComboESafe"))
                            return;

                        if (Me.DistanceToMouse() > (Me.AttackRange + Me.BoundingRadius) * 0.7 &&
                            target.Position.Distance(dashPos) < Me.AttackRange + Me.BoundingRadius)
                            E.Cast(dashPos, true);
                    }
                    break;
                case 1:
                    {
                        var dashRange = ComboOption.GetBool("ComboEShort")
                            ? (Me.DistanceToMouse() > Me.AttackRange + Me.BoundingRadius ? E.Range : 130)
                            : E.Range;
                        var dashPos = Me.ServerPosition.Extend(Game.CursorPosition, dashRange);

                        if (dashPos.CountEnemiesInRange(500) >= 3 && dashPos.CountAlliesInRange(400) < 3 &&
                            ComboOption.GetBool("ComboESafe"))
                            return;

                        E.Cast(dashPos, true);
                    }
                    break;
            }
        }
    }
}
