using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace NinjaMod.NinjaModCode.Cards;

internal static class AttackCommandCompat
{
    public static AttackCommand FromCardCompat(this AttackCommand command, CardModel card, CardPlay cardPlay)
    {
#if STS2_PUBLIC_BETA
        return command.FromCard(card, cardPlay);
#else
        return command.FromCard(card);
#endif
    }
}
