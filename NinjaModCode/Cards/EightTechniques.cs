using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 忍者八法（Eight Techniques）——技能牌（稀有），消耗（升级后移除消耗）。
/// 1 费，获得各 1 点：力量、抵抗、活力（敏捷）、能量、格挡、飞刀、最大生命、生命恢复。
/// </summary>
public class EightTechniques : NinjaModCard
{
    public EightTechniques() : base(BalanceCost(nameof(EightTechniques), 1), BalanceType(nameof(EightTechniques), CardType.Skill), BalanceRarity(nameof(EightTechniques), CardRarity.Rare), BalanceTarget(nameof(EightTechniques), TargetType.Self)) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [] : [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        int amount = BalanceValue("BaseEightTechniquesAmount", 1);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
        await PowerCmd.Apply<ResistPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
        await PlayerCmd.GainEnergy(amount, Owner);
        await CreatureCmd.GainBlock(Owner.Creature, amount, ValueProp.Move, cardPlay);
        for (int i = 0; combatState != null && i < amount; i++)
        {
            await Kunai.CreateInHand(Owner, combatState);
        }
        await CreatureCmd.GainMaxHp(Owner.Creature, amount);
        await CreatureCmd.Heal(Owner.Creature, amount, true);
    }

    protected override void OnUpgrade() { } // 升级仅移除消耗（由 CanonicalKeywords 处理）

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("忍者八法", "获得 1 点[gold]力量[/gold]、1 点[gold]抵挡[/gold]、1 点活力、1 点能量、1 点格挡、1 张飞刀、1 点最大生命，并回复 1 点生命。")
        : new CardLoc("Eight Techniques", "Gain 1 [gold]Strength[/gold], 1 [gold]Resist[/gold], 1 Vigor, 1 Energy, 1 Block, 1 Kunai, 1 Max HP, and heal 1 HP.");
}
