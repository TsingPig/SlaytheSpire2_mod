using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 土忍：唤石（Earth Ninjutsu: Stone Summon）——技能牌。
/// 1 费，获得（当前抵挡层数 × 4，升级为 × 5）点格挡。
/// 倍率用 <see cref="_multiplier"/> 变量表示，随升级改变。
/// </summary>
public class StoneSummon : NinjaModCard
{
    public StoneSummon() : base(BalanceCost(nameof(StoneSummon), 1), BalanceType(nameof(StoneSummon), CardType.Skill), BalanceRarity(nameof(StoneSummon), CardRarity.Common), BalanceTarget(nameof(StoneSummon), TargetType.Self)) { }

    public override bool GainsBlock => true;

    // 动态计算格挡 = 当前抵挡层数 × 倍率（4，升级 5）。卡面用 {CalculatedBlock:diff()} 显示实际值。
    // 注意：calc 传入的 creature 参数在手牌展示时不可靠（可能为空或为悬停的敌人），
    // 故统一用 card.Owner?.Creature 读取玩家自身的抵挡层数，与 OnPlay 保持一致。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        MakeCalculatedBlock(0, (card, creature) =>
        {
            var owner = ResolvePlayerCreatureForDisplay(card);
            if (owner == null) return 0m;
            int resist = owner.GetPower<ResistPower>()?.Amount ?? 0;
            return resist * (card.IsUpgraded ? BalanceValue(nameof(StoneSummon), "UpgradeStoneSummonMultiplier", 5) : BalanceValue(nameof(StoneSummon), "BaseStoneSummonMultiplier", 4));
        }, 1, ValueProp.Move);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int resist = Owner.Creature.GetPower<ResistPower>()?.Amount ?? 0;
        int block = resist * (IsUpgraded ? BalanceValue("UpgradeStoneSummonMultiplier", 5) : BalanceValue("BaseStoneSummonMultiplier", 4));
        if (block > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：唤石", "获得 {CalculatedBlock:diff()} 点格挡（随当前[gold]抵挡[/gold]层数提升）。")
        : new CardLoc("Earth Ninjutsu: Stone Summon", "Gain {CalculatedBlock:diff()} Block (scales with your [gold]Resist[/gold]).");
}
