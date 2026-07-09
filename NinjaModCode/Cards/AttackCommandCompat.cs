using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using NinjaMod.NinjaModCode.Compatibility;

namespace NinjaMod.NinjaModCode.Cards;

internal static class AttackCommandCompat
{
    /// <summary>
    /// 调用 AttackCommand.FromCard，自动适配 v0.107（1 参数）和 v0.108（2 参数）。
    /// </summary>
    public static AttackCommand FromCardCompat(this AttackCommand command, CardModel card, CardPlay cardPlay)
        => VersionCompat.AttackFromCard(command, card, cardPlay);
}

