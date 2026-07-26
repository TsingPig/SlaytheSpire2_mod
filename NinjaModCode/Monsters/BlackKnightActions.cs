using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using NinjaMod.NinjaModCode.Cards;
using NinjaMod.NinjaModCode.Compatibility;
using NinjaMod.NinjaModCode.Powers;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑暗骑士的战斗动作。所有改变战斗状态的操作都通过游戏命令执行，
/// 因而由同一条联机战斗动作链同步，客户端不会各自重复结算。
/// </summary>
internal sealed class BlackKnightActions
{
    private readonly BlackKnightEnemy _bk;

    public BlackKnightActions(BlackKnightEnemy bk) => _bk = bk;

    private ICombatState Combat => _bk.CombatState;
    private Creature Self => _bk.Creature;

    private IEnumerable<Player> AlivePlayers() =>
        Combat.Players.Where(player => player.Creature is { IsAlive: true });

    private Creature? MainTarget()
    {
        var opponents = Combat.GetOpponentsOf(Self);
        return opponents.FirstOrDefault(creature => creature.IsAlive && creature.IsPlayer)
               ?? opponents.FirstOrDefault(creature => creature.IsAlive);
    }

    private int PlayerSideCount() => Combat.GetOpponentsOf(Self).Count(creature => creature.IsAlive);

    public async Task CurseCast(IReadOnlyList<Creature> targets)
    {
        _bk.Anim.PlayOneShot(BlackKnightConfig.AnimCurseCast);
        _bk.Vfx.CurseCast();
        _bk.Sfx.Curse();

        await ApplyCursePackage(
            BlackKnightConfig.CurseCardCount,
            "诅咒发放");
    }

    /// <summary>
    /// 首回合与真身强化共同复用的完整诅咒效果包：
    /// 向每名存活玩家的抽牌堆随机洗入指定数量的幽冥诅咒，
    /// 再按实际成功加入的总数获得噬命诅印。
    /// </summary>
    private async Task ApplyCursePackage(int cardsPerPlayer, string sourceLabel)
    {
        int successfulAdds = 0;
        bool showedLocalPreview = false;

        foreach (Player player in AlivePlayers())
        {
            var results = new List<CardPileAddResult>(cardsPerPlayer);
            for (int i = 0; i < cardsPerPlayer; i++)
            {
                var card = Combat.CreateCard<NetherCurse>(player);
                CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(
                    card,
                    PileType.Draw,
                    player,
                    CardPilePosition.Random);
                results.Add(result);
                if (result.success)
                    successfulAdds++;
            }

            // 每轮发牌都复用原版“卡牌预览并飞入牌堆”的表现。
            // PreviewCardPileAdd 内部只会在该玩家的本地客户端显示。
            if (results.Any(result => result.success) && LocalContext.IsMe(player))
            {
                CardCmd.PreviewCardPileAdd(results, 1.2f, CardPreviewStyle.HorizontalLayout);
                showedLocalPreview = true;
            }
        }

        if (showedLocalPreview)
            await Cmd.Wait(1f);

        int sigilStacks = BlackKnightRules.LifeSiphonSigilsFromCurseAdds(successfulAdds);
        if (sigilStacks > 0)
        {
            await PowerCmd.Apply<LifeSiphonSigilPower>(
                new ThrowingPlayerChoiceContext(),
                Self,
                sigilStacks,
                Self,
                null);
        }

        BlackKnightLog.Info(
            $"{sourceLabel}：实际加入 {successfulAdds} 张幽冥诅咒，获得 {sigilStacks} 层噬命诅印。");
    }

    public Task DiagonalSlashA(IReadOnlyList<Creature> targets) =>
        Slash(
            BlackKnightConfig.DiagonalSlashDamage,
            BlackKnightConfig.AnimDiagonalSlashA,
            "斜劈A",
            BlackKnightConfig.DiagonalWindupSeconds,
            0.34f);

    public Task DiagonalSlashB(IReadOnlyList<Creature> targets) =>
        Slash(
            BlackKnightConfig.DiagonalSlashDamage,
            BlackKnightConfig.AnimDiagonalSlashB,
            "斜劈B",
            BlackKnightConfig.DiagonalBWindupSeconds,
            0.28f);

