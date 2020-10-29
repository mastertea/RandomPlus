using Verse;
using RimWorld;
using System;
using System.Runtime.Remoting.Messaging;

namespace RandomPlus
{
    public class SkillContainer : IExposable
    {
        private SkillDef skillDef;
        public SkillDef SkillDef {
            get => skillDef;
            set {
                skillDef = value;
                OnChange?.Invoke();
            }
        }


        private Passion passion;
        public Passion Passion
        {
            get => passion;
            set {
                passion = value;
                OnChange?.Invoke();
            }
        }

        private int minValue;
        public int MinValue
        {
            get { return minValue; }
            set
            {
                if (value > 20) minValue = 20;
                else if (value < 0) minValue = 0;
                else minValue = value;
                OnChange?.Invoke();
            }
        }

        private Action OnChange;

        public SkillContainer()
        {
            passion = Passion.None;
            MinValue = 0;
        }

        public SkillContainer(SkillDef skillDef, Action OnChange)
        {
            this.skillDef = skillDef;
            passion = Passion.None;
            MinValue = 0;
            this.OnChange = OnChange;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look<SkillDef>(ref skillDef, "skillDef");
            Scribe_Values.Look<Passion>(ref passion, "passion");
            Scribe_Values.Look<int>(ref minValue, "min_value");
        }
    }
}
