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
    using System.Collections.Generic;
    using System.Linq;

    using static MyCommon.MyMenuExtensions;

    internal class Caitlyn : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 1250f);
            W = new Spell(SpellSlot.W, 800f);
            E = new Spell(SpellSlot.E, 750f);
            R = new Spell(SpellSlot.R, 2000f);

            Q.SetSkillshot(0.50f, 50f, 2000f, false, SkillshotType.SkillshotLine);
			W.SetSkillshot(1.00f, 100f, 800f, false, SkillshotType.SkillshotCircle);			
            E.SetSkillshot(0.25f, 60f, 1600f, true, SkillshotType.SkillshotLine);

            ComboOption.AddQ();
            ComboOption.AddSlider("ComboQCount", "Use Q| Min Hit Target Count >= x(0 = off)", 3, 0, 5);
            ComboOption.AddSlider("ComboQRange", "Use Q| Min Cast Range >= x", 800, 500, 1100);
            ComboOption.AddW();
            ComboOption.AddSlider("ComboWCount", "Use W| Min Buff Count >= x", 1, 1, 3);
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSafe", "Use R| Safe Check");
            ComboOption.AddSlider("ComboRRange", "Use R| Min Cast Range >= x", 900, 500, 1500);

            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddQ();
            JungleClearOption.AddMana();

            LastHitOption.AddNothing();

            FleeOption.AddE();
            FleeOption.AddMove(false);

            KillStealOption.AddQ();

            MiscOption.AddQ();
            MiscOption.AddBool("AutoQ", "Auto Q| CC");
            MiscOption.AddW();
            MiscOption.AddBool("AutoWCC", "Auto Q| CC");
            MiscOption.AddBool("AutoWTP", "Auto Q| Teleport");
            MiscOption.AddE();
            MiscOption.AddBool("AntiAlistar", "Anti Alistar W");
            MiscOption.AddBool("AntiRengar", "Anti Rengar Jump");
            MiscOption.AddBool("AntiKhazix", "Anti Khazix R");
            MiscOption.AddBool("Gapcloser", "Anti Gapcloser");
            MiscOption.AddGapcloserTargetList();
            MiscOption.AddR();
            MiscOption.AddKey("SemiR", "Semi-manual R Key", SharpDX.DirectInput.Key.T);
            MiscOption.AddSetting("EQ");
            MiscOption.AddKey("EQKey", "One Key EQ target", SharpDX.DirectInput.Key.G);

            DrawOption.AddQ();
            DrawOption.AddW();
            DrawOption.AddE();
            DrawOption.AddR();
            DrawOption.AddFarm();
            DrawOption.AddEvent();

            Game.OnUpdate += OnUpdate;
            GameObject.OnCreate += OnCreate;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling()) { return; }

            R.Range = 500 * R.Level + 1500;

            if (isFleeMode)
            {
                Flee();

                if (FleeOption.DisableMove)
                {
                    Orbwalker.Move = false;
                }

                return;
            }

            Orbwalker.Move = true;

            if (MiscOption.GetKey("EQKey")) { OneKeyEQ(); }

            if (MiscOption.GetKey("SemiR") && R.IsReady()) { OneKeyCastR(); }

            Auto();
            KillSteal();

            if (isComboMode) { Combo(); }

            if (isHarassMode) { Harass(); }

            if (isFarmMode)
            {
                FarmHarass();

                if (isLaneClearMode) { LaneClear(); } 

                if (isJungleClearMode) { JungleClear(); }
            }
        }


        private static void OneKeyCastR()
        {
            var _SelectedTarget = TargetSelector.GetSelectedTarget();
            var _Target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

            if (_SelectedTarget != null && _SelectedTarget.IsValidTarget(R.Range))
            {
                R.CastOnUnit(_SelectedTarget);
            }
            else if (_Target != null && _Target.IsValidTarget(R.Range))
            {
                R.CastOnUnit(_Target);
            }
        }

        private static void Auto()
        {
            if (MiscOption.GetBool("AutoQ") && Q.IsReady() && !isComboMode && !isHarassMode)
            {
                var _Target = TargetSelector.GetTarget(Q.Range - 30, TargetSelector.DamageType.Physical);
               if (_Target.IsValidTarget(Q.Range) && !_Target.CanMoveMent())
                {
                    SpellManager.PredCast(Q, _Target);
                }
            }

            if (W.IsReady())
            {
                if (MiscOption.GetBool("AutoWCC"))
                {
                    foreach (var _Target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(W.Range) && !x.CanMoveMent() && !x.HasBuff("caitlynyordletrapinternal")))
                    {
                        if (Utils.TickCount - LastCastTickW > 1500)
                        {
                            W.Cast(_Target.Position, true);
                        }
                    }
                }

                if (MiscOption.GetBool("AutoWTP"))
                {
                    var _Object = ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(x => !x.IsAlly 
                        && !x.IsMe 
                        && x.DistanceToPlayer() <= W.Range 
                        && x.Buffs.Any(a =>a.Name.ToLower().Contains("teleport") || a.Name.ToLower().Contains("gate")) 
                        && !ObjectManager.Get<Obj_AI_Base>().Any(b => b.Name.ToLower().Contains("trap") 
                        && b.Distance(x) <= 150));

                    if (_Object != null)
                    {
                        if (Utils.TickCount - LastCastTickW > 1500)
                        {
                            W.Cast(_Object.Position, true);
                        }
                    }
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.IsReady())
            {
                foreach (var _Target in ObjectManager.Heroes.Enemies.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x)))
                {
                    if (Orbwalker.InAutoAttackRange(_Target) && _Target.Health <= Me.GetAutoAttackDamage(_Target, true))
                    {
                        if (!_Target.IsUnKillable())
                        {
                            SpellManager.PredCast(Q, _Target);
                        }
                    }     
                }
            }
        }

        private static void Combo()
        {
            var _Target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (_Target.IsValidTarget(R.Range))
            {
                if (ComboOption.UseE && E.IsReady() && _Target.IsValidTarget(700))
                {
                    var ePred = E.GetPrediction(_Target);

                    if (ePred.CollisionObjects.Count == 0 || ePred.Hitchance >= HitChance.VeryHigh)
                    {
                        if (ComboOption.UseQ && Q.IsReady())
                        {
                            if (E.Cast(_Target).IsCasted())
                            {
                                Q.Cast(_Target);
                            }
                        }
                        else
                        {
                            E.Cast(_Target);
                        }
                    }
                    else
                    {
                        if (ComboOption.UseQ && Q.IsReady() && _Target.IsValidTarget(Q.Range))
                        {
                            if (_Target.DistanceToPlayer() >= ComboOption.GetSlider("ComboQRange"))
                            {
                                SpellManager.PredCast(Q, _Target);
                                if (ComboOption.GetSlider("ComboQCount") != 0 && Me.CountEnemiesInRange(Q.Range) >= ComboOption.GetSlider("ComboQCount"))
                                {
                                    Q.CastIfWillHit(_Target, ComboOption.GetSlider("ComboQCount"), true);
                                }
                            }
                        }
                    }
                }

                if (ComboOption.UseQ && Q.IsReady() && !E.IsReady() && _Target.IsValidTarget(Q.Range) && _Target.DistanceToPlayer() >= ComboOption.GetSlider("ComboQRange"))
                {
                    if (_Target.DistanceToPlayer() >= ComboOption.GetSlider("ComboQRange"))
                    {
                        SpellManager.PredCast(Q, _Target);
                        if (ComboOption.GetSlider("ComboQCount") != 0 && Me.CountEnemiesInRange(Q.Range) >= ComboOption.GetSlider("ComboQCount"))
                        {
                            Q.CastIfWillHit(_Target, ComboOption.GetSlider("ComboQCount"), true);
                        }
                    }
                }

                if (ComboOption.UseW && W.IsReady() && _Target.IsValidTarget(W.Range) && W.Instance.SpellData.SpellDataInfos.AmmoUsed >= ComboOption.GetSlider("ComboWCount"))
                {
                    if (Utils.TickCount - LastCastTickW > 1500)
                    {
                        if (_Target.IsFacing(Me))
                        {
                            if (_Target.IsMelee() && _Target.DistanceToPlayer() < _Target.AttackRange + 100)
                            {
                                W.Cast(Me.Position, true);
                            }
                            else
                            {
                                var _PredictionW = W.GetPrediction(_Target);
                                if (_PredictionW.Hitchance >= HitChance.VeryHigh && _Target.IsValidTarget(W.Range))
                                {
                                    W.Cast(_PredictionW.CastPosition, true);
                                }
                            }
                        }
                        else
                        {
                            var _PredictionW = W.GetPrediction(_Target);
                            if (_PredictionW.Hitchance >= HitChance.VeryHigh && _Target.IsValidTarget(W.Range))
                            {
                                W.Cast(_PredictionW.CastPosition + Vector3.Normalize(_Target.ServerPosition - Me.ServerPosition) * 100, true);
                            }
                        }
                    }
                }

                if (ComboOption.UseR && R.IsReady() && Utils.TickCount - LastCastTickQ > 2500)
                {
                    if (ComboOption.GetBool("ComboRSafe")) { if (!Me.UnderTurret(true)) { return; } }

                    if (!_Target.IsValidTarget(R.Range)) { return; }

                    if (_Target.DistanceToPlayer() < ComboOption.GetSlider("ComboRRange")) { return; }

                    if (_Target.Health + _Target.HPRegenRate * 3 > R.GetDamage(_Target)) { return; }

                    if (HesaEngine.SDK.Collision.GetCollision
                    (
                        new List<Vector3>
                        {
                            _Target.ServerPosition
                        }, 
                        new PredictionInput
                        {
                            Delay = R.Delay,
                            Radius = R.Width,
                            Speed = R.Speed,
                            Unit = Me,
                            UseBoundingRadius = true,
                            Collision = true,
                            CollisionObjects = new[] 
                            {
                                CollisionableObjects.Heroes,
                                CollisionableObjects.YasuoWall
                            }
                        }
                    ).Any(x => x.NetworkId != _Target.NetworkId)) { R.CastOnUnit(_Target, true); }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana)
            {
                if (HarassOption.UseQ && Q.IsReady())
                {
                    var _Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true, ObjectManager.Heroes.Enemies.Where(x => !HarassOption.GetHarassTarget(x.ChampionName)));
                    if (_Target.IsValidTarget(Q.Range))
                    {
                        SpellManager.PredCast(Q, _Target);
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
                    var _Minions = MinionManager.GetMinions(Me.Position, Q.Range);
                    if (_Minions.Any())
                    {
                        var _FarmPredictionQ = MinionManager.GetBestLineFarmLocation(_Minions.Select(x => x.Position.To2D()).ToList(), Q.Width, Q.Range);
                        if (_FarmPredictionQ.MinionsHit >= LaneClearOption.GetSlider("LaneClearQCount"))
                        {
                            Q.Cast(_FarmPredictionQ.Position, true);
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
                    var _Mobs = MinionManager.GetMinions(Me.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                    if (_Mobs.Any())
                    {
                        Q.Cast(_Mobs.FirstOrDefault(), true);
                    }
                }
            }
        }

        private static void Flee()
        {
            if (FleeOption.UseE && E.IsReady())
            {
                E.Cast(Me.Position - (Game.CursorPosition - Me.Position), true);
            }
        }

        private static void OneKeyEQ()
        {
            Orbwalker.MoveTo(Game.CursorPosition);

            if (E.IsReady() && Q.IsReady())
            {
                var _Target = TargetSelector.GetSelectedTarget() ?? TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (_Target.IsValidTarget(E.Range))
                {
                    if (E.GetPrediction(_Target).CollisionObjects.Count == 0 && E.CanCast(_Target))
                    {
                        E.Cast(_Target);
                        SpellManager.PredCast(Q, _Target);
                    }
                }
            }
        }

        private static void OnCreate(GameObject sender, EventArgs Args)
        {
            var _Rengar = ObjectManager.Heroes.Enemies.Find(heros => heros.ChampionName.Equals("Rengar"));
            var _Khazix = ObjectManager.Heroes.Enemies.Find(heros => heros.ChampionName.Equals("Khazix"));

            if (_Rengar != null && MiscOption.GetBool("AntiRengar"))
            {
                if (sender.Name == "Rengar_LeapSound.troy" && sender.Position.Distance(Me.Position) < E.Range)
                {
                    E.Cast(_Rengar.Position, true);
                }
            }

            if (_Khazix != null && MiscOption.GetBool("AntiKhazix"))
            {
                if (sender.Name == "Khazix_Base_E_Tar.troy" && sender.Position.Distance(Me.Position) <= 300)
                {
                    E.Cast(_Khazix.Position, true);
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser Args)
        {
            if (E.IsReady())
            {
                if (MiscOption.GetBool("AntiAlistar") && Args.Sender.ChampionName == "Alistar" && Args.SkillType == GapcloserType.Targeted)
                {
                    E.Cast(Args.Sender.Position, true);
                }

                if (MiscOption.GetBool("Gapcloser") && MiscOption.GetGapcloserTarget(Args.Sender.ChampionName))
                {
                    if (Args.Sender.DistanceToPlayer() <= 200 && Args.Sender.IsValid())
                    {
                        E.Cast(Args.Sender.Position, true);
                    }
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (!sender.IsMe) { return; }

            if (Args.SData.Name == Q.Instance.SpellData.Name)
            {
                LastCastTickQ = Utils.TickCount;
            }

            if (Args.SData.Name == W.Instance.SpellData.Name)
            {
                LastCastTickW = Utils.TickCount;
            }
        }
    }
}
