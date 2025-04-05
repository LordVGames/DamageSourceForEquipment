using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using R2API;
using RoR2.Projectile;

namespace DamageSourceForEquipment
{
    internal static class ILHooks
    {
        internal static void SetupHooks()
        {
            DisposableMissileLauncher.SetupHooks();
            // can't really do Forgive Me Please since that just spawns on-kill effects so I can't add an equipment DamageSource to those effects
            FuelArray.SetupHook();
            // Goobo just spawns another entity that has it's own DamageSources from it's skills so there's no need to edit that
            Molotov.SetupHook();
            PreonAccumulator.SetupHook();
            RemoteCaffeinator.SetupHook();
            RoyalCapacitor.SetupHook();
            Crowdfunder.SetupHook();
            VolcanicEgg.SetupHooks();
            GildedAspect.SetupHook();
        }




        private static class DisposableMissileLauncher
        {
            internal static void SetupHooks()
            {
                IL.RoR2.EquipmentSlot.FireMissile += EquipmentSlot_FireMissile;
                IL.RoR2.MissileUtils.FireMissile_Vector3_CharacterBody_ProcChainMask_GameObject_float_bool_GameObject_DamageColorIndex_Vector3_float_bool += MissileUtils_FireMissle;
            }



            private static void EquipmentSlot_FireMissile(ILContext il)
            {
                ILCursor c = new(il);
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchLdloca(3),
                    x => x.MatchInitobj<ProcChainMask>(),
                    x => x.MatchLdloc(3)
                ))
                {
                    LogILError(il.Method.Name, il, c);
                    return;
                }

                // you can't normally pass a DamageSource/DamageTypeCombo here, so here's a hacky way of doing it:
                // we create a new ProcType and add that to the ProcChainMask that gets provided
                // when the method that actually fires the missile (hooked separately) sees the ProcType, it removes it from the mask and adds the equipment DamageSource
                c.EmitDelegate<Func<ProcChainMask, ProcChainMask>>((procChainMask) =>
                {
                    procChainMask.AddModdedProc(Main.AddEquipmentDamageSource);
                    return procChainMask;
                });
            }

