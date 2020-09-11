using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RandomPlus
{
    public class PanelOthers : PanelBase
    {
        private Rect mainRect = new Rect(0, 0, 500, 500);

        public PanelOthers()
        {
            Resize(new Rect(340, 240, 320, 226));
        }

        public override string PanelHeader
        {
            get
            {
                return "RandomPlus.PanelOthers.Header".Translate();
            }
        }

        private readonly static Vector2 otherLabelOffset = new Vector2(0, 24);
        private readonly static Vector2 otherButtonOffset = new Vector2(0, 24);

        private Rect randomRerollLimitLabelRect, randomRerollLimitButtonRect;
        private Rect genderLabelRect, genderButtonRect;
        private Rect ageLabelRect, ageRangeRect;

        private Rect healthConditionLabelRect, healthConditionButtonRect;
        private Rect incapableLabelRect, incapableButtonRect;

        public override void Resize(Rect rect)
        {
            base.Resize(rect);
            randomRerollLimitLabelRect = new Rect(18, 42, 150, 30);
            randomRerollLimitButtonRect = new Rect(150, 43, 154, 20);

            genderLabelRect = randomRerollLimitLabelRect.OffsetBy(otherLabelOffset);
            genderButtonRect = randomRerollLimitButtonRect.OffsetBy(otherButtonOffset);

            healthConditionLabelRect = genderLabelRect.OffsetBy(otherLabelOffset);
            healthConditionButtonRect = genderButtonRect.OffsetBy(otherButtonOffset);

            incapableLabelRect = healthConditionLabelRect.OffsetBy(otherLabelOffset);
            incapableButtonRect = healthConditionButtonRect.OffsetBy(otherButtonOffset);

            ageLabelRect = incapableLabelRect.OffsetBy(0, 28);
            ageLabelRect.width = 284;
            ageRangeRect = ageLabelRect.OffsetBy(0, 8);
        }
        
        protected override void DrawPanelContent()
        {
            base.DrawPanelContent();

            GUI.BeginGroup(mainRect);
            try
            {
                Widgets.Label(randomRerollLimitLabelRect, "RandomPlus.PanelOthers.RerollLimitLabel".Translate());
                drawRerollLimit(randomRerollLimitButtonRect);

                Widgets.Label(genderLabelRect, "RandomPlus.PanelOthers.GenderLabel".Translate());
                drawGender(genderButtonRect);

                Widgets.Label(healthConditionLabelRect, "RandomPlus.PanelOthers.HealthOptionLabel".Translate());
                drawHealthCondition(healthConditionButtonRect);

                Widgets.Label(incapableLabelRect, "RandomPlus.PanelOthers.IncapableOptionLabel".Translate());
                drawIncapable(incapableButtonRect);

                string labelText = string.Format("RandomPlus.PanelOthers.AgeLabel".Translate(),
                    RandomSettings.PawnFilter.AgeRange.min, 
                    RandomSettings.PawnFilter.AgeRange.max);
                Widgets.Label(ageLabelRect, labelText);
                Widgets.IntRange(ageRangeRect, 20, ref RandomSettings.PawnFilter.AgeRange,
                    0, //PawnFilter.MinAgeDefault, 
                    PawnFilter.MaxAgeDefault,
                    "", 2);

                //Widgets.CheckboxLabeled(healthRect, "No Health Conditions", ref RandomSettings.PawnFilter.NoHealthConditions);
                //Widgets.CheckboxLabeled(incapableRect, "No Incapabilities", ref RandomSettings.PawnFilter.NoIncapabilities);
                //Widgets.CheckboxLabeled(dumbLaborRect, "No Dumb Labor Incapability", ref RandomSettings.PawnFilter.NoDumbLabor);
            }
            finally
            {
                GUI.EndGroup();
            }

            GUI.color = Color.white;
        }

        private readonly static Action<Enum> rerollCallback = (Enum val) => RandomSettings.SetRandomRerollLimit((int)(PawnFilter.RerollLimitOptions)val);
        public void drawRerollLimit(Rect rect)
        {
            drawButton(rect, RandomSettings.RandomRerollLimit().ToString(), typeof(PawnFilter.RerollLimitOptions), PawnFilter.RerollLimitOptionValues, rerollCallback);
        }

        private readonly static Action<Enum> genderCallback = (Enum val) => RandomSettings.SetGenderFilter((Gender)val);
        public void drawGender(Rect rect)
        {
            var displayedNameArray = Enum.GetValues(typeof(Gender)).Cast<Gender>().ToList().Select((gender) => GenderUtility.GetLabel(gender)).ToArray();
            drawButton(rect, GenderUtility.GetLabel(RandomSettings.PawnFilter.gender), typeof(Gender), displayedNameArray, genderCallback, false);
        }

        private readonly static Action<Enum> healthCallback = (Enum val) => RandomSettings.PawnFilter.FilterHealthCondition = (PawnFilter.HealthOptions)val;
        public void drawHealthCondition(Rect rect)
        {
            drawButton(rect, PawnFilter.HealthOptionValues[(int)RandomSettings.PawnFilter.FilterHealthCondition], typeof(PawnFilter.HealthOptions), PawnFilter.HealthOptionValues, healthCallback);
        }

        private readonly static Action<Enum> incapableCallback = (Enum val) => RandomSettings.PawnFilter.FilterIncapable = (PawnFilter.IncapableOptions)val;
        public void drawIncapable(Rect rect)
        {
            drawButton(rect, PawnFilter.IncapableOptionValues[(int)RandomSettings.PawnFilter.FilterIncapable], typeof(PawnFilter.IncapableOptions), PawnFilter.IncapableOptionValues, incapableCallback);
        }

        public void drawButton(Rect rect, string label, Type enumOptionType, string[] displayedNameArray, Action<Enum> callback, bool translate = true)
        {
            string displayLabel = (translate) ? label.Translate().CapitalizeFirst().ToString() : label.CapitalizeFirst();
            if (Widgets.ButtonText(rect, displayLabel, true, true, true))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                var enumOptions = Enum.GetValues(enumOptionType).Cast<Enum>().ToArray();
                for (int i=0; i < enumOptions.Length; i++)
                {
                    var option = enumOptions[i];
                    var displayedName = (translate) ? displayedNameArray[i].Translate().CapitalizeFirst().ToString() : displayedNameArray[i].CapitalizeFirst();
                    var menuOption = new FloatMenuOption(displayedName, () => {
                        callback?.Invoke(option);
                    });
                    options.Add(menuOption);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}
