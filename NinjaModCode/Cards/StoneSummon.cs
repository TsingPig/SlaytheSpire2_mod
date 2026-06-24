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
    public StoneSummon() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    // 动态计算格挡 = 当前抵挡层数 × 倍率（4，升级 5）。卡面用 {CalculatedBlock:diff()} 显示实际值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        MakeCalculatedBlock(0, (card, creature) =>
        {
            if (creature == null) return 0m;
            int resist = creature.GetPower<ResistPower>()?.Amount ?? 0;
            return resist * (card.IsUpgraded ? 5 : 4);
        }, 0, ValueProp.Move);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int resist = Owner.Creature.GetPower<ResistPower>()?.Amount ?? 0;
        int block = resist * (IsUpgraded ? 5 : 4);
        if (block > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：唤石", $"获得 {{CalculatedBlock:diff()}} 点格挡（当前[gold]抵挡[/gold]层数 × {(IsUpgraded ? 5 : 4)}）。")
        : new CardLoc("Earth Ninjutsu: Stone Summon", $"Gain {{CalculatedBlock:diff()}} Block (current [gold]Resist[/gold] × {(IsUpgraded ? 5 : 4)}).");
}
