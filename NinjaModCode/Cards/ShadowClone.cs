using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// Shadow Clone (影分身) - Skill. Cost 3 (2 upgraded).
/// Activate a clone power for the current turn and the next turn; while active, every
/// non-Shadow-Clone card you play resolves one extra time. See <see cref="ShadowClonePower"/>.
/// </summary>
public class ShadowClone : NinjaModCard
{
    public ShadowClone() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Amount 2 => active this turn and next turn (ticks down at end of each owner turn).
        await PowerCmd.Apply<ShadowClonePower>(choiceContext, Owner.Creature, 2, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // Shadow Clone+ : reduce cost from 3 to 2.
        EnergyCost.UpgradeBy(-1);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("影分身", "本回合及下回合：①每张非影分身卡额外结算一次；②受到的攻击伤害减少40%；③若你有荆棘，克隆体对攻击者造成等量反击。")
        : new CardLoc("Shadow Clone", "This turn and next: ① Each non-Shadow-Clone card resolves once more. ② Incoming attack damage -40%. ③ If you have Thorns, the clone retaliates with equal damage.");
}
