using RimWorld;
using UnityEngine;
using Verse;

namespace RandomPlus
{
    public class Page_RandomEditor : Page
    {
        public static bool MOD_WELL_MET = false;

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
                return new Vector2(694, 40 + 590);
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
            if (ModsConfig.IsActive("Lakuna.WellMet"))
                MOD_WELL_MET = true;

            panelSkills = new PanelSkills();
            panelTraits = new PanelTraits();
            panelOthers = new PanelOthers();

            RectButtonResetAll = new Rect(InitialSize.x - (ButtonWidth + 50), ButtonHeight - 8, ButtonWidth, ButtonHeight);
            RectButtonSaveLoad = new Rect(InitialSize.x - (ButtonWidth * 2 + 60), ButtonHeight - 8, ButtonWidth, ButtonHeight);
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.DrawPageTitle(inRect);

            if (Prefs.DevMode && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.UpArrow)
            {
                GenCommandLine.Restart();
            }

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
