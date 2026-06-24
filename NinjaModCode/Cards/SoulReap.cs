using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 索命（Soul Reap）——技能牌。
/// 2（升级 1）费，移除目标身上最多 8（升级 12）层流血，回复等同于移除层数的生命。
/// 若移除后目标没有流血，则抽 1 张牌并回复 1 点能量。
/// </summary>
public class SoulReap : NinjaModCard
{
    public SoulReap() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    // 可移除的最大流血层数：8（升级 12）。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Remove", 8m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var bleed = cardPlay.Target.GetPower<BleedPower>();
        int cap = DynamicVars["Remove"].IntValue;
        int removed = Math.Min(bleed?.Amount ?? 0, cap);

        if (bleed != null && removed > 0)
        {
            if (removed >= bleed.Amount)
            {
                await PowerCmd.Remove(bleed);
            }
            else
            {
                for (int i = 0; i < removed; i++)
                {
                    await PowerCmd.Decrement(bleed);
                }
            }
            await CreatureCmd.Heal(Owner.Creature, removed, true);
        }

        var after = cardPlay.Target.GetPower<BleedPower>();
        if (after == null || after.Amount <= 0)
        {
            await CardPileCmd.Draw(choiceContext, Owner);
            await PlayerCmd.GainEnergy(1m, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Remove"].UpgradeValueBy(4m); // 8 -> 12

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("索命", "移除目标身上最多 {Remove:diff()} 层[gold]流血[/gold]，回复等同于移除层数的生命。若移除后目标没有[gold]流血[/gold]，抽 1 张牌并回复 1 点能量。")
        : new CardLoc("Soul Reap", "Remove up to {Remove:diff()} [gold]Bleed[/gold] from the target and heal that much HP. If the target then has no [gold]Bleed[/gold], draw 1 card and gain 1 Energy.");
}
