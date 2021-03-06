﻿// Karel Kroeze
// ManagerTab_Livestock.cs
// 2016-12-09

using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FluffyManager
{
    public class ManagerTab_Livestock : ManagerTab
    {
        #region Fields

        private float _actualHeight = 999f;

        private Vector2 _animalsScrollPosition = Vector2.zero;

        private List<PawnKindDef> _availablePawnKinds;

        private List<ManagerJob_Livestock> _currentJobs;

        private float _entryHeight = 30f;

        private float _listEntryHeight = Utilities.LargeListEntryHeight;

        // init with 5's if new job.
        private Dictionary<Utilities_Livestock.AgeAndSex, string> _newCounts =
            Utilities_Livestock.AgeSexArray.ToDictionary( k => k, v => "5" );

        private bool _onCurrentTab;

        private Vector2 _scrollPosition = Vector2.zero;

        private PawnKindDef _selectedAvailable;

        private ManagerJob_Livestock _selectedCurrent;

        private float _smallIconSize = Utilities.SmallIconSize;

        private float _topAreaHeight = 30f;

        #endregion Fields

        #region Constructors

        public ManagerTab_Livestock( Manager manager ) : base( manager )
        {
        }

        #endregion Constructors



        #region Properties

        public override Texture2D Icon => Resources.IconLivestock;

        // public override Texture2D Icon {                       get; }
        public override IconAreas IconArea => IconAreas.Middle;

        public override string Label => "FML.Livestock".Translate();

        public override ManagerJob Selected
        {
            get
            {
                return _selectedCurrent;
            }
            set
            {
                // set tab to current if we're moving to an actual job.
                // in either case, available selection can be cleared.
                _onCurrentTab = value != null;
                _selectedAvailable = null;
                _selectedCurrent = (ManagerJob_Livestock)value;
                _newCounts = _selectedCurrent?.Trigger?.CountTargets.ToDictionary( k => k.Key, v => v.Value.ToString() );
            }
        }

        #endregion Properties



        #region Methods

        public override void DoWindowContents( Rect canvas )
        {
            var leftRow = new Rect( 0f, 31f, DefaultLeftRowSize, canvas.height - 31f );
            var contentCanvas = new Rect( leftRow.xMax + Utilities.Margin, 0f,
                                          canvas.width - leftRow.width - Utilities.Margin, canvas.height );

            DoLeftRow( leftRow );
            DoContent( contentCanvas );
        }

        public override void PreOpen()
        {
            Refresh();
        }

        private void DoContent( Rect rect )
        {
            // cop out if nothing is selected.
            if ( _selectedCurrent == null )
            {
                return;
            }

            // background
            Widgets.DrawMenuSection( rect );

            // begin window
            GUI.BeginGroup( rect );
            rect = rect.AtZero();

            // rects
            var optionsColumnRect = new Rect( Utilities.Margin / 2,
                                              _topAreaHeight,
                                              rect.width / 2 - Utilities.Margin,
                                              rect.height - _topAreaHeight - Utilities.Margin - Utilities.ButtonSize.y );
            var animalsRect = new Rect( optionsColumnRect.xMax + Utilities.Margin,
                                        _topAreaHeight,
                                        rect.width / 2 - Utilities.Margin,
                                        rect.height - _topAreaHeight - Utilities.Margin - Utilities.ButtonSize.y );

            var optionsColumnTitle = new Rect( optionsColumnRect.xMin,
                                               0f,
                                               optionsColumnRect.width,
                                               _topAreaHeight );
            var animalsColumnTitle = new Rect( animalsRect.xMin,
                                               0f,
                                               animalsRect.width,
                                               _topAreaHeight );

            // backgrounds
            GUI.DrawTexture( optionsColumnRect, Resources.SlightlyDarkBackground );
            GUI.DrawTexture( animalsRect, Resources.SlightlyDarkBackground );

            // titles
            Utilities.Label( optionsColumnTitle, "FMP.Options".Translate(),
                             anchor: TextAnchor.LowerLeft, lrMargin: Utilities.Margin * 2, font: GameFont.Tiny );
            Utilities.Label( animalsColumnTitle, "FML.Animals".Translate(),
                             anchor: TextAnchor.LowerLeft, lrMargin: Utilities.Margin * 2, font: GameFont.Tiny );

            // options
            GUI.BeginGroup( optionsColumnRect );
            Vector2 cur = Vector2.zero;
            var optionIndex = 1;

            // counts header
            Utilities.Label( ref cur, optionsColumnRect.width, _entryHeight, "FML.TargetCounts".Translate(),
                             alt: optionIndex % 2 == 0 );

            // counts table
            var cols = 3;
            float fifth = optionsColumnRect.width / 5;
            float[] widths = { fifth, fifth * 2, fifth * 2 };
            float[] heights = { _entryHeight / 3 * 2, _entryHeight, _entryHeight };

            // set up a 3x3 table of rects
            var countRects = new Rect[cols, cols];
            for ( var x = 0; x < cols; x++ )
            {
                for ( var y = 0; y < cols; y++ )
                {
                    // kindof overkill for a 3x3 table, but ok.
                    countRects[x, y] = new Rect( widths.Take( x ).Sum(), cur.y + heights.Take( y ).Sum(), widths[x],
                                                 heights[y] );
                    if ( optionIndex % 2 == 0 )
                    {
                        Widgets.DrawAltRect( countRects[x, y] );
                    }
                }
            }

            optionIndex++;

            // headers
            Utilities.Label( countRects[1, 0], Gender.Female.ToString(), null, TextAnchor.LowerCenter,
                             font: GameFont.Tiny );
            Utilities.Label( countRects[2, 0], Gender.Male.ToString(), null, TextAnchor.LowerCenter, font: GameFont.Tiny );
            Utilities.Label( countRects[0, 1], "FML.Adult".Translate(), null, TextAnchor.MiddleRight,
                             font: GameFont.Tiny );
            Utilities.Label( countRects[0, 2], "FML.Juvenile".Translate(), null, TextAnchor.MiddleRight,
                             font: GameFont.Tiny );

            // fields
            DoCountField( countRects[1, 1], Utilities_Livestock.AgeAndSex.AdultFemale );
            DoCountField( countRects[2, 1], Utilities_Livestock.AgeAndSex.AdultMale );
            DoCountField( countRects[1, 2], Utilities_Livestock.AgeAndSex.JuvenileFemale );
            DoCountField( countRects[2, 2], Utilities_Livestock.AgeAndSex.JuvenileMale );
            cur.y += 3 * _entryHeight;

            // restrict to area
            var restrictAreaRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
            if ( optionIndex % 2 == 0 )
            {
                Widgets.DrawAltRect( restrictAreaRect );
            }
            Utilities.DrawToggle( restrictAreaRect, "FML.RestrictToArea".Translate(),
                                  ref _selectedCurrent.RestrictToArea );
            cur.y += _entryHeight;
            if ( _selectedCurrent.RestrictToArea )
            {
                // area selectors table
                // set up a 3x3 table of rects
                var areaRects = new Rect[cols, cols];
                for ( var x = 0; x < cols; x++ )
                {
                    for ( var y = 0; y < cols; y++ )
                    {
                        // kindof overkill for a 3x3 table, but ok.
                        areaRects[x, y] = new Rect( widths.Take( x ).Sum(), cur.y + heights.Take( y ).Sum(), widths[x],
                                                    heights[y] );
                        if ( optionIndex % 2 == 0 )
                        {
                            Widgets.DrawAltRect( areaRects[x, y] );
                        }
                    }
                }

                // headers
                Utilities.Label( areaRects[1, 0], Gender.Female.ToString(), null, TextAnchor.LowerCenter,
                                 font: GameFont.Tiny );
                Utilities.Label( areaRects[2, 0], Gender.Male.ToString(), null, TextAnchor.LowerCenter,
                                 font: GameFont.Tiny );
                Utilities.Label( areaRects[0, 1], "FML.Adult".Translate(), null, TextAnchor.MiddleRight,
                                 font: GameFont.Tiny );
                Utilities.Label( areaRects[0, 2], "FML.Juvenile".Translate(), null, TextAnchor.MiddleRight,
                                 font: GameFont.Tiny );

                // do the selectors
                _selectedCurrent.RestrictArea[0] = AreaAllowedGUI.DoAllowedAreaSelectors( areaRects[1, 1],
                                                                                          _selectedCurrent.RestrictArea[
                                                                                                                        0
                                                                                              ],
                                                                                          manager,
                                                                                          AllowedAreaMode.Animal,
                                                                                          Utilities.Margin );
                _selectedCurrent.RestrictArea[1] = AreaAllowedGUI.DoAllowedAreaSelectors( areaRects[2, 1],
                                                                                          _selectedCurrent.RestrictArea[
                                                                                                                        1
                                                                                              ],
                                                                                          manager,
                                                                                          AllowedAreaMode.Animal,
                                                                                          Utilities.Margin );
                _selectedCurrent.RestrictArea[2] = AreaAllowedGUI.DoAllowedAreaSelectors( areaRects[1, 2],
                                                                                          _selectedCurrent.RestrictArea[
                                                                                                                        2
                                                                                              ],
                                                                                          manager,
                                                                                          AllowedAreaMode.Animal,
                                                                                          Utilities.Margin );
                _selectedCurrent.RestrictArea[3] = AreaAllowedGUI.DoAllowedAreaSelectors( areaRects[2, 2],
                                                                                          _selectedCurrent.RestrictArea[
                                                                                                                        3
                                                                                              ],
                                                                                          manager,
                                                                                          AllowedAreaMode.Animal,
                                                                                          Utilities.Margin );

                cur.y += 3 * _entryHeight;
            }

            optionIndex++;

            // train
            Utilities.Label( ref cur, optionsColumnRect.width, _entryHeight, "FML.Training".Translate(),
                             alt: optionIndex % 2 == 0 );
            var trainingRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
            if ( optionIndex++ % 2 == 0 )
            {
                Widgets.DrawAltRect( trainingRect );
            }
            _selectedCurrent.DrawTrainingSelector( trainingRect, Utilities.Margin );
            cur.y += _entryHeight;

            if ( _selectedCurrent.Training.Any )
            {
                var trainYoungRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
                if ( optionIndex++ % 2 == 0 )
                    Widgets.DrawAltRect( trainYoungRect );
                Utilities.DrawToggle( trainYoungRect, "FML.TrainYoung".Translate(),
                                      ref _selectedCurrent.Training.TrainYoung );
                cur.y += _entryHeight;
            }

            // butchery stuff
            var butcherExcessRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
            if ( optionIndex % 2 == 0 )
                Widgets.DrawAltRect( butcherExcessRect );
            Utilities.DrawToggle( butcherExcessRect, "FML.ButcherExcess".Translate(), ref _selectedCurrent.ButcherExcess );
            cur.y += _entryHeight;

            if ( _selectedCurrent.ButcherExcess )
            {
                var butcherOptionsRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
                if (optionIndex % 2 == 0)
                    Widgets.DrawAltRect(butcherOptionsRect);
                var usableWidth = optionsColumnRect.width - Utilities.Margin * 2f;
                var butcherOptionRect = new Rect( cur.x + Utilities.Margin, cur.y, usableWidth / 3f, _entryHeight );

                Utilities.DrawToggle(butcherOptionRect, "FML.ButcherTrained".Translate(),
                                      ref _selectedCurrent.ButcherTrained, 16f, 0f, GameFont.Tiny );
                butcherOptionRect.x += usableWidth / 3f;

                Utilities.DrawToggle(butcherOptionRect, "FML.ButcherPregnant".Translate(),
                                      ref _selectedCurrent.ButcherPregnant, 16f, 0f, GameFont.Tiny );
                butcherOptionRect.x += usableWidth / 3f;

                Utilities.DrawToggle(butcherOptionRect, "FML.ButcherBonded".Translate(),
                                      ref _selectedCurrent.ButcherBonded, 16f, 0f, GameFont.Tiny );

                cur.y += _entryHeight;
            }
            optionIndex++;
            
            var sendToSlaughterAreaRect = new Rect(cur.x, cur.y, optionsColumnRect.width, _entryHeight);
            if (optionIndex % 2 == 0)
                Widgets.DrawAltRect(sendToSlaughterAreaRect);
            Utilities.DrawToggle(sendToSlaughterAreaRect, "FML.SendToSlaughterArea".Translate(),
                                  ref _selectedCurrent.SendToSlaughterArea );
            cur.y += _entryHeight;

            if ( _selectedCurrent.SendToSlaughterArea )
            {
                var slaughterAreaLabelRect = new Rect(cur.x + Utilities.Margin, cur.y, optionsColumnRect.width, _entryHeight);
                Widgets.Label( slaughterAreaLabelRect, "FML.SlaughterArea".Translate() );
                cur.y += _entryHeight;

                var slaughterAreaRect = new Rect(cur.x, cur.y, optionsColumnRect.width, _entryHeight);
                AreaAllowedGUI.DoAllowedAreaSelectors(slaughterAreaRect, ref _selectedCurrent.SlaughterArea, manager, AllowedAreaMode.Animal, Utilities.Margin );
                cur.y += _entryHeight;

                if ( optionIndex % 2 == 0 )
                {
                    Widgets.DrawAltRect(slaughterAreaLabelRect);
                    Widgets.DrawAltRect(sendToSlaughterAreaRect);
                }
            }
            optionIndex++;

            // try tame more?
            var tameMoreRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
            cur.y += _entryHeight;

            Utilities.DrawToggle( tameMoreRect, "FML.TameMore".Translate(), ref _selectedCurrent.TryTameMore );
            if ( optionIndex % 2 == 0 )
            {
                Widgets.DrawAltRect( tameMoreRect );
            }

            // area to train from (if taming more);
            if ( _selectedCurrent.TryTameMore )
            {
                var tameAreaRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
                if ( optionIndex % 2 == 0 )
                {
                    Widgets.DrawAltRect( tameAreaRect );
                }
                cur.y += _entryHeight;
                var tameAreaSelectorRect = new Rect( cur.x, cur.y, optionsColumnRect.width, _entryHeight );
                if ( optionIndex % 2 == 0 )
                {
                    Widgets.DrawAltRect( tameAreaSelectorRect );
                }
                cur.y += _entryHeight;

                Utilities.Label( tameAreaRect, "FML.TameArea".Translate() );
                AreaAllowedGUI.DoAllowedAreaSelectors( tameAreaSelectorRect, ref _selectedCurrent.TameArea, manager,
                                                       AllowedAreaMode.Any, Utilities.Margin );

                // why am I getting an error for not being at upperleft? Oh well, force it.
                Text.Anchor = TextAnchor.UpperLeft;
            }
            optionIndex++;

            GUI.EndGroup(); // options

            // Start animals list
            // get our pawnkind
            PawnKindDef pawnKind = _onCurrentTab ? _selectedCurrent.Trigger.pawnKind : _selectedAvailable;
            if ( pawnKind != null )
            {
                Rect viewRect = animalsRect;
                viewRect.height = _actualHeight;
                if ( _actualHeight > animalsRect.height )
                    viewRect.width -= 16f;

                Widgets.BeginScrollView( animalsRect, ref _animalsScrollPosition, viewRect );
                GUI.BeginGroup( viewRect );
                cur = Vector2.zero;

                // tamed animals
                DrawAnimalListheader( ref cur, new Vector2( viewRect.width, _entryHeight / 3 * 2 ), pawnKind,
                                      "FML.Tame".Translate().CapitalizeFirst() );
                List<Pawn> tame = pawnKind.GetTame( manager );
                if ( tame.Count == 0 )
                {
                    Utilities.Label( ref cur, viewRect.width, _entryHeight,
                                     "FML.NoAnimals".Translate( "FML.Tame".Translate() ),
                                     anchor: TextAnchor.MiddleCenter, color: Color.grey );
                }
                for ( var i = 0; i < tame.Count; i++ )
                {
                    DrawAnimalRow( ref cur, new Vector2( viewRect.width, _entryHeight ), tame[i], i % 2 == 0 );
                }

                cur.y += _entryHeight;

                // wild animals
                DrawAnimalListheader( ref cur, new Vector2( viewRect.width, _entryHeight / 3 * 2 ), pawnKind,
                                      "FML.Wild".Translate().CapitalizeFirst() );
                List<Pawn> wild = pawnKind.GetWild( manager );
                if ( wild.Count == 0 )
                {
                    Utilities.Label( ref cur, viewRect.width, _entryHeight,
                                     "FML.NoAnimals".Translate( "FML.Wild".Translate() ), null, TextAnchor.MiddleCenter,
                                     color: Color.grey );
                }
                for ( var i = 0; i < wild.Count; i++ )
                {
                    DrawAnimalRow( ref cur, new Vector2( animalsRect.width, _entryHeight ), wild[i], i % 2 == 0 );
                }

                // update list height
                _actualHeight = cur.y;

                GUI.EndGroup(); // animals
                Widgets.EndScrollView();
            }

            // bottom button
            var buttonRect = new Rect( rect.xMax - Utilities.ButtonSize.x, rect.yMax - Utilities.ButtonSize.y,
                                       Utilities.ButtonSize.x - Utilities.Margin,
                                       Utilities.ButtonSize.y - Utilities.Margin );

            // add / remove to the stack
            if ( _selectedCurrent.Managed )
            {
                if ( Widgets.ButtonText( buttonRect, "FM.Delete".Translate() ) )
                {
                    _selectedCurrent.Delete();
                    _selectedCurrent = null;
                    _onCurrentTab = false;
                    Refresh();
                    return; // just skip to the next tick to avoid null reference errors.
                }

                TooltipHandler.TipRegion( buttonRect, "FMP.DeleteBillTooltip".Translate() );
            }
            else
            {
                if ( Widgets.ButtonText( buttonRect, "FM.Manage".Translate() ) )
                {
                    _selectedCurrent.Managed = true;
                    _onCurrentTab = true;
                    Manager.For( manager ).JobStack.Add( _selectedCurrent );
                    Refresh();
                }
                TooltipHandler.TipRegion( buttonRect, "FMP.ManageBillTooltip".Translate() );
            }

            GUI.EndGroup(); // window
        }

        private void DoCountField( Rect rect, Utilities_Livestock.AgeAndSex ageSex )
        {
            if ( _newCounts == null ||
                 _newCounts[ageSex] == null )
            {
                _newCounts = _selectedCurrent?.Trigger?.CountTargets.ToDictionary( k => k.Key, v => v.Value.ToString() );
            }

            if ( !_newCounts[ageSex].IsInt() )
            {
                GUI.color = Color.red;
            }
            else
            {
                _selectedCurrent.Trigger.CountTargets[ageSex] = int.Parse( _newCounts[ageSex] );
            }
            _newCounts[ageSex] = Widgets.TextField( rect.ContractedBy( 1f ), _newCounts[ageSex] );
            GUI.color = Color.white;
        }

        private void DoLeftRow( Rect rect )
        {
            // background (minus top line so we can draw tabs.)
            Widgets.DrawMenuSection( rect, false );

            // tabs
            var tabs = new List<TabRecord>();
            var availableTabRecord = new TabRecord( "FMP.Available".Translate(), delegate
                                                                                     {
                                                                                         _onCurrentTab = false;
                                                                                         Refresh();
                                                                                     }, !_onCurrentTab );
            tabs.Add( availableTabRecord );
            var currentTabRecord = new TabRecord( "FMP.Current".Translate(), delegate
                                                                                 {
                                                                                     _onCurrentTab = true;
                                                                                     Refresh();
                                                                                 }, _onCurrentTab );
            tabs.Add( currentTabRecord );

            TabDrawer.DrawTabs( rect, tabs );

            // start the actual content.
            Rect outRect = rect;
            Rect viewRect = outRect.AtZero();

            if ( _onCurrentTab )
            {
                DrawCurrentJobList( outRect, viewRect );
            }
            else
            {
                DrawAvailableJobList( outRect, viewRect );
            }
        }

        private void DrawAnimalListheader( ref Vector2 cur, Vector2 size, PawnKindDef pawnKind, string header )
        {
            // use a third of available screenspace for labels
            var headerRect = new Rect( cur.x, cur.y, size.x / 3f, size.y );
            Utilities.Label( headerRect, header, anchor: TextAnchor.MiddleCenter, font: GameFont.Tiny );
            cur.x += size.x / 3f;

            // gender, lifestage, current meat (and if applicable, milking + shearing)
            var cols = 3;

            // extra columns?
            bool milk = pawnKind.Milkable();
            bool wool = pawnKind.Shearable();
            if ( milk )
            {
                cols++;
            }
            if ( wool )
            {
                cols++;
            }
            float colwidth = size.x * 2 / 3 / cols;

            // gender header
            var genderRect = new Rect( cur.x, cur.y, colwidth, size.y );
            Rect genderMale =
                new Rect( 0f, 0f, _smallIconSize, _smallIconSize ).CenteredIn( genderRect, -_smallIconSize / 2 );
            Rect genderFemale =
                new Rect( 0f, 0f, _smallIconSize, _smallIconSize ).CenteredIn( genderRect, _smallIconSize / 2 );
            GUI.DrawTexture( genderMale, Resources.MaleIcon );
            GUI.DrawTexture( genderFemale, Resources.FemaleIcon );
            TooltipHandler.TipRegion( genderRect, "FML.GenderHeader".Translate() );
            cur.x += colwidth;

            // lifestage header
            var ageRect = new Rect( cur.x, cur.y, colwidth, size.y );
            Rect ageRectC = new Rect( 0f, 0f, _smallIconSize, _smallIconSize ).CenteredIn( ageRect, _smallIconSize / 2 );
            Rect ageRectB = new Rect( 0f, 0f, _smallIconSize, _smallIconSize ).CenteredIn( ageRect );
            Rect ageRectA = new Rect( 0f, 0f, _smallIconSize, _smallIconSize ).CenteredIn( ageRect, -_smallIconSize / 2 );
            GUI.DrawTexture( ageRectC, Resources.LifeStages[2] );
            GUI.DrawTexture( ageRectB, Resources.LifeStages[1] );
            GUI.DrawTexture( ageRectA, Resources.LifeStages[0] );
            TooltipHandler.TipRegion( ageRect, "FML.AgeHeader".Translate() );
            cur.x += colwidth;

            // meat header
            var meatRect = new Rect( cur.x, cur.y, colwidth, size.y );
            Rect meatIconRect =
                new Rect( 0f, 0f, _smallIconSize, _smallIconSize ).CenteredIn( meatRect );
            GUI.DrawTexture( meatIconRect, Resources.MeatIcon );
            TooltipHandler.TipRegion( meatRect, "FML.MeatHeader".Translate() );
            cur.x += colwidth;

            // milk header
            if ( milk )
            {
                var milkRect = new Rect( cur.x, cur.y, colwidth, size.y );
                Rect milkIconRect =
                    new Rect( 0f, 0f, _smallIconSize, _smallIconSize ).CenteredIn( milkRect );
                GUI.DrawTexture( milkIconRect, Resources.MilkIcon );
                TooltipHandler.TipRegion( milkRect, "FML.MilkHeader".Translate() );
                cur.x += colwidth;
            }

            // wool header
            if ( wool )
            {
                var woolRect = new Rect( cur.x, cur.y, colwidth, size.y );
                Rect woolIconRect =
                    new Rect( 0f, 0f, Utilities.MediumIconSize, Utilities.MediumIconSize ).CenteredIn( woolRect );
                GUI.DrawTexture( woolIconRect, Resources.WoolIcon );
                TooltipHandler.TipRegion( woolRect, "FML.WoolHeader".Translate() );
                cur.x += colwidth;
            }

            // start next row
            cur.x = 0f;
            cur.y += size.y;
        }

        private void DrawAnimalRow( ref Vector2 cur, Vector2 size, Pawn p, bool alt )
        {
            // highlights and interactivity.
            var row = new Rect( cur.x, cur.y, size.x, size.y );
            if ( alt )
            {
                Widgets.DrawAltRect( row );
            }
            Widgets.DrawHighlightIfMouseover( row );
            if ( Widgets.ButtonInvisible( row ) )
            {
                // move camera and select
                Find.MainTabsRoot.EscapeCurrentTab();
                CameraJumper.TryJump( p.PositionHeld, p.Map );
                Find.Selector.ClearSelection();
                if ( p.Spawned )
                {
                    Find.Selector.Select( p );
                }
            }

            // use a third of available screenspace for labels
            var nameRect = new Rect( cur.x, cur.y, size.x / 3f, size.y );
            Utilities.Label( nameRect, p.LabelCap, anchor: TextAnchor.MiddleCenter, font: GameFont.Tiny );
            cur.x += size.x / 3f;

            // gender, lifestage, current meat (and if applicable, milking + shearing)
            var cols = 3;

            // extra columns?
            if ( p.kindDef.Milkable() )
                cols++;
            if ( p.kindDef.Shearable() )
                cols++;

            float colwidth = size.x * 2 / 3 / cols;

            // gender column
            var genderRect = new Rect( cur.x, cur.y, colwidth, size.y );
            Rect genderIconRect =
                new Rect( 0f, 0f, _smallIconSize, _smallIconSize ).CenteredIn( genderRect );
            switch ( p.gender )
            {
                case Gender.Female:
                    GUI.DrawTexture( genderIconRect, Resources.FemaleIcon );
                    break;

                case Gender.Male:
                    GUI.DrawTexture( genderIconRect, Resources.MaleIcon );
                    break;

                case Gender.None:
                    GUI.DrawTexture( genderIconRect, Resources.UnkownIcon );
                    break;
            }

            TooltipHandler.TipRegion( genderRect, p.gender.GetLabel() );
            cur.x += colwidth;

            // lifestage column
            var ageRect = new Rect( cur.x, cur.y, colwidth, size.y );
            Rect ageIconRect = new Rect( 0f, 0f, _smallIconSize, _smallIconSize ).CenteredIn( ageRect );
            GUI.DrawTexture( ageIconRect, Resources.LifeStages[p.ageTracker.CurLifeStageIndex] );
            TooltipHandler.TipRegion( ageRect, p.ageTracker.AgeTooltipString );
            cur.x += colwidth;

            // meat column
            var meatRect = new Rect( cur.x, cur.y, colwidth, size.y );
            // NOTE: When splitting tabs into separate mods; estimated meat count is defined in the Hunting helper.
            Utilities.Label( meatRect, p.EstimatedMeatCount().ToString(), p.EstimatedMeatCount().ToString(),
                             TextAnchor.MiddleCenter,
                             font: GameFont.Tiny );
            cur.x += colwidth;

            // milk column
            if ( p.Milkable() )
            {
                var milkRect = new Rect( cur.x, cur.y, colwidth, size.y );
                var comp = p.TryGetComp<CompMilkable>();
                Utilities.Label( milkRect, comp.Fullness.ToString( "0%" ),
                                 "FML.Yields".Translate( comp.Props.milkDef.LabelCap, comp.Props.milkAmount ),
                                 TextAnchor.MiddleCenter, font: GameFont.Tiny );
            }
            if ( p.kindDef.Milkable() )
                cur.x += colwidth;

            // wool column
            if ( p.Shearable() )
            {
                var woolRect = new Rect( cur.x, cur.y, colwidth, size.y );
                var comp = p.TryGetComp<CompShearable>();
                Utilities.Label( woolRect, comp.Fullness.ToString( "0%" ),
                                 "FML.Yields".Translate( comp.Props.woolDef.LabelCap, comp.Props.woolAmount ),
                                 TextAnchor.MiddleCenter, font: GameFont.Tiny );
            }
            if ( p.kindDef.Milkable() )
                cur.x += colwidth;

            // do the carriage return on ref cur
            cur.x = 0f;
            cur.y += size.y;
        }

        private void DrawAvailableJobList( Rect outRect, Rect viewRect )
        {
            // set sizes
            viewRect.height = _availablePawnKinds.Count * _listEntryHeight;
            if ( viewRect.height > outRect.height )
            {
                viewRect.width -= 16f;
            }

            Widgets.BeginScrollView( outRect, ref _scrollPosition, viewRect );
            GUI.BeginGroup( viewRect );

            for ( var i = 0; i < _availablePawnKinds.Count; i++ )
            {
                // set up rect
                var row = new Rect( 0f, _listEntryHeight * i, viewRect.width, _listEntryHeight );

                // highlights
                Widgets.DrawHighlightIfMouseover( row );
                if ( i % 2 == 0 )
                {
                    Widgets.DrawAltRect( row );
                }
                if ( _availablePawnKinds[i] == _selectedAvailable )
                {
                    Widgets.DrawHighlightSelected( row );
                }

                // draw label
                string label = _availablePawnKinds[i].LabelCap + "\n<i>" +
                               "FML.TameWildCount".Translate(
                                                             _availablePawnKinds[i].GetTame( manager ).Count,
                                                             _availablePawnKinds[i].GetWild( manager ).Count ) + "</i>";
                Utilities.Label( row, label, null, TextAnchor.MiddleLeft, Utilities.Margin * 2 );

                // button
                if ( Widgets.ButtonInvisible( row ) )
                {
                    _selectedAvailable = _availablePawnKinds[i]; // for highlighting to work
                    Selected = new ManagerJob_Livestock( _availablePawnKinds[i], manager ); // for details
                }
            }

            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        private void DrawCurrentJobList( Rect outRect, Rect viewRect )
        {
            // set sizes
            viewRect.height = _currentJobs.Count * _listEntryHeight;
            if ( viewRect.height > outRect.height )
            {
                viewRect.width -= 16f;
            }

            Widgets.BeginScrollView( outRect, ref _scrollPosition, viewRect );
            GUI.BeginGroup( viewRect );

            for ( var i = 0; i < _currentJobs.Count; i++ )
            {
                // set up rect
                var row = new Rect( 0f, _listEntryHeight * i, viewRect.width, _listEntryHeight );

                // highlights
                Widgets.DrawHighlightIfMouseover( row );
                if ( i % 2 == 0 )
                {
                    Widgets.DrawAltRect( row );
                }
                if ( _currentJobs[i] == _selectedCurrent )
                {
                    Widgets.DrawHighlightSelected( row );
                }

                // draw label
                _currentJobs[i].DrawListEntry( row, false, true );

                // button
                if ( Widgets.ButtonInvisible( row ) )
                {
                    Selected = _currentJobs[i];
                }
            }

            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        private void Refresh()
        {
            // currently managed
            _currentJobs = Manager.For( manager ).JobStack.FullStack<ManagerJob_Livestock>();

            // concatenate lists of animals on biome and animals in colony.
            _availablePawnKinds = manager.map.Biome.AllWildAnimals.ToList();
            _availablePawnKinds.AddRange(
                                         manager.map.mapPawns.AllPawns
                                                .Where( p => p.RaceProps.Animal )
                                                .Select( p => p.kindDef ) );
            _availablePawnKinds = _availablePawnKinds

                // get distinct pawnkinds from the merges
                .Distinct()

                // remove already managed pawnkinds
                .Where( pk => !_currentJobs.Select( job => job.Trigger.pawnKind ).Contains( pk ) )

                // order by label
                .OrderBy( def => def.LabelCap )
                .ToList();
        }

        #endregion Methods
    }
}
