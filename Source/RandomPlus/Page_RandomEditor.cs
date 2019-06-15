using System;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace RandomPlus
{
    public class Page_RandomEditor : Page
    {
        PanelSkills panelSkills;
        PanelTraits panelTraits;

        public Page_RandomEditor()
        {
            this.closeOnCancel = true;
            this.closeOnAccept = true;
            this.closeOnClickedOutside = true;
            this.doCloseButton = true;
            this.doCloseX = true;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(694f, 40 + 362 + 120);
            }
        }

        public override string PageTitle
        {
            get
            {
                return "Random Editor";
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            panelSkills = new PanelSkills(RandomSettings.PawnFilter);
            panelTraits = new PanelTraits(RandomSettings.PawnFilter);
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.DrawPageTitle(inRect);
            Rect mainRect = base.GetMainRect(inRect, 30f, false);

            panelSkills.Draw();
            panelTraits.Draw();

            var randomRerollLimitLabelRect = new Rect(320 + 20, 156 + 40 + 20, 320, 20);
            Widgets.Label(randomRerollLimitLabelRect, "Random Reroll Limit: ");
            var randomRerollLimitRect = new Rect(320 + 20, 156 + 40 + 20 + 20 + 4, 55, 30);
            foreach (var option in RandomSettings.RANDOM_REROLL_LIMIT_OPTIONS)
            {
                bool isSelected = Widgets.RadioButtonLabeled(randomRerollLimitRect, option.ToString(), RandomSettings.RandomRerollLimit() == option);
                if(isSelected)
                    RandomSettings.SetRandomRerollLimit(option);
                randomRerollLimitRect = new Rect(randomRerollLimitRect);
                randomRerollLimitRect.x += randomRerollLimitRect.width + 20;
                randomRerollLimitRect.width += 6;
            }

            var ageLabelRect = randomRerollLimitLabelRect.OffsetBy(0, 68);
            ageLabelRect.height = 26;
            string labelText = string.Format("Age: {0}-{1}", RandomSettings.PawnFilter.AgeRange.min, RandomSettings.PawnFilter.AgeRange.max);
            Widgets.Label(ageLabelRect, labelText);

            var ageRangeRect = ageLabelRect.OffsetBy(0, 18);
            ageRangeRect.width = 310;
            ageRangeRect.height = 18;
            Widgets.IntRange(ageRangeRect, 20, ref RandomSettings.PawnFilter.AgeRange,
                PawnFilter.MinAgeDefault, PawnFilter.MaxAgeDefault, "", 2);

            var healthRect = ageRangeRect.OffsetBy(0, 34);
            healthRect.width = 315;
            healthRect.height = 20;
            Widgets.CheckboxLabeled(healthRect, "No Health Conditions", ref RandomSettings.PawnFilter.NoHealthConditions);

            var incapableRect = healthRect.OffsetBy(0, 24);
            Widgets.CheckboxLabeled(incapableRect, "No Incapabilities", ref RandomSettings.PawnFilter.NoIncapabilities);

            var dumbLaborRect = incapableRect.OffsetBy(0, 24);
            Widgets.CheckboxLabeled(dumbLaborRect, "No Dumb Labor Incapability", ref RandomSettings.PawnFilter.NoDumbLabor);

            var genderLabelRect = dumbLaborRect.OffsetBy(0, 30);
            genderLabelRect.width = 87;
            Widgets.Label(genderLabelRect, "Gender:");

            var genderAnyRect = genderLabelRect.OffsetBy(70, 1);
            genderAnyRect.width = 60;
            bool genderIsSelected = Widgets.RadioButtonLabeled(
                genderAnyRect, "Any", RandomSettings.PawnFilter.gender == Gender.None);
            if (genderIsSelected)
                RandomSettings.SetGenderFilter(Gender.None);

            var genderMaleRect = genderAnyRect.OffsetBy(genderAnyRect.width + 20, 0);
            genderMaleRect.width = 65;
            genderIsSelected = Widgets.RadioButtonLabeled(
                genderMaleRect, "Male", RandomSettings.PawnFilter.gender == Gender.Male);
            if (genderIsSelected)
                RandomSettings.SetGenderFilter(Gender.Male);

            var genderFemaleRect = genderMaleRect.OffsetBy(genderMaleRect.width + 20, 0);
            genderFemaleRect.width = 80;
            genderIsSelected = Widgets.RadioButtonLabeled(
                genderFemaleRect, "Female", RandomSettings.PawnFilter.gender == Gender.Female);
            if (genderIsSelected)
                RandomSettings.SetGenderFilter(Gender.Female);
        }
    }
}
