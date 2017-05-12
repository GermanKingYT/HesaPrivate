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

    internal class Jayce : MyLogic
    {
        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 1050f);
            QExtend = new Spell(SpellSlot.Q, 1650f);
            Q2 = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W);
            W2 = new Spell(SpellSlot.W, 350f);
            E = new Spell(SpellSlot.E, 650f);
            E2 = new Spell(SpellSlot.E, 240f);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 79f, 1200f, true, SkillshotType.SkillshotLine);
            QExtend.SetSkillshot(0.35f, 98f, 1900f, true, SkillshotType.SkillshotLine);
            Q2.SetTargetted(0.25f, float.MaxValue);
            E.SetSkillshot(0.1f, 120, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E2.SetTargetted(.25f, float.MaxValue);


        }
    }
}
