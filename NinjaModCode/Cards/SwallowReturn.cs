using System;
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
/// 燕返（Swallow Return）——攻击牌。
/// 1 费，造成 4（升级 7）点伤害；若伤害被目标格挡完全抵消，则获得 1 点能量。
/// </summary>
public class SwallowReturn : NinjaModCard
{
    public SwallowReturn() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        var results = attack.Results.SelectMany(r => r).ToList();
        bool dealtSomething = results.Sum(r => r.BlockedDamage) > 0 || results.Sum(r => r.UnblockedDamage) > 0;
        bool fullyBlocked = dealtSomething && results.Sum(r => r.UnblockedDamage) <= 0;
        if (fullyBlocked)
        {
            await PlayerCmd.GainEnergy(1m, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("燕返", $"造成 {DynamicVars.Damage.BaseValue} 点伤害。如果伤害被完全格挡，获得 1 点能量。")
        : new CardLoc("Swallow Return", $"Deal {DynamicVars.Damage.BaseValue} damage. If fully blocked, gain 1 Energy.");
}
