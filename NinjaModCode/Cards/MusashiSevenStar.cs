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
/// 武藏：七星光芒斩（Musashi: Seven Star Radiance）——攻击牌。
/// 2 费，造成 7 点伤害，共 7 段（快速连斩）。
/// 升级后追加斩杀：目标每损失 5 点生命，每段额外造成 1 点伤害。
/// </summary>
public class MusashiSevenStar : NinjaModCard
{
    // 每损失多少生命提供 1 点斩杀加成。
    private const int HpPerExecute = 5;

    public MusashiSevenStar() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override bool IsMusashi => true;

    // 基础伤害 7、段数 7；ExecutePerFive 表示“每损失 5 生命的额外伤害”，0（升级 1），用于卡面/锻造前后对比显示。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move), new RepeatVar(7), new IntVar("ExecutePerFive", 0m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 升级后：每段额外伤害 = 目标已损失生命 / 5。
        int execute = 0;
        if (IsUpgraded)
        {
            int lost = cardPlay.Target.MaxHp - cardPlay.Target.CurrentHp;
            execute = lost / HpPerExecute;
        }
        int perHit = DynamicVars.Damage.IntValue + execute;

        // 单条多段攻击，连斩间隔很短。
        await DamageCmd.Attack(perHit)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars["ExecutePerFive"].UpgradeValueBy(1m); // 0 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：七星光芒斩", "造成 {Damage:diff()} 点伤害，共 {Repeat} 段。目标每损失 5 点生命，每段额外造成 {ExecutePerFive:diff()} 点伤害。")
        : new CardLoc("Musashi: Seven Star Radiance", "Deal {Damage:diff()} damage {Repeat} times. For every 5 HP the target has lost, each hit deals {ExecutePerFive:diff()} extra damage.");
}
