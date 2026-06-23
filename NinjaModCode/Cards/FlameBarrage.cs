using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// Fire Ninjutsu: Flame Barrage (火忍：火焰弹幕) - Skill.
/// Deal 2x3 (3x3 upgraded) damage, then apply 3 (4) Burning.
/// Kept as a Skill that performs attack hits (card type does not restrict OnPlay behaviour).
/// </summary>
public class FlameBarrage : NinjaModCard
{
    private int _hitDamage = 2;
    private int _hits = 3;
    private int _burning = 3;

    public FlameBarrage() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(_hitDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(_hits)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        await PowerCmd.Apply<BurningPower>(choiceContext, cardPlay.Target, _burning, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        _hitDamage = 3;
        _burning = 4;
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：火焰弹幕", "造成 3 次 2 点伤害，然后施加 3 层燃烧。")
        : new CardLoc("Fire Ninjutsu: Flame Barrage", "Deal 2 damage 3 times, then apply 3 Burning.");
}
