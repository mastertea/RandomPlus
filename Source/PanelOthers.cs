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

        private Rect randomRerollAlgorithmLabelRect, randomRerollAlgorithmButtonRect;
        private Rect randomRerollLimitLabelRect, randomRerollLimitButtonRect;
        private Rect genderLabelRect, genderButtonRect;
        private Rect ageLabelRect, ageRangeRect;

        private Rect healthConditionLabelRect, healthConditionButtonRect;
        private Rect incapableLabelRect, incapableButtonRect;

        public override void Resize(Rect rect)
        {
            base.Resize(rect);
            randomRerollAlgorithmLabelRect = new Rect(18, 42, 150, 30);
            randomRerollAlgorithmButtonRect = new Rect(150, 43, 154, 20);

            randomRerollLimitLabelRect = randomRerollAlgorithmLabelRect.OffsetBy(otherLabelOffset);
            randomRerollLimitButtonRect = randomRerollAlgorithmButtonRect.OffsetBy(otherButtonOffset);

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
                Widgets.Label(randomRerollAlgorithmLabelRect, "RandomPlus.PanelOthers.RerollAlgorithmLabel".Translate());
                drawRerollAlgorithm(randomRerollAlgorithmButtonRect);

                Widgets.Label(randomRerollLimitLabelRect, "RandomPlus.PanelOthers.RerollLimitLabel".Translate());
                drawRerollLimit(randomRerollLimitButtonRect);

                Widgets.Label(genderLabelRect, "RandomPlus.PanelOthers.GenderLabel".Translate());
                drawGender(genderButtonRect);

                Widgets.Label(healthConditionLabelRect, "RandomPlus.PanelOthers.HealthOptionLabel".Translate());
                drawHealthCondition(healthConditionButtonRect);

                Widgets.Label(incapableLabelRect, "RandomPlus.PanelOthers.IncapableOptionLabel".Translate());
                drawIncapable(incapableButtonRect);

                string minAgeString = RandomSettings.PawnFilter.ageRange.min.ToString();
                string maxAgeString = (RandomSettings.PawnFilter.ageRange.max == PawnFilter.MaxAgeDefault) ? "∞" : RandomSettings.PawnFilter.ageRange.max.ToString();

                string labelText = string.Format("RandomPlus.PanelOthers.AgeLabel".Translate(),
                    minAgeString,
                    maxAgeString);
                Widgets.Label(ageLabelRect, labelText);
                Widgets.IntRange(ageRangeRect, 20, ref RandomSettings.PawnFilter.ageRange,
                    0, //PawnFilter.MinAgeDefault, 
                    PawnFilter.MaxAgeDefault,
                    "", 2);
            }
            finally
            {
                GUI.EndGroup();
            }

            GUI.color = Color.white;
        }

        private readonly static Action<Enum> rerollAlgorithmCallback = (Enum val) => RandomSettings.PawnFilter.RerollAlgorithm = (PawnFilter.RerollAlgorithmOptions)val;
        public void drawRerollAlgorithm(Rect rect)
        {
            drawButton(
                rect, 
                PawnFilter.RerollAlgorithmOptionValues[(int)RandomSettings.PawnFilter.RerollAlgorithm], 
                typeof(PawnFilter.RerollAlgorithmOptions), 
                PawnFilter.RerollAlgorithmOptionValues, 
                rerollAlgorithmCallback);
        }

        private readonly static Action<Enum> rerollLimitCallback = (Enum val) => RandomSettings.PawnFilter.RerollLimit = (int)(PawnFilter.RerollLimitOptions)val;
        public void drawRerollLimit(Rect rect)
        {
            drawButton(rect, RandomSettings.PawnFilter.RerollLimit.ToString(), typeof(PawnFilter.RerollLimitOptions), PawnFilter.RerollLimitOptionValues, rerollLimitCallback);
        }

        private readonly static Action<Enum> genderCallback = (Enum val) => RandomSettings.SetGenderFilter((Gender)val);
        public void drawGender(Rect rect)
        {
            var displayedNameArray = Enum.GetValues(typeof(Gender)).Cast<Gender>().ToList().Select((gender) => GenderUtility.GetLabel(gender)).ToArray();
            drawButton(rect, GenderUtility.GetLabel(RandomSettings.PawnFilter.Gender), typeof(Gender), displayedNameArray, genderCallback, false);
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
                    if (displayedNameArray.Length > i)
                    {
                        var displayedName = (translate) ? displayedNameArray[i].Translate().CapitalizeFirst().ToString() : displayedNameArray[i].CapitalizeFirst();
                        var menuOption = new FloatMenuOption(displayedName, () => {
                            callback?.Invoke(option);
                        });
                        options.Add(menuOption);
                    }
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}
