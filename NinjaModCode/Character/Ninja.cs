using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using NinjaMod.NinjaModCode.Cards;
using NinjaMod.NinjaModCode.Extensions;
using NinjaMod.NinjaModCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace NinjaMod.NinjaModCode.Character;

/// <summary>
/// The playable Ninja character. Base-game animation-only assets still come from the
/// placeholder model, while combat and character-select visuals use Ninja art.
/// </summary>
public class Ninja : PlaceholderCharacterModel
{
    public const string CharacterId = NinjaConstants.CharacterId;

    // Dark steel / ninja color.
    public static readonly Color NinjaColor = new("3a4a5a");

    public override Color NameColor => NinjaColor;
    public override CharacterGender Gender => CharacterGender.Neutral;

    public override int StartingHp => 72;

    // Energy gained per turn.
    public override int MaxEnergy => 3;

    // StartingGold intentionally not overridden -> uses the template/base default.

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<NinjaStrike>(),
        ModelDb.Card<NinjaStrike>(),
        ModelDb.Card<NinjaStrike>(),
        ModelDb.Card<NinjaStrike>(),
        ModelDb.Card<NinjaDefend>(),
        ModelDb.Card<NinjaDefend>(),
        ModelDb.Card<NinjaDefend>(),
        ModelDb.Card<NinjaDefend>(),
        ModelDb.Card<Shuriken>(),
        ModelDb.Card<Assassination>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<HiddenBlade>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<NinjaModCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<NinjaModRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<NinjaModPotionPool>();

    // 战斗中人物视觉场景（使用 ninja.tscn 中的 ninja_battle.png 静态贴图）
    public override string CustomVisualPath => "creature_visuals/ninja.tscn".ScenePath();
    // 选中角色时展开的背景海报场景
    public override string CustomCharacterSelectBg => "screens/char_select/char_select_bg_ninja.tscn".ScenePath();

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CharacterLoc(
            Title: "忍者",
            TitleObject: "忍者",
            Description: "迅捷的刺客，将细小的伤口化作致命的压力。",
            PronounObject: "他",
            PronounSubject: "他",
            PronounPossessive: "他的",
            PossessiveAdjective: "他的",
            AromaPrinciple: "刃与影",
            EndTurnPingAlive: "结束回合。",
            EndTurnPingDead: "……",
            EventDeathPrevention: "忍术让其逃过一劫。",
            GoldMonologue: "影子收下了报酬。",
            CardsModifierTitle: "忍者",
            CardsModifierDescription: "忍者牌")
        : new CharacterLoc(
            Title: "Ninja",
            TitleObject: "the Ninja",
            Description: "A swift assassin who turns small wounds into fatal pressure.",
            PronounObject: "them",
            PronounSubject: "they",
            PronounPossessive: "theirs",
            PossessiveAdjective: "their",
            AromaPrinciple: "Blade and Shadow",
            EndTurnPingAlive: "Turn ends.",
            EndTurnPingDead: "...",
            EventDeathPrevention: "A ninjutsu spared them.",
            GoldMonologue: "The shadow accepts payment.",
            CardsModifierTitle: "Ninja",
            CardsModifierDescription: "Ninja cards");

    /* PlaceholderCharacterModel still supplies animation-only assets such as rest-site and
       merchant animations. The combat sprite, poster, and simple UI icons are overridden here. */
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}
