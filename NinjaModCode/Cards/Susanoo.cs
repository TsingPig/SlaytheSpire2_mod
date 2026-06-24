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
/// 须佐能乎（Susanoo）——攻击牌（稀有）。
/// 3 费，造成 7（升级 9）点伤害，共 6 段；每段伤害后立即追加 1 层流血。
/// </summary>
public class Susanoo : NinjaModCard
{
    public Susanoo() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move), new RepeatVar(6)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // 逐段攻击：每命中一次立即追加 1 层流血。
        int hits = DynamicVars.Repeat.IntValue;
        for (int i = 0; i < hits; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx(NinjaConstants.SlashVfx)
                .Execute(choiceContext);
            await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, 1, Owner.Creature, this);
        }

    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m); // 7 -> 9

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("须佐能乎", "造成 {Damage:diff()} 点伤害，共 {Repeat:diff()} 段，每段追加 1 层[gold]流血[/gold]。")
        : new CardLoc("Susanoo", "Deal {Damage:diff()} damage {Repeat:diff()} times; each hit applies 1 [gold]Bleed[/gold].");
}
