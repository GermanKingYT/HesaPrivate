namespace Flowers_ADCSeries.MyCommon
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    using SharpDX;

    internal static class MyExtensions
    {
        //Distance Extensions
        internal static float DistanceToPlayer(this Obj_AI_Base source)
        {
            return ObjectManager.Player.Distance(source);
        }

        internal static float DistanceToPlayer(this Vector3 position)
        {
            return position.To2D().DistanceToPlayer();
        }

        internal static float DistanceToPlayer(this Vector2 position)
        {
            return ObjectManager.Player.Distance(position);
        }

        internal static float DistanceToMouse(this Obj_AI_Base source)
        {
            return Game.CursorPosition.Distance(source.Position);
        }

        internal static float DistanceToMouse(this Vector3 position)
        {
            return position.To2D().DistanceToMouse();
        }

        internal static float DistanceToMouse(this Vector2 position)
        {
            return Game.CursorPosition.Distance(position.To3D());
        }

        internal static bool HaveShiled(this AIHeroClient target)
        {
            if (target == null || target.IsDead || target.Health <= 0)
                return false;

            if (target.HasBuff("BlackShield"))
                return true;

            if (target.HasBuff("bansheesveil"))
                return true;

            if (target.HasBuff("SivirE"))
                return true;

            if (target.HasBuff("NocturneShroudofDarkness"))
                return true;

            if (target.HasBuff("itemmagekillerveil"))
                return true;

            if (target.HasBuffOfType(BuffType.SpellShield))
                return true;

            return false;
        }

        internal static bool CanMoveMent(this AIHeroClient target)
        {
            return !(target.MovementSpeed < 50) && !target.IsStunned && !target.HasBuffOfType(BuffType.Stun) &&
                   !target.HasBuffOfType(BuffType.Fear) && !target.HasBuffOfType(BuffType.Snare) &&
                   !target.HasBuffOfType(BuffType.Knockup) && !target.HasBuff("Recall") &&
                   !target.HasBuffOfType(BuffType.Knockback)
                   && !target.HasBuffOfType(BuffType.Charm) && !target.HasBuffOfType(BuffType.Taunt) &&
                   !target.HasBuffOfType(BuffType.Suppression) && (!target.Spellbook.IsChanneling
                                                                   || target.IsMoving) &&
                   !target.HasBuff("zhonyasringshield") && !target.HasBuff("bardrstasis");
        }

        internal static bool IsUnKillable(this AIHeroClient target)
        {
            if (target == null || target.IsDead || target.Health <= 0)
                return true;

            if (target.HasBuff("KindredRNoDeathBuff"))
                return true;

            if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3 &&
                target.Health <= target.MaxHealth * 0.10f)
                return true;

            if (target.HasBuff("JudicatorIntervention"))
                return true;

            if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3 &&
                target.Health <= target.MaxHealth * 0.10f)
                return true;

            if (target.HasBuff("VladimirSanguinePool"))
                return true;

            if (target.HasBuff("ShroudofDarkness"))
                return true;

            if (target.HasBuff("SivirShield"))
                return true;

            if (target.HasBuff("itemmagekillerveil"))
                return true;

            return target.HasBuff("FioraW");
        }

        internal static Color ToSharpDX(this System.Drawing.Color color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }
    }
}