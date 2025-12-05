using BepInEx;
using RoR2;
using R2API;
using MonoDetour;
using MonoDetour.HookGen;
namespace DamageSourceForEquipment;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    public void Awake()
    {
        Log.Init(Logger);
        ConfigOptions.BindConfigOptions(Config);
        Main.AddEquipmentDamageSource = ProcTypeAPI.ReserveProcType();
        MonoDetourManager.InvokeHookInitializers(typeof(Plugin).Assembly);
    }

#if DEBUG
    [MonoDetourTargets(typeof(CharacterMaster))]
    private static class DebugLogDamageSourceOnHit
    {
        [MonoDetourHookInitialize]
        internal static void Setup()
        {
            Mdh.RoR2.CharacterMaster.OnBodyDamaged.Postfix(LogDamageSource);
        }

        private static void LogDamageSource(CharacterMaster self, ref DamageReport damageReport)
        {
            Log.Debug($"damageType == {damageReport.damageInfo.damageType}");
        }
    }
#endif
}