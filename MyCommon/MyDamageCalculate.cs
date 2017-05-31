namespace Flowers_ADCSeries.MyCommon
{
    using HesaEngine.SDK;
    using HesaEngine.SDK.Enums;
    using HesaEngine.SDK.GameObjects;

    internal class MyDamageCalculate
    {
        internal static float GetComboDamage(AIHeroClient target)
        {
            if (target == null || target.IsDead || target.IsZombie)
                return 0;

            var damage = 0d;

            if (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod >
                ObjectManager.Player.TotalMagicalDamage)
                damage += ObjectManager.Player.GetAutoAttackDamage(target);

            if (ObjectManager.Player.GetSpellSlot("SummonerDot") != SpellSlot.Unknown &&
                ObjectManager.Player.GetSpellSlot("SummonerDot").IsReady())
                damage += GetIgniteDmage(target);

            damage += GetQDamage(target);
            damage += GetWDamage(target);
            damage += GetEDamage(target);
            damage += GetRDamage(target);

            if (ObjectManager.Player.HasBuff("SummonerExhaust"))
                damage = damage * 0.6f;

            if (target.CharData.BaseSkinName == "Moredkaiser")
                damage -= target.Mana;

            if (target.HasBuff("GarenW"))
                damage = damage * 0.7f;

            if (target.HasBuff("ferocioushowl"))
                damage = damage * 0.7f;

            if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
                damage -= target.Mana / 2f;

            return (float) damage;
        }

        internal static float GetQDamage(Obj_AI_Base target)
        {
            if (ObjectManager.Player.GetSpell(SpellSlot.Q).Level == 0 ||
                !Extensions.IsReady(ObjectManager.Player.GetSpell(SpellSlot.Q)))
                return 0f;

            return (float) ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
        }

        internal static float GetWDamage(Obj_AI_Base target)
        {
            return (float) ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
        }

        internal static float GetEDamage(Obj_AI_Base target)
        {
            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0 ||
                !Extensions.IsReady(ObjectManager.Player.GetSpell(SpellSlot.E)))
                return 0f;

            return (float) GetPassiveDMG(target);
        }

        internal static float GetRDamage(Obj_AI_Base target, bool ignoreR = false, int CollsionCount = 0)
        {
            if (ObjectManager.Player.GetSpell(SpellSlot.R).Level == 0 ||
                !Extensions.IsReady(ObjectManager.Player.GetSpell(SpellSlot.R)))
                return 0f;

            var rAmmo = new float[] {20, 25, 30}[ObjectManager.Player.GetSpell(SpellSlot.R).Level - 1];
            var rDMG = new double[] {20, 35, 50}[ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level - 1] +
                       0.1 * ObjectManager.Player.TotalMagicalDamage + 0.2 * ObjectManager.Player.FlatPhysicalDamageMod;
            var result = ObjectManager.Player.CalcDamage(target, Damage.DamageType.Magical, rDMG);

            return (float) (result * rAmmo);
        }

        internal static float GetIgniteDmage(Obj_AI_Base target)
        {
            return 50 + 20 * ObjectManager.Player.Level - target.HPRegenRate / 5 * 3;
        }

        internal static double GetPassiveDMG(Obj_AI_Base target)
        {
            if (ObjectManager.Player.Level >= 13)
                return ObjectManager.Player.GetAutoAttackDamage(target) +
                       0.6 * ObjectManager.Player.GetAutoAttackDamage(target);

            if (ObjectManager.Player.Level >= 7)
                return ObjectManager.Player.GetAutoAttackDamage(target) +
                       0.5 * ObjectManager.Player.GetAutoAttackDamage(target);

            return ObjectManager.Player.GetAutoAttackDamage(target) +
                   0.4 * ObjectManager.Player.GetAutoAttackDamage(target);
        }
    }
}