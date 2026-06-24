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
/// 切腹（Seppuku）——技能牌，X 费，消耗。
/// 失去 X 点生命，获得 X 点能量、抽 X 张牌、获得 X 点力量，并使自身获得 X 层流血。
/// 升级后：不再使自身获得流血。
/// </summary>
public class Seppuku : NinjaModCard
{
    // base 费用 0；通过 HasEnergyCostX 标记为 X 费（消耗当前全部剩余能量）。
    public Seppuku() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    // 标记为 X 费牌：实际花费为当前剩余能量，X = 投入的能量值。
    protected override bool HasEnergyCostX => true;

    // 升级后移除消耗：随 IsUpgraded 实时变化。
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? [] : [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 解析本次打出时投入的能量值 X。
        int x = ResolveEnergyXValue();
        if (x <= 0) return;

        // 失去 2X 点生命（无法格挡、不受加成影响的自残）。
        await CreatureCmd.Damage(choiceContext, Owner.Creature, x * 2,
            ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature, this);

        // 获得 X 点能量。
        await PlayerCmd.GainEnergy(x, Owner);

        // 抽 X 张牌。
        await CardPileCmd.Draw(choiceContext, x, Owner, false);

        // 获得 X 点力量。
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, x, Owner.Creature, this);
    }

    protected override void OnUpgrade() { } // 升级仅移除消耗（由 CanonicalKeywords 处理）

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("切腹", "失去 2X 点生命，获得 X 点能量、抽 X 张牌、获得 X 点[gold]力量[/gold]。")
        : new CardLoc("Seppuku", "Lose 2X HP. Gain X Energy, draw X cards, gain X [gold]Strength[/gold].");
}
