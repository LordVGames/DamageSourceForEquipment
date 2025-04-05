using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RoR2;
using RoR2.Projectile;

namespace DamageSourceForEquipment
{
    internal static class AssetEdits
    {
        internal static void LoadAndEditAssets()
        {
            EditMoltovAssets();
            EditPreonAsset();
            if (ConfigOptions.AspectPassiveDamageIsEquipment.Value)
            {
                EditMalachiteSpikeAsset();
                EditTwistedProjectile();
            }
        }



        private static void EditMoltovAssets()
        {
            GameObject molotovSingle = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Molotov/MolotovSingleProjectile.prefab").WaitForCompletion();
            ProjectileDamage molotovSingleProjectileDamage = molotovSingle.GetComponent<ProjectileDamage>();
            molotovSingleProjectileDamage.damageType.damageSource = DamageSource.Equipment;

            GameObject molotovDotZone = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Molotov/MolotovProjectileDotZone.prefab").WaitForCompletion();
            ProjectileDamage molotovDotZoneProjectileDamage = molotovDotZone.GetComponent<ProjectileDamage>();
            molotovDotZoneProjectileDamage.damageType.damageSource = DamageSource.Equipment;
        }

        private static void EditPreonAsset()
        {
            GameObject preonProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/BFG/BeamSphere.prefab").WaitForCompletion();
            ProjectileProximityBeamController preonProximityBeamController = preonProjectile.GetComponent<ProjectileProximityBeamController>();
            preonProximityBeamController.inheritDamageType = true;
        }



        private static void EditMalachiteSpikeAsset()
        {
            GameObject malachiteSpike = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ElitePoison/PoisonStakeProjectile.prefab").WaitForCompletion();
            ProjectileDamage malachiteSpikeProjectileDamage = malachiteSpike.GetComponent<ProjectileDamage>();
            malachiteSpikeProjectileDamage.damageType.damageSource = DamageSource.Equipment;
        }

        private static void EditTwistedProjectile()
        {
            GameObject twistedProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Elites/EliteBead/BeadProjectileTrackingBomb.prefab").WaitForCompletion();
            ProjectileDamage twistedProjectileProjectileDamage = twistedProjectile.GetComponent<ProjectileDamage>();
            twistedProjectileProjectileDamage.damageType.damageSource = DamageSource.Equipment;
        }
    }
}