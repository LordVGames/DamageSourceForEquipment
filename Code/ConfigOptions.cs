using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using MiscFixes.Modules;

namespace DamageSourceForEquipment
{
    internal static class ConfigOptions
    {
        public static ConfigEntry<bool> AspectDamageIsEquipment;

        internal static void BindConfigOptions(ConfigFile config)
        {
            AspectDamageIsEquipment = config.BindOption<bool>(
                "Aspects",
                "Make aspect effect damage count as equipment damage",
                "Should damage from aspect's passive effects (i.e blazing fire trail, malachite spikes) count as equipment damage?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );

            config.WipeConfig();
        }
    }
}