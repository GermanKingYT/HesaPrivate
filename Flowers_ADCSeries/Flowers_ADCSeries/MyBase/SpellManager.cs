using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HesaEngine.SDK;
using HesaEngine.SDK.GameObjects;

namespace Flowers_ADCSeries.MyBase
{
    public static class SpellManager
    {
        public static void PredCast(Spell spell, Obj_AI_Base target, bool isAOE = false)
        {
            if (!spell.IsReady())
            {
                return;
            }

            var pred = spell.GetPrediction(target, isAOE);

            if (pred.Hitchance >= HitChance.VeryHigh)
            {
                spell.Cast(pred.CastPosition, true);
            }
        }
    }
}
