using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 土忍：地震（Earth Ninjutsu: Earthquake）——技能牌（稀有）。
/// 5 费，对所有敌人造成 8（升级 12）点伤害，并使所有敌人【眩晕】。
/// </summary>
public class Earthquake : NinjaModCard
{
    public Earthquake() : base(BalanceCost(nameof(Earthquake), 5), BalanceType(nameof(Earthquake), CardType.Skill), BalanceRarity(nameof(Earthquake), CardRarity.Rare), BalanceTarget(nameof(Earthquake), TargetType.AllEnemies)) { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat([StunIntent.GetStaticHoverTip()]);

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BalanceDecimal("BaseDamage", 8m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        foreach (var enemy in CombatState.HittableEnemies.Where(e => e.IsAlive).ToList())
        {
            await CreatureCmd.Stun(enemy);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 4m)); // 8 -> 12

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：地震", "对所有敌人造成 {Damage:diff()} 点伤害，并使所有敌人[gold]眩晕[/gold]。")
        : new CardLoc("Earth Ninjutsu: Earthquake", "Deal {Damage:diff()} damage to ALL enemies and [gold]Stun[/gold] them.");
}
