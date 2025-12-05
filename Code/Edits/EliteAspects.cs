using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RoR2;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoMod.Cil;
using R2API;
using RoR2.Projectile;
using RoR2BepInExPack.Utilities;
using RoR2.ContentManagement;
namespace DamageSourceForEquipment.Edits;

public static class EliteAspects
{
    [MonoDetourTargets(typeof(AffixAurelioniteBehavior))]
    private static class GildedAspect
    {
        [MonoDetourHookInitialize]
        private static void Setup()
        {
            if (!ConfigOptions.AspectDamageIsEquipment.Value)
            {
                return;
            }
            

            Mdh.RoR2.AffixAurelioniteBehavior.FireAurelioniteAttack.ILHook(AffixAurelioniteBehavior_FireAurelioniteAttack);
        }

        private static void AffixAurelioniteBehavior_FireAurelioniteAttack(ILManipulationInfo info)
        {
            ILWeaver w = new(info);
            
            ILHelpers.Projectiles.OverrideNextFireProjectileWithoutDamageType(Main.GenericEquipment, w);
            ILHelpers.Projectiles.OverrideNextFireProjectileWithoutDamageType(Main.GenericEquipment, w);
        }
    }



    [MonoDetourTargets(typeof(DamageTrail))]
    [MonoDetourTargets(typeof(CharacterBody))]
    private static class BlazingAspect
    {
        // didn't want to set the DamageSource for DamageTrails directly in case some mod adds a DamageTrail not tied to an equipment
        // so we're going with a FixedConditionalWeakTable to add a DamageSource to DamageTrail
        public static readonly FixedConditionalWeakTable<DamageTrail, DamageTrailDamageSource> DamageTrailDamageSourceTable = [];
        public class DamageTrailDamageSource
        {
            public DamageSource DamageSource;
        }


        [MonoDetourHookInitialize]
        private static void Setup()
        {
            if (!ConfigOptions.AspectDamageIsEquipment.Value)
            {
                return;
            }


            Mdh.RoR2.DamageTrail.Awake.Postfix(DamageTrail_Awake);
            Mdh.RoR2.DamageTrail.OnDisable.Postfix(DamageTrail_OnDisable);
            Mdh.RoR2.DamageTrail.DoDamage.ILHook(DamageTrail_DoDamage);
            Mdh.RoR2.CharacterBody.UpdateFireTrail.ILHook(CharacterBody_UpdateFireTrail);
        }

        private static void DamageTrail_Awake(DamageTrail self)
        {
            DamageTrailDamageSourceTable.GetOrCreateValue(self);
        }

        private static void DamageTrail_OnDisable(DamageTrail self)
        {
            if (DamageTrailDamageSourceTable.TryGetValue(self, out _))
            {
                DamageTrailDamageSourceTable.Remove(self);
            }
        }

