﻿// Manager/Utilities.cs
// 
// Copyright Karel Kroeze, 2015.
// 
// Created 2015-11-04 19:28

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FM
{
    public static class Utilities
    {
        // globals
        public const float Margin = 6f;

        public const float ListEntryHeight = 50f;
        public static Texture2D SlightlyDarkBackground = SolidColorMaterials.NewSolidColorTexture( 0f, 0f, 0f, .1f );
        public static Texture2D DeleteX = ContentFinder< Texture2D >.Get( "UI/Buttons/Delete", true );

        public static Dictionary< ThingFilter, Cache > CountCache = new Dictionary< ThingFilter, Cache >();

        private static bool TryGetCached( ThingFilter filter, out int count )
        {
            if ( CountCache.ContainsKey( filter ) )
            {
                Cache cache = CountCache[filter];
                if ( Find.TickManager.TicksGame - cache.lastCache < 250 )
                {
                    count = cache.cachedCount;
                    return true;
                }
            }
#if DEBUG_COUNTS
            Log.Message("not cached");
#endif
            count = 0;
            return false;
        }

        public static string TimeString( this int ticks )
        {
            int days = ticks / GenDate.TicksPerDay,
                hours = ticks % GenDate.TicksPerDay / GenDate.TicksPerHour;

            string s = string.Empty;

            if ( days > 0 )
            {
                s += days + "LetterDay".Translate() + " ";
            }
            s += hours + "LetterHour".Translate();

            return s;
        }

        public static int CountProducts( ThingFilter filter )
        {
            var count = 0;
            if ( filter != null &&
                 TryGetCached( filter, out count ) )
            {
                return count;
            }

#if DEBUG_COUNTS
            Log.Message("Obtaining new count");
#endif

            if ( filter != null )
            {
                foreach ( ThingDef td in filter.AllowedThingDefs )
                {
                    // if it counts as a resource, use the ingame counter (e.g. only steel in stockpiles.)
                    if ( td.CountAsResource )
                    {
#if DEBUG_COUNTS
                        Log.Message(td.LabelCap + ", " + Find.ResourceCounter.GetCount(td));
#endif
                        count += Find.ResourceCounter.GetCount( td );
                    }
                    else
                    {
                        foreach ( Thing t in Find.ListerThings.ThingsOfDef( td ) )
                        {
                            // otherwise, go look for stuff that matches our filters.
                            // TODO: does this catch minified things?
                            QualityCategory quality;
                            if ( t.TryGetQuality( out quality ) )
                            {
                                if ( !filter.AllowedQualityLevels.Includes( quality ) )
                                {
                                    continue;
                                }
                            }
                            if ( filter.AllowedHitPointsPercents.IncludesEpsilon( t.HitPoints ) )
                            {
                                continue;
                            }

#if DEBUG_COUNTS
                            Log.Message(t.LabelCap + ": " + CountProducts(t));
#endif

                            count += t.stackCount;
                        }
                    }
                }

                // update cache if exists.
                if ( CountCache.ContainsKey( filter ) )
                {
                    CountCache[filter].cachedCount = count;
                    CountCache[filter].lastCache = Find.TickManager.TicksGame;
                }
                else
                {
                    CountCache.Add( filter, new Cache( count ) );
                }
            }
            return count;
        }

        public static bool IsInt( this string text )
        {
            int num;
            return int.TryParse( text, out num );
        }

        public static void DrawToggle( Rect rect, string label, ref bool checkOn, float size = 24f,
                                       float margin = Margin )
        {
            // set up rects
            Rect labelRect = rect;
            var checkRect = new Rect( rect.xMax - size - margin * 2, 0f, size, size );

            // finetune rects
            labelRect.xMin += margin;
            checkRect = checkRect.CenteredOnYIn( labelRect );

            // draw label
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label( labelRect, label );
            Text.Anchor = TextAnchor.UpperLeft;

            // draw check
            if ( checkOn )
            {
                GUI.DrawTexture( checkRect, Widgets.CheckboxOnTex );
            }
            else
            {
                GUI.DrawTexture( checkRect, Widgets.CheckboxOffTex );
            }

            // interactivity
            Widgets.DrawHighlightIfMouseover( rect );
            if ( Widgets.InvisibleButton( rect ) )
            {
                checkOn = !checkOn;
            }
        }

        public static void DrawToggle( Rect rect, string label, bool checkOn, Action on, Action off, float size = 24f,
                                       float margin = Margin )
        {
            // set up rects
            Rect labelRect = rect;
            var checkRect = new Rect( rect.xMax - size - margin * 2, 0f, size, size );

            // finetune rects
            labelRect.xMin += margin;
            checkRect = checkRect.CenteredOnYIn( labelRect );

            // draw label
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label( labelRect, label );
            Text.Anchor = TextAnchor.UpperLeft;

            // draw check
            if ( checkOn )
            {
                GUI.DrawTexture( checkRect, Widgets.CheckboxOnTex );
            }
            else
            {
                GUI.DrawTexture( checkRect, Widgets.CheckboxOffTex );
            }

            // interactivity
            Widgets.DrawHighlightIfMouseover( rect );
            if ( Widgets.InvisibleButton( rect ) )
            {
                if ( checkOn )
                {
                    off();
                }
                else
                {
                    on();
                }
            }
        }

        public static void DrawToggle( Rect rect, string label, bool checkOn, Action toggle, float size = 24f,
                                       float margin = Margin )
        {
            DrawToggle( rect, label, checkOn, toggle, toggle, size );
        }

        // count cache for multiple products
        public class Cache
        {
            public int cachedCount;
            public int lastCache;

            public Cache( int count )
            {
                cachedCount = count;
                lastCache = Find.TickManager.TicksGame;
            }
        }
    }
}