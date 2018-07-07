using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RandomPlus
{
    public class PanelTraits : PanelBase
    {
        private ProviderTraits providerTraits = new ProviderTraits();
        protected ScrollViewVertical scrollView = new ScrollViewVertical();
        protected List<Field> fields = new List<Field>();
        protected List<Trait> traitsToRemove = new List<Trait>();
        protected HashSet<TraitDef> disallowedTraitDefs = new HashSet<TraitDef>();
        protected Dictionary<Trait, string> conflictingTraitList = new Dictionary<Trait, string>();

        protected Vector2 SizeField;
        protected Vector2 SizeTrait;
        protected Vector2 SizeFieldPadding = new Vector2(5, 6);
        protected Vector2 SizeTraitMargin = new Vector2(4, -6);
        protected Rect RectScrollFrame;
        protected Rect RectScrollView;

        protected PawnFilter pawnFilter;

        public PanelTraits(PawnFilter pawnFilter)
        {
            this.pawnFilter = pawnFilter;
            Resize(new Rect(320 + 20, 40,
                320, 
                156));
        }

        public override string PanelHeader
        {
            get
            {
                return "Traits".Translate();
            }
        }

        public override void Resize(Rect rect)
        {
            base.Resize(rect);

            float panelPadding = 10;
            float fieldHeight = 28;
            SizeTrait = new Vector2(PanelRect.width - panelPadding * 2, fieldHeight + SizeFieldPadding.y * 2);
            SizeField = new Vector2(SizeTrait.x - SizeFieldPadding.x * 2, SizeTrait.y - SizeFieldPadding.y * 2);

            RectScrollFrame = new Rect(panelPadding, BodyRect.y,
                PanelRect.width - panelPadding * 2, BodyRect.height - panelPadding);
            RectScrollView = new Rect(0, 0, RectScrollFrame.width, RectScrollFrame.height);
        }

        protected override void DrawPanelContent()
        {
            base.DrawPanelContent();

            float cursor = 0;
            GUI.color = Color.white;
            GUI.BeginGroup(RectScrollFrame);
            try
            {
                if (pawnFilter.Traits.Count() == 0)
                {
                    GUI.color = Style.ColorText;
                    Widgets.Label(RectScrollView.InsetBy(6, 0, 0, 0), "EdB.PC.Panel.Traits.None".Translate());
                }
                GUI.color = Color.white;

                scrollView.Begin(RectScrollView);

                int index = 0;
                foreach (Trait trait in pawnFilter.Traits)
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

                    if (trait != null)
                    {
                        field.Label = trait.LabelCap;
                        //field.Tip = GetTraitTip(trait, currentPawn);
                    }
                    else
                    {
                        field.Label = null;
                        field.Tip = null;
                    }
                    Trait localTrait = trait;
                    int localIndex = index;
                    field.ClickAction = () => {
                        Trait originalTrait = localTrait;
                        Trait selectedTrait = originalTrait;
                        Dialog_Options<Trait> dialog = new Dialog_Options<Trait>(providerTraits.Traits)
                        {
                            NameFunc = (Trait t) => {
                                return t.LabelCap;
                            },
                            DescriptionFunc = (Trait t) => {
                                return null;
                                //return GetTraitTip(t, currentPawn);
                            },
                            SelectedFunc = (Trait t) => {
                                if ((selectedTrait == null || t == null) && selectedTrait != t)
                                {
                                    return false;
                                }
                                return selectedTrait.def == t.def && selectedTrait.Label == t.Label;
                            },
                            SelectAction = (Trait t) => {
                                selectedTrait = t;
                            },
                            EnabledFunc = (Trait t) => {
                                return !disallowedTraitDefs.Contains(t.def);
                            },
                            CloseAction = () => {
                                TraitUpdated(localIndex, selectedTrait);
                            },
                            NoneSelectedFunc = () => {
                                return selectedTrait == null;
                            },
                            SelectNoneAction = () => {
                                selectedTrait = null;
                            }
                        };
                        Find.WindowStack.Add(dialog);
                    };
                    field.PreviousAction = () => {
                        SelectPreviousTrait(index);
                    };
                    field.NextAction = () => {
                        SelectNextTrait(index);
                    };
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
                        traitsToRemove.Add(trait);
                    }

                    index++;

                    cursor += SizeTrait.y + SizeTraitMargin.y;
                }
                cursor -= SizeTraitMargin.y;
            }
            finally
            {
                scrollView.End(cursor);
                GUI.EndGroup();
            }

            GUI.color = Color.white;

            // Randomize traits button.
            //Rect randomizeRect = new Rect(PanelRect.width - 32, 9, 22, 22);
            //if (randomizeRect.Contains(Event.current.mousePosition))
            //{
            //    GUI.color = Style.ColorButtonHighlight;
            //}
            //else
            //{
            //    GUI.color = Style.ColorButton;
            //}
            //GUI.DrawTexture(randomizeRect, Textures.TextureButtonRandom);
            //if (Widgets.ButtonInvisible(randomizeRect, false))
            //{
            //    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            //    TraitsRandomized();
            //}

            // Add trait button.
            Rect addRect = new Rect(PanelRect.width - 24, 12, 16, 16);
            Style.SetGUIColorForButton(addRect);
            int traitCount = pawnFilter.Traits.Count();
            bool addButtonEnabled = (traitCount < 3);
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
                Dialog_Options<Trait> dialog = new Dialog_Options<Trait>(providerTraits.Traits)
                {
                    ConfirmButtonLabel = "EdB.PC.Common.Add".Translate(),
                    NameFunc = (Trait t) => {
                        return t.LabelCap;
                    },
                    DescriptionFunc = (Trait t) => {
                        return null;
                        //return GetTraitTip(t, state.CurrentPawn);
                    },
                    SelectedFunc = (Trait t) => {
                        return selectedTrait == t;
                    },
                    SelectAction = (Trait t) => {
                        selectedTrait = t;
                    },
                    EnabledFunc = (Trait t) => {
                        return !disallowedTraitDefs.Contains(t.def);
                    },
                    CloseAction = () => {
                        if (selectedTrait != null)
                        {
                            TraitAdded(selectedTrait);
                        }
                    }
                };
                Find.WindowStack.Add(dialog);
            }

            if (traitsToRemove.Count > 0)
            {
                foreach (var trait in traitsToRemove)
                {
                    TraitRemoved(trait);
                }
                traitsToRemove.Clear();
            }
        }

        public void TraitAdded(Trait trait)
        {
            pawnFilter.Traits.Add(trait);
        }

        public void TraitUpdated(int index, Trait trait)
        {
            pawnFilter.Traits[index] = trait;
        }

        public void TraitRemoved(Trait trait)
        {
            pawnFilter.Traits.Remove(trait);
        }

        protected void ComputeDisallowedTraits(Trait traitToReplace)
        {
            disallowedTraitDefs.Clear();
            foreach (Trait t in pawnFilter.Traits)
            {
                if (t == traitToReplace)
                {
                    continue;
                }
                disallowedTraitDefs.Add(t.def);
                if (t.def.conflictingTraits != null)
                {
                    foreach (var c in t.def.conflictingTraits)
                    {
                        disallowedTraitDefs.Add(c);
                    }
                }
            }
        }

        protected void SelectNextTrait(int traitIndex)
        {
            Trait currentTrait = pawnFilter.Traits[traitIndex];
            ComputeDisallowedTraits(currentTrait);
            int index = -1;
            if (currentTrait != null)
            {
                index = providerTraits.Traits.FindIndex((Trait t) => {
                    return t.Label.Equals(currentTrait.Label);
                });
            }
            int count = 0;
            do
            {
                index++;
                if (index >= providerTraits.Traits.Count)
                {
                    index = 0;
                }
                if (++count > providerTraits.Traits.Count + 1)
                {
                    index = -1;
                    break;
                }
            }
            while (index != -1 && (pawnFilter.Traits.Contains(providerTraits.Traits[index]) || disallowedTraitDefs.Contains(providerTraits.Traits[index].def)));

            Trait newTrait = null;
            if (index > -1)
            {
                newTrait = providerTraits.Traits[index];
            }
            TraitUpdated(traitIndex, newTrait);
        }

        protected void SelectPreviousTrait(int traitIndex)
        {
            Trait currentTrait = pawnFilter.Traits[traitIndex];
            ComputeDisallowedTraits(currentTrait);
            int index = -1;
            if (currentTrait != null)
            {
                index = providerTraits.Traits.FindIndex((Trait t) => {
                    return t.Label.Equals(currentTrait.Label);
                });
            }
            int count = 0;
            do
            {
                index--;
                if (index < 0)
                {
                    index = providerTraits.Traits.Count - 1;
                }
                if (++count > providerTraits.Traits.Count + 1)
                {
                    index = -1;
                    break;
                }
            }
            while (index != -1 && (pawnFilter.Traits.Contains(providerTraits.Traits[index]) || disallowedTraitDefs.Contains(providerTraits.Traits[index].def)));

            Trait newTrait = null;
            if (index > -1)
            {
                newTrait = providerTraits.Traits[index];
            }
            TraitUpdated(traitIndex, newTrait);
        }

        protected void ClearTrait(int traitIndex)
        {
            TraitUpdated(traitIndex, null);
        }

        public void ScrollToTop()
        {
            scrollView.ScrollToTop();
        }
        public void ScrollToBottom()
        {
            scrollView.ScrollToBottom();
        }
    }
}
