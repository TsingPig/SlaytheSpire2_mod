using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 守鹤之盾（Crane Guard）——技能牌，消耗。
/// 2（升级 1）费，获得等同于当前已损失生命值的格挡。
/// </summary>
public class CraneShield : NinjaModCard
{
    public CraneShield() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 动态格挡 = 已损失生命 = 最大生命 - 当前生命。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        MakeCalculatedBlock(0, (card, creature) =>
        {
            var owner = card.Owner?.Creature;
            if (owner == null) return 0m;
            return owner.MaxHp - owner.CurrentHp;
        }, 0, ValueProp.Move);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int lost = Owner.Creature.MaxHp - Owner.Creature.CurrentHp;
        if (lost > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, lost, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("守鹤之盾", "获得 {CalculatedBlock:diff()} 点格挡（等同于当前已损失的生命值）。")
        : new CardLoc("Crane Guard", "Gain {CalculatedBlock:diff()} Block (equal to your current lost HP).");
}
