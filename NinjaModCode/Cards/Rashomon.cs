using System.Collections.Generic;
using System.Linq;
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
/// 多重罗生门（Manifold Rashomon）——技能牌。
/// 2 费，抽 3（升级 4）张牌；随后手牌中每有一张攻击牌，获得 9 点格挡。
/// </summary>
public class Rashomon : NinjaModCard
{
    // 每张攻击牌提供的格挡。
    private const int BlockPerAttack = 9;

    public Rashomon() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 只统计“本次抽到”的牌中的攻击牌（而非整个手牌）。
        var drawn = (await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, false)).ToList();

        int attackCount = drawn.Count(c => c.Type == CardType.Attack);
        if (attackCount > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, attackCount * BlockPerAttack, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m); // 3 -> 4

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("多重罗生门", $"抽 {{Cards:diff()}} 张牌。抽到的牌中每有一张[gold]攻击[/gold]牌，获得 {BlockPerAttack} 点格挡。")
        : new CardLoc("Manifold Rashomon", $"Draw {{Cards:diff()}} cards. Gain {BlockPerAttack} Block for each [gold]Attack[/gold] among the drawn cards.");
}
