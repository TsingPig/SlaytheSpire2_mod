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
    /// BaseLib 会在每次卡牌节点绑定/刷新时重建这里提供的 UI。
    /// 只有带燃烧追加的卡牌才挂载动态火焰边框。
    /// </summary>
    public void CreateCustomUi(Control toAdd)
    {
        if (BurningInfusion > 0)
        {
            BurningInfusionCardEffect.Attach(toAdd);
        }
    }
}
