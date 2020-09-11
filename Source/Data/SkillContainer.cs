using Verse;
using RimWorld;

namespace RandomPlus
{
    public class SkillContainer : IExposable
    {
        public SkillDef skillDef;

        public Passion passion;

        private int minValue;
        public int MinValue
        {
            get { return minValue; }
            set
            {
                if (value > 20) minValue = 20;
                else if (value < 0) minValue = 0;
                else minValue = value;
            }
        }

        public SkillContainer()
        {
            passion = Passion.None;
            MinValue = 0;
        }

        public SkillContainer(SkillDef skillDef)
        {
            this.skillDef = skillDef;
            passion = Passion.None;
            MinValue = 0;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look<SkillDef>(ref skillDef, "skillDef");
            Scribe_Values.Look<Passion>(ref passion, "passion");
            Scribe_Values.Look<int>(ref minValue, "min_value");
        }
    }
}
