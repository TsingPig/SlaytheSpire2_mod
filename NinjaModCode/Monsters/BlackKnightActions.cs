using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NinjaMod.NinjaModCode.Cards;
using NinjaMod.NinjaModCode.Compatibility;
using NinjaMod.NinjaModCode.Powers;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑骑士的行动实现（诅咒发放 / 斜劈 / 横砍 / 竖劈+ / 暗鬼铠甲 / 真身强化）。
/// 每个方法签名匹配 <c>Func&lt;IReadOnlyList&lt;Creature&gt;, Task&gt;</c>，作为状态机中各
/// <see cref="MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine.MoveState"/> 的执行体。
///
/// 所有伤害均走游戏正式伤害管线（受力量等修正影响）：
///  • 可格挡攻击用 <c>DamageCmd.Attack(...).FromMonster(bk)</c>，读取 <c>Results</c> 的
///    <c>UnblockedDamage</c> 得到“本次实际生命伤害”，用于伤口判定与竖劈治疗。
///  • 竖劈+ 默认用 <c>ValueProp.Move | ValueProp.Unblockable</c> 绕过格挡（合法 HP 结算），
///    携带 <see cref="VerticalSlashProtectionPower"/> 时改为可格挡攻击并按实际生命伤害治疗。
/// </summary>
internal sealed class BlackKnightActions
{
    private readonly BlackKnightEnemy _bk;

    public BlackKnightActions(BlackKnightEnemy bk) => _bk = bk;

    private ICombatState Combat => _bk.CombatState;
    private Creature Self => _bk.Creature;

    private IEnumerable<Player> AlivePlayers() =>
        Combat.Players.Where(p => p.Creature is { IsAlive: true });

    /// <summary>本次攻击的主目标：优先活着的玩家角色，其次任意活着的玩家方实体。</summary>
    private Creature? MainTarget()
    {
        var opponents = Combat.GetOpponentsOf(Self);
        return opponents.FirstOrDefault(c => c.IsAlive && c.IsPlayer)
               ?? opponents.FirstOrDefault(c => c.IsAlive);
    }

    /// <summary>玩家方存活实体数量 N（玩家角色 + 其召唤物，均需存活）。</summary>
    private int PlayerSideCount() => Combat.GetOpponentsOf(Self).Count(c => c.IsAlive);

