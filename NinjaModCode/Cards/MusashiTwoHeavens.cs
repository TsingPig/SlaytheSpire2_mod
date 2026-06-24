using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 武藏：二天一流（Musashi: Two Heavens）——技能牌。
/// 3（升级 2）费，造成 16 点伤害，共 2 段。若目标同时拥有流血和燃烧，则使其虚弱、易伤各 2（近似“眩晕”）。
/// </summary>
public class MusashiTwoHeavens : NinjaModCard
{
    // 流血+燃烧时施加的虚弱/易伤层数（用于近似“眩晕 1 回合”，因为游戏没有通用的对敌眩晕接口）。
    private const int DebuffOnStun = 2;

    public MusashiTwoHeavens() : base(3, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override bool IsMusashi => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(16m, ValueProp.Move), new RepeatVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        var bleed = cardPlay.Target.GetPower<BleedPower>();
        var burning = cardPlay.Target.GetPower<BurningPower>();
        if (bleed != null && bleed.Amount > 0 && burning != null && burning.Amount > 0)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, DebuffOnStun, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, DebuffOnStun, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 3 -> 2

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：二天一流", "造成 {Damage:diff()} 点伤害，共 {Repeat} 段。若目标同时拥有[gold]流血[/gold]与[gold]燃烧[/gold]，则使其[gold]虚弱[/gold]、[gold]易伤[/gold]各 2。")
        : new CardLoc("Musashi: Two Heavens", "Deal {Damage:diff()} damage {Repeat} times. If the target has both [gold]Bleed[/gold] and [gold]Burning[/gold], apply 2 [gold]Weak[/gold] and 2 [gold]Vulnerable[/gold].");
}
