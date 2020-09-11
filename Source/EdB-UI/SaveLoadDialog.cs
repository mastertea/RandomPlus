using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace RandomPlus
{
    public class SaveLoadDialog : Window
    {
        protected const float DeleteButtonSpace = 5;
        protected const float MapDateExtraLeftMargin = 220;

        private static readonly Color ManualSaveTextColor = new Color(1, 1, 0.6f);
        private static readonly Color AutosaveTextColor = new Color(0.75f, 0.75f, 0.75f);

        protected const float MapEntrySpacing = 8;
        protected const float BoxMargin = 20;
        protected const float MapNameExtraLeftMargin = 15;
        protected const float MapEntryMargin = 6;

        private Vector2 scrollPosition = Vector2.zero;

        //protected string interactButLabel = "Error";
        //protected float bottomAreaHeight;

        protected static string Filename = "";
        private bool focusedColonistNameArea;

        private int selectedIndex = -1;

        public SaveLoadDialog()
        {
            this.closeOnCancel = true;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;

            SaveLoader.LoadAll();
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(600, 400);
            }
        }

        public override void PostClose()
        {
            GUI.FocusControl(null);
        }

        public override void DoWindowContents(Rect inRect)
        {
            int padding = 16;
            int scrollBarWidth = 20;
            Vector2 buttonSize = new Vector2(150, 30);
            Vector2 rowSize = new Vector2(inRect.width - buttonSize.x - padding - scrollBarWidth, 36);

            List<PawnFilter> list = RandomSettings.pawnFilterList.ToList();
            float listHeight = list.Count * rowSize.y;
            Rect listViewRect = new Rect(0, 0, rowSize.x, listHeight);

            inRect.height -= 40;

            Rect outRect = new Rect(0, 0, inRect.width - buttonSize.x - padding, inRect.height - buttonSize.y - padding - 20);
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, listViewRect);
            try
            {
                float num2 = 0;
                int num3 = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    PawnFilter current = list[i];
                    Rect rect = new Rect(0, num2, rowSize.x, rowSize.y);
                    if (selectedIndex == i)
                    {
                        GUI.DrawTexture(rect, Textures.TextureHighlightRow);
                    }
                    else if (num3 % 2 == 0)
                    {
                        GUI.DrawTexture(rect, Textures.TextureAlternateRow);
                    }

                    Color color = selectedIndex == i ? new Color(0.7f, 0.7f, 0.7f, 1) : Color.white;
                    if (Widgets.ButtonText(rect, "", false, true, color))
                    {
                        selectedIndex = i;
                        Filename = current.name;
                    }

                    Rect innerRect = new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6);

                    GUI.BeginGroup(innerRect);
                    try
                    {
                        GUI.color = ManualSaveTextColor;
                        Rect rect2 = new Rect(15, 0, rowSize.x, rowSize.y);
                        Text.Anchor = TextAnchor.MiddleLeft;
                        Text.Font = GameFont.Small;
                        Widgets.Label(rect2, current.name);
                        GUI.color = Color.white;
                    }
                    finally
                    {
                        GUI.EndGroup();
                    }
                    num2 += rowSize.y;
                    num3++;
                }
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }

            Rect buttonAreaRect = new Rect(listViewRect.x + listViewRect.width + padding + scrollBarWidth, 0, buttonSize.x, outRect.height);
            GUI.BeginGroup(buttonAreaRect);
            try
            {
                if (selectedIndex == -1)
                    GUI.enabled = false;

                Rect loadButtonRect = new Rect(0, 0, buttonSize.x, buttonSize.y);
                if (Widgets.ButtonText(loadButtonRect, "RandomPlus.SaveLoadDialog.LoadButton".Translate(), true, false, true))
                {
                    SaveLoader.Load(RandomSettings.pawnFilterList[selectedIndex]);
                    Close();
                }

                Rect deleteButtonRect = loadButtonRect.OffsetBy(new Vector2(0, buttonSize.y + padding));
                if (Widgets.ButtonText(deleteButtonRect, "RandomPlus.SaveLoadDialog.DeleteButton".Translate(), true, false, true))
                {
                    SaveLoader.Delete(RandomSettings.pawnFilterList[selectedIndex]);
                    selectedIndex = -1;
                }

                if (selectedIndex == -1)
                    GUI.enabled = true;
            }
            finally
            {
                GUI.EndGroup();
            }


            this.DrawFooter(inRect.AtZero());
        }

        protected void DrawFooter(Rect inRect)
        {
            GUI.BeginGroup(inRect);
            bool flag = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;
            float top = inRect.height - 52;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.SetNextControlName("ColonistNameField");
            Rect rect = new Rect(5, top, 400, 35);
            string text = Widgets.TextField(rect, Filename);
            if (GenText.IsValidFilename(text))
            {
                Filename = text;
                var matchingIndex = RandomSettings.pawnFilterList.FindIndex(i => i.name == Filename);
                if (matchingIndex >= 0)
                {
                    selectedIndex = matchingIndex;
                }
                else
                {
                    selectedIndex = -1;
                }
            }
            if (!this.focusedColonistNameArea)
            {
                GUI.FocusControl("ColonistNameField");
                this.focusedColonistNameArea = true;
            }


            Rect butRect = new Rect(420, top, inRect.width - 400 - 20, 35);

            GUI.SetNextControlName("SaveButton");
            string buttonName = selectedIndex >= 0 ? "RandomPlus.SaveLoadDialog.OverwriteButton".Translate() : "RandomPlus.SaveLoadDialog.SaveButton".Translate();
            if (Widgets.ButtonText(butRect, buttonName, true, false, true) || flag)
            {
                if (Filename.Length == 0)
                {
                    Messages.Message("NeedAName".Translate(), MessageTypeDefOf.RejectInput);
                }
                else if (selectedIndex >= 0)
                {
                    RandomSettings.PawnFilter.name = text;
                    SaveLoader.SaveOverwrite(selectedIndex, RandomSettings.PawnFilter);
                    Close(true);
                }
                else
                {
                    RandomSettings.PawnFilter.name = text;
                    SaveLoader.Save(RandomSettings.PawnFilter);
                    Close(true);
                }
            }

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

    }
}