        private static void DamageTrail_DoDamage(ILManipulationInfo info)
        {
            ILWeaver w = new(info);

            w.MatchRelaxed(
                x => x.MatchStfld<DamageInfo>("damageType") && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            w.InsertAfterCurrent(
                w.Create(OpCodes.Ldarg_0),
                w.Create(OpCodes.Ldloc_3),
                w.CreateDelegateCall((DamageTrail damageTrail, DamageInfo damageInfo) =>
                {
                    if (DamageTrailDamageSourceTable.TryGetValue(damageTrail, out var damageTrailDamageSource))
                    {
                        damageInfo.damageType.damageSource = damageTrailDamageSource.DamageSource;
                    }
                })
            );
        }

        private static void CharacterBody_UpdateFireTrail(ILManipulationInfo info)
        {
            ILWeaver w = new(info);

            w.MatchNextRelaxed(
                x => x.MatchStfld<CharacterBody>("fireTrail") && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            w.InsertAfterCurrent(
                w.Create(OpCodes.Ldarg_0),
                w.CreateDelegateCall((CharacterBody characterBody) =>
                {
                    // didn't have to null check here before but now i do
                    // probably should've been null checking characterbody already lol
                    if (characterBody != null && characterBody.fireTrail != null &&  DamageTrailDamageSourceTable.TryGetValue(characterBody.fireTrail, out var damageTrailDamageSource))
                    {
                        damageTrailDamageSource.DamageSource = DamageSource.Equipment;
                    }
                })
            );
        }
    }



    [MonoDetourTargets(typeof(CharacterBody))]
    private static class MalachiteAspect
    {
        private static readonly AssetReferenceT<GameObject> _malachiteSpikeProjectile = new(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_Base_ElitePoison.PoisonStakeProjectile_prefab);

        [MonoDetourHookInitialize]
        private static void Setup()
        {
            if (!ConfigOptions.AspectDamageIsEquipment.Value)
            {
                return;
            }


            Mdh.RoR2.CharacterBody.UpdateAffixPoison.ILHook(CharacterBody_UpdateAffixPoison);


            AssetAsyncReferenceManager<GameObject>.LoadAsset(_malachiteSpikeProjectile).Completed += (handle) =>
            {
                handle.Result.GetComponent<ProjectileDamage>().damageType.damageSource = DamageSource.Equipment;
                AssetAsyncReferenceManager<GameObject>.UnloadAsset(_malachiteSpikeProjectile);
            };
        }

        private static void CharacterBody_UpdateAffixPoison(ILManipulationInfo info)
        {
            ILHelpers.Projectiles.OverrideFireProjectileWithoutDamageType(Main.GenericEquipment, info);
        }
    }



    [MonoDetourTargets(typeof(GlobalEventManager))]
    private static class GlacialAspect
    {
        [MonoDetourHookInitialize]
        private static void Setup()
        {
            if (!ConfigOptions.AspectDamageIsEquipment.Value)
            {
                return;
            }


            Mdh.RoR2.GlobalEventManager.OnCharacterDeath.ILHook(GlobalEventManager_OnCharacterDeath);
        }

        private static void GlobalEventManager_OnCharacterDeath(ILManipulationInfo info)
        {
            ILWeaver w = new(info);
            int ldLocNumber = 25;

            w.MatchRelaxed(
                x => x.MatchLdloc(out ldLocNumber),
                x => x.MatchLdcI4(out _),
                x => x.MatchCall<DamageTypeCombo>("op_Implicit"),
                x => x.MatchStfld<DelayBlast>("damageType") && w.SetCurrentTo(x)
            ).ThrowIfFailure();

            w.InsertAfterCurrent(
                w.Create(OpCodes.Ldloc, ldLocNumber),
                w.CreateDelegateCall((DelayBlast delayBlast) =>
                {
                    delayBlast.damageType.damageSource = DamageSource.Equipment;
                })
            );
        }
    }



    // this is copied from above so monodetour will run the Setup here automatically, there's no hooking going on for this one
    [MonoDetourTargets(typeof(GlobalEventManager))]
    private static class TwistedAspect
    {
        private static readonly AssetReferenceT<GameObject> _twistedProjectile = new(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_DLC2_Elites_EliteBead.BeadProjectileTrackingBomb_prefab);

        [MonoDetourHookInitialize]
        private static void Setup()
        {
            if (!ConfigOptions.AspectDamageIsEquipment.Value)
            {
                return;
            }


            AssetAsyncReferenceManager<GameObject>.LoadAsset(_twistedProjectile).Completed += (handle) =>
            {
                handle.Result.GetComponent<ProjectileDamage>().damageType.damageSource = DamageSource.Equipment;
                AssetAsyncReferenceManager<GameObject>.UnloadAsset(_twistedProjectile);
            };
        }
    }



    [MonoDetourTargets(typeof(GlobalEventManager))]
    private static class OverloadingAspect
    {
        [MonoDetourHookInitialize]
        private static void Setup()
        {
            if (!ConfigOptions.AspectDamageIsEquipment.Value)
            {
                return;
            }


            Mdh.RoR2.GlobalEventManager.OnHitAllProcess.ILHook(GlobalEventManager_OnHitAllProcess);
        }

        private static void GlobalEventManager_OnHitAllProcess(ILManipulationInfo info)
        {
            ILWeaver w = new(info);

            // move to close to the needed FireProjectileWithoutDamageType
            w.MatchNextRelaxed(
                x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", "AffixBlue") && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            
            ILHelpers.Projectiles.OverrideNextFireProjectileWithoutDamageType(Main.GenericEquipment, w);
        }
    }
}