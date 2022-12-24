using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace StockpileLimit
{
    [HarmonyPatch(typeof(ThingFilterUI), "DoThingFilterConfigWindow")]
    public class ThingFilterUIWindowPatch
    {
        public static string buffer;
        private static ISlotGroupParent selected;
        private static Dictionary<int, string> menuLimit = new Dictionary<int, string>()
        {
            [-1] = "No limit",
            [0] = "0",
            [1] = "1",
            [2] = "2",
            [5] = "5",
            [10] = "10",
        };
        private static Dictionary<int, string> tooltipLimit = new Dictionary<int, string>()
        {
            [-1] = "No limit is default, like vanilla.",
            [0] = "Keep empty, like there's no stockpile",
        };
        private static string otherlimit_text = "Other limit:";
        private static Dictionary<int, string> menuRefill = new Dictionary<int, string>()
        {
            [100] = "Full",
            [50] = "Half",
            [0] = "Empty",
        };
        private static Dictionary<int, string> tooltipRefill = new Dictionary<int, string>()
        {
            [100] = "Full is default. Always keep fully stocked, like vanilla.",
            [0] = "Start refilling when empty",
        };
        private static string otherrefill_text = "Other percent:";

        public static void Prefix(ref Rect rect)
        {
            selected = Find.Selector.SingleSelectedObject as ISlotGroupParent;
            if (selected != null)
            {
                rect.yMin += 90f;
            }
        }

        public static void Postfix(ref Rect rect)
        {
            if (selected == null)
            {
                return;
            }

            if (Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape))
            {
                UI.UnfocusCurrentControl();
                Event.current.Use();
            }

            var settings = selected.GetStoreSettings();

            DoSetLimitRect(rect, settings);
            DoRefillAtRect(rect, settings);
            DoPauseRefillRect(rect, settings);
        }

        public static void DoSetLimitRect(Rect rect, StorageSettings settings)
        {
            int limit = settings.GetStackLimit();

            Rect drawArea = new Rect(rect.xMin, rect.yMin - 48f - 3f - 90f, rect.width, 24f);
            drawArea.SplitVerticallyWithMargin(out Rect labelArea, out drawArea, out var _, 5f, 70f);
            drawArea.SplitVerticallyWithMargin(out Rect buttonArea, out drawArea, out var _, 5f, 100f);
            Rect inputArea = drawArea.LeftPartPixels(50f);
            Widgets.Label(labelArea, "Set limit");
            TooltipHandler.TipRegion(labelArea, "Set an upper limit for each tile in this stockpile. The limit is strictly in effect for pawns. Pawns will not haul items exceeding the upper limit.");
            if (tooltipLimit.ContainsKey(limit))
            {
                TooltipHandler.TipRegion(buttonArea, tooltipLimit[limit]);
            }
            if (Widgets.ButtonText(buttonArea, menuLimit.ContainsKey(limit) ? menuLimit[limit] : otherlimit_text))
            {
                var options = new List<FloatMenuOption>(
                    menuLimit.Select(p => new FloatMenuOption(
                        p.Value, () => settings.SetStackLimitAndNotifyChange(p.Key))));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            buffer = null;
            var new_limit = limit;
            Widgets.TextFieldNumeric(inputArea, ref new_limit, ref buffer, -1, AdditionalStorageSettings.MaxLimit);
            if (new_limit != limit)
            {
                settings.SetStackLimitAndNotifyChange(new_limit);
            }
        }

        public static void DoRefillAtRect(Rect rect, StorageSettings settings)
        {
            var refillpercent = settings.GetRefillPercent();
            Rect drawArea = new Rect(rect.xMin, rect.yMin - 48f - 3f - 60f, rect.width, 24f);
            drawArea.SplitVerticallyWithMargin(out Rect labelArea, out drawArea, out var _, 5f, 70f);
            drawArea.SplitVerticallyWithMargin(out Rect buttonArea, out drawArea, out var _, 5f, 100f);
            drawArea.SplitVerticallyWithMargin(out Rect inputArea, out drawArea, out var _, 5f, 50f);
            Widgets.Label(labelArea, "Refill at");

            TooltipHandler.TipRegion(labelArea, "Set the refill threshold of *each tile* in this stockpile. This can be considered as the lower limit.");

            if (tooltipRefill.ContainsKey(refillpercent))
            {
                TooltipHandler.TipRegion(buttonArea, tooltipRefill[refillpercent]);
            }

            if (Widgets.ButtonText(buttonArea, menuRefill.ContainsKey(refillpercent) ? menuRefill[refillpercent] : otherrefill_text))
            {
                var options = new List<FloatMenuOption>(menuRefill.Select(p => new FloatMenuOption(p.Value, () =>
                settings.SetRefillPercent(p.Key))));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            buffer = null;
            var new_refillpercent = refillpercent;
            Widgets.TextFieldNumeric(inputArea, ref new_refillpercent, ref buffer, 0, 100);
            if (new_refillpercent != refillpercent) settings.SetRefillPercent(new_refillpercent);
            Widgets.Label(drawArea, "%");
        }

        public static void DoPauseRefillRect(Rect rect, StorageSettings settings)
        {
            Rect drawArea = new Rect(rect.xMin, rect.yMin - 48f - 3f - 30f, rect.width, 24f);
            var checkOn = settings.IsRefillingDisabled();
            var new_checkOn = checkOn;
            TooltipHandler.TipRegion(drawArea, "Disable refilling for all tiles in this stockpile.");
            Widgets.CheckboxLabeled(drawArea, "Pause refilling", ref new_checkOn, placeCheckboxNearText: true);
            if (new_checkOn != checkOn)
            {
                if (new_checkOn)
                {
                    settings.SetRefillingDisabled();
                }
                else
                {
                    settings.UnsetRefillingDisabled();
                }
            }
        }
    }
}