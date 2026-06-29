using System;
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
/// 追魂（Soul Chase）——技能牌，消耗。
/// 3 费，消耗牌堆中每有一张【飞刀】（含残影飞刀），对目标造成其伤害并附加 1 层流血。
/// 若成功击杀，抽 2（升级 3）张牌。
/// </summary>
public class SoulChase : NinjaModCard
{
    // 每张飞刀的伤害（与飞刀基础值一致）。
    private int KunaiDamage => BalanceConst(nameof(SoulChase), nameof(KunaiDamage), 5);

    public SoulChase() : base(BalanceCost(nameof(SoulChase), 3), BalanceType(nameof(SoulChase), CardType.Skill), BalanceRarity(nameof(SoulChase), CardRarity.Uncommon), BalanceTarget(nameof(SoulChase), TargetType.AnyEnemy)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(BalanceValue("BaseCards", 2))];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int startingHp = cardPlay.Target.CurrentHp;
        // 消耗牌堆中的所有飞刀，含【残影】生成的飞刀复制牌（它们也是 Kunai 实例）。
        var kunais = CardPile.GetCards(Owner, new[] { PileType.Exhaust }).OfType<Kunai>().ToList();

        foreach (var kunai in kunais)
        {
            if (cardPlay.Target.CurrentHp <= 0) break;
            // 真正复刻飞刀被完整打出一遍：造成该飞刀的伤害，未被完全格挡则施加 1 层流血。
            int dmg = kunai.DynamicVars.Damage?.IntValue ?? KunaiDamage;
            var attack = await DamageCmd.Attack(dmg)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx(NinjaConstants.SlashVfx)
                .Execute(choiceContext);

            if (cardPlay.Target.CurrentHp > 0 &&
                attack.Results.SelectMany(r => r).Sum(r => r.UnblockedDamage) > 0)
            {
                await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, BalanceValue("BaseSoulChaseBleed", 1), Owner.Creature, this);
            }
        }

        if (startingHp > 0 && cardPlay.Target.CurrentHp <= 0)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, false);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(BalanceDelta("BaseCards", "UpgradeCards", 1m)); // 2 -> 3
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("追魂", "消耗牌堆中每有一张[gold]飞刀[/gold]，对目标造成其伤害。如果未被完全格挡，施加 1 层[gold]流血[/gold]。若击杀目标，抽 {Cards:diff()} 张牌。")
        : new CardLoc("Soul Chase", "For each [gold]Kunai[/gold] in your exhaust pile, deal its damage to the target. If unblocked, apply 1 [gold]Bleed[/gold]. If this kills, draw {Cards:diff()} cards.");
}
