using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using Verse;
using RimWorld;
using System.Reflection;

namespace StartWithNumbers
{
    public class NumbersWindow : Window
    {
        public static bool pawnListDescending = false;

        public enum OrderBy
        {
            Name,
            Column
        }

        OrderBy chosenOrderBy = OrderBy.Name;

        public List<KListObject> kList = new List<KListObject>();
        public KListObject kListObj;

        //public const float maxWindowWidth = 1060f;
        public const int cFreeSpaceAtTheEnd = 50;

        public const float buttonWidth = 160f;

        public const float PawnRowHeight = 35f;

        protected const float NameColumnWidth = 175f;

        protected const float NameLeftMargin = 15f;

        protected Vector2 scrollPosition = Vector2.zero;

        public float kListDesiredWidth = 0f;

        List<StatDef> pawnHumanlikeStatDef = new List<StatDef>();

        public virtual List<Pawn> pawns {
            get { return Find.GameInitData.startingAndOptionalPawns; }
        }

        public NumbersWindow()
        {
            //base.forcePause = true;
            base.absorbInputAroundWindow = true;
            base.closeOnClickedOutside = true;
            base.doCloseX = true;

            foreach (var skill in DefDatabase<SkillDef>.AllDefs)
            {
                var kListObj = new KListObject(KListObject.objectType.Skill, skill.LabelCap, skill);
                kListObj.minWidthDesired = 60f;
                kList.Add(kListObj);
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
        }

        public override void PostOpen()
        {
            base.PostOpen();
            this.windowRect.size = this.InitialSize;
        }

        public override void DoWindowContents(Rect r)
        {
            if (!Find.WindowStack.IsOpen<Page_ConfigureStartingPawns>())
            {
                this.Close();
                return;
            }

            //var component = Find.World.GetComponent<WorldComponent_Numbers>();
            this.windowRect = new Rect(0, Screen.height - (Screen.height / 3), Screen.width, Screen.height / 3);
            //UpdatePawnList();

            Rect position = new Rect(0f, 0f, r.width, 115f);
            GUI.BeginGroup(position);

            float x = 0f;
            Text.Font = GameFont.Small;

            //stats btn
            Rect addColumnButton = new Rect(x, 0f, buttonWidth, PawnRowHeight);
            if (Widgets.ButtonText(addColumnButton, "koisama.Numbers.AddColumnLabel".Translate()))
            {
                StatsOptionsMaker();
            }
            x += buttonWidth + 10;

            //skills btn
            Rect skillColumnButton = new Rect(x, 0f, buttonWidth, PawnRowHeight);
            if (Widgets.ButtonText(skillColumnButton, "koisama.Numbers.AddSkillColumnLabel".Translate()))
            {
                SkillsOptionsMaker();
            }
            x += buttonWidth + 10;

            //cap btn
            Rect capacityColumnButton = new Rect(x, 0f, buttonWidth, PawnRowHeight);
            if (Widgets.ButtonText(capacityColumnButton, "koisama.Numbers.AddCapacityColumnLabel".Translate()))
            {
                CapacityOptionsMaker();
            }
            x += buttonWidth + 10;

            Rect otherColumnBtn = new Rect(x, 0f, buttonWidth, PawnRowHeight);
            if (Widgets.ButtonText(otherColumnBtn, "koisama.Numbers.AddOtherColumnLabel".Translate()))
            {
                OtherOptionsMaker();
            }
            x += buttonWidth + 10;

            Rect thingCount = new Rect(10f, 45f, 200f, 30f);
            Widgets.Label(thingCount, "koisama.Numbers.Count".Translate() + ": " + pawns.Count().ToString());

            x = 0;
            //names
            Rect nameLabel = new Rect(x, 75f, NameColumnWidth, PawnRowHeight);
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(nameLabel, "koisama.Numbers.Name".Translate());
            if (Widgets.ButtonInvisible(nameLabel))
            {
                if (chosenOrderBy == OrderBy.Name)
                {
                    pawnListDescending = !pawnListDescending;
                }
                else
                {
                    chosenOrderBy = OrderBy.Name;
                    pawnListDescending = false;
                }
                //isDirty = true;
            }

            TooltipHandler.TipRegion(nameLabel, "koisama.Numbers.SortByTooltip".Translate("koisama.Numbers.Name".Translate()));
            Widgets.DrawHighlightIfMouseover(nameLabel);
            x += NameColumnWidth;

            //header
            //TODO: better interface - auto width calculation
            bool offset = false;
            kListDesiredWidth = 175f;
            Text.Anchor = TextAnchor.MiddleCenter;

            for (int i = 0; i < kList.Count; i++)
            {
                float colWidth = kList[i].minWidthDesired;

                kListDesiredWidth += colWidth;

                Rect defLabel = new Rect(x - 35, 25f + (offset ? 10f : 50f), colWidth + 70, 40f);
                Widgets.DrawLine(new Vector2(x + colWidth / 2, 55f + (offset ? 15f : 55f)), new Vector2(x + colWidth / 2, 113f), Color.gray, 1);
                Widgets.Label(defLabel, kList[i].label);

                StringBuilder labelSB = new StringBuilder();
                labelSB.AppendLine("koisama.Numbers.SortByTooltip".Translate(kList[i].label));
                labelSB.AppendLine("koisama.Numbers.RemoveTooltip".Translate());
                TooltipHandler.TipRegion(defLabel, labelSB.ToString());
                Widgets.DrawHighlightIfMouseover(defLabel);

                if (Widgets.ButtonInvisible(defLabel))
                {
                    if (Event.current.button == 1)
                    {
                        kList.RemoveAt(i);
                    }
                    else
                    {

                        if (chosenOrderBy == OrderBy.Column && kList[i].Equals(kListObj))
                        {
                            pawnListDescending = !pawnListDescending;
                        }
                        else
                        {
                            kListObj = kList[i];
                            chosenOrderBy = OrderBy.Column;
                            pawnListDescending = false;
                        }
                    }
                }
                offset = !offset;
                x += colWidth;
            }
            GUI.EndGroup();

            //content
            Rect content = new Rect(0f, position.yMax, r.width, r.height - position.yMax);
            GUI.BeginGroup(content);
            DrawRows(new Rect(0f, 0f, content.width, content.height));
            GUI.EndGroup();
        }

        protected void DrawRows(Rect outRect)
        {
            float winWidth = outRect.width - 16f;
            Rect viewRect = new Rect(0f, 0f, winWidth, (float)this.pawns.Count * PawnRowHeight);

            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
            float num = 0f;
            for (int i = 0; i < this.pawns.Count; i++)
            {
                Pawn p = this.pawns[i];
                Rect rect = new Rect(0f, num, viewRect.width, PawnRowHeight);
                if (num - this.scrollPosition.y + PawnRowHeight >= 0f && num - this.scrollPosition.y <= outRect.height)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.2f);
                    Widgets.DrawLineHorizontal(0f, num, viewRect.width);
                    GUI.color = Color.white;
                    this.PreDrawPawnRow(rect, p);
                    this.DrawPawnRow(rect, p);
                    this.PostDrawPawnRow(rect, p);
                }
                num += PawnRowHeight;
            }
            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void PreDrawPawnRow(Rect rect, Pawn p)
        {
            float randomButtonWidth = PawnRowHeight;
            Rect rectRandomButton = new Rect(0f, rect.y, randomButtonWidth, PawnRowHeight);
            if (Widgets.ButtonText(rectRandomButton, "*"))
            {
                StartingPawnUtility.RandomizeInPlace(p);
                return;
            }

            Rect rect2 = new Rect(randomButtonWidth, rect.y, rect.width, PawnRowHeight);
            if (Mouse.IsOver(rect2))
            {
                GUI.DrawTexture(rect2, TexUI.HighlightTex);
            }
            Rect rect3 = new Rect(randomButtonWidth, rect.y, 175f, PawnRowHeight);
            Rect position = rect3.ContractedBy(3f);

            if (p.health.summaryHealth.SummaryHealthPercent < 0.999f)
            {
                Rect rect4 = new Rect(rect3);
                rect4.xMin -= 4f;
                rect4.yMin += 4f;
                rect4.yMax -= 6f;
                Widgets.FillableBar(rect4, p.health.summaryHealth.SummaryHealthPercent, GenMapUI.OverlayHealthTex, BaseContent.ClearTex, false);
            }
            
            if (Mouse.IsOver(rect3))
            {
                GUI.DrawTexture(position, TexUI.HighlightTex);
            }
            string label;
            if (!p.RaceProps.Humanlike && p.Name != null && !p.Name.Numerical)
            {
                label = p.Name.ToStringShort.CapitalizeFirst() + ", " + p.KindLabel;
            }
            else
            {
                label = p.LabelCap;
            }
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Rect rect5 = new Rect(rect3);
            rect5.xMin += 15f;
            Widgets.Label(rect5, label);
            Text.WordWrap = true;
            if (Widgets.ButtonInvisible(rect3))
            {
                var window = Find.WindowStack.WindowOfType<Page_ConfigureStartingPawns>();
                if (window != null)
                {
                    window.SelectPawn(p as Pawn);
                }
                return;
            }
            TipSignal tooltip = p.GetTooltip();
            tooltip.text = "ClickToJumpTo".Translate() + "\n\n" + tooltip.text;
            TooltipHandler.TipRegion(rect3, tooltip);
        }

