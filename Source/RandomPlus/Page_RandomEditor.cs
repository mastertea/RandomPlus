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

            panelSkills = new PanelSkills(RandomSettings.PawnFilter);
            panelTraits = new PanelTraits(RandomSettings.PawnFilter);
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(694f, 40 + 362 + 100);
            }
        }

        public override string PageTitle
        {
            get
            {
                return "Random Editor";
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            Rect mainRect = base.GetMainRect(inRect, 30f, false);

            panelSkills.Draw();
            panelTraits.Draw();

            var randomRerollLimitLabelRect = new Rect(320 + 20, 156 + 40 + 20, 320, 20);
            Widgets.Label(randomRerollLimitLabelRect, "Random Reroll Limit: ");
            var randomRerollLimitRect = new Rect(320 + 20, 156 + 40 + 20 + 20 + 4, 60, 30);
            foreach (var option in RandomSettings.RANDOM_REROLL_LIMIT_OPTIONS)
            {
                bool isSelected = Widgets.RadioButtonLabeled(randomRerollLimitRect, option.ToString(), RandomSettings.RandomRerollLimit() == option);
                if(isSelected)
                    RandomSettings.SetRandomRerollLimit(option);
                randomRerollLimitRect = new Rect(randomRerollLimitRect);
                randomRerollLimitRect.x += 60 + 20;
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
            healthRect.width = 320;
            healthRect.height = 20;
            Widgets.CheckboxLabeled(healthRect, "No Health Conditions", ref RandomSettings.PawnFilter.NoHealthConditions);

            var dumbLaborRect = healthRect.OffsetBy(0, 24);
            Widgets.CheckboxLabeled(dumbLaborRect, "No Dumb Labor", ref RandomSettings.PawnFilter.NoDumbLabor);

            

            
        }
    }
}
