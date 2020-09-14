using RimWorld;
using UnityEngine;
using Verse;

namespace RandomPlus
{
    public class Page_RandomEditor : Page
    {
        PanelSkills panelSkills;
        PanelTraits panelTraits;
        PanelOthers panelOthers;

        private static readonly int ButtonWidth = 100;
        private static readonly int ButtonHeight = 20;

        Rect RectButtonResetAll;
        Rect RectButtonSaveLoad;

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
                return new Vector2(694, 40 + 362 + 150);
            }
        }

        public override string PageTitle
        {
            get
            {
                return "RandomPlus.RandomEditor.Header".Translate();
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            panelSkills = new PanelSkills();
            panelTraits = new PanelTraits();
            panelOthers = new PanelOthers();

            RectButtonResetAll = new Rect(InitialSize.x - (ButtonWidth + 50), InitialSize.y - (ButtonHeight + 520), ButtonWidth, ButtonHeight);
            RectButtonSaveLoad = new Rect(InitialSize.x - (ButtonWidth * 2 + 60), InitialSize.y - (ButtonHeight + 520), ButtonWidth, ButtonHeight);
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.DrawPageTitle(inRect);
            //Rect mainRect = base.GetMainRect(inRect, 30f, false);

            panelSkills.Draw();
            panelTraits.Draw();
            panelOthers.Draw();

            if (Widgets.ButtonText(RectButtonSaveLoad, "RandomPlus.RandomEditor.SaveLoadButton".Translate(), true, false, true))
            {
                Find.WindowStack.Add(new SaveLoadDialog());
            }

            if (Widgets.ButtonText(RectButtonResetAll, "RandomPlus.RandomEditor.ResetAllButton".Translate(), true, true, true))
            {
                RandomSettings.PawnFilter.ResetAll();
            }
        }
    }
}
