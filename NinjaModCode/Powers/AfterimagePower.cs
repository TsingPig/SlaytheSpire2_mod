using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Afflictions;
using NinjaMod.NinjaModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 残影（Afterimage）能力——“残影术”施加。
/// 每当你打出一张攻击牌，在弃牌堆中额外生成【残影层数】张该攻击牌的复制牌。
/// 复制牌带【残影】与【消耗】：费用为 0，造成原本伤害的 50%，且不会再触发残影。
/// </summary>
public class AfterimagePower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;

    // 层数即每次生成的残影攻击牌数量。
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Owner.IsPlayer) return;
        if (Amount <= 0) return;

        var card = cardPlay.Card;
        if (card.Type != CardType.Attack) return;
        if (card is AfterimageAttack) return;            // Legacy afterimage token guard.
        if (card.Affliction is AfterimageAffliction) return;
        if (card.Owner?.Creature != Owner) return;

        var player = Owner.Player;
        var combatState = card.CombatState;
        if (player == null || combatState == null) return;

        Flash();
        for (int i = 0; i < Amount; i++)
        {
            var token = card.CreateClone();
            if (token.Affliction != null)
            {
                // 残影复制牌必须显示/执行残影机制；若原复制牌带有其他战斗内 affliction，
                // 这里让【残影】优先，避免 CardCmd.Afflict 因单 affliction 槽位失败。
                CardCmd.ClearAffliction(token);
            }

            var afterimage = await CardCmd.Afflict<AfterimageAffliction>(token, 1m);
            if (afterimage == null)
            {
                // 兜底：即使 affliction 因战斗结束等边界失败，也不要生成可无限循环的普通复制牌。
                token.EnergyCost.SetThisCombat(0, false);
                CardCmd.ApplyKeyword(token, CardKeyword.Exhaust);
            }

            // 加入弃牌堆，而不是手牌/抽牌堆/消失；这张牌是额外生成的复制品。
            await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Discard, player);
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("残影",
            "每当你打出一张攻击牌，在弃牌堆中额外生成等同于层数的该攻击牌复制牌。复制牌带[gold]残影[/gold]与[gold]消耗[/gold]：耗费为 0，造成原本伤害的 50%。",
            "每当你打出一张攻击牌，在弃牌堆中额外生成等同于层数的该攻击牌复制牌。复制牌带[gold]残影[/gold]与[gold]消耗[/gold]：耗费为 0，造成原本伤害的 50%。")
        : new PowerLoc("Afterimage",
            "Whenever you play an Attack, add that many extra copies of it to your discard pile. The copies have [gold]Afterimage[/gold] and [gold]Exhaust[/gold]: they cost 0 and deal 50% of their original damage.",
            "Whenever you play an Attack, add that many extra copies of it to your discard pile. The copies have [gold]Afterimage[/gold] and [gold]Exhaust[/gold]: they cost 0 and deal 50% of their original damage.");
}
