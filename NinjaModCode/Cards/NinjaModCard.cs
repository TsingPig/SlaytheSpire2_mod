using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Patches.UI;
using BaseLib.Utils;
using Godot;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Extensions;
using NinjaMod.NinjaModCode.Generated;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
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
    public virtual int BurningInfusion => BalanceValue("BurningInfusion", 0);

    /// <summary>
    /// 是否为“武藏”系列卡牌。用于【圆明】效果判定（每打出一张武藏牌按圆明层数回复生命）。
    /// 武藏系列卡牌应重写此属性返回 true。
    /// </summary>
    public virtual bool IsMusashi => CardBalance.Find(GetType().Name)?.IsMusashi ?? false;

    /// <summary>
    /// 【静默】机制：拥有静默的卡牌打出后不会破除你的隐身。默认 false。
    /// 需要静默的卡牌重写为 true；接口预留：也可写成 => IsUpgraded 实现“升级后获得静默”。
    /// </summary>
    public virtual bool HasSilence => CardBalance.Find(GetType().Name)?.HasSilence ?? false;

    /// <summary>
    /// 卡面预览/悬浮刷新时，BaseLib 的 CalculatedBlock 回调可能拿到的是 canonical card，
    /// 这时 card.Owner / card.CombatState 不稳定，导致显示值回落为 0。
    /// 这里统一提供当前战斗兜底；只用于显示/只读计算，实际打牌仍使用卡牌自身 Owner/CombatState。
    /// </summary>

    protected static int BalanceCost(string cardId, int fallback)
    {
        string? raw = CardBalance.Find(cardId)?.Cost;
        return int.TryParse(raw, out int value) ? value : fallback;
    }

    protected static CardType BalanceType(string cardId, CardType fallback) =>
        BalanceEnum(cardId, entry => entry.Type, fallback);

    protected static CardRarity BalanceRarity(string cardId, CardRarity fallback) =>
        BalanceEnum(cardId, entry => entry.Rarity, fallback);

    protected static TargetType BalanceTarget(string cardId, TargetType fallback) =>
        BalanceEnum(cardId, entry => entry.Target, fallback);

    protected static int BalanceUpgradeCostDelta(string cardId, int fallbackDelta)
    {
        var entry = CardBalance.Find(cardId);
        if (entry == null)
        {
            return fallbackDelta;
        }

        if (!int.TryParse(entry.Cost, out int baseCost) ||
            !int.TryParse(entry.UpgradeCost, out int upgradeCost))
        {
            return fallbackDelta;
        }

        return upgradeCost - baseCost;
    }

    protected static int BalanceValue(string cardId, string key, int fallback) =>
        CardBalance.Value(cardId, key, fallback);

    protected int BalanceValue(string key, int fallback) =>
        BalanceValue(GetType().Name, key, fallback);

    protected static decimal BalanceDecimal(string cardId, string key, decimal fallback) =>
        BalanceValue(cardId, key, (int)fallback);

    protected decimal BalanceDecimal(string key, decimal fallback) =>
        BalanceDecimal(GetType().Name, key, fallback);

    protected static int BalanceConst(string cardId, string constName, int fallback) =>
        BalanceValue(cardId, $"Const.{constName}", BalanceValue(cardId, $"Const{constName}", fallback));

    protected int BalanceConst(string constName, int fallback) =>
        BalanceConst(GetType().Name, constName, fallback);

    protected static int BalanceExtra(string cardId, string varName, int fallback) =>
        BalanceValue(cardId, $"Extra.{varName}", BalanceValue(cardId, $"Base{varName}", fallback));

    protected int BalanceExtra(string varName, int fallback) =>
        BalanceExtra(GetType().Name, varName, fallback);

    protected static decimal BalanceDelta(string cardId, string baseKey, string upgradeKey, decimal fallbackDelta)
    {
        var entry = CardBalance.Find(cardId);
        if (entry == null)
        {
            return fallbackDelta;
        }

        bool hasBase = entry.Values.TryGetValue(baseKey, out int baseValue);
        bool hasUpgrade = entry.Values.TryGetValue(upgradeKey, out int upgradeValue);
        return hasBase && hasUpgrade ? upgradeValue - baseValue : fallbackDelta;
    }

    protected decimal BalanceDelta(string baseKey, string upgradeKey, decimal fallbackDelta) =>
        BalanceDelta(GetType().Name, baseKey, upgradeKey, fallbackDelta);

    protected static bool BalanceIsMusashi(string cardId, bool fallback) =>
        CardBalance.Find(cardId)?.IsMusashi ?? fallback;

    protected static bool BalanceHasSilence(string cardId, bool fallback) =>
        CardBalance.Find(cardId)?.HasSilence ?? fallback;

    private static TEnum BalanceEnum<TEnum>(
        string cardId,
        Func<CardBalanceEntry, string> selector,
        TEnum fallback)
        where TEnum : struct, Enum
    {
        var entry = CardBalance.Find(cardId);
        if (entry == null)
        {
            return fallback;
        }

        string raw = selector(entry);
        return Enum.TryParse(raw, ignoreCase: true, out TEnum value) ? value : fallback;
    }

    protected static ICombatState? ResolveCombatStateForDisplay(CardModel card) =>
        card.CombatState
        ?? (card.IsMutable ? card.Owner?.Creature.CombatState : null)
        ?? CombatManager.Instance?.DebugOnlyGetState();

    protected static Player? ResolveCardOwnerForDisplay(CardModel card)
    {
        if (card.IsMutable)
        {
            var owner = card.Owner;
            if (owner != null)
            {
                return owner;
            }
        }

        return ResolveCombatStateForDisplay(card)?.Players.FirstOrDefault();
    }

    protected static Creature? ResolvePlayerCreatureForDisplay(CardModel card)
    {
        var owner = ResolveCardOwnerForDisplay(card);
        if (owner?.Creature != null)
        {
            return owner.Creature;
        }

        return ResolveCombatStateForDisplay(card)?.PlayerCreatures.FirstOrDefault();
    }

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
        ("静默",     "Silence",         BuildSilenceTip),
        ("潜行",     "Prowl",           BuildProwlTextOnlyTip),
        ("九字护身", "Kuji Protection", () => ModelDb.Power<KujiProtectionPower>()?.DumbHoverTip),
        ("八咫镜",   "Yata Mirror",     () => ModelDb.Power<YataMirrorPower>()?.DumbHoverTip),
        ("残影",     "Afterimage",      () => ModelDb.Power<AfterimagePower>()?.DumbHoverTip),
        ("圆明",     "Enmei",           () => ModelDb.Power<EnmeiPower>()?.DumbHoverTip),
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

            // 自定义关键词【静默】：不是 Power，单独构造提示。
            // 这里是兜底：即使某张静默牌的描述没有直接写出“静默”，仍然显示提示。
            string silenceNeedle = Lang.Zh ? "静默" : "Silence";
            if (HasSilence && emitted.Add(silenceNeedle))
            {
                var silenceTip = BuildSilenceTip();
                if (silenceTip != null)
                {
                    result.Add(silenceTip);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 构造【静默】关键词的悬浮提示（静默不是 Power，需手动构造 HoverTip）。
    /// 图标按需求复用【潜行】Power 的小图标；提示本身不绑定 Prowl，避免去重/文本错误。
    /// </summary>
    private static HoverTip? BuildSilenceTip()
    {
        var prowl = ModelDb.Power<ProwlPower>();
        if (prowl == null) return null;

        var tip = new HoverTip(prowl.Title, prowl.DumbHoverTip.Icon ?? prowl.Icon);
        tip.Title = Lang.Zh ? "静默" : "Silence";
        tip.Description = Lang.Zh
            ? "打出这张牌不会破除你的[gold]隐身[/gold]。"
            : "Playing this card does not break your [gold]Stealth[/gold].";
        tip.Id = "NinjaMod:Silence";
        tip.IsSmart = false;
        tip.IsDebuff = false;
        tip.IsInstanced = false;
        tip.CanonicalModel = null;
        return tip;
    }

    /// <summary>构造无图标版本的【潜行】悬浮提示；文字保留，图标让给【静默】。</summary>
    private static HoverTip? BuildProwlTextOnlyTip()
    {
        var prowl = ModelDb.Power<ProwlPower>();
        if (prowl == null) return null;

        var source = prowl.DumbHoverTip;
        var tip = new HoverTip(prowl.Title, null);
        tip.Title = source.Title;
        tip.Description = source.Description;
        tip.Id = source.Id;
        tip.IsSmart = source.IsSmart;
        tip.IsDebuff = source.IsDebuff;
        tip.IsInstanced = source.IsInstanced;
        tip.CanonicalModel = source.CanonicalModel;
        return tip;
    }

    /// <summary>
    /// BaseLib 会在每次卡牌节点绑定/刷新时重建这里提供的 UI。
    /// 仅为带【燃烧追加】的卡牌挂载火焰边框特效。
    /// </summary>
    public void CreateCustomUi(Control toAdd)
    {
        // 燃烧追加的火焰边框特效会在全屏错误显示，已按需求停用（“实在做不到就删掉”）。
    }
}
