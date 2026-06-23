using System;
using System.Collections.Generic;
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
/// 武士刀法（Katana Art）——攻击牌。
/// 1 费，对所有敌人造成 5 点伤害，命中 2（升级 3）次。
/// 命中次数用 <see cref="_hits"/> 变量表示，升级时改变。
/// </summary>
public class KatanaArt : NinjaModCard
{
    // 每个目标的命中次数，升级后提升到 3。
    private int _hits = 2;

    public KatanaArt() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对所有敌人造成多段伤害（TargetingAllOpponents 需要战斗状态）。
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitCount(_hits)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => _hits = 3;

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武士刀法", $"对所有敌人造成 {DynamicVars.Damage.BaseValue} 点伤害，共 {_hits} 次。")
        : new CardLoc("Katana Art", $"Deal {DynamicVars.Damage.BaseValue} damage to ALL enemies {_hits} times.");
}
