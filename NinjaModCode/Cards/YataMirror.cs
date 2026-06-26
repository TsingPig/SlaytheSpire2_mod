using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 八咫镜（Yata Mirror）——能力牌。
/// 1 费，施加 <see cref="YataMirrorPower"/>：每回合开始获得 2（升级 3）点格挡。
/// </summary>
public class YataMirror : NinjaModCard
{
    public YataMirror() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("BlockPerTurn", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int block = (int)DynamicVars["BlockPerTurn"].BaseValue;
        await PowerCmd.Apply<YataMirrorPower>(choiceContext, Owner.Creature, block, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["BlockPerTurn"].UpgradeValueBy(1m); // 3 -> 4

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("八咫镜", "每回合开始时，获得 {BlockPerTurn:diff()} 点格挡。")
        : new CardLoc("Yata Mirror", "At the start of each turn, gain {BlockPerTurn:diff()} Block.");
}
