using HarmonyLib;
using RimWorld;

namespace StockpileLimit
{
    [HarmonyPatch(typeof(StorageSettingsClipboard), nameof(StorageSettingsClipboard.Copy))]
    public static class CopyPatch
    {
        public static int clipboardLimit = -1;
        public static int refillpercent = 100;

        public static void Postfix(StorageSettings s)
        {
            clipboardLimit = s.GetStackLimit();
            refillpercent = s.GetRefillPercent();
        }
    }

    [HarmonyPatch(typeof(StorageSettingsClipboard), nameof(StorageSettingsClipboard.PasteInto))]
    public static class PasteIntoPatch
    {
        public static void Postfix(StorageSettings s)
        {
            s.SetStackLimitAndNotifyChange(CopyPatch.clipboardLimit);
            s.SetRefillPercent(CopyPatch.refillpercent);
        }
    }
}