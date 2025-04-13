using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DamageSourceForEquipment
{
    internal static class ModSupport
    {
        internal static class ItemStatisticsMod
        {
            internal const string GUID = ItemStatistics.ItemStatisticsPlugin.ModGuid;
            private static bool? _modexists;
            internal static bool ModIsRunning
            {
                get
                {
                    _modexists ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID);
                    return (bool)_modexists;
                }
            }
        }
    }
}