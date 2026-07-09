using System;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Compatibility;

/// <summary>
/// 运行时游戏版本兼容层。
/// 在 mod 初始化时检测当前运行的游戏版本（v0.107 正式版 vs v0.108 public-beta），
/// 并提供版本无关的包装器，以反射方式调用在两个版本间签名有变化的 API。
///
/// 受影响的 API：
///   • CreatureCmd.Damage  — v107 为 6 参数，v108 增加了 CardPlay? 参数（7 参数）
///   • AttackCommand.FromCard — v107 为 1 参数，v108 增加了 CardPlay? 参数（2 参数）
///
/// ModifyDamageAdditive / ModifyDamageMultiplicative 虚方法 override 的跨版本兼容
/// 由 VersionCompatPatches（Harmony）负责，不在本文件中处理。
/// </summary>
internal static class VersionCompat
{
    /// <summary>如果当前运行的游戏是 v0.108+（public-beta API）则为 true。</summary>
    internal static bool IsV108 { get; private set; }

    // 缓存的委托，在 Initialize() 时绑定到正确的版本。
    // 签名统一采用 7 参数（含 CardPlay?），v107 路径忽略最后一个参数。
    internal static Func<PlayerChoiceContext, Creature, decimal, ValueProp, Creature?, CardModel?, CardPlay?, Task>
        CreatureDamage = null!;

    // AttackCommand.FromCard — 实例方法，第一个参数是 AttackCommand 实例。
    internal static Func<AttackCommand, CardModel, CardPlay?, AttackCommand>
        AttackFromCard = null!;

    internal static void Initialize()
    {
        var tCtx      = typeof(PlayerChoiceContext);
        var tCreature = typeof(Creature);
        var tDecimal  = typeof(decimal);
        var tVP       = typeof(ValueProp);
        var tCard     = typeof(CardModel);
        var tCP       = typeof(CardPlay);

        // ── CreatureCmd.Damage ──────────────────────────────────────────────
        var damage7 = typeof(CreatureCmd).GetMethod("Damage",
            new[] { tCtx, tCreature, tDecimal, tVP, tCreature, tCard, tCP });
        var damage6 = typeof(CreatureCmd).GetMethod("Damage",
            new[] { tCtx, tCreature, tDecimal, tVP, tCreature, tCard });

        IsV108 = damage7 != null;
        MainFile.Logger.Info($"[VersionCompat] 检测到游戏版本: {(IsV108 ? "v0.108+ (public-beta)" : "v0.107 (stable)")}");

        if (IsV108)
        {
            var m = damage7!;
            CreatureDamage = (ctx, creature, amount, props, dealer, source, cardPlay) =>
                (Task)m.Invoke(null, new object?[] { ctx, creature, amount, props, dealer, source, cardPlay })!;
        }
        else
        {
            var m = damage6!;
            CreatureDamage = (ctx, creature, amount, props, dealer, source, _) =>
                (Task)m.Invoke(null, new object?[] { ctx, creature, amount, props, dealer, source })!;
        }

        // ── AttackCommand.FromCard ──────────────────────────────────────────
        var tAttack = typeof(AttackCommand);
        var fromCard2 = tAttack.GetMethod("FromCard", new[] { tCard, tCP });
        var fromCard1 = tAttack.GetMethod("FromCard", new[] { tCard });

        if (IsV108)
        {
            var m = fromCard2!;
            AttackFromCard = (cmd, card, cardPlay) =>
                (AttackCommand)m.Invoke(cmd, new object?[] { card, cardPlay })!;
        }
        else
        {
            var m = fromCard1!;
            AttackFromCard = (cmd, card, _) =>
                (AttackCommand)m.Invoke(cmd, new object?[] { card })!;
        }
    }
}