            private static void MissileUtils_FireMissle(ILContext il)
            {
                ILCursor c = new(il);
                if (!c.TryGotoNext(MoveType.Before,
                    x => x.MatchCall<ProjectileManager>("get_instance"),
                    x => x.MatchLdloc(4),
                    x => x.MatchCallvirt<ProjectileManager>("FireProjectile")
                ))
                {
                    LogILError(il.Method.Name, il, c);
                    return;
                }

                c.Emit(OpCodes.Ldloc, 4);
                c.EmitDelegate<Func<FireProjectileInfo, FireProjectileInfo>>((fireProjectileInfo) =>
                {
                    if (!fireProjectileInfo.procChainMask.HasModdedProc(Main.AddEquipmentDamageSource))
                    {
                        return fireProjectileInfo;
                    }

                    fireProjectileInfo.procChainMask.RemoveModdedProc(Main.AddEquipmentDamageSource);
                    fireProjectileInfo.damageTypeOverride = new DamageTypeCombo?(Main.GenericEquipment);
                    return fireProjectileInfo;
                });
                c.Emit(OpCodes.Stloc, 4);
            }
        }

        private static class FuelArray
        {
            internal static void SetupHook()
            {
                IL.EntityStates.QuestVolatileBattery.CountDown.Detonate += CountDown_Detonate;
            }



            private static void CountDown_Detonate(ILContext il)
            {
                ILCursor c = new(il);
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<BlastAttack>("falloffModel")
                ))
                {
                    LogILError(il.Method.Name, il, c);
                    return;
                }

                c.Emit(OpCodes.Dup);
                c.EmitDelegate<Action<BlastAttack>>((blastAttack) =>
                {
                    blastAttack.damageType.damageSource = DamageSource.Equipment;
                });
            }
        }

        private static class Molotov
        {
            internal static void SetupHook()
            {
                IL.RoR2.EquipmentSlot.FireMolotov += EquipmentSlot_FireMolotov;
            }



            private static void EquipmentSlot_FireMolotov(ILContext il)
            {
                ILCursor c = new(il);
                VariableDefinition[] requiredMethodParameters = CreateArrayForParametersAsLocalILVariables(il);
                AssignValuesToLocalILParametersArray(il, c, requiredMethodParameters);
                ReplaceBadFireProjectileLine(il, c, requiredMethodParameters);
            }

            private static void AssignValuesToLocalILParametersArray(ILContext il, ILCursor c, VariableDefinition[] requiredMethodParameters)
            {
                int i = 1;

                // prefab
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<ProjectileManager>("get_instance"),
                    x => x.MatchLdloc(1)
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // position
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<Ray>("get_origin")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // rotation
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<Quaternion>("LookRotation")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // owner
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<Component>("get_gameObject")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // damage
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<CharacterBody>("get_damage")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // force
                // going past ldc.r4 0.0
                c.Index++;
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // crit
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall("RoR2.Util", "CheckRoll")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
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

        private static class GildedAspect
        {
            internal static void SetupHook()
            {
                IL.RoR2.AffixAurelioniteBehavior.FireAurelioniteAttack += AffixAurelioniteBehavior_FireAurelioniteAttack;
            }



            private static void AffixAurelioniteBehavior_FireAurelioniteAttack(ILContext il)
            {
                ILCursor c = new(il);
                // there's 2 FireProjectileblahblahblah lines that are almost the exact same one right after another
                ReplaceBadFireProjectileLine(il, c, 1);
                ReplaceBadFireProjectileLine(il, c, 2);
            }
            private static void ReplaceBadFireProjectileLine(ILContext il, ILCursor c, int hookPart)
            {
                ILLabel afterBadFireProjectile = il.DefineLabel();
                VariableDefinition[] requiredMethodParameters = CreateArrayForParametersAsLocalILVariables(il);
                AssignValuesToLocalILParametersArray(il, c, requiredMethodParameters);


                // from here the cursor is before the FireProjectileWithoutDamageType callvirt
                foreach (VariableDefinition requiredMethodParameter in requiredMethodParameters)
                {
                    c.Emit(OpCodes.Ldloc, requiredMethodParameter);
                }
                c.EmitDelegate<Func<DamageTypeCombo?>>(() =>
                {
                    return new DamageTypeCombo?(Main.GenericEquipment);
                });
                c.Emit<ProjectileManager>(OpCodes.Callvirt, "FireProjectile");


                c.Emit(OpCodes.Br, afterBadFireProjectile);
                c.Emit<ProjectileManager>(OpCodes.Call, "get_instance");
                foreach (VariableDefinition requiredMethodParameter in requiredMethodParameters)
                {
                    c.Emit(OpCodes.Ldloc, requiredMethodParameter);
                }
                c.Index++;
                c.MarkLabel(afterBadFireProjectile);
            }



            private static void AssignValuesToLocalILParametersArray(ILContext il, ILCursor c, VariableDefinition[] requiredMethodParameters)
            {
                int i = 1;

                // prefab
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<ProjectileManager>("get_instance"),
                    x => x.MatchLdsfld(out _)
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // position
                // past ldarg.0
                c.Index++;
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // rotation
                // past ldloc.1
                c.Index++;
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // owner
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(2),
                    x => x.MatchCallvirt<Component>("get_gameObject")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // damage
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchMul()
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // force
                // going past forceAmount
                c.Index++;
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // crit
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall("RoR2.Util", "CheckRoll")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // going past ldc.i4.0
                c.Index++;
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;

                // going past ldnull
                c.Index++;
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;

                // going past ldc.r4 -1
                c.Index++;
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;
            }
        }

        private static class PreonAccumulator
        {
            internal static void SetupHook()
            {
                IL.RoR2.EquipmentSlot.MyFixedUpdate += EquipmentSlot_MyFixedUpdate;
            }



            private static void EquipmentSlot_MyFixedUpdate(ILContext il)
            {
                ILCursor c = new(il);
                ILLabel afterBadFireProjectile = il.DefineLabel();
                VariableDefinition[] requiredMethodParameters = CreateArrayForParametersAsLocalILVariables(il);
                AssignValuesToLocalILParametersArray(il, c, requiredMethodParameters);
                ReplaceBadFireProjectileLine(il, c, requiredMethodParameters);
            }

            private static void AssignValuesToLocalILParametersArray(ILContext il, ILCursor c, VariableDefinition[] requiredMethodParameters)
            {
                int i = 1;

                // prefab
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchLdstr("Prefabs/Projectiles/BeamSphere"),
                    x => x.MatchCall(out _)
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // position
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<Ray>("get_origin")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // rotation
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall("RoR2.Util", "QuaternionSafeLookRotation")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // owner
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<Component>("get_gameObject")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // damage
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchMul()
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
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
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // damageColorIndex
                // going past ldc.i4.3
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

        private static class RemoteCaffeinator
        {
            internal static void SetupHook()
            {
                IL.RoR2.EquipmentSlot.FireVendingMachine += EquipmentSlot_FireVendingMachine;
            }



            private static void EquipmentSlot_FireVendingMachine(ILContext il)
            {
                ILCursor c = new(il);
                ILLabel afterBadFireProjectile = il.DefineLabel();
                VariableDefinition[] requiredMethodParameters = CreateArrayForParametersAsLocalILVariables(il);
                AssignValuesToLocalILParametersArray(il, c, requiredMethodParameters);
                ReplaceBadFireProjectileLine(il, c, requiredMethodParameters);
            }

            private static void AssignValuesToLocalILParametersArray(ILContext il, ILCursor c, VariableDefinition[] requiredMethodParameters)
            {
                int i = 1;

                // prefab
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchLdloc(4)
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // position
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<RaycastHit>("get_point")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // rotation
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<Quaternion>("get_identity")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // owner
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<Component>("get_gameObject")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
                    return;
                }
                c.Emit(OpCodes.Stloc, requiredMethodParameters[i - 1]);
                i++;


                // damage
                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<CharacterBody>("get_damage")
                ))
                {
                    LogILError($"{il.Method.Name} PART {i}", il, c);
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
                    LogILError($"{il.Method.Name} PART {i}", il, c);
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

        private static class RoyalCapacitor
        {
            internal static void SetupHook()
            {
                IL.RoR2.Orbs.LightningStrikeOrb.OnArrival += LightningStrikeOrb_OnArrival;
            }



            private static void LightningStrikeOrb_OnArrival(ILContext il)
            {
                ILCursor c = new(il);

                if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<BlastAttack>("damageType")
                ))
                {
                    LogILError(il.Method.Name, il, c);
                    return;
                }

                c.Emit(OpCodes.Dup);
                c.EmitDelegate<Action<BlastAttack>>((blastAttack) =>
                {
                    blastAttack.damageType.damageSource = DamageSource.Equipment;
                });
            }
        }

        private static class Crowdfunder
        {
            internal static void SetupHook()
            {
                IL.EntityStates.GoldGat.GoldGatFire.FireBullet += GoldGatFire_FireBullet;
            }



            private static void GoldGatFire_FireBullet(ILContext il)
            {
                ILCursor c = new(il);

                if (!c.TryGotoNext(MoveType.Before,
                    x => x.MatchCallvirt<BulletAttack>("Fire")
                ))
                {
                    LogILError(il.Method.Name, il, c);
                    return;
                }

                c.Emit(OpCodes.Dup);
                c.EmitDelegate<Action<BulletAttack>>((bulletAttack) =>
                {
                    bulletAttack.damageType.damageSource = DamageSource.Equipment;
                });
            }
        }

        private static class VolcanicEgg
        {
            internal static void SetupHooks()
            {
                IL.RoR2.FireballVehicle.OnPassengerEnter += FireballVehicle_OnPassengerEnter;
                IL.RoR2.FireballVehicle.DetonateServer += FireballVehicle_DetonateServer;
            }



            private static void FireballVehicle_OnPassengerEnter(ILContext il)
            {
                ILCursor c = new(il);

                if (!c.TryGotoNext(MoveType.Before,
                    x => x.MatchStfld<FireballVehicle>("overlapAttack")
                ))
                {
                    LogILError(il.Method.Name, il, c);
                    return;
                }

                c.Emit(OpCodes.Dup);
                c.EmitDelegate<Action<OverlapAttack>>((overlapAttack) =>
                {
                    overlapAttack.damageType.damageSource = DamageSource.Equipment;
                });
            }

            private static void FireballVehicle_DetonateServer(ILContext il)
            {
                ILCursor c = new(il);

                if (!c.TryGotoNext(MoveType.Before,
                    x => x.MatchCallvirt<BlastAttack>("Fire")
                ))
                {
                    LogILError(il.Method.Name, il, c);
                    return;
                }

                c.Emit(OpCodes.Dup);
                c.EmitDelegate<Action<BlastAttack>>((blastAttack) =>
                {
                    blastAttack.damageType.damageSource = DamageSource.Equipment;
                });
            }
        }



        internal static VariableDefinition[] CreateArrayForParametersAsLocalILVariables(ILContext il)
        {
            VariableDefinition prefab = new(il.Import(typeof(GameObject)));
            VariableDefinition position = new(il.Import(typeof(Vector3)));
            VariableDefinition rotation = new(il.Import(typeof(Quaternion)));
            VariableDefinition owner = new(il.Import(typeof(GameObject)));
            VariableDefinition damage = new(il.Import(typeof(float)));
            VariableDefinition force = new(il.Import(typeof(float)));
            VariableDefinition crit = new(il.Import(typeof(bool)));
            VariableDefinition damageColorIndex = new(il.Import(typeof(DamageColorIndex)));
            VariableDefinition target = new(il.Import(typeof(GameObject)));
            VariableDefinition speedOverride = new(il.Import(typeof(float)));
            VariableDefinition[] requiredMethodParameters = [prefab, position, rotation, owner, damage, force, crit, damageColorIndex, target, speedOverride];
            foreach (VariableDefinition requiredMethodParameter in requiredMethodParameters)
            {
                il.Method.Body.Variables.Add(requiredMethodParameter);
            }
            return requiredMethodParameters;
        }
        internal static void ReplaceBadFireProjectileLine(ILContext il, ILCursor c, VariableDefinition[] requiredMethodParameters)
        {
            ILLabel afterBadFireProjectile = il.DefineLabel();
            // from here the cursor is before the FireProjectileWithoutDamageType callvirt

            // re-emit all the needed parameters since assigning values to them removed them from the stack
            foreach (VariableDefinition requiredMethodParameter in requiredMethodParameters)
            {
                c.Emit(OpCodes.Ldloc, requiredMethodParameter);
            }
            c.EmitDelegate<Func<DamageTypeCombo?>>(() =>
            {
                return new DamageTypeCombo?(Main.GenericEquipment);
            });
            c.Emit<ProjectileManager>(OpCodes.Callvirt, "FireProjectile");


            // skip over old FireProjectileWithoutDamageType callvirt
            c.Emit(OpCodes.Br, afterBadFireProjectile);
            // we still need to re-emit all required parameters for the old callvirt, including doing another get_instance
            c.Emit<ProjectileManager>(OpCodes.Call, "get_instance");
            foreach (VariableDefinition requiredMethodParameter in requiredMethodParameters)
            {
                c.Emit(OpCodes.Ldloc, requiredMethodParameter);
            }
            // go past the old callvirt
            c.Index++;
            c.MarkLabel(afterBadFireProjectile);
        }

        internal static void LogILError(string methodName, ILContext il, ILCursor c)
        {
            Log.Error($"COULD NOT IL HOOK {methodName}");
            Log.Warning($"cursor is {c}");
            Log.Warning($"il is {il}");
        }
    }
}