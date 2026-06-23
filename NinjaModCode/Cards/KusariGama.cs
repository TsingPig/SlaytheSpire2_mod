using System;
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
/// 锁镰（Kusari-Gama）——攻击牌。
/// 1 费，造成 9（升级 12）点伤害；若目标有流血，则额外造成 4（升级 6）点伤害。
/// </summary>
public class KusariGama : NinjaModCard
{
    // 对流血目标的额外伤害，升级后提升到 6。
    private int _bonus = 4;

    public KusariGama() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool hadBleed = cardPlay.Target.GetPower<BleedPower>() != null;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        if (hadBleed)
        {
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, _bonus,
                ValueProp.Move, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); // 9 -> 12
        _bonus = 6;
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("锁镰", $"造成 {DynamicVars.Damage.BaseValue} 点伤害。如果目标有流血，额外造成 {_bonus} 点伤害。")
        : new CardLoc("Kusari-Gama", $"Deal {DynamicVars.Damage.BaseValue} damage. If the target has Bleed, deal {_bonus} extra damage.");
}
