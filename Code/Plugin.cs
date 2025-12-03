using BepInEx;
using RoR2;
using R2API;
using MonoDetour;
using MonoDetour.HookGen;
namespace DamageSourceForEquipment;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "LordVGames";
    public const string PluginName = "DamageSourceForEquipment";
    public const string PluginVersion = "2.0.0";
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
            MonoDetourHooks.RoR2.CharacterMaster.OnBodyDamaged.Postfix(LogDamageSource);
        }

        private static void LogDamageSource(CharacterMaster self, ref DamageReport damageReport)
        {
            Log.Debug($"damageType == {damageReport.damageInfo.damageType}");
        }
    }
#endif
}