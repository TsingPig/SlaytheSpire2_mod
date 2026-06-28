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
/// 2 费，对目标打出消耗牌堆中的所有【飞刀】（含残影飞刀），每张造成其伤害并附加 1 层流血。
/// 升级后获得【保留】。
/// </summary>
public class SoulChase : NinjaModCard
{
    // 每张飞刀的伤害（与飞刀基础值一致）。
    private int KunaiDamage => BalanceConst(nameof(SoulChase), nameof(KunaiDamage), 5);

    public SoulChase() : base(BalanceCost(nameof(SoulChase), 2), BalanceType(nameof(SoulChase), CardType.Skill), BalanceRarity(nameof(SoulChase), CardRarity.Uncommon), BalanceTarget(nameof(SoulChase), TargetType.AnyEnemy)) { }

    // 基础：消耗；升级：额外获得保留（随 IsUpgraded 实时变化）。
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [CardKeyword.Exhaust, CardKeyword.Retain] : [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
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
    }

    protected override void OnUpgrade()
    {
        // 升级获得保留；AddKeyword 触发 KeywordsChanged 让卡面关键词行刷新。
        AddKeyword(CardKeyword.Retain);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("追魂", "对目标打出消耗牌堆中的所有[gold]飞刀[/gold]（每张造成其伤害）。")
        : new CardLoc("Soul Chase", "Play all [gold]Kunai[/gold] from your exhaust pile at the target (each deals its damage).");
}
