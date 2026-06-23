using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 火忍：灰烬（Fire Ninjutsu: Ashes）——技能牌。
/// 1（升级 0）费，点燃所有敌人身上的燃烧（造成燃烧 2 倍的无法格挡伤害并移除）。
/// 若至少成功点燃一次，则消耗抽牌堆顶部一张牌。
/// </summary>
public class Ashes : NinjaModCard
{
    public Ashes() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool ignitedAny = false;
        foreach (var enemy in CombatState.HittableEnemies.ToList())
        {
            var burning = enemy.GetPower<BurningPower>();
            if (burning != null && burning.Amount > 0)
            {
                await burning.IgniteAsync(choiceContext, Owner.Creature);
                ignitedAny = true;
            }
        }

        // 成功点燃则消耗抽牌堆中的一张牌。
        if (ignitedAny)
        {
            var drawCard = CardPile.GetCards(Owner, new[] { PileType.Draw }).FirstOrDefault();
            if (drawCard != null)
            {
                await CardPileCmd.RemoveFromCombat(drawCard, true);
            }
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 1 -> 0

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：灰烬", "点燃所有敌人身上的燃烧（造成燃烧 2 倍的无法格挡伤害并移除）。如果成功点燃，消耗抽牌堆中的一张牌。")
        : new CardLoc("Fire Ninjutsu: Ashes", "Ignite all Burning on enemies (deal twice Burning as unblockable damage, then remove). If anything ignited, exhaust a card from your draw pile.");
}
