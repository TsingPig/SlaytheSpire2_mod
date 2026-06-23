using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 骨法（Bone Art）——技能牌。
/// 1 费，获得 11 点格挡，并获得 2 点活力（Vigor，即“敏捷”——下次攻击附加伤害）。
/// </summary>
public class BoneArt : NinjaModCard
{
    // 活力层数（常量）。
    private const int Vigor = 2;

    public BoneArt() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(11m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        // 施加基础游戏的活力 Power。
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, Vigor, Owner.Creature, this);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("骨法", $"获得 {DynamicVars.Block.BaseValue} 点格挡，并获得 {Vigor} 点活力。")
        : new CardLoc("Bone Art", $"Gain {DynamicVars.Block.BaseValue} Block and {Vigor} Vigor.");
}
