using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using NinjaMod.NinjaModCode.Cards;
using NinjaMod.NinjaModCode.Compatibility;

namespace NinjaMod.NinjaModCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "NinjaMod"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        // 必须在 PatchAll 前初始化版本检测，后续代码依赖 VersionCompat.IsV108。
        VersionCompat.Initialize();

        Harmony harmony = new(ModId);

        harmony.PatchAll();

        // 为 ModifyDamageAdditive / ModifyDamageMultiplicative 挂载跨版本运行时桥接。
        VersionCompatPatches.Apply(harmony);

        NinjaModCard.RegisterDescriptionOverrides();
    }
}
