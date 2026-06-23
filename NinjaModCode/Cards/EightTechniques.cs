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
    // 是否消耗（升级后为 false）。
    private bool _exhaust = true;

    public EightTechniques() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        _exhaust ? [CardKeyword.Exhaust] : [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this); // 力量
        await PowerCmd.Apply<ResistPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);   // 抵抗
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);     // 活力/敏捷
        await PlayerCmd.GainEnergy(1m, Owner);                                                         // 能量
        await CreatureCmd.GainBlock(Owner.Creature, 1, ValueProp.Move, cardPlay);                      // 格挡
        await Kunai.CreateInHand(Owner, CombatState);                                                  // 飞刀
        await CreatureCmd.GainMaxHp(Owner.Creature, 1);                                                // 最大生命
        await CreatureCmd.Heal(Owner.Creature, 1, true);                                               // 生命恢复
    }

    protected override void OnUpgrade() => _exhaust = false; // 升级移除消耗

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("忍者八法", "获得 1 点力量、1 点抵抗、1 点活力、1 点能量、1 点格挡、1 张飞刀、1 点最大生命，并回复 1 点生命。" + (_exhaust ? "消耗。" : ""))
        : new CardLoc("Eight Techniques", "Gain 1 Strength, 1 Resist, 1 Vigor, 1 Energy, 1 Block, 1 Kunai, 1 Max HP, and heal 1 HP." + (_exhaust ? " Exhaust." : ""));
}