    // ── 阶段 1：诅咒发放 ────────────────────────────────────────────────
    public async Task CurseCast(IReadOnlyList<Creature> targets)
    {
        _bk.Anim.PlayOneShot(BlackKnightConfig.AnimCurseCast);
        _bk.Vfx.CurseCast();
        _bk.Sfx.Curse();

        foreach (Player player in AlivePlayers())
        {
            for (int i = 0; i < BlackKnightConfig.CurseCardCount; i++)
            {
                var card = Combat.CreateCard<NetherCurse>(player);
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, player, CardPilePosition.Random);
            }
        }
        BlackKnightLog.Info($"诅咒发放：为每名玩家把 {BlackKnightConfig.CurseCardCount} 张幽冥诅咒洗入抽牌堆。");
    }

    // ── 阶段 2/3：斜劈（普通可格挡）──────────────────────────────────────
    public Task DiagonalSlashA(IReadOnlyList<Creature> targets) =>
        Slash(BlackKnightConfig.DiagonalSlashDamage, BlackKnightConfig.AnimDiagonalSlashA, "斜劈A");

    public Task DiagonalSlashB(IReadOnlyList<Creature> targets) =>
        Slash(BlackKnightConfig.DiagonalSlashDamage, BlackKnightConfig.AnimDiagonalSlashB, "斜劈B");

    private async Task Slash(int damage, string anim, string label)
    {
        var target = MainTarget();
        if (target == null) return;
        _bk.Anim.PlayOneShot(anim);
        _bk.Vfx.DiagonalSlash();
        _bk.Sfx.Swing();
        int hp = await BlockableAttack(target, damage);
        BlackKnightLog.Info($"{label}：可格挡伤害 {damage}(+力量)，实际生命伤害 {hp}。");
    }

    // ── 阶段 4：横砍（未完全格挡则洗入 1 张伤口）───────────────────────────
    public Task HorizontalSlash(IReadOnlyList<Creature> targets) => Horizontal("横砍");
    public Task TrueFormHorizontalA(IReadOnlyList<Creature> targets) => Horizontal("真身横砍A");
    public Task TrueFormHorizontalB(IReadOnlyList<Creature> targets) => Horizontal("真身横砍B");

    private async Task Horizontal(string label)
    {
        var target = MainTarget();
        if (target == null) return;
        _bk.Anim.PlayOneShot(BlackKnightConfig.AnimHorizontalSlash);
        _bk.Vfx.HorizontalSlash();
        _bk.Sfx.Swing();

        int hp = await BlockableAttack(target, BlackKnightConfig.HorizontalSlashDamage);
        if (BlackKnightRules.ShouldAddWound(hp))
        {
            var wound = Combat.CreateCard<Wound>(target.Player);
            await CardPileCmd.AddGeneratedCardToCombat(wound, PileType.Draw, target.Player, CardPilePosition.Random);
            BlackKnightLog.Info($"{label}：未完全格挡（实际生命伤害 {hp}），向抽牌堆洗入 1 张伤口。");
        }
        else
        {
            BlackKnightLog.Info($"{label}：完全格挡，不加入伤口。");
        }
    }

    // ── 阶段 5：竖劈+ ───────────────────────────────────────────────────
    public Task VerticalSlash(IReadOnlyList<Creature> targets) => Vertical(exitTrueForm: false);
    public Task TrueFormVertical(IReadOnlyList<Creature> targets) => Vertical(exitTrueForm: true);

    private async Task Vertical(bool exitTrueForm)
    {
        var target = MainTarget();
        _bk.Anim.PlayOneShot(BlackKnightConfig.AnimVerticalSlash);
        _bk.Vfx.VerticalImpact();
        _bk.Sfx.Cleave();

        if (target != null)
        {
            var protection = Self.GetPower<VerticalSlashProtectionPower>();
            if (protection != null)
            {
                // 已被幽冥诅咒转化：可格挡，并按实际生命伤害治疗黑骑士。
                int hp = await BlockableAttack(target, BlackKnightConfig.VerticalSlashDamage, hitSfx: false);
                await PowerCmd.Remove(protection); // 一次性消耗保护
                int heal = BlackKnightRules.VerticalHeal(hp, warded: true);
                if (heal > 0)
                {
                    await CreatureCmd.Heal(Self, heal, true); // Heal 自动不超过最大生命
                    _bk.Vfx.Heal();
                }
                BlackKnightLog.Info($"竖劈+（已保护）：可格挡，实际生命伤害 {hp}，黑骑士恢复 {heal}。");
            }
            else
            {
                // 默认不可格挡：通过合法伤害系统结算（Move => 受力量影响，Unblockable => 绕过格挡）。
                await VersionCompat.CreatureDamage(new ThrowingPlayerChoiceContext(), target,
                    BlackKnightConfig.VerticalSlashDamage, ValueProp.Move | ValueProp.Unblockable, Self, null, null);
                _bk.Vfx.BloodBurst(target, BlackKnightConfig.VerticalSlashDamage);
                BlackKnightLog.Info("竖劈+（不可格挡）：绕过格挡结算，黑骑士不治疗。");
            }
        }

        if (exitTrueForm)
        {
            _bk.InTrueForm = false;
            _bk.Anim.ShowPhantom(false);
            _bk.Anim.PlayIdle(false);
            BlackKnightLog.Info("真身阶段结束：幽影消散，返回诅咒发放循环。");
        }
    }

    // ── 阶段 6：暗鬼铠甲 ─────────────────────────────────────────────────
    public async Task DarkArmor(IReadOnlyList<Creature> targets)
    {
        _bk.Anim.PlayOneShot(BlackKnightConfig.AnimDarkArmor);
        int n = PlayerSideCount();
        _bk.Vfx.DarkArmor(n);
        _bk.Sfx.Armor();

        // 黑骑士获得 50 × N 点格挡。
        int block = BlackKnightRules.DarkArmorBlock(n);
        await CreatureCmd.GainBlock(Self, block, ValueProp.Move, null);

        // 每名存活玩家获得 N 层怗懦（单人即当前玩家；多人对每名玩家分别施加）。
        int cowardice = BlackKnightRules.CowardiceStacks(n);
        var ctx = new ThrowingPlayerChoiceContext();
        foreach (Player player in AlivePlayers())
            await PowerCmd.Apply<CowardicePower>(ctx, player.Creature, cowardice, Self, null);

        BlackKnightLog.Info($"暗鬼铠甲：N={n}，黑骑士 +{block} 格挡，每名玩家 +{cowardice} 层怗懦。");
    }

    // ── 阶段 7：强化——真身 ─────────────────────────────────────────────
    public async Task TrueForm(IReadOnlyList<Creature> targets)
    {
        _bk.Anim.PlayOneShot(BlackKnightConfig.AnimTrueFormEnter);
        _bk.Anim.ShowPhantom(true);
        _bk.Vfx.PhantomReveal();
        _bk.Sfx.TrueForm();
        _bk.InTrueForm = true;

        // 永久获得 5 点力量（允许正常叠加）。
        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(ctx, Self, BlackKnightConfig.TrueFormStrength, Self, null);

        _bk.Anim.PlayIdle(trueForm: true);
        BlackKnightLog.Info($"强化——真身：永久 +{BlackKnightConfig.TrueFormStrength} 力量，显现真身，接下来 横砍→横砍→竖劈+。");
    }

    // ── 共享：可格挡攻击，返回本次实际生命伤害（UnblockedDamage 之和）──────
    //
    // 关键：FromMonster(...) 内部已调用 TargetingAllOpponents(CombatState)，会设置 _combatState。
    // 因此绝不能再链式 .Targeting(target)——否则 AttackCommand 会抛
    // "Already set to target opponents of attacker"，使怪物回合在首次攻击时中断卡死。
    // 单人时 FromMonster 的“所有对手”就是当前玩家，正是我们要打的目标。
    private async Task<int> BlockableAttack(Creature target, int damage, bool hitSfx = true)
    {
        var cmd = await DamageCmd.Attack(damage).FromMonster(_bk).Execute(null);
        int hp = 0;
        foreach (var results in cmd.Results)
            foreach (var r in results)
                hp += r.UnblockedDamage;
        // 勈砍命中：血液粒子从玩家身上迸射（按实际生命伤害强度）。
        if (hp > 0) _bk.Vfx.BloodBurst(target, hp);
        if (hitSfx) _bk.Sfx.Hit();
        return hp;
    }
}
