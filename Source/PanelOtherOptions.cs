using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RandomPlus
{
    public class PanelOtherOptions : PanelBase
    {
        private Rect mainRect = new Rect(0, 0, 500, 500);

        public PanelOtherOptions()
        {
            Resize(new Rect(340, 240, 320, 226));
        }

        public override string PanelHeader
        {
            get
            {
                return "Other Options";
            }
        }

        private Rect randomRerollLimitLabelRect;
        private Rect randomRerollLimitButtonRect;

        private Rect genderLabelRect;
        private Rect genderButtonRect;

        private Rect ageLabelRect;
        private Rect ageRangeRect;

        private Rect healthRect;
        private Rect incapableRect;
        private Rect dumbLaborRect;

        public override void Resize(Rect rect)
        {
            base.Resize(rect);
            randomRerollLimitLabelRect = new Rect(18, 42, 200, 30);
            randomRerollLimitButtonRect = new Rect(200, 43, 104, 20);

            genderLabelRect = randomRerollLimitLabelRect.OffsetBy(0, 24);
            genderButtonRect = randomRerollLimitButtonRect.OffsetBy(0, 24);

            ageLabelRect = genderLabelRect.OffsetBy(0, 28);
            ageLabelRect.width = 284;
            ageRangeRect = ageLabelRect.OffsetBy(0, 8);

            healthRect = ageRangeRect.OffsetBy(0, 38);
            healthRect.height = 20;
            incapableRect = healthRect.OffsetBy(0, 24);
            dumbLaborRect = incapableRect.OffsetBy(0, 24);
        }
        
        protected override void DrawPanelContent()
        {
            base.DrawPanelContent();

            GUI.BeginGroup(mainRect);
            try
            {
                Widgets.Label(randomRerollLimitLabelRect, "Random Reroll Limit: ");
                drawRerollLimit(randomRerollLimitButtonRect);

                Widgets.Label(genderLabelRect, "Gender Filter: ");
                drawGender(genderButtonRect);

                string labelText = string.Format("Age: {0} - {1}",
                    RandomSettings.PawnFilter.AgeRange.min, 
                    RandomSettings.PawnFilter.AgeRange.max);
                Widgets.Label(ageLabelRect, labelText);
                Widgets.IntRange(ageRangeRect, 20, ref RandomSettings.PawnFilter.AgeRange,
                    0, //PawnFilter.MinAgeDefault, 
                    PawnFilter.MaxAgeDefault,
                    "", 2);

                Widgets.CheckboxLabeled(healthRect, "No Health Conditions", ref RandomSettings.PawnFilter.NoHealthConditions);
                Widgets.CheckboxLabeled(incapableRect, "No Incapabilities", ref RandomSettings.PawnFilter.NoIncapabilities);
                Widgets.CheckboxLabeled(dumbLaborRect, "No Dumb Labor Incapability", ref RandomSettings.PawnFilter.NoDumbLabor);
            }
            finally
            {
                GUI.EndGroup();
            }

            GUI.color = Color.white;
        }

        public void drawRerollLimit(Rect rect)
        {
            if (Widgets.ButtonText(rect, RandomSettings.RandomRerollLimit().ToString(), true, true, true))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var limitOption in RandomSettings.RANDOM_REROLL_LIMIT_OPTIONS)
                {
                    var menuOption = new FloatMenuOption(limitOption.ToString(), () => {
                        RandomSettings.SetRandomRerollLimit(limitOption);
                    });
                    options.Add(menuOption);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public void drawGender(Rect rect)
        {
            if (Widgets.ButtonText(rect, RandomSettings.PawnFilter.gender.ToString(), true, true, true))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var genderOption in Enum.GetValues(typeof(Gender)))
                {
                    var menuOption = new FloatMenuOption(genderOption.ToString(), () => {
                        RandomSettings.SetGenderFilter((Gender)genderOption);
                    });
                    options.Add(menuOption);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}
