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
/// 八咫镜（Yata Mirror）——能力牌。
/// 1 费，施加 <see cref="YataMirrorPower"/>：每回合开始获得 2（升级 3）点格挡。
/// </summary>
public class YataMirror : NinjaModCard
{
    // 每回合格挡量，升级后提升到 3。
    private int _block = BalanceValue(nameof(YataMirror), "BaseBlock", 2);

    public YataMirror() : base(BalanceCost(nameof(YataMirror), 1), BalanceType(nameof(YataMirror), CardType.Power), BalanceRarity(nameof(YataMirror), CardRarity.Uncommon), BalanceTarget(nameof(YataMirror), TargetType.Self)) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<YataMirrorPower>(choiceContext, Owner.Creature, _block, Owner.Creature, this);
    }

    protected override void OnUpgrade() => _block = BalanceValue("UpgradeBlock", 3);

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("八咫镜", $"每回合开始时，获得 {_block} 点格挡。")
        : new CardLoc("Yata Mirror", $"At the start of each turn, gain {_block} Block.");
}
