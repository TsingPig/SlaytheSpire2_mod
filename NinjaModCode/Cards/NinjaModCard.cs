using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Patches.UI;
using BaseLib.Utils;
using Godot;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Extensions;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// This is the base class for your mod's cards, which is set up to load the card's images from your mod's resources.
/// When creating a card, right click the Cards folder and create a new file with the Custom Card template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
[Pool(typeof(NinjaModCardPool))]
public abstract class NinjaModCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target), ICustomUiModel
{
    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    
    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190
    
    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    /// <summary>
    /// 燃烧追加（Burning Infusion）——本 mod 自定义的卡牌效果关键词。
    /// 表示本卡的攻击命中敌人后，额外对其施加对应层数的燃烧。默认 0（无此效果）。
    /// 拥有此效果（值 > 0）的卡牌应在描述中标注“燃烧追加 N”，并显示火焰特效。
    /// </summary>
    public virtual int BurningInfusion => 0;

    /// <summary>
    /// 若本卡拥有燃烧追加（<see cref="BurningInfusion"/> &gt; 0），对目标施加对应层数燃烧。
    /// 应在攻击命中后调用。
    /// </summary>
    protected async System.Threading.Tasks.Task ApplyBurningInfusion(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (BurningInfusion > 0 && target != null)
        {
            await PowerCmd.Apply<BurningPower>(choiceContext, target, BurningInfusion, Owner.Creature, this);
        }
    }

    /// <summary>
    /// 本 mod 自定义效果关键词 → 对应 Power 模型的提示。
    /// 卡牌描述里出现这些关键词（中/英）时，会在卡牌悬浮提示区追加该效果的说明，
    /// 与基础游戏的「消耗」等关键词提示表现一致。
    /// </summary>
    private static readonly (string Zh, string En, System.Func<HoverTip?> Tip)[] KnownEffectTips =
    {
        ("流血",     "Bleed",           () => ModelDb.Power<BleedPower>()?.DumbHoverTip),
        ("燃烧",     "Burning",         () => ModelDb.Power<BurningPower>()?.DumbHoverTip),
        ("抵挡",     "Resist",          () => ModelDb.Power<ResistPower>()?.DumbHoverTip),
        ("淬火",     "Quenching",       () => ModelDb.Power<QuenchingPower>()?.DumbHoverTip),
        ("火盾",     "Flame Shield",    () => ModelDb.Power<FlameShieldPower>()?.DumbHoverTip),
        ("影分身",   "Shadow Clone",    () => ModelDb.Power<ShadowClonePower>()?.DumbHoverTip),
        ("免疫负面", "Debuff Immunity", () => ModelDb.Power<DebuffImmunityPower>()?.DumbHoverTip),
        ("隐身",     "Stealth",         () => ModelDb.Power<StealthPower>()?.DumbHoverTip),
        ("潜行",     "Prowl",           () => ModelDb.Power<ProwlPower>()?.DumbHoverTip),
        ("九字护身", "Kuji Protection", () => ModelDb.Power<KujiProtectionPower>()?.DumbHoverTip),
        ("八咫镜",   "Yata Mirror",     () => ModelDb.Power<YataMirrorPower>()?.DumbHoverTip),
    };

    /// <summary>
    /// 在卡牌原有悬浮提示之外，自动追加本卡描述中提及的自定义效果说明。
    /// 扫描本卡的 <see cref="CustomCardModel.Localization"/> 文本，命中关键词即附加对应 Power 的提示。
    /// </summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var result = new List<IHoverTip>();
            var baseTips = base.ExtraHoverTips;
            if (baseTips != null)
            {
                result.AddRange(baseTips);
            }

            var loc = Localization;
            string text = loc == null
                ? string.Empty
                : string.Join(" ", loc.Select(entry => entry.Item2 ?? string.Empty));

            var emitted = new HashSet<string>();
            foreach (var (zh, en, tipFactory) in KnownEffectTips)
            {
                string needle = Lang.Zh ? zh : en;
                if (!text.Contains(needle)) continue;
                if (!emitted.Add(needle)) continue;

                var tip = tipFactory();
                if (tip != null)
                {
                    result.Add(tip);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// BaseLib 会在每次卡牌节点绑定/刷新时重建这里提供的 UI。
    /// 带燃烧追加的卡牌、或在玩家拥有淬火时的攻击牌，会挂载可动态显隐的火焰边框。
    /// </summary>
    public void CreateCustomUi(Control toAdd)
    {
        // 带燃烧追加的卡牌恒显边框；攻击牌挂载后由边框自身根据淬火状态实时显隐。
        if (BurningInfusion > 0 || Type == CardType.Attack)
        {
            BurningInfusionCardEffect.Attach(toAdd, this);
        }
    }
}
