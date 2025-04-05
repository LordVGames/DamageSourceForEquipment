using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;

namespace DamageSourceForEquipment
{
    public static class ConfigOptions
    {
        public static ConfigEntry<bool> AspectPassiveDamageIsEquipment;

        internal static void BindConfigOptions(ConfigFile config)
        {
            AspectPassiveDamageIsEquipment = config.Bind<bool>(
                "Aspects",
                "Aspect passive effect damage", false,
                "Should damage from aspect's passive effects (i.e blazing fire trail, malachite spikes) count as equipment damage? This is configurable since Wake of Vultures makes the aspect effects not tied to an equipment."
            );
        }
    }
}