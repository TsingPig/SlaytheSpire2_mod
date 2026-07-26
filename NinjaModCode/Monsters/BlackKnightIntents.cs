using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using NinjaMod.NinjaModCode.Cards;
using NinjaMod.NinjaModCode.Powers;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑暗骑士的自定义意图。图标复用游戏原生意图动画，
/// 标题与规则文本由 BlackKnightLoc 注入。
/// </summary>
internal static class BlackKnightIntents
{
    internal sealed class CurseCast : StatusIntent
    {
        private readonly int _cardCount;

        public CurseCast(int cardCount = BlackKnightConfig.CurseCardCount) : base(cardCount)
        {
            _cardCount = cardCount;
            BlackKnightLoc.EnsureInjected();
        }
        protected override string IntentPrefix => "BK_CURSE";

        public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) =>
            new StatusIntent(_cardCount).GetAnimation(targets, owner);

        protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
        {
            BlackKnightLoc.EnsureInjected();
            return base.GetIntentDescription(targets, owner);
        }
    }

    internal sealed class Slash : SingleAttackIntent
    {
        private readonly string _prefix;

        public Slash(Func<decimal> damage, string prefix) : base(damage)
        {
            _prefix = prefix;
            BlackKnightLoc.EnsureInjected();
        }

        protected override string IntentPrefix => _prefix;

        public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) =>
            new SingleAttackIntent(DamageCalc!).GetAnimation(targets, owner);

        protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
        {
            BlackKnightLoc.EnsureInjected();
            return base.GetIntentDescription(targets, owner);
        }
    }

    internal sealed class Vertical : SingleAttackIntent
    {
        public Vertical(Func<decimal> damage) : base(damage) => BlackKnightLoc.EnsureInjected();
        protected override string IntentPrefix => "BK_VERTICAL";

        public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
        {
            BlackKnightLoc.EnsureInjected();

            Player? localPlayer = LocalContext.GetMe(owner.CombatState);
            bool hasNetherCurse = localPlayer != null
                && PileType.Hand
                    .GetPile(localPlayer)
                    .Cards
                    .Any(card => card is NetherCurse);
            bool hasSavedExposure = localPlayer?.Creature
                .GetPower<VerticalSlashExposurePower>() != null;
            bool isUnblockable = BlackKnightRules.VerticalIntentIsUnblockable(
                hasNetherCurse,
                hasSavedExposure);

            var label = new LocString(
                "intents",
                isUnblockable
                    ? "BK_VERTICAL.damage.unblockable"
                    : "BK_VERTICAL.damage.blockable");
            label.Add("Damage", GetSingleDamage(targets, owner));
            return label;
        }

        public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) =>
            new SingleAttackIntent(DamageCalc!).GetAnimation(targets, owner);

        protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
        {
            BlackKnightLoc.EnsureInjected();
            return base.GetIntentDescription(targets, owner);
        }
    }

    internal sealed class DarkArmor : DefendIntent
    {
        public DarkArmor() => BlackKnightLoc.EnsureInjected();
        protected override string IntentPrefix => "BK_DARK_ARMOR";

        public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) =>
            new DefendIntent().GetAnimation(targets, owner);

        protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
        {
            BlackKnightLoc.EnsureInjected();
            return base.GetIntentDescription(targets, owner);
        }
    }

    internal sealed class TrueForm : BuffIntent
    {
        public TrueForm() => BlackKnightLoc.EnsureInjected();
        protected override string IntentPrefix => "BK_TRUE_FORM";

        public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) =>
            new BuffIntent().GetAnimation(targets, owner);

        protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
        {
            BlackKnightLoc.EnsureInjected();
            return base.GetIntentDescription(targets, owner);
        }
    }
}
