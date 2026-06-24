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
/// 影心刺（Shadow Pierce）——攻击牌（稀有）。
/// 0 费，只有在隐身状态下才能打出。造成 9（升级 14）点伤害，附加 5（升级 6）层流血。
/// </summary>
public class ShadowPierce : NinjaModCard
{
    // 附加的流血层数，升级后提升到 6。
    private int _bleed = 5;

    public ShadowPierce() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override bool PreservesStealth => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, _bleed, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m); // 9 -> 14
        _bleed = 6;
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("影心刺", $"只能在[gold]隐身[/gold]状态下打出。造成 {{Damage:diff()}} 点伤害，附加 {_bleed} 层[gold]流血[/gold]。打出这张牌不会失去[gold]隐身[/gold]。")
        : new CardLoc("Shadow Pierce", $"Can only be played while [gold]Stealthed[/gold]. Deal {{Damage:diff()}} damage and apply {_bleed} [gold]Bleed[/gold]. Playing this card does not break [gold]Stealth[/gold].");
}
