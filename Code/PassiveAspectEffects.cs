using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Projectile;
using RoR2BepInExPack.Utilities;
using R2API;

namespace DamageSourceForEquipment
{
    public class PassiveAspectEffects
    {
        internal static void SetupHooks()
        {
            BlazingDamageTrail.SetupHooks();
            MalachiteSpikes.SetupHooks();
            GlacialDeathBubble.SetupHooks();
        }



        public class BlazingDamageTrail
        {
            // didn't want to set the DamageSource for DamageTrails directly in case some mod adds a DamageTrail not tied to an equipment
            // so we're going with a FixedConditionalWeakTable to add a DamageSource to DamageTrail
            public static readonly FixedConditionalWeakTable<DamageTrail, DamageTrailDamageSource> DamageTrailDamageSourceTable = new();
            public class DamageTrailDamageSource
            {
                public DamageSource DamageSource;
            }



            internal static void SetupHooks()
            {
                On.RoR2.DamageTrail.Awake += DamageTrail_Awake;
                On.RoR2.DamageTrail.OnDisable += DamageTrail_OnDisable;
                IL.RoR2.CharacterBody.UpdateFireTrail += CharacterBody_UpdateFireTrail;
                IL.RoR2.DamageTrail.DoDamage += DamageTrail_DoDamage;
            }



            private static void DamageTrail_Awake(On.RoR2.DamageTrail.orig_Awake orig, DamageTrail self)
            {
                orig(self);
                DamageTrailDamageSourceTable.GetOrCreateValue(self);
            }

            private static void DamageTrail_OnDisable(On.RoR2.DamageTrail.orig_OnDisable orig, DamageTrail self)
            {
                orig(self);
                if (DamageTrailDamageSourceTable.TryGetValue(self, out _))
                {
                    DamageTrailDamageSourceTable.Remove(self);
                }
            }



            private static void DamageTrail_DoDamage(ILContext il)
            {
                ILCursor c = new(il);

                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<DamageInfo>("damageType")
                ))
                {
                    ILHooks.LogILError(il.Method.Name, il, c);
                    return;
                }

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_3);
                c.EmitDelegate<Action<DamageTrail, DamageInfo>>((damageTrail, damageInfo) =>
                {
                    if (DamageTrailDamageSourceTable.TryGetValue(damageTrail, out var damageTrailDamageSource))
                    {
                        damageInfo.damageType.damageSource = damageTrailDamageSource.DamageSource;
                    }
                });
            }

            private static void CharacterBody_UpdateFireTrail(ILContext il)
            {
                ILCursor c = new(il);

                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<CharacterBody>("fireTrail")
                ))
                {
                    ILHooks.LogILError(il.Method.Name, il, c);
                    return;
                }

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<CharacterBody>>((characterBody) =>
                {
                    if (DamageTrailDamageSourceTable.TryGetValue(characterBody.fireTrail, out var damageTrailDamageSource))
                    {
                        damageTrailDamageSource.DamageSource = DamageSource.Equipment;
                    }
                });
            }
        }

        internal static class MalachiteSpikes
        {
            internal static void SetupHooks()
            {
                IL.RoR2.CharacterBody.UpdateAffixPoison += CharacterBody_UpdateAffixPoison;
            }



            private static void CharacterBody_UpdateAffixPoison(ILContext il)
            {
                ILCursor c = new(il);
                ILLabel afterBadFireProjectile = il.DefineLabel();
                VariableDefinition[] requiredMethodParameters = ILHooks.CreateArrayForParametersAsLocalILVariables(il);
                AssignValuesToLocalILParametersArray(il, c, requiredMethodParameters);
                ILHooks.ReplaceBadFireProjectileLine(il, c, requiredMethodParameters);
            }

            private static void AssignValuesToLocalILParametersArray(ILContext il, ILCursor c, VariableDefinition[] requiredMethodParameters)
            {
                int i = 1;

                // prefab
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall("RoR2.LegacyResourcesAPI", "Load")
                ))
                {
                    ILHooks.LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // position
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<CharacterBody>("get_corePosition")
                ))
                {
                    ILHooks.LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // rotation
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall("RoR2.Util", "QuaternionSafeLookRotation")
                ))
                {
                    ILHooks.LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // owner
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<Component>("get_gameObject")
                ))
                {
                    ILHooks.LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // damage
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchMul()
                ))
                {
                    ILHooks.LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // force
                // going past ldc.r4 0
                c.Index++;
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // crit
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall("RoR2.Util", "CheckRoll")
                ))
                {
                    ILHooks.LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // damageColorIndex
                // going past ldc.i4.0
                c.Index++;
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;

                // target
                // going past ldnull
                c.Index++;
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;

                // speedOverride
                // going past ldc.r4 -1
                c.Index++;
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;
            }
        }

        internal static class GlacialDeathBubble
        {
            internal static void SetupHooks()
            {
                IL.RoR2.GlobalEventManager.OnCharacterDeath += GlobalEventManager_OnCharacterDeath;
            }



            private static void GlobalEventManager_OnCharacterDeath(ILContext il)
            {
                ILCursor c = new(il);

                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<DelayBlast>("damageType")
                ))
                {
                    ILHooks.LogILError(il.Method.Name, il, c);
                    return;
                }

                c.Emit(OpCodes.Ldloc, 24);
                c.EmitDelegate<Action<DelayBlast>>((delayBlast) =>
                {
                    delayBlast.damageType.damageSource = DamageSource.Equipment;
                });
            }
        }
    }
}