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
    private static GradientTexture2D? _streakTexture;

    public BlackKnightVfxController(Creature creature) => _creature = creature;

    // ── 对外的语义化特效入口 ────────────────────────────────────────────

    public void CurseCast()
        => Burst(BlackKnightConfig.ColorPurple, BlackKnightConfig.ColorSoulGreen, amount: 48, speed: 180f, spread: 180f, gravity: -40f);

    public void DiagonalSlash()
        => Burst(BlackKnightConfig.ColorSoulGreen, BlackKnightConfig.ColorPurple, amount: 34, speed: 290f, spread: 42f, gravity: 20f);

    /// <summary>
    /// 横劈命中分为四层：贯穿战场的三重刀光、高速水平刀锋碎片、
    /// 反向剥落的暗影甲片，以及短促的放射火星。
    /// 三层都以目标为中心，方向由黑骑士指向目标，因此多人/不同站位下仍会朝正确方向喷射。
    /// </summary>
    public void HorizontalSlash(Creature target)
    {
        Vector2? selfPos = CreaturePos(_creature);
        Vector2? targetPos = CreaturePos(target);
        if (targetPos == null)
        {
            Burst(BlackKnightConfig.ColorSilver, BlackKnightConfig.ColorDarkRed,
                amount: 68, speed: 460f, spread: 24f, gravity: 0f);
            Shake(ShakeStrength.Strong, ShakeDuration.Short, BlackKnightConfig.HorizontalScreenShakeStrength);
            return;
        }

        Vector2 direction = selfPos.HasValue
            ? targetPos.Value - selfPos.Value
            : Vector2.Left;
        if (direction.LengthSquared() < 0.001f)
            direction = Vector2.Left;
        else
            direction = direction.Normalized();

        // 不是围绕角色的小半圆，而是一道略微倾斜、贯穿玩家区域和屏幕边缘的三层刀光。
        SpawnBladeWave(targetPos.Value, direction);

        // Razor layer: very fast, narrow and bright, extending the perceived blade.
        BurstAt(
            targetPos.Value,
            new Color("fff4ff"),
            BlackKnightConfig.ColorPurple,
            amount: 132,
            speed: 940f,
            spread: 9f,
            gravity: 0f,
            lifetime: 0.42f,
            scaleMin: 0.65f,
            scaleMax: 1.65f,
            direction: direction,
            texture: StreakTexture(),
            alignToVelocity: true,
            additive: true,
            damping: 240f);

        // Shadow layer: heavier fragments peel backward from the cut and linger.
        BurstAt(
            targetPos.Value,
            BlackKnightConfig.ColorDarkRed,
            BlackKnightConfig.ColorBlack,
            amount: 104,
            speed: 520f,
            spread: 54f,
            gravity: 130f,
            lifetime: 0.9f,
            scaleMin: 4.5f,
            scaleMax: 14f,
            direction: -direction,
            damping: 110f);

        // Spark layer: a compact radial flash sells the instant of contact.
        BurstAt(
            targetPos.Value,
            BlackKnightConfig.ColorSilver,
            new Color("d42f72"),
            amount: 78,
            speed: 460f,
            spread: 180f,
            gravity: 220f,
            lifetime: 0.5f,
            scaleMin: 2.5f,
            scaleMax: 8.5f,
            direction: direction,
            additive: true);

        Shake(ShakeStrength.Strong, ShakeDuration.Short, BlackKnightConfig.HorizontalScreenShakeStrength);
    }

    /// <summary>
    /// 伤害结算后的二次冲击：补一层紫红碎光、滞后的黑色甲片和较弱的余震，
    /// 让横劈不是“一闪即逝”，同时与实际命中/血液粒子形成前后层次。
    /// </summary>
    public void HorizontalAftershock(Creature target)
    {
        Vector2? pos = CreaturePos(target);
        Vector2? selfPos = CreaturePos(_creature);
        if (pos == null)
            return;

        Vector2 direction = selfPos.HasValue ? pos.Value - selfPos.Value : Vector2.Left;
        direction = direction.LengthSquared() < 0.001f ? Vector2.Left : direction.Normalized();

        BurstAt(
            pos.Value,
            new Color("f4b4ff"),
            new Color("8f1f62"),
            amount: 86,
            speed: 610f,
            spread: 24f,
            gravity: 40f,
            lifetime: 0.58f,
            scaleMin: 0.45f,
            scaleMax: 1.2f,
            direction: -direction,
            texture: StreakTexture(),
            alignToVelocity: true,
            additive: true,
            damping: 300f);

        BurstAt(
            pos.Value,
            new Color("3a163f"),
            BlackKnightConfig.ColorBlack,
            amount: 54,
            speed: 330f,
            spread: 140f,
            gravity: 280f,
            lifetime: 1.05f,
            scaleMin: 5f,
            scaleMax: 15f,
            direction: direction,
            damping: 85f);

        Shake(
            ShakeStrength.Medium,
            ShakeDuration.Short,
            BlackKnightConfig.HorizontalAftershockShakeStrength);
    }

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

    private void BurstAt(
        Vector2 pos,
        Color colorA,
        Color colorB,
        int amount,
        float speed,
        float spread,
        float gravity,
        float lifetime = 0.8f,
        float scaleMin = 2.0f,
        float scaleMax = 5.0f,
        Vector2? direction = null,
        Texture2D? texture = null,
        bool alignToVelocity = false,
        bool additive = false,
        float damping = 0f)
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
                Direction = direction ?? new Vector2(0f, -1f),
                Spread = spread,
                Gravity = new Vector2(0f, gravity),
                InitialVelocityMin = speed * 0.5f,
                InitialVelocityMax = speed,
                ScaleAmountMin = scaleMin,
                ScaleAmountMax = scaleMax,
                Color = colorA,
                Texture = texture,
                ParticleFlagAlignY = alignToVelocity,
                DampingMin = damping * 0.7f,
                DampingMax = damping,
            };

            if (additive)
            {
                p.Material = new CanvasItemMaterial
                {
                    BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
                };
            }

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

    /// <summary>
    /// 生成长条软边贴图。粒子的 Y 轴会随速度旋转，因此看起来是刀锋碎线而不是默认方块。
    /// </summary>
    private static GradientTexture2D StreakTexture()
    {
        if (_streakTexture != null)
            return _streakTexture;

        var gradient = new Gradient();
        gradient.SetColor(0, Colors.White);
        gradient.SetColor(gradient.GetPointCount() - 1, new Color(1f, 1f, 1f, 0f));

        _streakTexture = new GradientTexture2D
        {
            Gradient = gradient,
            Width = 10,
            Height = 72,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1f, 0.5f),
            Repeat = GradientTexture2D.RepeatEnum.Mirror,
        };
        return _streakTexture;
    }

    private static void SpawnBladeWave(Vector2 targetPos, Vector2 direction)
    {
        try
        {
            Control? container = NCombatRoom.Instance?.CombatVfxContainer;
            if (container == null)
                return;

            float tilt = Mathf.DegToRad(BlackKnightConfig.HorizontalBladeWaveTiltDegrees);
            Vector2 bladeDirection = direction.Rotated(direction.X >= 0f ? -tilt : tilt).Normalized();
            float halfLength = BlackKnightConfig.HorizontalBladeWaveLength * 0.5f;
            Vector2 start = -bladeDirection * halfLength;
            Vector2 end = bladeDirection * halfLength;

            // 外层暗红冲击带、中层紫色能量、内层白色刀芯。
            SpawnBladeLine(container, targetPos, start, end, 176f,
                new Color(0.20f, 0.01f, 0.08f, 0.72f), 0.34f, additive: false);
            SpawnBladeLine(container, targetPos, start, end, 92f,
                new Color(0.56f, 0.12f, 0.72f, 0.88f), 0.25f, additive: true);
            SpawnBladeLine(container, targetPos, start, end, 19f,
                new Color(1.55f, 1.35f, 1.7f, 1f), 0.16f, additive: true);

            // 平行的破碎副刃令刀光更粗、更有层次，而不是一根单薄直线。
            Vector2 normal = new(-bladeDirection.Y, bladeDirection.X);
            SpawnBladeLine(container, targetPos + normal * 48f, start * 0.86f, end * 0.92f, 24f,
                new Color(0.92f, 0.28f, 0.62f, 0.72f), 0.22f, additive: true);
            SpawnBladeLine(container, targetPos - normal * 56f, start * 0.72f, end * 0.82f, 15f,
                new Color(0.38f, 0.10f, 0.46f, 0.68f), 0.29f, additive: true);
        }
        catch { }
    }

    private static void SpawnBladeLine(
        Control container,
        Vector2 globalPosition,
        Vector2 start,
        Vector2 end,
        float width,
        Color color,
        float lifetime,
        bool additive)
    {
        var line = new Line2D
        {
            Width = width,
            DefaultColor = color,
            Antialiased = true,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
            ZIndex = 90,
        };
        line.AddPoint(start);
        line.AddPoint(end);

        var ramp = new Gradient();
        var transparent = color;
        transparent.A = 0f;
        ramp.SetColor(0, transparent);
        ramp.AddPoint(0.18f, color);
        ramp.AddPoint(0.82f, color);
        ramp.SetColor(ramp.GetPointCount() - 1, transparent);
        line.Gradient = ramp;

        if (additive)
        {
            line.Material = new CanvasItemMaterial
            {
                BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            };
        }

        container.AddChild(line);
        line.GlobalPosition = globalPosition;

        var fade = line.CreateTween();
        fade.TweenProperty(line, "modulate:a", 0f, lifetime)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.In);
        fade.Finished += line.QueueFree;
    }

    private void Shake(ShakeStrength strength, ShakeDuration duration, float? customStrength = null)
    {
        try
        {
            var room = NCombatRoom.Instance;
            if (room == null) return;
            var shaker = FindShaker(room.GetTree()?.Root);
            shaker?.Shake(strength, duration, customStrength ?? BlackKnightConfig.ScreenShakeStrength);
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
