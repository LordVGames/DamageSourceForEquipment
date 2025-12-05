using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using R2API;
using RoR2BepInExPack.Utilities;

namespace DamageSourceForEquipment
{
    internal static class Main
    {
        public static ModdedProcType AddEquipmentDamageSource;

        // gearbox didn't even make a GenericEquipment DamageTypeCombo how
        public static readonly DamageTypeCombo GenericEquipment = new ()
        {
            damageType = DamageType.Generic,
            damageTypeExtended = DamageTypeExtended.Generic,
            damageSource = DamageSource.Equipment
        };

        internal static readonly Type[] GoodFireProjectileTypes = [
            typeof(GameObject),
            typeof(Vector3),
            typeof(Quaternion),
            typeof(GameObject),
            typeof(float),
            typeof(float),
            typeof(bool),
            typeof(DamageColorIndex),
            typeof(GameObject),
            typeof(float),
            typeof(DamageTypeCombo?)
        ];
    }
}