﻿using RimWorld;
using UnityEngine;

namespace RandomPlus
{
    public class Page_RandomEditor : Page
    {
        PanelSkills panelSkills;
        PanelTraits panelTraits;
        PanelOtherOptions panelOthers;

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
                return "Random Editor";
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            panelSkills = new PanelSkills();
            panelTraits = new PanelTraits();
            panelOthers = new PanelOtherOptions();
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.DrawPageTitle(inRect);
            Rect mainRect = base.GetMainRect(inRect, 30f, false);

            panelSkills.Draw();
            panelTraits.Draw();
            panelOthers.Draw();
            
        }
    }
}
