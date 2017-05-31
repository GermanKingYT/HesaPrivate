namespace Flowers_ADCSeries.MyPlugin
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Args;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;
    using HesaEngine.SDK.Notifications;

    using MyBase;
    using MyCommon;

    using SharpDX;

    using System;
    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class Jhin : MyLogic
    {
        private static AIHeroClient rShotTarget;
        private static int LastPingT;
        private static int LastECast;
        private static int LastShowNoit;
        private static bool IsAttack;
        private static Vector2 PingLocation;

        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W, 2500f);
            E = new Spell(SpellSlot.E, 750f);
            R = new Spell(SpellSlot.R, 3500f);

            W.SetSkillshot(0.75f, 40, float.MaxValue, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.5f, 120, 1600, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.21f, 80, 5000, false, SkillshotType.SkillshotLine);

            ComboOption.AddQ();
            ComboOption.AddBool("ComboQMinion", "Use Q| On Minion", false);
            ComboOption.AddW();
            ComboOption.AddBool("ComboWAA", "Use W| After Attack");
            ComboOption.AddBool("ComboWOnly", "Use W| Only Use to MarkTarget");
            ComboOption.AddE();
            ComboOption.AddR();

            HarassOption.AddQ();
            HarassOption.AddBool("HarassQMinion", "Use Q| On Minion");
            HarassOption.AddW();
            HarassOption.AddBool("HarassWOnly", "Use W| Only Use to MarkTarget");
            HarassOption.AddE();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddW();
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddQ();
            LastHitOption.AddMana();

            FleeOption.AddMove(false);

            KillStealOption.AddQ();
            KillStealOption.AddW();
            KillStealOption.AddBool("KillStealWInAttackRange", "Use W| Target In Attack Range");

            MiscOption.AddW();
            MiscOption.AddBool("AutoW", "Auto W| When target Cant Move");
            MiscOption.AddBool("GapW", "Anti GapCloser W| When target HavePassive");
            MiscOption.AddE();
            MiscOption.AddBool("AutoE", "Auto E| When target Cant Move");
            MiscOption.AddBool("GapE", "Anti GapCloser E");
            MiscOption.AddR();
            MiscOption.AddBool("rMenuAuto", "Auto R?");
            MiscOption.AddKey("rMenuSemi", "Semi R Key(One Press One Shot)", SharpDX.DirectInput.Key.T);
            MiscOption.AddBool("rMenuCheck", "Use R| Check is Safe?");
            MiscOption.AddSlider("rMenuMin", "Use R| Min Range >= x", 1000, 500, 2500);
            MiscOption.AddSlider("rMenuMax", "Use R| Man Range <= x", 3000, 1500, 3500);
            MiscOption.AddSlider("rMenuKill", "Use R| Min Shot Can Kill >= x", 3, 1, 4);
            MiscOption.AddSetting("Notification");
            MiscOption.AddBool("PingKill", "Auto Ping Killable Target");
            MiscOption.AddBool("NormalPingKill", "Normal Ping Notification", false);
            MiscOption.AddBool("NotificationKill", "Notification Show Killable Target");

            DrawOption.AddQ();
            DrawOption.AddW();
            DrawOption.AddE();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            Orbwalker.AfterAttack += AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
        }


        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            foreach (var enemy in ObjectManager.Heroes.Enemies.Where(h => R.IsReady() && h.IsValidTarget(R.Range) &&
                                                                 Me.GetSpellDamage(h, SpellSlot.R) *
                                                                 MiscOption.GetSlider("rMenuKill") > h.Health + h.HPRegenRate * 3))
            {
                if (MiscOption.GetBool("PingKill"))
                {
                    Ping(enemy.Position.To2D());
                }

                if (MiscOption.GetBool("NotificationKill") && Utils.TickCount - LastShowNoit > 10000)
                {
                    Notifications.AddNotification(
                        new Notification("R Kill: " + enemy.ChampionName + "!", 3000, true).SetTextColor(
                            System.Drawing.Color.FromArgb(255, 0, 0)));
                    LastShowNoit = Utils.TickCount;
                }
            }

            if (isFleeMode && FleeOption.DisableMove)
            {
                Orbwalker.Move = false;
                return;
            }

            Orbwalker.Move = true;

            RLogic();

            if (R.Instance.SpellData.Name == "JhinRShot")
            {
                Orbwalker.Attack = false;
                Orbwalker.Move = false;
                return;
            }

            Orbwalker.Attack = true;
            Orbwalker.Move = true;


            KillSteal();
            Auto();

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

        private static void RLogic()
        {
            AIHeroClient target = null;

            if (TargetSelector.GetSelectedTarget() != null &&
                TargetSelector.GetSelectedTarget().DistanceToPlayer() <= MiscOption.GetSlider("rMenuMax"))
            {
                target = TargetSelector.GetSelectedTarget();
            }
            else
            {
                target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            }

            if (R.IsReady() && target.IsValidTarget(R.Range))
            {
                switch (R.Instance.SpellData.Name)
                {
                    case "JhinR":
                        if (MiscOption.GetKey("rMenuSemi"))
                        {
                            if (R.Cast(R.GetPrediction(target).UnitPosition))
                            {
                                rShotTarget = target;
                                return;
                            }
                        }

                        if (!MiscOption.GetBool("rMenuAuto"))
                        {
                            return;
                        }

                        if (MiscOption.GetBool("rMenuCheck") && Me.CountEnemiesInRange(800f) > 0)
                        {
                            return;
                        }

                        if (target.DistanceToPlayer() <= MiscOption.GetSlider("rMenuMin"))
                        {
                            return;
                        }

                        if (target.DistanceToPlayer() > MiscOption.GetSlider("rMenuMax"))
                        {
                            return;
                        }

                        if (target.Health > R.GetDamage(target) * MiscOption.GetSlider("rMenuKill"))
                        {
                            return;
                        }

                        if (IsSpellHeroCollision(target, R))
                        {
                            return;
                        }

                        if (R.Cast(R.GetPrediction(target).UnitPosition))
                        {
                            rShotTarget = target;
                        }
                        break;
                    case "JhinRShot":
                        var selectTarget = TargetSelector.GetSelectedTarget();

                        if (selectTarget != null && selectTarget.IsValidTarget(R.Range) && InRCone(selectTarget))
                        {
                            if (MiscOption.GetKey("rMenuSemi"))
                            {
                                AutoUse(rShotTarget);
                                SpellManager.PredCast(R, rShotTarget);
                                return;
                            }

                            if (ComboOption.UseR && isComboMode)
                            {
                                AutoUse(rShotTarget);
                                SpellManager.PredCast(R, rShotTarget);
                                return;
                            }

                            if (!MiscOption.GetBool("rMenuAuto"))
                            {
                                return;
                            }

                            AutoUse(rShotTarget);
                            SpellManager.PredCast(R, rShotTarget);
                            return;
                        }

                        foreach (
                            var t in
                            ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(R.Range) && InRCone(x))
                                .OrderBy(x => x.Health).ThenByDescending(x => R.GetDamage(x)))
                        {
                            if (MiscOption.GetKey("rMenuSemi"))
                            {
                                AutoUse(t);
                                SpellManager.PredCast(R, t);
                                return;
                            }

                            if (ComboOption.UseR && isComboMode)
                            {
                                AutoUse(t);
                                SpellManager.PredCast(R, t);
                                return;
                            }

                            if (!MiscOption.GetBool("rMenuAuto"))
                            {
                                return;
                            }

                            AutoUse(t);
                            SpellManager.PredCast(R, t);
                            return;
                        }
                        break;
                }
            }
        }

        private static void KillSteal()
        {
            if (R.Instance.SpellData.Name == "JhinRShot")
            {
                return;
            }

            if (KillStealOption.UseQ && Q.IsReady())
            {
                foreach (
                    var target in
                    ObjectManager.Heroes.Enemies.Where(
                        x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                {
                    if (target.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(target, true);
                    }
                }
            }

            if (KillStealOption.UseW && W.IsReady())
            {
                foreach (
                    var target in
                    ObjectManager.Heroes.Enemies.Where(
                        x => x.IsValidTarget(W.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.W)))
                {
                    if (target.IsValidTarget(W.Range))
                    {
                        if (target.Health < Me.GetSpellDamage(target, SpellSlot.Q) && Q.IsReady() &&
                            target.IsValidTarget(Q.Range))
                        {
                            return;
                        }

                        if (KillStealOption.GetBool("KillStealWInAttackRange") && Orbwalker.InAutoAttackRange(target))
                        {
                            SpellManager.PredCast(W, target, true);
                            return;
                        }

                        if (Orbwalker.InAutoAttackRange(target) && target.Health <= Me.GetAutoAttackDamage(target, true))
                        {
                            return;
                        }

                        SpellManager.PredCast(W, target, true);
                        return;
                    }
                }
            }
        }

        private static void Auto()
        {
            if (R.Instance.SpellData.Name == "JhinRShot")
            {
                return;
            }

            foreach (var target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(W.Range) && !x.CanMoveMent()))
            {
                if (MiscOption.GetBool("AutoW") && W.IsReady() && target.IsValidTarget(W.Range))
                {
                    SpellManager.PredCast(W, target, true);
                }

                if (MiscOption.GetBool("AutoE") && E.IsReady() &&
                    target.IsValidTarget(E.Range) && Utils.TickCount - LastECast > 2500 && !IsAttack)
                {
                    SpellManager.PredCast(E, target, true);
                }
            }
        }

        private static void Combo()
        {
            if (R.Instance.SpellData.Name == "JhinRShot")
            {
                return;
            }

            var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);

            if (ComboOption.UseW && W.IsReady() && wTarget.IsValidTarget(W.Range))
            {
                if (ComboOption.GetBool("ComboWOnly"))
                {
                    if (HasPassive(wTarget))
                    {
                        SpellManager.PredCast(W, wTarget, true);
                    }
                }
                else
                {
                    SpellManager.PredCast(W, wTarget, true);
                }
            }

            if (ComboOption.UseQ && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range + 300, TargetSelector.DamageType.Physical);
                var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

                if (qTarget.IsValidTarget(Q.Range) && !Orbwalker.CanAttack())
                {
                    Q.CastOnUnit(qTarget, true);
                }
                else if (target.IsValidTarget(Q.Range + 300) && ComboOption.GetBool("ComboQMinion"))
                {
                    if (Me.HasBuff("JhinPassiveReload") || (!Me.HasBuff("JhinPassiveReload") &&
                         Me.CountEnemiesInRange(Orbwalker.GetRealAutoAttackRange(Me) + Me.BoundingRadius) == 0))
                    {
                        var qPred = Core.Prediction.GetPrediction(target, 0.25f);
                        var bestQMinion =
                            MinionManager.GetMinions(qPred.CastPosition, 300)
                                .Where(x => x.IsValidTarget(Q.Range))
                                .OrderBy(x => x.Health)
                                .ThenBy(x => x.Distance(target))
                                .FirstOrDefault();

                        if (bestQMinion != null && bestQMinion.IsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(bestQMinion, true);
                        }
                    }
                }
            }

            var eTarget = TargetSelector.GetTarget(E.Range);

            if (ComboOption.UseE && E.IsReady() && eTarget.IsValidTarget(E.Range) && Utils.TickCount - LastECast > 2500 && !IsAttack)
            {
                if (!eTarget.CanMoveMent())
                {
                    SpellManager.PredCast(E, eTarget, true);
                }
                else
                {
                    if (E.GetPrediction(eTarget).Hitchance >= HitChance.High)
                    {
                        E.Cast(E.GetPrediction(eTarget).UnitPosition);
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
                    var target = TargetSelector.GetTarget(Q.Range + 300, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                    if (target.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(target, true);
                    }
                    else if (target.IsValidTarget(Q.Range + 300) && HarassOption.GetBool("HarassQMinion"))
                    {
                        if (Me.HasBuff("JhinPassiveReload") || (!Me.HasBuff("JhinPassiveReload") &&
                             Me.CountEnemiesInRange(Orbwalker.GetRealAutoAttackRange(Me)) == 0))
                        {
                            var qPred = Core.Prediction.GetPrediction(target, 0.25f);
                            var bestQMinion =
                                MinionManager.GetMinions(qPred.CastPosition, 300)
                                    .Where(x => x.IsValidTarget(Q.Range))
                                    .OrderBy(x => x.Distance(target))
                                    .ThenBy(x => x.Health)
                                    .FirstOrDefault();

                            if (bestQMinion != null)
                            {
                                Q.CastOnUnit(bestQMinion, true);
                            }
                        }
                    }
                }

                if (HarassOption.UseE && E.IsReady() && Utils.TickCount - LastECast > 2500 && !IsAttack)
                {
                    var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                    if (eTarget.IsValidTarget(E.Range))
                        SpellManager.PredCast(E, eTarget, true);
                }

                if (HarassOption.UseW && W.IsReady())
                {
                    var target = TargetSelector.GetTarget(1500f, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));

                    if (target.IsValidTarget(W.Range))
                    {
                        if (HarassOption.GetBool("HarassWOnly") && !HasPassive(target))
                        {
                            return;
                        }

                        SpellManager.PredCast(W, target, true);
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
                var minions = MinionManager.GetMinions(Me.Position, Q.Range);

                if (!minions.Any())
                {
                    return;
                }

                var minion = minions.MinOrDefault(x => x.Health);

                if (LaneClearOption.UseQ && Q.IsReady())
                {
                    if (minion != null && minion.IsValidTarget(Q.Range) && minions.Count > 2)
                    {
                        Q.Cast(minion, true);
                    }
                }

                if (LaneClearOption.UseW && W.IsReady() && minion != null)
                {
                    W.Cast(minion, true);
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana)
            {
                var mobs = MinionManager.GetMinions(Me.Position, 700, MinionTypes.All, MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

                    if (mob != null)
                    {
                        if (JungleClearOption.UseW && W.IsReady())
                        {
                            W.Cast(mob);
                        }

                        if (JungleClearOption.UseQ && Q.IsReady())
                        {
                            Q.CastOnUnit(mob);
                        }

                        if (JungleClearOption.UseE && E.IsReady() && mob.IsValidTarget(E.Range) &&
                            Utils.TickCount - LastECast > 2500 && !IsAttack)
                        {
                            E.Cast(mob);
                        }
                    }
                    else if (mobs.FirstOrDefault() != null)
                    {
                        if (JungleClearOption.UseW && W.IsReady())
                        {
                            W.Cast(mobs.FirstOrDefault());
                        }

                        if (JungleClearOption.UseQ && Q.IsReady() && mobs.FirstOrDefault().IsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(mobs.FirstOrDefault());
                        }

                        if (JungleClearOption.UseE && E.IsReady() && mobs.FirstOrDefault().IsValidTarget(E.Range) &&
                            Utils.TickCount - LastECast > 2500 && !IsAttack)
                        {
                            E.Cast(mobs.FirstOrDefault());
                        }
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
                    var minion =
                        MinionManager.GetMinions(Me.Position, Q.Range)
                            .Where(x => x.IsValidTarget(Q.Range) && MinionHealthPrediction.GetHealthPrediction(x, 250) > 0)
                            .OrderBy(x => x.Health)
                            .FirstOrDefault(x => x.Health < Q.GetDamage(x));

                    if (minion != null)
                    {
                        Q.CastOnUnit(minion, true);
                    }
                }
            }
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit ArgsTarget)
        {
            if (!unit.IsMe)
            {
                return;
            }

            if (isComboMode)
            {
                var target = ArgsTarget as AIHeroClient;

                if (target != null && !target.IsDead && !target.IsZombie)
                {
                    if (ComboOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(target, true);
                    }
                    else if (ComboOption.UseW && ComboOption.GetBool("ComboWAA") && W.IsReady() &&
                        target.IsValidTarget(W.Range) && target.HasBuff("jhinespotteddebuff"))
                    {
                        SpellManager.PredCast(W, target, true);
                    }
                }
            }
            else if (((isFarmMode && MyManaManager.SpellHarass) || isHarassMode) && HarassOption.HasEnouguMana)
            {
                var target = ArgsTarget as AIHeroClient;

                if (target != null && !target.IsDead)
                {
                    if (HarassOption.UseQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(target, true);
                    }
                    else if (HarassOption.UseW && W.IsReady() && target.IsValidTarget(W.Range) && target.HasBuff("jhinespotteddebuff"))
                    {
                        SpellManager.PredCast(W, target, true);
                    }
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            var spellslot = Me.GetSpellSlot(Args.SData.Name);

            if (spellslot == SpellSlot.E)
            {
                LastECast = Utils.TickCount;
            }

            if (Orbwalker.IsAutoAttack(Args.SData.Name))
            {
                IsAttack = true;
                Core.DelayAction(() => { IsAttack = false; }, 500);
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser Args)
        {
            if (R.Instance.SpellData.Name == "JhinRShot")
            {
                return;
            }

            var target = Args.Sender;

            if (target.IsValidTarget(E.Range) && (Args.End.DistanceToPlayer() <= 300 || target.DistanceToPlayer() <= 300))
            {
                if (MiscOption.GetBool("GapE") && E.IsReady() && Utils.TickCount - LastECast > 2500 && !IsAttack)
                {
                    SpellManager.PredCast(E, target, true);
                }

                if (MiscOption.GetBool("GapW") && W.IsReady() && HasPassive(target))
                {
                    SpellManager.PredCast(W, target, true);
                }
            }
        }

        private static void AutoUse(Obj_AI_Base target)
        {
            if (Item.HasItem(3363) && Item.CanUseItem(3363))
            {
                Item.UseItem(3363, target);
            }
        }

        private static bool HasPassive(Obj_AI_Base target)
        {
            return target.HasBuff("jhinespotteddebuff");
        }

        private static bool InRCone(GameObject target)
        {
            // Asuvril
            // https://github.com/VivianGit/LeagueSharp/blob/master/Jhin%20As%20The%20Virtuoso/Jhin%20As%20The%20Virtuoso/Extensions.cs#L67-L79
            var range = R.Range;
            const float angle = 70f * (float)Math.PI / 180;
            var end2 = target.Position.To2D() - Me.Position.To2D();
            var edge1 = end2.Rotated(-angle / 2);
            var edge2 = edge1.Rotated(angle);
            var point = target.Position.To2D() - Me.Position.To2D();

            return point.Distance(new Vector2(), true) < range * range && edge1.CrossProduct(point) > 0 && point.CrossProduct(edge2) > 0;
        }

        private static bool IsSpellHeroCollision(AIHeroClient t, Spell QWER, int extraWith = 50)
        {
            foreach (
                var hero in
                ObjectManager.Heroes.Enemies.FindAll(
                    hero =>
                        hero.IsValidTarget(QWER.Range + QWER.Width, true, QWER.RangeCheckFrom) &&
                        t.NetworkId != hero.NetworkId))
            {
                var prediction = QWER.GetPrediction(hero);
                var powCalc = Math.Pow(QWER.Width + extraWith + hero.BoundingRadius, 2);

                if (
                    prediction.UnitPosition.To2D()
                        .Distance(QWER.From.To2D(), QWER.GetPrediction(t).CastPosition.To2D(), true, true) <= powCalc)
                {
                    return true;
                }

                if (prediction.UnitPosition.To2D().Distance(QWER.From.To2D(), t.ServerPosition.To2D(), true, true) <= powCalc)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Ping(Vector2 position)
        {
            if (Utils.TickCount - LastPingT < 30 * 1000)
            {
                return;
            }

            LastPingT = Utils.TickCount;
            PingLocation = position;

            SimplePing();
            Core.DelayAction(SimplePing, 150);
            Core.DelayAction(SimplePing, 300);
            Core.DelayAction(SimplePing, 400);
            Core.DelayAction(SimplePing, 800);
        }

        private static void SimplePing()
        {
            TacticalMap.ShowPing(MiscOption.GetBool("NormalPingKill") ? PingCategory.Normal : PingCategory.Fallback, PingLocation, true);
        }
    }
}