    private async Task Slash(
        int damage,
        string animation,
        string label,
        float hitDelaySeconds,
        float recoverySeconds)
    {
        Creature? target = MainTarget();
        if (target == null)
            return;

        _bk.Anim.PlayLunge(animation, target);
        _bk.Sfx.Swing();
        await Cmd.Wait(hitDelaySeconds);
        _bk.Vfx.DiagonalSlash();
        int hpDamage = await BlockableAttack(target, damage);
        await Cmd.Wait(recoverySeconds);
        BlackKnightLog.Info($"{label}：实际生命伤害 {hpDamage}。");
    }

    public Task HorizontalSlash(IReadOnlyList<Creature> targets) => Horizontal("横劈");
    public Task TrueFormHorizontalA(IReadOnlyList<Creature> targets) => Horizontal("真身横劈A");
    public Task TrueFormHorizontalB(IReadOnlyList<Creature> targets) => Horizontal("真身横劈B");

    private async Task Horizontal(string label)
    {
        Creature? target = MainTarget();
        if (target == null)
            return;

        _bk.Anim.PlayHorizontalLunge(BlackKnightConfig.AnimHorizontalSlash, target);
        await Cmd.Wait(BlackKnightConfig.HorizontalWindupSeconds);
        _bk.Sfx.Swing();
        _bk.Vfx.HorizontalSlash(target);

        int hpDamage = await BlockableAttack(target, BlackKnightConfig.HorizontalSlashDamage);
        _bk.Vfx.HorizontalAftershock(target);
        if (BlackKnightRules.ShouldAddWound(hpDamage))
        {
            Player? owner = target.Player ?? target.PetOwner;
            if (owner == null)
                return;

            var wound = Combat.CreateCard<Wound>(owner);
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(
                wound,
                PileType.Draw,
                owner,
                CardPilePosition.Random);

            // 复用原版“卡牌预览并飞入牌堆”的表现；只在卡牌确实成功加入、
            // 且目标是本地玩家时播放，避免联机客户端重复显示。
            if (result.success)
            {
                if (LocalContext.IsMe(owner))
                {
                    CardCmd.PreviewCardPileAdd(
                        new List<CardPileAddResult> { result },
                        0.9f,
                        CardPreviewStyle.HorizontalLayout);
                    await Cmd.Wait(0.65f);
                }

                BlackKnightLog.Info($"{label}：造成 {hpDamage} 点实际生命伤害，加入 1 张伤口。");
            }
            else
            {
                BlackKnightLog.Info($"{label}：造成 {hpDamage} 点实际生命伤害，但伤口未能加入抽牌堆。");
            }
        }

        await Cmd.Wait(BlackKnightConfig.HorizontalRecoverySeconds);
    }

    public Task VerticalSlash(IReadOnlyList<Creature> targets) => Vertical(exitTrueForm: false);
    public Task TrueFormVertical(IReadOnlyList<Creature> targets) => Vertical(exitTrueForm: true);

    private async Task Vertical(bool exitTrueForm)
    {
        _bk.Anim.PlayOneShot(BlackKnightConfig.AnimVerticalSlash);
        _bk.Vfx.VerticalImpact();
        _bk.Sfx.Cleave();

        List<Creature> playerTargets = AlivePlayers()
            .Select(player => player.Creature)
            .ToList();
        Dictionary<Creature, int> hpBefore = SnapshotHp(playerTargets);
        var context = new ThrowingPlayerChoiceContext();

        // 一次竖劈可能命中多名玩家，但仍属于一次攻击行为。
        // 每名玩家分别读取自己回合结束时保存的标记。
        foreach (Creature target in playerTargets)
        {
            var exposure = target.GetPower<VerticalSlashExposurePower>();
            ValueProp props = ValueProp.Move;
            if (exposure != null)
                props |= ValueProp.Unblockable;

            await VersionCompat.CreatureDamage(
                context,
                target,
                BlackKnightConfig.VerticalSlashDamage,
                props,
                Self,
                null,
                null);

            if (exposure != null)
                await PowerCmd.Remove(exposure);
        }

        int hpDamage = TotalHpLost(hpBefore);
        PlayBloodVfx(hpBefore);
        await ResolveLifeSiphon(hpDamage);
        _bk.Sfx.Hit();

        BlackKnightLog.Info($"竖劈：对所有玩家共造成 {hpDamage} 点实际生命伤害。");

        if (exitTrueForm)
        {
            _bk.InTrueForm = false;
            _bk.Anim.ShowPhantom(false);
            _bk.Anim.PlayIdle(false);
        }
    }

