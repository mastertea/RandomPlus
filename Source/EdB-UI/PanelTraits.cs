using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RandomPlus
{
    public class PanelTraits : PanelBase
    {
        protected ScrollViewVertical scrollView = new ScrollViewVertical();
        protected List<Field> fields = new List<Field>();
        protected List<Trait> traitsToRemove = new List<Trait>();
        protected HashSet<TraitDef> disallowedTraitDefs = new HashSet<TraitDef>();
        protected HashSet<String> disallowedTraitLabels = new HashSet<String>();
        protected Dictionary<Trait, string> conflictingTraitList = new Dictionary<Trait, string>();

        protected Vector2 SizeField;
        protected Vector2 SizeTrait;
        protected Vector2 SizeFieldPadding = new Vector2(5, 6);
        protected Vector2 SizeTraitMargin = new Vector2(4, -6);
        protected Rect RectScrollFrame;
        protected Rect RectScrollView;

        protected Rect traitPoolLabelRect;
        protected Rect traitPoolButtonRect;

        protected Rect disabledLabelRect;

        public PanelTraits()
        {
            Resize(new Rect(340, 40, 320, 180));
        }

        public override string PanelHeader
        {
            get
            {
                return "RandomPlus.PanelTraits.Header".Translate();
            }
        }

        public override void Resize(Rect rect)
        {
            base.Resize(rect);

            float panelPadding = 10;
            float fieldHeight = 28;
            SizeTrait = new Vector2(PanelRect.width - panelPadding * 2 - 20, fieldHeight + SizeFieldPadding.y * 2);
            SizeField = new Vector2(SizeTrait.x - SizeFieldPadding.x * 2, SizeTrait.y - SizeFieldPadding.y * 2);

            RectScrollFrame = new Rect(panelPadding, BodyRect.y,
                PanelRect.width - panelPadding * 2, BodyRect.height - panelPadding);
            RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height - 20);

            traitPoolLabelRect = new Rect(8, RectScrollView.y + RectScrollView.height, 200, 30);
            traitPoolButtonRect = new Rect(190, RectScrollView.y + RectScrollView.height + 1, 104, 20);

            disabledLabelRect = new Rect(20, 30, 100, 20);
        }

        protected override void DrawPanelContent()
        {
            base.DrawPanelContent();

            if (Page_RandomEditor.MOD_WELL_MET)
            {
                Widgets.Label(disabledLabelRect, "RandomPlus.PanelTraits.Disabled".Translate());
                return;
            }
                
            float cursor = 0;
            GUI.color = Color.white;
            GUI.BeginGroup(RectScrollFrame);
            try
            {
                if (!RandomSettings.PawnFilter.Traits.Any())
                {
                    GUI.color = Style.ColorText;
                    Widgets.Label(RectScrollView.InsetBy(6, 0, 0, 0), "RandomPlus.PanelTraits.NoTraits".Translate());
                }
                GUI.color = Color.white;

                scrollView.Begin(RectScrollView);

                int index = 0;
                foreach (TraitContainer traitContainer in RandomSettings.PawnFilter.Traits)
                {
                    if (index >= fields.Count)
                    {
                        fields.Add(new Field());
                    }
                    Field field = fields[index];

                    GUI.color = Style.ColorPanelBackgroundItem;
                    Rect traitRect = new Rect(0, cursor, SizeTrait.x - (scrollView.ScrollbarsVisible ? 16 : 0), SizeTrait.y);
                    GUI.DrawTexture(traitRect, BaseContent.WhiteTex);
                    GUI.color = Color.white;

                    Rect fieldRect = new Rect(SizeFieldPadding.x, cursor + SizeFieldPadding.y, SizeField.x, SizeField.y);
                    if (scrollView.ScrollbarsVisible)
                    {
                        fieldRect.width = fieldRect.width - 16;
                    }
                    field.Rect = fieldRect;
                    Rect fieldClickRect = fieldRect;
                    fieldClickRect.width = fieldClickRect.width - 36;
                    field.ClickRect = fieldClickRect;

                    if (traitContainer != null)
                    {
                        field.Label = $"{traitContainer.trait.LabelCap}{LabelForTraitFilter(index)} {RandomSettings.GetTraitRollChanceText(traitContainer.trait.def)}";
                        field.Tip = traitContainer.trait.CurrentData.description;
                    }
                    else
                    {
                        field.Label = null;
                    }
                    Trait localTrait = traitContainer.trait;
                    int localIndex = index;
                    field.ClickAction = () =>
                    {
                        Trait originalTrait = localTrait;
                        Trait selectedTrait = originalTrait;
                        ComputeDisallowedTraits(originalTrait);
                        Dialog_Options<Trait> dialog = new Dialog_Options<Trait>(ProviderTraits.Traits)
                        {
                            NameFunc = (Trait t) =>
                            {
                                return t.LabelCap;
                            },
                            DescriptionFunc = (Trait t) =>
                            {
                                return t.CurrentData.description;
                            },
                            SelectedFunc = (Trait t) =>
                            {
                                if ((selectedTrait == null || t == null) && selectedTrait != t)
                                {
                                    return false;
                                }
                                return selectedTrait.def == t.def && selectedTrait.Label == t.Label;
                            },
                            SelectAction = (Trait t) =>
                            {
                                selectedTrait = t;
                            },
                            EnabledFunc = (Trait t) =>
                            {
                                // filter out conflicting traits
                                return !(disallowedTraitDefs.Contains(t.def) || disallowedTraitLabels.Contains(t.Label));
                            },
                            CloseAction = () =>
                            {
                                RandomSettings.PawnFilter.TraitUpdated(localIndex, selectedTrait);
                            },
                            NoneSelectedFunc = () =>
                            {
                                return selectedTrait == null;
                            },
                            SelectNoneAction = () =>
                            {
                                selectedTrait = null;
                            }
                        };
                        Find.WindowStack.Add(dialog);
                    };
                    //field.PreviousAction = () =>
                    //{
                    //    SelectPreviousTrait(index);
                    //};
                    //field.NextAction = () =>
                    //{
                    //    SelectNextTrait(index);
                    //};
                    field.Draw();

                    // Remove trait button.
                    Rect deleteRect = new Rect(field.Rect.xMax - 32, field.Rect.y + field.Rect.HalfHeight() - 6, 12, 12);
                    if (deleteRect.Contains(Event.current.mousePosition))
                    {
                        GUI.color = Style.ColorButtonHighlight;
                    }
                    else
                    {
                        GUI.color = Style.ColorButton;
                    }
                    GUI.DrawTexture(deleteRect, Textures.TextureButtonDelete);
                    if (Widgets.ButtonInvisible(deleteRect, false))
                    {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        traitsToRemove.Add(traitContainer.trait);
                    }

                    //cycling required/ optional / exluded trait filter button
                    Rect traitFilterTypeRect = new Rect(field.Rect.xMax + 12, field.Rect.y + field.Rect.HalfHeight() - 6, 12, 12);
                    GUI.color = traitFilterTypeRect.Contains(Event.current.mousePosition) ?
                        Style.ColorButtonHighlight : Style.ColorButton;
                    GUI.DrawTexture(traitFilterTypeRect, Textures.TextureButtonReset);
                    if (Widgets.ButtonInvisible(traitFilterTypeRect, false))
                    {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                        CycleTraitFilter(index);
                    }

                    index++;

                    cursor += SizeTrait.y + SizeTraitMargin.y;
                }
                cursor -= SizeTraitMargin.y;
            }
            finally
            {
                scrollView.End(cursor);

                GUI.color = Color.white;
                drawTraitPool();
                GUI.EndGroup();
            }

            // Add trait button.
            Rect addRect = new Rect(PanelRect.width - 24, 12, 16, 16);
            Style.SetGUIColorForButton(addRect);
            int traitCount = RandomSettings.PawnFilter.Traits.Count();
            bool addButtonEnabled = (traitCount < ProviderTraits.Traits.Count());
            if (!addButtonEnabled)
            {
                GUI.color = Style.ColorButtonDisabled;
            }
            GUI.DrawTexture(addRect, Textures.TextureButtonAdd);
            if (addButtonEnabled && Widgets.ButtonInvisible(addRect, false))
            {
                ComputeDisallowedTraits(null);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                Trait selectedTrait = null;
                Dialog_Options<Trait> dialog = new Dialog_Options<Trait>(ProviderTraits.Traits)
                {
                    ConfirmButtonLabel = "RandomPlus.PanelTraits.AddDialog.AddButton".Translate(),
                    NameFunc = (Trait t) => {
                        return $"{t.LabelCap} {RandomSettings.GetTraitRollChanceText(t.def)}";
                    },
                    DescriptionFunc = (Trait t) => {
                        return t.CurrentData.description;
                    },
                    SelectedFunc = (Trait t) => {
                        return selectedTrait == t;
                    },
                    SelectAction = (Trait t) => {
                        selectedTrait = t;
                    },
                    EnabledFunc = (Trait t) => {
                        return !(disallowedTraitDefs.Contains(t.def) || disallowedTraitLabels.Contains(t.Label));
                    },
                    CloseAction = () => {
                        if (selectedTrait != null)
                        {
                            RandomSettings.PawnFilter.AddTrait(selectedTrait);
                        }
                    }
                };
                Find.WindowStack.Add(dialog);
            }

            if (traitsToRemove.Count > 0)
            {
                foreach (var trait in traitsToRemove)
                {
                    RandomSettings.PawnFilter.TraitRemoved(trait);
                }
                traitsToRemove.Clear();
            }
        }

        protected void ComputeDisallowedTraits(Trait traitToReplace)
        {
            disallowedTraitDefs.Clear();
            disallowedTraitLabels.Clear();
            List<string> exTags = new List<string>();

            foreach (TraitContainer tc in RandomSettings.PawnFilter.Traits)
            {
                if (tc.trait == traitToReplace)
                {
                    continue;
                }

                if (tc.traitFilter == TraitContainer.TraitFilterType.Required)
                {
                    // add exTags
                    tc.trait.def.exclusionTags.ForEach((t) => exTags.AddDistinct(t));

                    // filter conflicting traits
                    if (tc.trait.def.conflictingTraits != null)
                    {
                        foreach (var c in tc.trait.def.conflictingTraits)
                        {
                            disallowedTraitDefs.Add(c);
                        }
                    }
                    disallowedTraitDefs.Add(tc.trait.def);
                }
                else
                    disallowedTraitLabels.Add(tc.trait.Label);
            }

            foreach (TraitDef def in DefDatabase<TraitDef>.AllDefs)
            {
                if (def.exclusionTags.Intersect(exTags).Any())
                {
                    disallowedTraitDefs.Add(def);
                }
            }
        }

        //protected void SelectNextTrait(int traitIndex)
        //{
        //    Trait currentTrait = RandomSettings.PawnFilter.Traits.ElementAt(traitIndex).trait;
        //    ComputeDisallowedTraits(currentTrait);
        //    int index = -1;
        //    if (currentTrait != null)
        //    {
        //        index = ProviderTraits.Traits.FindIndex((Trait t) => {
        //            return t.Label.Equals(currentTrait.Label);
        //        });
        //    }
        //    int count = 0;
        //    do
        //    {
        //        index++;
        //        if (index >= ProviderTraits.Traits.Count)
        //        {
        //            index = 0;
        //        }
        //        if (++count > ProviderTraits.Traits.Count + 1)
        //        {
        //            index = -1;
        //            break;
        //        }
        //    }
        //    while (index != -1 &&
        //           (RandomSettings.PawnFilter.Traits.ContainsTrait(ProviderTraits.Traits[index]) ||
        //           disallowedTraitDefs.Contains(ProviderTraits.Traits[index].def) ||
        //           disallowedTraitLabels.Contains(ProviderTraits.Traits[index].Label)));

        //    Trait newTrait = null;
        //    if (index > -1)
        //    {
        //        newTrait = ProviderTraits.Traits[index];
        //    }
        //    RandomSettings.PawnFilter.TraitUpdated(traitIndex, newTrait);
        //}

        //protected void SelectPreviousTrait(int traitIndex)
        //{
        //    Trait currentTrait = RandomSettings.PawnFilter.Traits.ElementAt(traitIndex).trait;
        //    ComputeDisallowedTraits(currentTrait);
        //    int index = -1;
        //    if (currentTrait != null)
        //    {
        //        index = ProviderTraits.Traits.FindIndex((Trait t) => {
        //            return t.Label.Equals(currentTrait.Label);
        //        });
        //    }
        //    int count = 0;
        //    do
        //    {
        //        index--;
        //        if (index < 0)
        //        {
        //            index = ProviderTraits.Traits.Count - 1;
        //        }
        //        if (++count > ProviderTraits.Traits.Count + 1)
        //        {
        //            index = -1;
        //            break;
        //        }
        //    }
        //    while (index != -1 &&
        //           (RandomSettings.PawnFilter.Traits.ContainsTrait(ProviderTraits.Traits[index]) ||
        //           disallowedTraitDefs.Contains(ProviderTraits.Traits[index].def) ||
        //           disallowedTraitLabels.Contains(ProviderTraits.Traits[index].Label)));

        //    Trait newTrait = null;
        //    if (index > -1)
        //    {
        //        newTrait = ProviderTraits.Traits[index];
        //    }
        //    RandomSettings.PawnFilter.TraitUpdated(traitIndex, newTrait);
        //}

        protected void ClearTrait(int traitIndex)
        {
            RandomSettings.PawnFilter.TraitUpdated(traitIndex, null);
        }

        public void ScrollToTop()
        {
            scrollView.ScrollToTop();
        }
        public void ScrollToBottom()
        {
            scrollView.ScrollToBottom();
        }

        protected void CycleTraitFilter(int index)
        {
            if (index < 0 || index >= RandomSettings.PawnFilter.Traits.Count())
                return;

            TraitContainer.TraitFilterType traitFilter = RandomSettings.PawnFilter.Traits.ElementAt(index).traitFilter;
            
            switch (traitFilter)
            {
                case TraitContainer.TraitFilterType.Required:
                    traitFilter = TraitContainer.TraitFilterType.Optional;
                    break;
                case TraitContainer.TraitFilterType.Optional:
                    traitFilter = TraitContainer.TraitFilterType.Excluded;
                    break;
                case TraitContainer.TraitFilterType.Excluded:
                    traitFilter = TraitContainer.TraitFilterType.Required;
                    break;
            }

            RandomSettings.PawnFilter.Traits.ElementAt(index).traitFilter = traitFilter;
        }

        protected String LabelForTraitFilter(int index)
        {
            if (index < 0 || index >= RandomSettings.PawnFilter.Traits.Count())
                return "";

            switch (RandomSettings.PawnFilter.Traits.ElementAt(index).traitFilter)
            {
                case TraitContainer.TraitFilterType.Required: return "RandomPlus.PanelTraits.FilterType.Required".Translate();
                case TraitContainer.TraitFilterType.Optional: return "RandomPlus.PanelTraits.FilterType.Pool".Translate();
                case TraitContainer.TraitFilterType.Excluded: return "RandomPlus.PanelTraits.FilterType.Excluded".Translate();
            }

            return "";
        }

        public void drawTraitPool()
        {
            Widgets.Label(traitPoolLabelRect, "RandomPlus.PanelTraits.PoolLabel".Translate());
            if (Widgets.ButtonText(traitPoolButtonRect, RandomSettings.PawnFilter.RequiredTraitsInPool.ToString(), true, true, true))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var rangeOption in new int[] { 0, 1, 2, 3})
                {
                    var menuOption = new FloatMenuOption(rangeOption.ToString(), () => {
                        RandomSettings.PawnFilter.RequiredTraitsInPool = rangeOption;
                    });
                    options.Add(menuOption);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}
