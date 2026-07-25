using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using NinjaMod.NinjaModCode.Extensions;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑骑士的程序化音效播放器。加载 <c>NinjaMod/audio/bk_*.wav</c>（由
/// <c>BlackKnight/tools/generate_sfx.py</c> 生成的原创音效）并通过 Godot 的
/// <see cref="AudioStreamPlayer"/> 播放——独立于游戏的 FMOD 音频系统。
///
/// 整体音色低沉、厚重、带“面罩后发声”的闷响：诅咒吟唱 / 挥斧破风 / 重劈砸击 /
/// 铠甲金属声 / 真身咆哮。全部“尽力而为”：找不到战斗节点或资源时安全降级为 no-op。
/// </summary>
internal sealed class BlackKnightSfxController
{
    private static readonly Dictionary<string, AudioStream?> Cache = new();

    public void Swing()    => Play("bk_swing", volumeDb: -3f, pitchJitter: 0.06f);
    public void Hit()      => Play("bk_hit", volumeDb: -1f, pitchJitter: 0.05f);
    public void Cleave()   => Play("bk_cleave", volumeDb: 1f, pitchJitter: 0.03f);
    public void Curse()    => Play("bk_curse", volumeDb: -2f);
    public void TrueForm() => Play("bk_trueform", volumeDb: 0f);
    public void Armor()    => Play("bk_armor", volumeDb: -2f, pitchJitter: 0.05f);
    public void Hurt()     => Play("bk_hurt", volumeDb: -3f, pitchJitter: 0.08f);

    private static AudioStream? Load(string name)
    {
        if (Cache.TryGetValue(name, out var cached)) return cached;
        AudioStream? stream = null;
        try
        {
            // Godot res:// 需要正斜杠；Path.Join 在 Windows 上会产生反斜杠，这里统一替换。
            string path = $"{name}.wav".AudioPath().Replace('\\', '/');
            if (ResourceLoader.Exists(path))
                stream = ResourceLoader.Load<AudioStream>(path);
        }
        catch { /* 资源缺失时静默降级 */ }
        Cache[name] = stream;
        return stream;
    }

    private static void Play(string name, float volumeDb = 0f, float pitchJitter = 0f)
    {
        try
        {
            AudioStream? stream = Load(name);
            if (stream == null) return;

            Node? parent = NCombatRoom.Instance;
            if (parent == null && Engine.GetMainLoop() is SceneTree tree)
                parent = tree.Root;
            if (parent == null || !GodotObject.IsInstanceValid(parent)) return;

            var player = new AudioStreamPlayer
            {
                Stream = stream,
                VolumeDb = volumeDb,
                PitchScale = pitchJitter > 0f
                    ? 1f + (float)GD.RandRange(-pitchJitter, pitchJitter)
                    : 1f,
            };
            parent.AddChild(player);
            player.Finished += player.QueueFree;
            player.Play();
        }
        catch { /* 音频不影响战斗结算，忽略任何异常 */ }
    }
}
