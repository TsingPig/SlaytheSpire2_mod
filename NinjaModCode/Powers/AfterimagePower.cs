using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 残影（Afterimage）能力——“残影术”施加。
/// 每当你打出一张攻击牌，在弃牌堆中额外生成【残影层数】张 0 费的【残影攻击牌】，
/// 每张残影攻击牌造成原攻击伤害一半的伤害。残影攻击牌本身不会再触发残影。
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
        if (card is AfterimageAttack) return;            // 残影攻击牌不再触发残影
        if (card.Owner?.Creature != Owner) return;

        // 取该攻击牌的基础伤害，减半（向下取整）。
        int dmg = card.DynamicVars.Damage?.IntValue ?? 0;
        int half = dmg / 2;
        if (half <= 0) return;

        var player = Owner.Player;
        var combatState = card.CombatState;
        if (player == null || combatState == null) return;

        Flash();
        for (int i = 0; i < Amount; i++)
        {
            var token = combatState.CreateCard<AfterimageAttack>(player);
            token.SetDamage(half);
            await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Discard, player);
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("残影",
            "每当你打出一张攻击牌，在弃牌堆中生成等同于层数的 0 费残影攻击牌（各造成原伤害一半的伤害）。",
            "每当你打出一张攻击牌，在弃牌堆中生成等同于层数的 0 费残影攻击牌（各造成原伤害一半的伤害）。")
        : new PowerLoc("Afterimage",
            "Whenever you play an Attack, add that many 0-cost Afterimage Attacks to your discard pile (each deals half the original damage).",
            "Whenever you play an Attack, add that many 0-cost Afterimage Attacks to your discard pile (each deals half the original damage).");
}
