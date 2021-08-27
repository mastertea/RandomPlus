using RimWorld;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RandomPlus
{
    public class PanelSkills : PanelBase
    {
        protected ScrollViewVertical scrollView = new ScrollViewVertical();

        public PanelSkills()
        {
            Resize(new Rect(0, 40, 320, 500));
        }

        public override string PanelHeader
        {
            get
            {
                return "RandomPlus.PanelSkills.Header".Translate();
            }
        }

        private static Color ColorSkillDisabled = new Color(1f, 1f, 1f, 0.5f);

        protected static Rect RectButtonClearSkills;
        protected static Rect RectLabel;
        protected static Rect RectPassion;
        protected static Rect RectSkillBar;
        protected static Rect RectButtonDecrement;
        protected static Rect RectButtonIncrement;
        protected static Rect RectScrollFrame;
        protected static Rect RectScrollView;

        //protected static Rect totalSkillsLabelRect;
        //protected static Rect totalSkillsRangeRect;
        protected static Rect totalPassionLabelRect;
        protected static Rect totalPassionRangeRect;

        protected static Rect totalSkillLabelRect;
        protected static Rect totalSkillRangeRect;

        protected static Rect optionCountOnlyHighestAttackBoxRect;
        protected static Rect optionCountOnlyPassionBoxRect;

        public override void Resize(Rect rect)
        {
            base.Resize(rect);

            float panelPaddingLeft = 16;
            float panelPaddingRight = 10;
            float panelPaddingBottom = 10;
            float panelPaddingTop = 4;
            float top = BodyRect.y + panelPaddingTop;

            GameFont savedFont = Text.Font;
            Text.Font = GameFont.Small;
            Vector2 maxLabelSize = new Vector2(float.MinValue, float.MinValue);
            foreach (SkillDef current in DefDatabase<SkillDef>.AllDefs)
            {
                Vector2 labelSize = Text.CalcSize(current.skillLabel);
                // Need to add some padding because the "n" at the end of "Construction" gets cut off if we don't.
                labelSize += new Vector2(4, 0);
                maxLabelSize.x = Mathf.Max(labelSize.x, maxLabelSize.x);
                maxLabelSize.y = Mathf.Max(labelSize.y, maxLabelSize.y);
            }
            Text.Font = savedFont;

            float labelPadding = 4;
            float availableContentWidth = PanelRect.width - panelPaddingLeft - panelPaddingRight;
            Vector2 passionSize = new Vector2(24, 24);
            float passionPadding = 2;
            Vector2 arrowButtonSize = new Vector2(16, 16);
            float arrowsWidth = 32;
            Vector2 skillBarSize = new Vector2(availableContentWidth - passionSize.x - passionPadding
                - arrowsWidth - maxLabelSize.x - labelPadding, 22);

            RectButtonClearSkills = new Rect(PanelRect.width - 38, 8, 23, 21);
            RectLabel = new Rect(0, 0, maxLabelSize.x, maxLabelSize.y);
            RectPassion = new Rect(RectLabel.xMax + labelPadding, (maxLabelSize.y * 0.5f - passionSize.y * 0.5f),
                passionSize.x, passionSize.y);
            RectSkillBar = new Rect(RectPassion.xMax + passionPadding, (maxLabelSize.y * 0.5f - skillBarSize.y * 0.5f),
                skillBarSize.x, skillBarSize.y);
            RectButtonDecrement = new Rect(RectSkillBar.xMax, (maxLabelSize.y * 0.5f - arrowButtonSize.y * 0.5f),
                arrowButtonSize.x, arrowButtonSize.y);
            RectButtonIncrement = new Rect(RectButtonDecrement.xMax, (maxLabelSize.y * 0.5f - arrowButtonSize.y * 0.5f),
                arrowButtonSize.x, arrowButtonSize.y);
            RectScrollFrame = new Rect(panelPaddingLeft, top,
                availableContentWidth, BodyRect.height - panelPaddingTop - panelPaddingBottom);
            RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height - 60);

            totalPassionLabelRect = new Rect(0, 318, RectScrollFrame.width - 8, 30);
            totalPassionRangeRect = totalPassionLabelRect.OffsetBy(0, 18);

            totalSkillLabelRect = totalPassionLabelRect.OffsetBy(0, 48);
            optionCountOnlyHighestAttackBoxRect = totalSkillLabelRect.OffsetBy(10, 16);
            optionCountOnlyHighestAttackBoxRect.width -= 10;
            optionCountOnlyPassionBoxRect = optionCountOnlyHighestAttackBoxRect.OffsetBy(0, 24);
            totalSkillRangeRect = totalSkillLabelRect.OffsetBy(0, 62);
            totalPassionRangeRect.height = 18;
            totalSkillRangeRect.height = 18;
        }

        protected override void DrawPanelContent()
        {
            base.DrawPanelContent();

            // Clear button
            Style.SetGUIColorForButton(RectButtonClearSkills);
            GUI.DrawTexture(RectButtonClearSkills, Textures.TextureButtonClearSkills);
            if (Widgets.ButtonInvisible(RectButtonClearSkills, false))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                RandomSettings.PawnFilter.ResetSkills();
            }
            
            int skillCount = RandomSettings.PawnFilter.Skills.Count();
            float rowHeight = 26;
            float height = rowHeight * skillCount;
            bool willScroll = height > RectScrollView.height;

            float cursor = 0;
            GUI.BeginGroup(RectScrollFrame);
            try
            {
                scrollView.Begin(RectScrollView);

                Rect rect;
                Text.Font = GameFont.Small;
                foreach (var skillFilter in RandomSettings.PawnFilter.Skills)
                {
                    SkillDef def = skillFilter.SkillDef;
                    //bool disabled = skill.TotallyDisabled;

                    // Draw the label.
                    GUI.color = Style.ColorText;
                    rect = RectLabel;
                    rect.y = rect.y + cursor;
                    Widgets.Label(rect, def.skillLabel.CapitalizeFirst());

                    // Draw the passion.
                    rect = RectPassion;
                    rect.y = rect.y + cursor;
                    
                    //Passion passion = customPawn.currentPassions[skill.def];
                    Texture2D image;
                    if (skillFilter.Passion == Passion.Minor)
                    {
                        image = Textures.TexturePassionMinor;
                    }
                    else if (skillFilter.Passion == Passion.Major)
                    {
                        image = Textures.TexturePassionMajor;
                    }
                    else
                    {
                        image = Textures.TexturePassionNone;
                    }
                    GUI.color = Color.white;
                    GUI.DrawTexture(rect, image);
                    if (Widgets.ButtonInvisible(rect, false))
                    {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        if (Event.current.button != 1)
                        {
                            IncreasePassion(skillFilter);
                        }
                        else
                        {
                            DecreasePassion(skillFilter);
                        }
                    }
                    

                    // Draw the skill bar.
                    rect = RectSkillBar;
                    rect.y = rect.y + cursor;
                    if (willScroll)
                    {
                        rect.width = rect.width - 16;
                    }
                    DrawSkill(skillFilter, rect);

                    // Handle the tooltip.
                    // TODO: Should cover the whole row, not just the skill bar rect.
                    //TooltipHandler.TipRegion(rect, new TipSignal(GetSkillDescription(skill),
                    //    skill.def.GetHashCode() * 397945));

                    
                    // Draw the decrement button.
                    rect = RectButtonDecrement;
                    rect.y = rect.y + cursor;
                    rect.x = rect.x - (willScroll ? 16 : 0);
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        GUI.color = Style.ColorButtonHighlight;
                    }
                    else
                    {
                        GUI.color = Style.ColorButton;
                    }
                    GUI.DrawTexture(rect, Textures.TextureButtonPrevious);
                    if (Widgets.ButtonInvisible(rect, false))
                    {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        skillFilter.MinValue -= 1;
                        //DecreaseSkill(customPawn, skill);
                    }

                    // Draw the increment button.
                    rect = RectButtonIncrement;
                    rect.y = rect.y + cursor;
                    rect.x = rect.x - (willScroll ? 16 : 0);
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        GUI.color = Style.ColorButtonHighlight;
                    }
                    else
                    {
                        GUI.color = Style.ColorButton;
                    }
                    GUI.DrawTexture(rect, Textures.TextureButtonNext);
                    if (Widgets.ButtonInvisible(rect, false))
                    {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        skillFilter.MinValue += 1;
                        //IncreaseSkill(customPawn, skill);
                    }
                    

                    cursor += rowHeight;
                }

                scrollView.End(cursor);
            }
            finally
            {
                GUI.color = Color.white;
                string labelText = string.Format("RandomPlus.PanelSkills.PassionSlider".Translate(),
                    RandomSettings.PawnFilter.passionRange.min,
                    RandomSettings.PawnFilter.passionRange.max);
                Widgets.Label(totalPassionLabelRect, labelText);
                Widgets.IntRange(totalPassionRangeRect, 2042, ref RandomSettings.PawnFilter.passionRange,
                    0,
                    PawnFilter.PassionMaxDefault,
                    "", 2);

                string skillRangeLabelText = string.Format("RandomPlus.PanelSkills.SkillSlider".Translate(),
                    RandomSettings.PawnFilter.skillRange.min,
                    RandomSettings.PawnFilter.skillRange.max == PawnFilter.SkillMaxDefault ? "∞" : RandomSettings.PawnFilter.skillRange.max.ToString());
                Widgets.Label(totalSkillLabelRect, skillRangeLabelText);
                Widgets.IntRange(totalSkillRangeRect, 2043, ref RandomSettings.PawnFilter.skillRange,
                    0,
                    PawnFilter.SkillMaxDefault,
                    "", 2);

                Widgets.CheckboxLabeled(optionCountOnlyHighestAttackBoxRect,
                    "*"+"RandomPlus.PanelSkills.CountOnlyHighestAttack".Translate(),
                    ref RandomSettings.PawnFilter.countOnlyHighestAttack);

                Widgets.CheckboxLabeled(optionCountOnlyPassionBoxRect, 
                    "*"+"RandomPlus.PanelSkills.CountOnlyPassion".Translate(), 
                    ref RandomSettings.PawnFilter.countOnlyPassion);

                GUI.EndGroup();
            }
            
        }

        public static void FillableBar(Rect rect, float fillPercent, Texture2D fillTex)
        {
            rect.width *= fillPercent;
            GUI.DrawTexture(rect, fillTex);
        }

        private void DrawSkill(SkillContainer filter, Rect rect)
        {
            int level = filter.MinValue;
            
            float barSize = (level > 0 ? (float)level : 0) / 20f;
            FillableBar(rect, barSize, Textures.TextureSkillBarFill);

            //int baseLevel = customPawn.GetSkillModifier(skill.def);
            //float baseBarSize = (baseLevel > 0 ? (float)baseLevel : 0) / 20f;
            FillableBar(rect, 0, Textures.TextureSkillBarFill);

            GUI.color = new Color(0.25f, 0.25f, 0.25f);
            Widgets.DrawBox(rect, 1);
            GUI.color = Style.ColorText;

            if (Widgets.ButtonInvisible(rect, false))
            {
                Vector2 pos = Event.current.mousePosition;
                float x = pos.x - rect.x;
                int value = 0;
                if (Mathf.Floor(x / rect.width * 20f) == 0)
                {
                    if (x <= 1)
                    {
                        value = 0;
                    }
                    else
                    {
                        value = 1;
                    }
                }
                else
                {
                    value = Mathf.CeilToInt(x / rect.width * 20f);
                }
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                filter.MinValue = value;
                //SetSkillLevel(customPawn, skill, value);
            }
            

            string label;
            label = GenString.ToStringCached(level);
            
            Text.Anchor = TextAnchor.MiddleLeft;
            rect.x = rect.x + 3;
            rect.y = rect.y + 1;
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        protected void IncreasePassion(SkillContainer filter)
        {
            if (filter.Passion == Passion.None)
            {
                filter.Passion = Passion.Minor;
            }
            else if (filter.Passion == Passion.Minor)
            {
                filter.Passion = Passion.Major;
            }
            else if (filter.Passion == Passion.Major)
            {
                filter.Passion = Passion.None;
            }
        }

        protected void DecreasePassion(SkillContainer filter)
        {
            if (filter.Passion == Passion.None)
            {
                filter.Passion = Passion.Major;
            }
            else if (filter.Passion == Passion.Minor)
            {
                filter.Passion = Passion.None;
            }
            else if (filter.Passion == Passion.Major)
            {
                filter.Passion = Passion.Minor;
            }
        }

        public void ScrollToTop()
        {
            scrollView.ScrollToTop();
        }
    }
}
