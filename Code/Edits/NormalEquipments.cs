using System;
using UnityEngine;
using RoR2;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoMod.Cil;
using R2API;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using RoR2.ContentManagement;

namespace DamageSourceForEquipment.Edits;

internal static class NormalEquipments
{
    [MonoDetourTargets(typeof(EquipmentSlot))]
    [MonoDetourTargets(typeof(MissileUtils))]
    private static class DisposableMissileLauncher
    {
        [MonoDetourHookInitialize]
        private static void Setup()
        {
            MonoDetourHooks.RoR2.EquipmentSlot.FireMissile.ILHook(EquipmentSlot_FireMissile);
            MonoDetourHooks.RoR2.MissileUtils.FireMissile_UnityEngine_Vector3_RoR2_CharacterBody_RoR2_ProcChainMask_UnityEngine_GameObject_System_Single_System_Boolean_UnityEngine_GameObject_RoR2_DamageColorIndex_UnityEngine_Vector3_System_Single_System_Boolean.ILHook(MissileUtils_FireMissile);
        }

        private static void EquipmentSlot_FireMissile(ILManipulationInfo info)
        {
            ILWeaver w = new(info);


            w.MatchRelaxed(
                x => x.MatchLdnull() && w.SetCurrentTo(x)
            ).ThrowIfFailure();


            // you can't normally pass a DamageSource/DamageTypeCombo here, so here's a hacky way of doing it:
            // we create a new ProcType and add that to the ProcChainMask that gets provided
            // when the method that actually fires the missile (hooked separately) sees the ProcType, it removes it from the mask and adds the equipment DamageSource
            w.InsertBeforeCurrent(
                w.CreateDelegateCall((ProcChainMask procChainMask) =>
                {
                    procChainMask.AddModdedProc(Main.AddEquipmentDamageSource);
                    Log.Warning(procChainMask);
                    return procChainMask;
                })
            );
        }

        private static void MissileUtils_FireMissile(ILManipulationInfo info)
        {
            ILWeaver w = new(info);

            w.MatchNextRelaxed(
                x => x.MatchCall<ProjectileManager>("get_instance"),
                x => x.MatchLdloc(out _) && w.SetCurrentTo(x),
                x => x.MatchCallvirt<ProjectileManager>("FireProjectile")
            ).ThrowIfFailure();


            w.InsertAfterCurrent(
                w.CreateDelegateCall((FireProjectileInfo fireProjectileInfo) =>
                {
                    Log.Warning(fireProjectileInfo.projectilePrefab);
                    if (!fireProjectileInfo.procChainMask.HasModdedProc(Main.AddEquipmentDamageSource))
                    {
                        return fireProjectileInfo;
                    }

                    fireProjectileInfo.procChainMask.RemoveModdedProc(Main.AddEquipmentDamageSource);
                    fireProjectileInfo.damageTypeOverride = new DamageTypeCombo?(Main.GenericEquipment);
                    return fireProjectileInfo;
                })
            );
        }
    }



    [MonoDetourTargets(typeof(EntityStates.QuestVolatileBattery.CountDown))]
    private static class FuelArray
    {
        [MonoDetourHookInitialize]
        private static void Setup()
        {
            MonoDetourHooks.EntityStates.QuestVolatileBattery.CountDown.Detonate.ILHook(CountDown_Detonate);
        }

        private static void CountDown_Detonate(ILManipulationInfo info)
        {
            ILHelpers.BlastAttacks.OverrideBlastAttackDamageSource(DamageSource.Equipment, info);
        }
    }



    [MonoDetourTargets(typeof(EquipmentSlot))]
    private static class Molotov
    {
        private static readonly AssetReferenceT<GameObject> _molotovProjectile = new(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_DLC1_Molotov.MolotovSingleProjectile_prefab);
        private static readonly AssetReferenceT<GameObject> _molotovGroundDamageZone = new(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_DLC1_Molotov.MolotovProjectileDotZone_prefab);

        [MonoDetourHookInitialize]
        private static void Setup()
        {
            MonoDetourHooks.RoR2.EquipmentSlot.FireMolotov.ILHook(EquipmentSlot_FireMolotov);


            AssetAsyncReferenceManager<GameObject>.LoadAsset(_molotovProjectile).Completed += (handle) =>
            {
                handle.Result.GetComponent<ProjectileDamage>().damageType.damageSource = DamageSource.Equipment;
                AssetAsyncReferenceManager<GameObject>.UnloadAsset(_molotovProjectile);
            };
            AssetAsyncReferenceManager<GameObject>.LoadAsset(_molotovGroundDamageZone).Completed += (handle) =>
            {
                handle.Result.GetComponent<ProjectileDamage>().damageType.damageSource = DamageSource.Equipment;
                AssetAsyncReferenceManager<GameObject>.UnloadAsset(_molotovGroundDamageZone);
            };
        }

