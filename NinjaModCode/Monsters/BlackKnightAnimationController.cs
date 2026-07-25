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

    /// <summary>切换 Idle（普通 / 真身）。</summary>
    public void PlayIdle(bool trueForm)
    {
        Resolve();
        if (_anim == null) return;
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
