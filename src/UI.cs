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

            StorageSettings settings = _selected.GetStoreSettings();

            DoSetLimitRect(rect, settings);
            DoRefillAtRect(rect, settings);
            DoPauseRefillRect(rect, settings);
        }

        public static void DoSetLimitRect(Rect rect, StorageSettings settings)
        {
            int currentValue = settings.GetStackLimit();

            Rect drawArea = new Rect(rect.xMin, rect.yMin - 48f - 3f - 90f, rect.width, 24f);
            drawArea.SplitVerticallyWithMargin(out Rect labelArea, out drawArea, out float _, 5f, 70f);
            drawArea.SplitVerticallyWithMargin(out Rect buttonArea, out drawArea, out  _, 5f, 100f);
            Rect inputArea = drawArea.LeftPartPixels(50f);

            Widgets.Label(
                labelArea,
                "stl.limit_set.label".Translate());
            TooltipHandler.TipRegion(
                labelArea,
                "stl.limit_set.tooltip".Translate());

            string buttonTooltipKey = $"stl.limit_set.{currentValue}.tooltip";
            if (buttonTooltipKey.CanTranslate())
            {
                TooltipHandler.TipRegion(buttonArea, buttonTooltipKey.Translate());
            }

            string buttonLabel = _labelCustom;
            if (_menuLimit.ContainsKey(currentValue))
            {
                buttonLabel = _menuLimit[currentValue];
            }
            if (Widgets.ButtonText(buttonArea, buttonLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>(
                    _menuLimit.Select(p => new FloatMenuOption(
                        p.Value, () => settings.SetStackLimitAndNotifyChange(p.Key))));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            _buffer = null;
            int newValue = currentValue;
            Widgets.TextFieldNumeric(inputArea, ref newValue, ref _buffer, -1, AdditionalStorageSettings.MaxLimit);
            if (newValue != currentValue)
            {
                settings.SetStackLimitAndNotifyChange(newValue);
            }
        }

        public static void DoRefillAtRect(Rect rect, StorageSettings settings)
        {
            int currentValue = settings.GetRefillPercent();

            Rect drawArea = new Rect(rect.xMin, rect.yMin - 48f - 3f - 60f, rect.width, 24f);
            drawArea.SplitVerticallyWithMargin(out Rect labelArea, out drawArea, out float _, 5f, 70f);
            drawArea.SplitVerticallyWithMargin(out Rect buttonArea, out drawArea, out float _, 5f, 100f);
            drawArea.SplitVerticallyWithMargin(out Rect inputArea, out drawArea, out float _, 5f, 50f);

            Widgets.Label(
                labelArea,
                "stl.refill_at.label".Translate());
            TooltipHandler.TipRegion(
                labelArea,
                "stl.refill_at.tooltip".Translate());

            string buttonTooltipKey = $"stl.refill_at.{currentValue}.tooltip";
            if (buttonTooltipKey.CanTranslate())
            {
                TooltipHandler.TipRegion(buttonArea, buttonTooltipKey.Translate());
            }

            string buttonLabel =  _labelCustom;
            if (_menuRefill.ContainsKey(currentValue))
            {
                buttonLabel = _menuRefill[currentValue];
            }
            if (Widgets.ButtonText(buttonArea, buttonLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>(
                    _menuRefill.Select(p => new FloatMenuOption(
                        p.Value, () => settings.SetRefillPercent(p.Key))));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            _buffer = null;
            int newValue = currentValue;
            Widgets.TextFieldNumeric(inputArea, ref newValue, ref _buffer, 0, 100);
            if (newValue != currentValue)
            {
                settings.SetRefillPercent(newValue);
            }

            Widgets.Label(drawArea, "%");
        }

        public static void DoPauseRefillRect(Rect rect, StorageSettings settings)
        {
            bool currentValue = settings.IsRefillingDisabled();
            bool newValue = currentValue;

            Rect drawArea = new Rect(rect.xMin, rect.yMin - 48f - 3f - 30f, rect.width, 24f);

            TooltipHandler.TipRegion(
                drawArea,
                "stl.refill_pause.tooltip".Translate());
            Widgets.CheckboxLabeled(
                drawArea,
                "stl.refill_pause.label".Translate(),
                ref newValue,
                placeCheckboxNearText: true);

            if (newValue != currentValue)
            {
                if (newValue)
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