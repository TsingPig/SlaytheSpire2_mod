using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑骑士动画桥接。由于黑骑士使用 Godot 原生分层 Sprite2D + AnimationPlayer（非 Spine），
/// 游戏内置的 <c>SetAnimationTrigger</c>（只驱动 Spine）不会播放它们，因此这里从 C# 侧驱动：
///
/// 通过 <see cref="NCombatRoom.Instance"/>.GetCreatureNode(creature) 找到战斗中的 <see cref="NCreature"/>，
/// 再在其 <c>Visuals</c>（<see cref="NCreatureVisuals"/> 场景）中递归查找 AnimationPlayer 与真身幽影节点。
///
/// 全部为“尽力而为”：找不到视觉节点时安全降级为 no-op，保证战斗逻辑不受影响、可正常测试。
/// </summary>
internal sealed class BlackKnightAnimationController
{
    private readonly Creature _creature;
    private AnimationPlayer? _anim;
    private Node2D? _motionRoot;
    private Tween? _lungeTween;
    private Vector2 _lungeOrigin;
    private CanvasItem? _phantom;
    private bool _resolved;

    public BlackKnightAnimationController(Creature creature) => _creature = creature;

    private void Resolve()
    {
        if (_resolved && _anim != null && GodotObject.IsInstanceValid(_anim)) return;

        var room = NCombatRoom.Instance;
        if (room == null) return;
        NCreature? node = room.GetCreatureNode(_creature);
        NCreatureVisuals? visuals = node?.Visuals;
        if (visuals == null) return;

        _anim = FindAnimationPlayer(visuals);
        _motionRoot = _anim?.GetParent() as Node2D;
        if (_motionRoot != null)
            _lungeOrigin = _motionRoot.Position;
        _phantom = visuals.FindChild("Phantom", recursive: true, owned: false) as CanvasItem
                   ?? visuals.FindChild("%Phantom", recursive: true, owned: false) as CanvasItem;
        _resolved = _anim != null;
    }

    private static AnimationPlayer? FindAnimationPlayer(Node root)
    {
        if (root is AnimationPlayer ap) return ap;
        foreach (Node child in root.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>播放一次性动画后回到 Idle（找不到动画时安全跳过）。</summary>
    public void PlayOneShot(string animName)
    {
        Resolve();
        if (_anim == null) return;
        ResetLunge();
        try
        {
            if (_anim.HasAnimation(animName))
            {
                _anim.Play(animName);
                if (_anim.HasAnimation(BlackKnightConfig.AnimIdle))
                    _anim.Queue(BlackKnightConfig.AnimIdle);
            }
        }
        catch { /* 视觉节点可能在动画切换瞬间失效，忽略。 */ }
    }

    /// <summary>
    /// 播放斜劈并按目标的实时位置快速贴近。位移驱动整个 Visuals 节点，
    /// 因此身体与真身幽影会一起移动；攻击结束后自动回到出发点。
    /// </summary>
    public void PlayLunge(string animName, Creature target)
        => PlayLunge(
            animName,
            target,
            BlackKnightConfig.DiagonalApproachDelaySeconds,
            BlackKnightConfig.DiagonalApproachSeconds,
            BlackKnightConfig.DiagonalReturnDelaySeconds,
            BlackKnightConfig.DiagonalReturnSeconds,
            BlackKnightConfig.DiagonalTargetStandoff,
            BlackKnightConfig.DiagonalMaxLungeDistance);

    /// <summary>
    /// 播放横劈：先快速贴近目标，在挥刀与命中停帧期间保持位置，
    /// 再以略慢于接近的速度后滑回原位。
    /// </summary>
    public void PlayHorizontalLunge(string animName, Creature target)
        => PlayLunge(
            animName,
            target,
            BlackKnightConfig.HorizontalApproachDelaySeconds,
            BlackKnightConfig.HorizontalApproachSeconds,
            BlackKnightConfig.HorizontalReturnDelaySeconds,
            BlackKnightConfig.HorizontalReturnSeconds,
            BlackKnightConfig.HorizontalTargetStandoff,
            BlackKnightConfig.HorizontalMaxLungeDistance);

    private void PlayLunge(
        string animName,
        Creature target,
        float approachDelaySeconds,
        float approachSeconds,
        float returnDelaySeconds,
        float returnSeconds,
        float targetStandoff,
        float maxLungeDistance)
    {
        PlayOneShot(animName);
        Resolve();
        if (_motionRoot == null || !GodotObject.IsInstanceValid(_motionRoot)) return;

        try
        {
            NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
            if (targetNode == null) return;

            float targetDeltaX = targetNode.GlobalPosition.X - _motionRoot.GlobalPosition.X;
            float direction = Mathf.Sign(targetDeltaX);
            float travel = Mathf.Clamp(
                Mathf.Abs(targetDeltaX) - targetStandoff,
                0f,
                maxLungeDistance);
            if (travel <= 1f) return;

            Vector2 attackPosition = _lungeOrigin + new Vector2(direction * travel, 0f);
            _lungeTween = _motionRoot.CreateTween();
            _lungeTween.TweenInterval(approachDelaySeconds);
            _lungeTween.TweenProperty(
                    _motionRoot,
                    new NodePath("position"),
                    attackPosition,
                    approachSeconds)
                .SetTrans(Tween.TransitionType.Quart)
                .SetEase(Tween.EaseType.Out);
            _lungeTween.TweenInterval(returnDelaySeconds);
            _lungeTween.TweenProperty(
                    _motionRoot,
                    new NodePath("position"),
                    _lungeOrigin,
                    returnSeconds)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
        }
        catch
        {
            ResetLunge();
        }
    }

    private void ResetLunge()
    {
        try
        {
            _lungeTween?.Kill();
            if (_motionRoot != null && GodotObject.IsInstanceValid(_motionRoot))
                _motionRoot.Position = _lungeOrigin;
        }
        catch { }
        finally
        {
            _lungeTween = null;
        }
    }

    /// <summary>切换 Idle（普通 / 真身）。</summary>
    public void PlayIdle(bool trueForm)
    {
        Resolve();
        if (_anim == null) return;
        ResetLunge();
        string name = trueForm && _anim.HasAnimation(BlackKnightConfig.AnimTrueFormIdle)
            ? BlackKnightConfig.AnimTrueFormIdle
            : BlackKnightConfig.AnimIdle;
        try { if (_anim.HasAnimation(name)) _anim.Play(name); } catch { }
    }

    /// <summary>显示 / 隐藏真身幽影。</summary>
    public void ShowPhantom(bool visible)
    {
        Resolve();
        if (_phantom != null && GodotObject.IsInstanceValid(_phantom))
        {
            try { _phantom.Visible = visible; } catch { }
        }
    }
}
