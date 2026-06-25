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
/// 武藏：七星光芒斩（Musashi: Seven Star Radiance）——技能牌。
/// 2 费，造成 7 点伤害，共 7 段。
/// 升级后追加第 8 段斩杀伤害：目标每损失 5 点生命，造成 1 点伤害。
/// </summary>
public class MusashiSevenStar : NinjaModCard
{
    // 每损失多少生命提供 1 点斩杀加成。
    private const int HpPerExecute = 5;

    public MusashiSevenStar() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override bool IsMusashi => true;

    // 基础伤害 7、段数 7；ExecutePerFive 表示升级后第 8 段斩杀伤害的系数，0（升级 1），用于卡面/锻造前后对比显示。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move), new RepeatVar(7), new IntVar("ExecutePerFive", 0m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 前 7 段永远是固定的 7 点伤害；升级不再把斩杀加成加到每一段上。
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        // 升级后追加第 8 段斩杀伤害：按前 7 段结算后的当前已损失生命计算。
        if (IsUpgraded)
        {
            int lost = Math.Max(0, cardPlay.Target.MaxHp - cardPlay.Target.CurrentHp);
            int executeDamage = lost / HpPerExecute;
            if (executeDamage > 0 && cardPlay.Target.CurrentHp > 0)
            {
                await DamageCmd.Attack(executeDamage)
                    .FromCard(this)
                    .Targeting(cardPlay.Target)
                    .WithHitFx(NinjaConstants.SlashVfx)
                    .Execute(choiceContext);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars["ExecutePerFive"].UpgradeValueBy(1m); // 0 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：七星光芒斩", "造成 {Damage:diff()} 点伤害，共 {Repeat} 段。追加一段斩杀：目标每损失 5 点生命，造成 {ExecutePerFive:diff()} 点伤害。")
        : new CardLoc("Musashi: Seven Star Radiance", "Deal {Damage:diff()} damage {Repeat} times. Execute hit: deal {ExecutePerFive:diff()} damage for every 5 HP the target has lost.");
}
