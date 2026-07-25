using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using NinjaMod.NinjaModCode.Powers;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑骑士自定义意图集合。均复用游戏内置意图图标（攻击 / 防御 / Debuff / 状态牌 / Buff），
/// 但把标题与描述指向注入 "intents" 表的 <c>BK_*</c> 键（见 <see cref="BlackKnightLoc"/>）。
///
/// 攻击类意图继承 <see cref="SingleAttackIntent"/>，因此显示的数值会自动叠加力量等修正
/// （<c>GetSingleDamage</c> 走 <c>Hook.ModifyDamage</c>），保证意图与实际结算一致。
/// </summary>
internal static class BlackKnightIntents
{
    /// <summary>诅咒发放：状态牌图标 + 自定义描述（洗入 5 张幽冥诅咒）。</summary>
    internal sealed class CurseCast : StatusIntent
    {
        public CurseCast() : base(BlackKnightConfig.CurseCardCount) => BlackKnightLoc.EnsureInjected();
        protected override string IntentPrefix => "BK_CURSE";

        // IntentPrefix also drives NIntent's animated-icon lookup. BK_* prefixes
        // only exist in our localization table, not in the game's IntentAnimData,
        // so use the canonical Status animation while retaining custom text.
        public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) =>
            new StatusIntent(BlackKnightConfig.CurseCardCount).GetAnimation(targets, owner);

        protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
        {
            BlackKnightLoc.EnsureInjected();
            return base.GetIntentDescription(targets, owner);
        }
    }

    /// <summary>斜劈 / 横砍：攻击图标 + 数值（含力量）+ 自定义描述。</summary>
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

    /// <summary>
    /// 竖劈+：攻击图标 + 数值。描述随黑骑士当前是否携带
    /// <see cref="VerticalSlashProtectionPower"/> 在“不可格挡”与“可格挡+吸血”之间动态切换，
    /// 保证意图文本与实际结算一致。
    /// </summary>
    internal sealed class Vertical : SingleAttackIntent
    {
        public Vertical(Func<decimal> damage) : base(damage) => BlackKnightLoc.EnsureInjected();
        protected override string IntentPrefix => "BK_VERTICAL";

        public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) =>
            new SingleAttackIntent(DamageCalc!).GetAnimation(targets, owner);

        protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
        {
            BlackKnightLoc.EnsureInjected();
            bool warded = owner.GetPower<VerticalSlashProtectionPower>() != null;
            var loc = new LocString("intents",
                warded ? "BK_VERTICAL_BLOCK.description" : "BK_VERTICAL_UNBLOCK.description");
            loc.Add("Damage", GetSingleDamage(targets, owner));
            loc.Add("Repeat", Repeats);
            var cs = owner.CombatState;
            loc.Add("IsMultiplayer", cs != null && cs.Players.Count > 1);
            return loc;
        }
    }

    /// <summary>暗鬼铠甲：防御图标 + 自定义描述。</summary>
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

    /// <summary>强化——真身：Buff 图标 + 自定义描述。</summary>
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