    public async Task DarkArmor(IReadOnlyList<Creature> targets)
    {
        _bk.Anim.PlayOneShot(BlackKnightConfig.AnimDarkArmor);
        int livingPlayerSideEntities = PlayerSideCount();
        _bk.Vfx.DarkArmor(livingPlayerSideEntities);
        _bk.Sfx.Armor();

        int block = BlackKnightRules.DarkArmorBlock(livingPlayerSideEntities);
        await CreatureCmd.GainBlock(Self, block, ValueProp.Move, null);

        BlackKnightLog.Info($"暗魂铠甲：获得 {block} 点格挡。");
    }

    public async Task TrueForm(IReadOnlyList<Creature> targets)
    {
        _bk.Anim.PlayOneShot(BlackKnightConfig.AnimTrueFormEnter);
        _bk.Anim.ShowPhantom(true);
        _bk.Vfx.PhantomReveal();
        _bk.Sfx.TrueForm();
        _bk.InTrueForm = true;

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Self,
            BlackKnightConfig.TrueFormStrength,
            Self,
            null);

        // 真身显现后，黑暗骑士给自己施加幽冥侵蚀。
        // 该 Boss Buff 内部按玩家分别记录“每回合首次抽到”的状态。
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<NetherErosionPower>(
            context,
            Self,
            1,
            Self,
            null);

        // 增量迁移：BK_TRUE_FORM 的稳定状态 ID 与后继保持不变，
        // 但本回合完整重施首回合的诅咒效果包。
        _bk.Vfx.CurseCast();
        _bk.Sfx.Curse();
        await ApplyCursePackage(
            BlackKnightConfig.TrueFormCurseCardCount,
            "强化——真身追加诅咒");

        _bk.Anim.PlayIdle(trueForm: true);
    }

    private async Task<int> BlockableAttack(Creature primaryTarget, int damage, bool hitSfx = true)
    {
        List<Creature> playerTargets = AlivePlayers()
            .Select(player => player.Creature)
            .ToList();
        Dictionary<Creature, int> hpBefore = SnapshotHp(playerTargets);

        await DamageCmd.Attack(damage).FromMonster(_bk).Execute(null);

        int hpDamage = TotalHpLost(hpBefore);
        PlayBloodVfx(hpBefore);
        await ResolveLifeSiphon(hpDamage);
        if (hitSfx)
            _bk.Sfx.Hit();
        return hpDamage;
    }

    private static Dictionary<Creature, int> SnapshotHp(IEnumerable<Creature> targets) =>
        targets.ToDictionary(creature => creature, creature => creature.CurrentHp);

    private static int TotalHpLost(IReadOnlyDictionary<Creature, int> hpBefore) =>
        hpBefore.Sum(pair => System.Math.Max(0, pair.Value - pair.Key.CurrentHp));

    private void PlayBloodVfx(IReadOnlyDictionary<Creature, int> hpBefore)
    {
        foreach ((Creature creature, int before) in hpBefore)
        {
            int hpLost = System.Math.Max(0, before - creature.CurrentHp);
            if (hpLost > 0)
                _bk.Vfx.BloodBurst(creature, hpLost);
        }
    }

    private async Task ResolveLifeSiphon(int actualHpDamage)
    {
        var sigil = Self.GetPower<LifeSiphonSigilPower>();
        if (sigil == null || sigil.Amount <= 0)
            return;

        sigil.Flash();
        int heal = BlackKnightRules.LifeSiphonHeal(actualHpDamage, sigil.Amount);
        if (heal > 0)
        {
            await CreatureCmd.Heal(Self, heal, true);
            _bk.Vfx.Heal();
        }

        // 无论本次攻击是否被完全格挡，只要攻击结算时有层数，就消耗一层。
        await PowerCmd.Decrement(sigil);
        BlackKnightLog.Info($"噬命诅印：回复 {heal} 点生命并消耗 1 层。");
    }
}