        protected void DrawPawnRow(Rect r, ThingWithComps p)
        {
            float x = 175f;
            float y = r.yMin;

            Text.Anchor = TextAnchor.MiddleCenter;

            //TODO: better interface - auto width calculation, make sure columns won't overlap
            for (int i = 0; i < kList.Count; i++)
            {
                float colWidth = kList[i].minWidthDesired;
                
                Rect capCell = new Rect(x, y, colWidth, PawnRowHeight);
                kList[i].Draw(capCell, p);
                x += colWidth;
            }
        }

        private void PostDrawPawnRow(Rect rect, ThingWithComps p)
        {
            if (p is Pawn)
            {
                if ((p as Pawn).Downed)
                {
                    GUI.color = new Color(1f, 0f, 0f, 0.5f);
                    Widgets.DrawLineHorizontal(rect.x, rect.center.y, rect.width);
                    GUI.color = Color.white;
                }
            }
        }

        public void StatsOptionsMaker()
        {
            if (pawnHumanlikeStatDef.Count == 0)
            {
                MethodInfo statsToDraw = typeof(StatsReportUtility).GetMethod("StatsToDraw", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, new Type[] { typeof(Thing) }, null);

                var tmpPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.AncientSoldier, Faction.OfPlayer);

                pawnHumanlikeStatDef = (from s in ((IEnumerable<StatDrawEntry>)statsToDraw.Invoke(null, new[] { tmpPawn })) where s.ShouldDisplay && s.stat != null select s.stat).OrderBy(stat => stat.LabelCap).ToList();
            }
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (StatDef stat in pawnHumanlikeStatDef)
            {
                Action action = delegate
                {
                    KListObject kl = new KListObject(KListObject.objectType.Stat, stat.LabelCap, stat);
                    //if (fits(kl.minWidthDesired))
                    kList.Add(kl);
                };
                list.Add(new FloatMenuOption(stat.LabelCap, action, MenuOptionPriority.Default, null, null));
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        public void SkillsOptionsMaker()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading)
            {
                Action action = delegate
                {
                    KListObject kl = new KListObject(KListObject.objectType.Skill, skill.LabelCap, skill);
                    kList.Add(kl);
                };
                list.Add(new FloatMenuOption(skill.LabelCap, action, MenuOptionPriority.Default, null, null));
                Debug.Log(skill.LabelCap);
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        public void NeedsOptionsMaker()
        {
            //List<FloatMenuOption> list = new List<FloatMenuOption>();
            //foreach (NeedDef need in pNeedDef)
            //{
            //    Action action = delegate
            //    {
            //        KListObject kl = new KListObject(KListObject.objectType.Need, need.LabelCap, need);
            //        //if (fits(kl.minWidthDesired))
            //        kList.Add(kl);
            //    };
            //    list.Add(new FloatMenuOption(need.LabelCap, action, MenuOptionPriority.Default, null, null));
            //}
            //Find.WindowStack.Add(new FloatMenu(list));
        }

        public void CapacityOptionsMaker()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (PawnCapacityDef pcd in DefDatabase<PawnCapacityDef>.AllDefsListForReading)
            {
                Action action = delegate
                {
                    KListObject kl = new KListObject(KListObject.objectType.Capacity, pcd.LabelCap, pcd);
                    //if (fits(kl.minWidthDesired))
                    kList.Add(kl);
                };
                list.Add(new FloatMenuOption(pcd.LabelCap, action, MenuOptionPriority.Default, null, null));
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        //other hardcoded options
        public void OtherOptionsMaker()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            //equipment bearers            
            Action action = delegate
            {
                KListObject kl = new KListObject(KListObject.objectType.Gear, "koisama.Equipment".Translate(), null);
                //if (fits(kl.minWidthDesired))
                kList.Add(kl);
            };
            list.Add(new FloatMenuOption("koisama.Equipment".Translate(), action, MenuOptionPriority.Default, null, null));

            //all living things
            action = delegate
            {
                KListObject kl = new KListObject(KListObject.objectType.Age, "koisama.Age".Translate(), null);
                //if (fits(kl.minWidthDesired))
                kList.Add(kl);
            };
            list.Add(new FloatMenuOption("koisama.Age".Translate(), action, MenuOptionPriority.Default, null, null));

            action = delegate
            {
                KListObject kl = new KListObject(KListObject.objectType.MentalState, "koisama.MentalState".Translate(), null);
                //if (fits(kl.minWidthDesired))
                kList.Add(kl);
            };
            list.Add(new FloatMenuOption("koisama.MentalState".Translate(), action, MenuOptionPriority.Default, null, null));

            //healable
            action = delegate
            {
                KListObject kl = new KListObject(KListObject.objectType.ControlMedicalCare, "koisama.MedicalCare".Translate(), null);
                kList.Add(kl);
            };
            list.Add(new FloatMenuOption("koisama.MedicalCare".Translate(), action, MenuOptionPriority.Default, null, null));

            Find.WindowStack.Add(new FloatMenu(list));
        }
    }
}