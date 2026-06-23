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
/// The playable Ninja character. Uses the Ironclad as a visual placeholder (via
/// <see cref="PlaceholderCharacterModel"/>) until custom art/animations are added.
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

    /*  PlaceholderCharacterModel uses placeholder basegame assets for most character assets.
        The simplest UI icons are overridden below to differentiate the Ninja. Replace the PNGs
        under NinjaMod/images/charui/ to customise. */
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
