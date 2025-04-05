using System;
using RoR2;
using HarmonyLib;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RoR2.Projectile;

namespace DamageSourceForEquipment
{
    [HarmonyPatch]
    internal class HarmonyPatches
    {
        [HarmonyPatch(typeof(EquipmentSlot), "<FireSaw>g__FireSingleSaw|86_0")]
        [HarmonyILManipulator]
        internal static void AddSawmerangDamageSource(ILContext il)
        {
            ILCursor c = new(il);

            if (!c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(1)
            ))
            {
                ILHooks.LogILError("<FireSaw>g__FireSingleSaw|86_0", il, c);
                return;
            }

            c.Emit(OpCodes.Ldloc_1);
            c.EmitDelegate<Func<FireProjectileInfo, FireProjectileInfo>>((fireProjectileInfo) =>
            {
                fireProjectileInfo.damageTypeOverride = new DamageTypeCombo?(Main.GenericEquipment);
                return fireProjectileInfo;
            });
            c.Emit(OpCodes.Stloc_1);
        }
    }
}