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
    // 升级后为 true：不再让自身获得流血。
    private bool _noSelfBleed;

    // base 费用 -1 表示 X 费（CanonicalEnergyCost = -1）。
    public Seppuku() : base(-1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 解析本次打出时投入的能量值 X。
        int x = ResolveEnergyXValue();
        if (x <= 0) return;

        // 失去 X 点生命（无法格挡、不受加成影响的自残）。
        await CreatureCmd.Damage(choiceContext, Owner.Creature, x,
            ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature, this);

        // 获得 X 点能量。
        await PlayerCmd.GainEnergy(x, Owner);

        // 抽 X 张牌。
        await CardPileCmd.Draw(choiceContext, x, Owner, false);

        // 获得 X 点力量。
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, x, Owner.Creature, this);

        // 自身获得 X 层流血（升级后跳过）。
        if (!_noSelfBleed)
        {
            await PowerCmd.Apply<BleedPower>(choiceContext, Owner.Creature, x, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => _noSelfBleed = true;

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("切腹", _noSelfBleed
            ? "失去 X 点生命，获得 X 点能量、抽 X 张牌、获得 X 点力量。消耗。"
            : "失去 X 点生命，获得 X 点能量、抽 X 张牌、获得 X 点力量。自身获得 X 层流血。消耗。")
        : new CardLoc("Seppuku", _noSelfBleed
            ? "Lose X HP. Gain X Energy, draw X cards, gain X Strength. Exhaust."
            : "Lose X HP. Gain X Energy, draw X cards, gain X Strength. Gain X Bleed. Exhaust.");
}