        private static void EquipmentSlot_FireMolotov(ILManipulationInfo info)
        {
            ILHelpers.Projectiles.OverrideFireProjectileWithoutDamageType(Main.GenericEquipment, info);
        }
    }



    [MonoDetourTargets(typeof(EquipmentSlot))]
    private static class PreonAccumulator
    {
        private static readonly AssetReferenceT<GameObject> _preonProjectile = new(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_Base_BFG.BeamSphere_prefab);

        [MonoDetourHookInitialize]
        private static void Setup()
        {
            MonoDetourHooks.RoR2.EquipmentSlot.MyFixedUpdate.ILHook(EquipmentSlot_MyFixedUpdate);


            AssetAsyncReferenceManager<GameObject>.LoadAsset(_preonProjectile).Completed += (handle) =>
            {
                handle.Result.GetComponent<ProjectileProximityBeamController>().inheritDamageType = true;
                AssetAsyncReferenceManager<GameObject>.UnloadAsset(_preonProjectile);
            };
        }

        private static void EquipmentSlot_MyFixedUpdate(ILManipulationInfo info)
        {
            ILHelpers.Projectiles.OverrideFireProjectileWithoutDamageType(Main.GenericEquipment, info);
        }
    }



    [MonoDetourTargets(typeof(EquipmentSlot))]
    private static class RemoteCaffeinator
    {
        [MonoDetourHookInitialize]
        private static void Setup()
        {
            MonoDetourHooks.RoR2.EquipmentSlot.FireVendingMachine.ILHook(EquipmentSlot_FireVendingMachine);
        }

        private static void EquipmentSlot_FireVendingMachine(ILManipulationInfo info)
        {
            ILHelpers.Projectiles.OverrideFireProjectileWithoutDamageType(Main.GenericEquipment, info);
        }
    }



    [MonoDetourTargets(typeof(RoR2.Orbs.LightningStrikeOrb))]
    private static class RoyalCapacitor
    {
        [MonoDetourHookInitialize]
        private static void Setup()
        {
            MonoDetourHooks.RoR2.Orbs.LightningStrikeOrb.OnArrival.ILHook(LightningStrikeOrb_OnArrival);
        }

        private static void LightningStrikeOrb_OnArrival(ILManipulationInfo info)
        {
            ILHelpers.BlastAttacks.OverrideBlastAttackDamageSource(DamageSource.Equipment, info);
        }
    }



    [MonoDetourTargets(typeof(EntityStates.GoldGat.GoldGatFire))]
    private static class Crowdfunder
    {
        [MonoDetourHookInitialize]
        private static void Setup()
        {
            MonoDetourHooks.EntityStates.GoldGat.GoldGatFire.FireBullet.ILHook(GoldGatFire_FireBullet);
        }

        private static void GoldGatFire_FireBullet(ILManipulationInfo info)
        {
            ILHelpers.BulletAttacks.OverrideBulletAttackDamageSource(DamageSource.Equipment, info);
        }
    }



    [MonoDetourTargets(typeof(FireballVehicle))]
    private static class VolcanicEgg
    {
        [MonoDetourHookInitialize]
        private static void Setup()
        {
            MonoDetourHooks.RoR2.FireballVehicle.OnPassengerEnter.ILHook(FireballVehicle_OnPassengerEnter);
            MonoDetourHooks.RoR2.FireballVehicle.DetonateServer.ILHook(FireballVehicle_DetonateServer);
        }

        private static void FireballVehicle_OnPassengerEnter(ILManipulationInfo info)
        {
            ILHelpers.OverlapAttacks.AddDamageSourceToOverlapAttack(DamageSource.Equipment, info);
        }

        private static void FireballVehicle_DetonateServer(ILManipulationInfo info)
        {
            ILHelpers.BlastAttacks.OverrideBlastAttackDamageSource(DamageSource.Equipment, info);
        }
    }



    [MonoDetourTargets(typeof(EquipmentSlot))]
    private static class Sawmerang
    {
        [MonoDetourHookInitialize]
        private static void Setup()
        {
            MonoDetourHooks.RoR2.EquipmentSlot._FireSaw_g__FireSingleSaw_96_0.ILHook(EquipmentSlot__FireSaw_g__FireSingleSaw_96_0);
        }

        private static void EquipmentSlot__FireSaw_g__FireSingleSaw_96_0(ILManipulationInfo info)
        {
            ILHelpers.Projectiles.AddDamageTypeComboToFireProjectileInfo(Main.GenericEquipment, info);
        }
    }
}