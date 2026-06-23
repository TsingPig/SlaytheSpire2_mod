using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// Fire Ninjutsu: Demon Flame Burst (火忍：火魔爆) - Skill.
/// Deal 12 (16) damage, then ignite all Burning on the target (deal Burning*2 unblockable, remove it).
/// </summary>
public class DemonFlameBurst : NinjaModCard
{
    public DemonFlameBurst() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.HeavyVfx)
            .Execute(choiceContext);

        var burning = cardPlay.Target.GetPower<BurningPower>();
        if (burning != null)
        {
            await burning.IgniteAsync(choiceContext, Owner.Creature);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：火魔爆", $"造成 {DynamicVars.Damage.BaseValue} 点伤害，然后引爆目标身上的所有燃烧（造成燃烧层数 2 倍的无法格挡伤害并移除）。")
        : new CardLoc("Fire Ninjutsu: Demon Flame Burst", $"Deal {DynamicVars.Damage.BaseValue} damage, then ignite all Burning on the target (deal twice Burning as unblockable damage, then remove it).");
}
