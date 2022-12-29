using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StockpileLimit
{
    [HarmonyPatch(typeof(StorageSettings), nameof(StorageSettings.ExposeData))]
    public static class AdditionalStorageSettings
    {
        public static readonly Dictionary<StorageSettings, int> Limits =
            new Dictionary<StorageSettings, int>();
        public static readonly Dictionary<StorageSettings, int> RefillPercents =
            new Dictionary<StorageSettings, int>();
        public static readonly HashSet<StorageSettings> RefillingDisabled =
            new HashSet<StorageSettings>();

        public const int MaxLimit = 99999;
        public const int NoLimit = -1;
        public const int RefillFull = 100;

        public static void Postfix(StorageSettings __instance)
        {
            int limit;
            int refillPercent;
            bool refillingDisabled;

            switch (Scribe.mode)
            {
                case LoadSaveMode.Saving:
                    limit = __instance.GetStackLimit();
                    if (limit > NoLimit)
                    {
                        Scribe.saver.WriteElement("stacklimit", limit.ToString());
                    }
                    refillPercent = __instance.GetRefillPercent();
                    if (refillPercent < RefillFull)
                    {
                        Scribe.saver.WriteElement("refillpercent", refillPercent.ToString());
                    }
                    refillingDisabled = __instance.IsRefillingDisabled();
                    if (refillingDisabled)
                    {
                        Scribe.saver.WriteElement("refillingdisabled", refillingDisabled.ToString());
                    }
                    break;
                case LoadSaveMode.LoadingVars:
                    limit = ScribeExtractor.ValueFromNode(
                        Scribe.loader.curXmlParent["stacklimit"],
                        NoLimit);
                    __instance.SetStackLimit(limit);
                    refillPercent = ScribeExtractor.ValueFromNode(
                        Scribe.loader.curXmlParent["refillpercent"],
                        RefillFull);
                    __instance.SetRefillPercent(refillPercent);
                    refillingDisabled = ScribeExtractor.ValueFromNode(
                        Scribe.loader.curXmlParent["refillingdisabled"],
                        false);
                    if (refillingDisabled)
                    {
                        __instance.SetRefillingDisabled();
                    }
                    break;
                default:
                    break;
            }
        }

        public static int CalculateStackLimit(StorageSettings setting, int defaultValue = MaxLimit)
        {
            int limit = GetStackLimit(setting);
            if (limit > NoLimit)
            {
                return limit;
            }
            return defaultValue;
        }

        public static int CalculateStackLimit(SlotGroup slotGroup, int defaultValue)
        {
            if (slotGroup != null)
            {
                return CalculateStackLimit(slotGroup.Settings, defaultValue);
            }
            return defaultValue;
        }

        public static int CalculateStackLimit(Thing thing)
        {
            return CalculateStackLimit(
                thing.GetSlotGroup(),
                thing.def.stackLimit);
        }

        public static int CalculateStackLimit(Map map, IntVec3 cell)
        {
            return CalculateStackLimit(
                map.haulDestinationManager.SlotGroupAt(cell),
                MaxLimit);
        }

        public static bool TryGetLimit(SlotGroup slotGroup, out int limit)
        {
            var setting = slotGroup.Settings;
            bool result = Limits.TryGetValue(setting, out limit);
            if (!result) {
                limit = NoLimit;
            }
            return result;
        }

        public static int GetStackLimit(this StorageSettings settings, int defaultValue = NoLimit)
        {
            if (Limits.ContainsKey(settings))
            {
                return Limits[settings];
            }
            return defaultValue;
        }

        public static void SetStackLimit(this StorageSettings settings, int limit)
        {
            if (limit < 0)
            {
                Limits.Remove(settings);
            }
            else
            {
                Limits[settings] = limit;
            }
        }

        /// <summary>
        /// create open instance delegate of RimWorld.StorageSettings.TryNotifyChanged()
        /// </summary>
        public static Action<StorageSettings> StorageSettings_TryNotifyChanged = 
            AccessTools.MethodDelegate<Action<StorageSettings>>(
                AccessTools.DeclaredMethod(typeof(StorageSettings), "TryNotifyChanged"));

        public static void SetStackLimitAndNotifyChange(this StorageSettings settings, int limit)
        {
            if (limit == NoLimit)
            {
                Limits.Remove(settings);
            }
            else
            {
                if (limit < GetStackLimit(settings, MaxLimit))
                {
                    StorageSettings_TryNotifyChanged(settings);
                }
                Limits[settings] = limit;
            }
        }

        public static int GetRefillPercent(this StorageSettings settings)
        {
            if (RefillPercents.ContainsKey(settings))
            {
                return RefillPercents[settings];
            }
            return RefillFull;
        }

        public static void SetRefillPercent(this StorageSettings settings, int refillPercent)
        {
            if (refillPercent < RefillFull)
            {
                RefillPercents[settings] = refillPercent;
            }
            else
            {
                RefillPercents.Remove(settings);
            }
        }

        public static bool IsRefillingDisabled(this StorageSettings settings)
        {
            return RefillingDisabled.Contains(settings);
        }

        public static void SetRefillingDisabled(this StorageSettings settings)
        {
            RefillingDisabled.Add(settings);
        }

        public static void UnsetRefillingDisabled(this StorageSettings settings)
        {
            RefillingDisabled.Remove(settings);
        }
    }
}