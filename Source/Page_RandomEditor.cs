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

        // RimWorld 1.6: Scale-aware UI constants
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
                // RimWorld 1.6: Apply UI scaling for Unity 2022.3 compatibility
                // Doesn't work so quick fix set multipler to 1
                //float uiScale = Prefs.UIScale;

                float uiScale = 1;

                return new Vector2(694f * uiScale, (40f + 590f) * uiScale);
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

            // RimWorld 1.6: Scale-aware button positioning
            float uiScale = Prefs.UIScale;
            RectButtonResetAll = new Rect(
                (InitialSize.x / uiScale) - (ButtonWidth + 50), 
                ButtonHeight - 8, 
                ButtonWidth, 
                ButtonHeight
            );
            RectButtonSaveLoad = new Rect(
                (InitialSize.x / uiScale) - (ButtonWidth * 2 + 60), 
                ButtonHeight - 8, 
                ButtonWidth, 
                ButtonHeight
            );
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.DrawPageTitle(inRect);

            // RimWorld 1.6: Enhanced developer mode check
            if (Prefs.DevMode && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.UpArrow)
            {
                try
                {
                    GenCommandLine.Restart();
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"RandomPlus: Failed to restart via GenCommandLine: {ex.Message}");
                }
            }

            // Draw panels
            try
            {
                panelSkills?.Draw();
                panelTraits?.Draw();
                panelOthers?.Draw();
            }
            catch (System.Exception ex)
            {
                Log.Error($"RandomPlus: Error drawing panels: {ex.Message}");
            }

            // RimWorld 1.6: Safe button drawing with null checks
            try
            {
                if (Widgets.ButtonText(RectButtonSaveLoad, "RandomPlus.RandomEditor.SaveLoadButton".Translate(), true, false, true))
                {
                    Find.WindowStack.Add(new SaveLoadDialog());
                }

                if (Widgets.ButtonText(RectButtonResetAll, "RandomPlus.RandomEditor.ResetAllButton".Translate(), true, true, true))
                {
                    RandomSettings.PawnFilter?.ResetAll();
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"RandomPlus: Error drawing buttons: {ex.Message}");
            }
        }

        // RimWorld 1.6: Override for better window management
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            
            // Handle UI scale changes dynamically
            if (Event.current.type == EventType.Layout)
            {
                float uiScale = Prefs.UIScale;
                RectButtonResetAll = new Rect(
                    (InitialSize.x / uiScale) - (ButtonWidth + 50), 
                    ButtonHeight - 8, 
                    ButtonWidth, 
                    ButtonHeight
                );
                RectButtonSaveLoad = new Rect(
                    (InitialSize.x / uiScale) - (ButtonWidth * 2 + 60), 
                    ButtonHeight - 8, 
                    ButtonWidth, 
                    ButtonHeight
                );
            }
        }
    }
}
