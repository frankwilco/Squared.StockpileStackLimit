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
        private static string _buffer;
        private static ISlotGroupParent _selected;

        private static readonly string _labelCustom = "stl.custom".Translate();
        private static readonly Dictionary<int, string> _menuLimit = new Dictionary<int, string>()
        {
            [-1] = "stl.limit_set.-1.label".Translate(),
            [0] = "0",
            [1] = "1",
            [2] = "2",
            [5] = "5",
            [10] = "10"
        };
        private static readonly Dictionary<int, string> _menuRefill = new Dictionary<int, string>()
        {
            [100] = "stl.refill_at.100.label".Translate(),
            [50] = "stl.refill_at.50.label".Translate(),
            [0] = "stl.refill_at.0.label".Translate()
        };

        public static void Prefix(ref Rect rect)
        {
            _selected = Find.Selector.SingleSelectedObject as ISlotGroupParent;
            if (_selected != null)
            {
                rect.yMin += 90f;
            }
        }

        public static void Postfix(ref Rect rect)
        {
            if (_selected == null)
            {
                return;
            }

            if (Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape))
            {
                UI.UnfocusCurrentControl();
                Event.current.Use();
            }

            var settings = _selected.GetStoreSettings();

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

            Widgets.Label(
                labelArea,
                "stl.limit_set.label".Translate());
            TooltipHandler.TipRegion(
                labelArea,
                "stl.limit_set.tooltip".Translate());

            string buttonTooltipKey = $"stl.limit_set.{limit}.tooltip";
            if (buttonTooltipKey.CanTranslate())
            {
                TooltipHandler.TipRegion(buttonArea, buttonTooltipKey.Translate());
            }

            string buttonLabel = _labelCustom;
            if (_menuLimit.ContainsKey(limit))
            {
                buttonLabel = _menuLimit[limit];
            }
            if (Widgets.ButtonText(buttonArea, buttonLabel))
            {
                var options = new List<FloatMenuOption>(
                    _menuLimit.Select(p => new FloatMenuOption(
                        p.Value, () => settings.SetStackLimitAndNotifyChange(p.Key))));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            _buffer = null;
            var new_limit = limit;
            Widgets.TextFieldNumeric(inputArea, ref new_limit, ref _buffer, -1, AdditionalStorageSettings.MaxLimit);
            if (new_limit != limit)
            {
                settings.SetStackLimitAndNotifyChange(new_limit);
            }
        }

        public static void DoRefillAtRect(Rect rect, StorageSettings settings)
        {
            var refillPercent = settings.GetRefillPercent();
            Rect drawArea = new Rect(rect.xMin, rect.yMin - 48f - 3f - 60f, rect.width, 24f);
            drawArea.SplitVerticallyWithMargin(out Rect labelArea, out drawArea, out var _, 5f, 70f);
            drawArea.SplitVerticallyWithMargin(out Rect buttonArea, out drawArea, out var _, 5f, 100f);
            drawArea.SplitVerticallyWithMargin(out Rect inputArea, out drawArea, out var _, 5f, 50f);

            Widgets.Label(
                labelArea,
                "stl.refill_at.label".Translate());
            TooltipHandler.TipRegion(
                labelArea,
                "stl.refill_at.tooltip".Translate());

            string buttonTooltipKey = $"stl.refill_at.{refillPercent}.tooltip";
            if (buttonTooltipKey.CanTranslate())
            {
                TooltipHandler.TipRegion(buttonArea, buttonTooltipKey.Translate());
            }

            string buttonLabel =  _labelCustom;
            if (_menuRefill.ContainsKey(refillPercent))
            {
                buttonLabel = _menuRefill[refillPercent];
            }
            if (Widgets.ButtonText(buttonArea, buttonLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>(
                    _menuRefill.Select(p => new FloatMenuOption(
                        p.Value, () => settings.SetRefillPercent(p.Key))));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            _buffer = null;
            var new_refillpercent = refillPercent;
            Widgets.TextFieldNumeric(inputArea, ref new_refillpercent, ref _buffer, 0, 100);
            if (new_refillpercent != refillPercent)
            {
                settings.SetRefillPercent(new_refillpercent);
            }

            Widgets.Label(drawArea, "%");
        }

        public static void DoPauseRefillRect(Rect rect, StorageSettings settings)
        {
            Rect drawArea = new Rect(rect.xMin, rect.yMin - 48f - 3f - 30f, rect.width, 24f);
            var checkOn = settings.IsRefillingDisabled();
            var new_checkOn = checkOn;

            TooltipHandler.TipRegion(
                drawArea,
                "stl.refill_pause.tooltip".Translate());
            Widgets.CheckboxLabeled(
                drawArea,
                "stl.refill_pause.label".Translate(),
                ref new_checkOn,
                placeCheckboxNearText: true);

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