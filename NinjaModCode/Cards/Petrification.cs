using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 土忍：石化术（Earth Ninjutsu: Petrification）——技能牌。
/// 2（升级 1）费，获得 13 点格挡，并清除自身所有负面效果（Debuff）。
/// </summary>
public class Petrification : NinjaModCard
{
    public Petrification() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(13m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 清除自身所有负面效果。
        var debuffs = Owner.Creature.Powers
            .Where(p => p.Type == PowerType.Debuff)
            .ToList();
        foreach (var debuff in debuffs)
        {
            await PowerCmd.Remove(debuff);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：石化术", "获得 {Block:diff()} 点格挡，并清除自身所有[gold]负面效果[/gold]。")
        : new CardLoc("Earth Ninjutsu: Petrification", "Gain {Block:diff()} Block and remove all [gold]Debuffs[/gold].");
}
