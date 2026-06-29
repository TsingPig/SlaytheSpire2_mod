using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 火焰之舞（Flame Dance）能力——“火忍：火焰之舞”施加。
/// 每回合第一次打出【火忍】牌时，获得等同于层数（1/升级 2）点能量。
/// </summary>
public class FlameDancePower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 本回合是否已触发（每回合仅第一次火忍牌生效）。
    private bool _triggeredThisTurn;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Owner.IsPlayer) return;
        if (_triggeredThisTurn) return;
        if (Amount <= 0) return;
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card is not NinjaModCard { IsFireNinjutsu: true }) return;
        if (CombatManager.Instance == null || CombatManager.Instance.IsOverOrEnding) return;
        var player = Owner.Player;
        if (player == null) return;

        _triggeredThisTurn = true;
        Flash();
        await PlayerCmd.GainEnergy(Amount, player);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            _triggeredThisTurn = false;
        }

        return Task.CompletedTask;
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("火焰之舞",
            "每回合第一次打出[gold]火忍[/gold]牌时，获得等同于层数的能量。",
            "每回合第一次打出[gold]火忍[/gold]牌时，获得等同于层数的能量。")
        : new PowerLoc("Flame Dance",
            "The first time you play a Fire Ninjutsu card each turn, gain energy equal to its amount.",
            "The first time you play a Fire Ninjutsu card each turn, gain energy equal to its amount.");
}
