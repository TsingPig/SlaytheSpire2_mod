using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NinjaMod.NinjaModCode;

namespace NinjaMod.NinjaModCode.Afflictions;

/// <summary>
/// 【残影】卡牌机制。
/// 只挂在【残影】能力生成的复制牌上：该牌变为 0 费、消耗，且由该牌造成的攻击伤害变为 50%。
/// 使用 Affliction 而不是 Enchantment，是为了避免覆盖/冲突原牌已有的附魔。
/// </summary>
public class AfterimageAffliction : AfflictionModel, ICustomModel, ILocalizationProvider
{
    public override bool HasExtraCardText => true;

    public override bool CanAfflictCardType(CardType cardType) => cardType == CardType.Attack;

    public override void AfterApplied()
    {
        // 残影复制牌固定 0 费，并且打出后消耗。
        Card.EnergyCost.SetThisCombat(0, false);
        if (!Card.LocalKeywords.Contains(CardKeyword.Exhaust))
        {
            Card.AddKeyword(CardKeyword.Exhaust);
        }
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (!HasCard) return 1m;
        if (cardSource != Card) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        return 0.5m;
    }

    public List<(string, string)>? Localization => Lang.Zh
        ?
        [
            ("title", "残影"),
            ("description", "这张牌耗费为 0，并且造成原本伤害的 50%。"),
            ("extraCardText", "[gold]残影[/gold]。")
        ]
        :
        [
            ("title", "Afterimage"),
            ("description", "This card costs 0 and deals 50% of its original damage."),
            ("extraCardText", "[gold]Afterimage[/gold].")
        ];
}
