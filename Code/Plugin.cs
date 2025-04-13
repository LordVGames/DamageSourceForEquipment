using BepInEx;
using RoR2;
using R2API;
using HarmonyLib;

namespace DamageSourceForEquipment
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "LordVGames";
        public const string PluginName = "DamageSourceForEquipment";
        public const string PluginVersion = "1.0.1";
        public void Awake()
        {
            Log.Init(Logger);
            ConfigOptions.BindConfigOptions(Config);
            Main.AddEquipmentDamageSource = ProcTypeAPI.ReserveProcType();

            AssetEdits.LoadAndEditAssets();
            ILHooks.SetupHooks();
            // sawmerang is handled in a Harmony IL patch due to where we need to hook being compiler generated
            Harmony harmony = new(PluginGUID);
            harmony.CreateClassProcessor(typeof(HarmonyPatches)).Patch();
            if (ConfigOptions.AspectPassiveDamageIsEquipment.Value)
            {
                // these are in their own class since one of them needs more than just IL hooks
                PassiveAspectEffects.SetupHooks();
            }

#if DEBUG
            On.RoR2.CharacterMaster.OnBodyDamaged += CharacterMaster_OnBodyDamaged;
#endif
        }

#if DEBUG
        private void CharacterMaster_OnBodyDamaged(On.RoR2.CharacterMaster.orig_OnBodyDamaged orig, CharacterMaster self, DamageReport damageReport)
        {
            orig(self, damageReport);
            Log.Debug($"damageSource == {damageReport.damageInfo.damageType.damageSource}");
        }
#endif
    }
}