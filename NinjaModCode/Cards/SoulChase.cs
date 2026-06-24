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
/// 追魂（Soul Chase）——技能牌，消耗。
/// 3（升级 2）费，对目标打出消耗牌堆中的所有飞刀（每张造成飞刀的伤害）。
/// 若成功击杀目标，抽 2（升级 3）张牌。
/// </summary>
public class SoulChase : NinjaModCard
{
    // 每张飞刀的伤害（与飞刀基础值一致）。
    private const int KunaiDamage = 5;

    public SoulChase() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 击杀后抽牌数：2（升级 3）。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var kunais = CardPile.GetCards(Owner, new[] { PileType.Exhaust }).OfType<Kunai>().ToList();

        foreach (var kunai in kunais)
        {
            if (cardPlay.Target.CurrentHp <= 0) break;
            int dmg = kunai.DynamicVars.Damage?.IntValue ?? KunaiDamage;
            await DamageCmd.Attack(dmg)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx(NinjaConstants.SlashVfx)
                .Execute(choiceContext);
        }

        // 成功击杀则抽牌。
        if (cardPlay.Target.CurrentHp <= 0)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, false);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m); // 2 -> 3

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("追魂", "对目标打出消耗牌堆中的所有[gold]飞刀[/gold]（每张造成其伤害）。若成功击杀，抽 {Cards:diff()} 张牌。")
        : new CardLoc("Soul Chase", "Play all [gold]Kunai[/gold] from your exhaust pile at the target (each deals its damage). If this kills, draw {Cards:diff()} cards.");
}
