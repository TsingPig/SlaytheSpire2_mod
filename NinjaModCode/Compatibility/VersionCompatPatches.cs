using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NinjaMod.NinjaModCode.Afflictions;
using NinjaMod.NinjaModCode.Powers;

namespace NinjaMod.NinjaModCode.Compatibility;

/// <summary>
/// Harmony 补丁：为 ModifyDamageAdditive / ModifyDamageMultiplicative 提供跨版本运行时桥接。
///
/// 背景：
///   v0.107 中，AbstractModel 的虚方法签名为 5 参数（无 CardPlay）。
///   v0.108 中，签名扩展为 6 参数（增加 CardPlay?）。
///   编译时通过 #if STS2_PUBLIC_BETA 选择正确的 override 签名。
///   但运行时如果版本不匹配（例如用 v107 DLL 编译的 mod 运行在 v108 游戏上），
///   override 的虚表槽不一致，导致自定义 power 的伤害修正逻辑失效。
///
/// 解决方案：
///   本补丁在初始化时检测当前游戏实际存在的方法签名（5 参数 or 6 参数），
///   并对找到的方法打 Postfix 补丁。
///   Postfix 使用参数名注入（Harmony 按名称匹配），无需 __args 即可兼容两种签名。
///   当某个 NinjaMod Power/Affliction 实例没有正确 override 对应签名的虚方法时，
///   游戏会调用基类默认实现（返回 0/1），我们的 Postfix 拦截并替换为正确结果。
///
/// 注意：
///   当 DLL 编译版本与游戏版本一致时（override 签名匹配），虚表分发直接走 override，
///   基类方法不被调用，Postfix 对我们自己的类型不会触发——完全无副作用。
/// </summary>
internal static class VersionCompatPatches
{
    private static readonly Type[] Params5 = {
        typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel)
    };
    private static readonly Type[] Params6 = {
        typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)
    };

    internal static void Apply(Harmony harmony)
    {
        var baseType = typeof(AbstractModel);

        // 找到实际存在的 ModifyDamageAdditive 签名（5 参数 = v107，6 参数 = v108）
        var addMethod = baseType.GetMethod("ModifyDamageAdditive", Params5)
                     ?? baseType.GetMethod("ModifyDamageAdditive", Params6);
        if (addMethod != null)
        {
            harmony.Patch(addMethod, postfix: new HarmonyMethod(
                typeof(VersionCompatPatches), nameof(ModifyDamageAdditivePostfix)));
            MainFile.Logger.Info($"[VersionCompatPatches] ModifyDamageAdditive({addMethod.GetParameters().Length} params) 桥接已挂载");
        }

        var mulMethod = baseType.GetMethod("ModifyDamageMultiplicative", Params5)
                     ?? baseType.GetMethod("ModifyDamageMultiplicative", Params6);
        if (mulMethod != null)
        {
            harmony.Patch(mulMethod, postfix: new HarmonyMethod(
                typeof(VersionCompatPatches), nameof(ModifyDamageMultiplicativePostfix)));
            MainFile.Logger.Info($"[VersionCompatPatches] ModifyDamageMultiplicative({mulMethod.GetParameters().Length} params) 桥接已挂载");
        }
    }

    // Harmony 按参数名注入；仅声明我们需要的 5 个参数，
    // 对 6 参数签名也适用（cardPlay 未声明则忽略）。

    // ReSharper disable InconsistentNaming
    public static void ModifyDamageAdditivePostfix(
        AbstractModel __instance, ref decimal __result,
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        switch (__instance)
        {
            case ResistPower rp:
                // 仅在基类未被正确 override 时才生效（若 override 已处理则此处无副作用）
                if (__result != 0m) return;           // override 已返回非零值，不覆盖
                if (target != rp.Owner) return;
                if (dealer == rp.Owner) return;
                if (!props.IsCardOrMonsterMove()) return;
                __result = -rp.Amount;
                break;

            case ShadowClonePower sp:
                if (__result != 0m) return;
                if (target != sp.Owner) return;
                if (dealer == sp.Owner) return;
                if (!props.IsCardOrMonsterMove()) return;
                if (sp.Amount <= 0) return;
                __result = -Math.Floor(amount * 0.4m);
                break;

            case StealthPower stp:
                if (__result != 0m) return;
                if (target != stp.Owner) return;
                if (dealer == stp.Owner) return;
                if (!props.IsCardOrMonsterMove()) return;
                __result = -amount;
                break;
        }
    }

    public static void ModifyDamageMultiplicativePostfix(
        AbstractModel __instance, ref decimal __result,
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        switch (__instance)
        {
            case AfterimageAffliction aa:
                if (__result != 1m) return;           // override 已返回非 1 值，不覆盖
                if (!aa.HasCard) return;
                if (cardSource != aa.Card) return;
                if (!props.IsPoweredAttack()) return;
                __result = 0.5m;
                break;
        }
    }
    // ReSharper restore InconsistentNaming
}
