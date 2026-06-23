using System.Collections.Generic;
using System.Linq;
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
/// 土忍：碎石（Earth Ninjutsu: Rock Shatter）——技能牌，消耗。
/// 1 费，获得 8（升级 13）点格挡；自动免费打出手牌中所有“忍者防御”（各结算其格挡）；
/// 随后移除自身 1 层抵挡（Resist）。
/// </summary>
public class RockShatter : NinjaModCard
{
    // 每张忍者防御提供的格挡（与 NinjaDefend 基础值一致）。
    private const int DefendBlock = 5;

    public RockShatter() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1) 本牌自身的格挡。
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2) 自动免费打出手牌中的所有“忍者防御”：逐张结算格挡并移出战斗（进入弃牌）。
        var defends = CardPile.GetCards(Owner, new[] { PileType.Hand })
            .OfType<NinjaDefend>()
            .ToList();
        foreach (var defend in defends)
        {
            await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(DefendBlock, ValueProp.Move), cardPlay);
            await CardPileCmd.RemoveFromCombat(defend, false);
        }

        // 3) 移除 1 层抵挡。
        var resist = Owner.Creature.GetPower<ResistPower>();
        if (resist != null)
        {
            await PowerCmd.Decrement(resist);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5m); // 8 -> 13

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：碎石", $"获得 {DynamicVars.Block.BaseValue} 点格挡。自动免费打出手牌中所有“忍者防御”，随后移除 1 层抵挡。消耗。")
        : new CardLoc("Earth Ninjutsu: Rock Shatter", $"Gain {DynamicVars.Block.BaseValue} Block. Auto-play all Ninja Defends in your hand, then remove 1 Resist. Exhaust.");
}
