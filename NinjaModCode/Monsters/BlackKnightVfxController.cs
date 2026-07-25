using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑骑士的程序化特效控制器：在关键时刻（诅咒施法、护甲凝聚、竖劈冲击、治疗吸收、真身显现）
/// 于黑骑士位置生成配色统一的粒子迸发，并在竖劈重击时触发屏幕震动。
///
/// 全部“尽力而为”：找不到战斗视觉容器或震动节点时安全降级为 no-op，不影响战斗逻辑与测试。
/// 粒子颜色与参数集中在 <see cref="BlackKnightConfig"/>，便于统一调整。
/// </summary>
internal sealed class BlackKnightVfxController
{
    private readonly Creature _creature;

    public BlackKnightVfxController(Creature creature) => _creature = creature;

    // ── 对外的语义化特效入口 ────────────────────────────────────────────

    public void CurseCast()
        => Burst(BlackKnightConfig.ColorPurple, BlackKnightConfig.ColorSoulGreen, amount: 48, speed: 180f, spread: 180f, gravity: -40f);

    public void DiagonalSlash()
        => Burst(BlackKnightConfig.ColorSilver, BlackKnightConfig.ColorPurple, amount: 24, speed: 260f, spread: 35f, gravity: 0f);

    public void HorizontalSlash()
        => Burst(BlackKnightConfig.ColorSilver, BlackKnightConfig.ColorDarkRed, amount: 30, speed: 300f, spread: 20f, gravity: 0f);

    public void VerticalImpact()
    {
        Burst(BlackKnightConfig.ColorDarkRed, BlackKnightConfig.ColorSoulGreen, amount: 60, speed: 340f, spread: 180f, gravity: 500f, yOffset: 40f);
        Shake(ShakeStrength.Strong, ShakeDuration.Short);
    }

    public void DarkArmor(int n)
        => Burst(BlackKnightConfig.ColorSilver, BlackKnightConfig.ColorPurple, amount: 24 + 12 * Math.Max(1, n), speed: 120f, spread: 180f, gravity: -120f);

    public void Heal()
        => Burst(BlackKnightConfig.ColorSoulGreen, BlackKnightConfig.ColorSilver, amount: 36, speed: 90f, spread: 180f, gravity: -160f);

    public void PhantomReveal()
    {
        Burst(BlackKnightConfig.ColorPurple, BlackKnightConfig.ColorBlack, amount: 54, speed: 60f, spread: 180f, gravity: -30f);
        Shake(ShakeStrength.Medium, ShakeDuration.Short);
    }

    /// <summary>
    /// 劈砍命中：血液粒子从<b>玩家（<paramref name="target"/>）身上</b>向外迸射，
    /// 强度随本次实际生命伤害 <paramref name="hp"/> 提升，并附带命中闪光与屏幕震动。
    /// </summary>
    public void BloodBurst(Creature target, int hp)
    {
        var pos = CreaturePos(target);
        if (pos == null) return;

        int intensity = Math.Clamp(hp, 0, 60);
        int amount = 26 + intensity * 2;                 // 血滴数量随伤害增加
        float speed = 260f + intensity * 6f;

        var bloodDark = new Color("6e0f1a");
        var bloodBright = new Color("c22a3a");
        // 主血雾：向四周迸射并受重力下坠。
        BurstAt(pos.Value, bloodBright, bloodDark, amount, speed, spread: 150f, gravity: 700f,
            lifetime: 0.9f, scaleMin: 2.5f, scaleMax: 6.5f);
        // 命中白闪：短促的亮色核心。
        BurstAt(pos.Value, new Color("ffd9d9"), bloodBright, amount: 14, speed: speed * 1.3f,
            spread: 180f, gravity: 120f, lifetime: 0.28f, scaleMin: 3f, scaleMax: 7f);

        // 打击感：伤害越高震动越强。
        var strength = intensity >= 30 ? ShakeStrength.Strong
            : intensity >= 12 ? ShakeStrength.Medium : ShakeStrength.Weak;
        Shake(strength, ShakeDuration.Short);
    }

    // ── 实现 ────────────────────────────────────────────────────────────

    /// <summary>解析某个战斗实体的特效生成位置（优先 VfxSpawnPosition，其次节点中心）。</summary>
    private static Vector2? CreaturePos(Creature creature)
    {
        try
        {
            var room = NCombatRoom.Instance;
            NCreature? node = room?.GetCreatureNode(creature);
            var spawn = node?.Visuals?.VfxSpawnPosition;
            if (spawn != null) return spawn.GlobalPosition;
            if (node != null && GodotObject.IsInstanceValid(node)) return node.GlobalPosition;
        }
        catch { }
        return null;
    }

    private void Burst(Color colorA, Color colorB, int amount, float speed, float spread, float gravity, float yOffset = 0f)
    {
        var self = CreaturePos(_creature);
        if (self == null) return;
        BurstAt(self.Value + new Vector2(0f, yOffset), colorA, colorB, amount, speed, spread, gravity);
    }

    private void BurstAt(Vector2 pos, Color colorA, Color colorB, int amount, float speed, float spread, float gravity,
        float lifetime = 0.8f, float scaleMin = 2.0f, float scaleMax = 5.0f)
    {
        try
        {
            var room = NCombatRoom.Instance;
            Control? container = room?.CombatVfxContainer;
            if (container == null) return;

            var p = new CpuParticles2D
            {
                Emitting = false,
                OneShot = true,
                Explosiveness = 0.85f,
                Amount = Math.Max(1, amount),
                Lifetime = lifetime,
                Direction = new Vector2(0f, -1f),
                Spread = spread,
                Gravity = new Vector2(0f, gravity),
                InitialVelocityMin = speed * 0.5f,
                InitialVelocityMax = speed,
                ScaleAmountMin = scaleMin,
                ScaleAmountMax = scaleMax,
                Color = colorA,
            };

            // 用颜色渐变（A→B→透明）表现幽魂/护甲/冲击/血液的能量流动。
            var ramp = new Gradient();
            ramp.SetColor(0, colorA);
            ramp.AddPoint(0.5f, colorB);
            var end = colorB; end.A = 0f;
            ramp.SetColor(ramp.GetPointCount() - 1, end);
            p.ColorRamp = ramp;

            container.AddChild(p);
            p.GlobalPosition = pos;
            p.Finished += p.QueueFree;
            p.Emitting = true;
        }
        catch { /* 特效不影响战斗结算，忽略任何视觉侧异常。 */ }
    }

    private void Shake(ShakeStrength strength, ShakeDuration duration)
    {
        try
        {
            var room = NCombatRoom.Instance;
            if (room == null) return;
            var shaker = FindShaker(room.GetTree()?.Root);
            shaker?.Shake(strength, duration, BlackKnightConfig.ScreenShakeStrength);
        }
        catch { }
    }

    private static NScreenShake? FindShaker(Node? root)
    {
        if (root == null) return null;
        if (root is NScreenShake s) return s;
        foreach (Node child in root.GetChildren())
        {
            var found = FindShaker(child);
            if (found != null) return found;
        }
        return null;
    }
}
