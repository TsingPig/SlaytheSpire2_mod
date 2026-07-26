using System;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using NinjaMod.NinjaModCode.Extensions;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// Owns the Black Knight encounter music for exactly one live combat room.
/// The compressed track is intentionally not marked as looping: when it ends,
/// the controller waits three seconds before starting it again.
/// </summary>
internal static class BlackKnightMusicController
{
    private const string TrackFile = "blackknight_boss_theme.mp3";
    private const float VolumeDb = -6f;
    private const double LoopGapSeconds = 3.0;
    private const double WatchdogSeconds = 0.25;

    private static AudioStreamPlayer? _player;
    private static Godot.Timer? _watchdog;
    private static NCombatRoom? _room;
    private static Creature? _owner;
    private static bool _ownsMusicOverride;
    private static int _generation;

    /// <summary>
    /// True for the whole lifetime of the Black Knight combat room, including
    /// phase changes and the short post-defeat window before combat formally ends.
    /// </summary>
    internal static bool IsActive =>
        _ownsMusicOverride
        && _room != null
        && GodotObject.IsInstanceValid(_room)
        && NCombatRoom.Instance == _room;

    /// <summary>
    /// Starts the encounter track once. Repeated calls from reloads or another
    /// Black Knight in the same room do not create overlapping players.
    /// </summary>
    public static void Start(Creature owner)
    {
        try
        {
            NCombatRoom? room = NCombatRoom.Instance;
            if (room == null || !GodotObject.IsInstanceValid(room))
                return;

            if (_room == room
                && _player != null
                && GodotObject.IsInstanceValid(_player))
            {
                bool isNewEncounterOwner = !ReferenceEquals(_owner, owner);
                _owner = owner;
                StopNativeMusic();
                if (isNewEncounterOwner && CombatManager.Instance.IsInProgress)
                {
                    // An in-place retry can reuse the room node. A new monster
                    // instance means a new encounter, so restart from the top.
                    _generation++;
                    _player.Stop();
                    _player.Play();
                }
                else if (!_player.Playing && CombatManager.Instance.IsInProgress)
                {
                    // Covers an in-place combat restart that reuses the room
                    // node before the watchdog has observed the old combat end.
                    _generation++;
                    _player.Play();
                }
                return;
            }

            StopInternal(restoreGameMusic: false);

            string path = TrackFile.AudioPath().Replace('\\', '/');
            if (!ResourceLoader.Exists(path))
            {
                BlackKnightLog.Warn($"黑骑士 BGM 资源不存在：{path}");
                return;
            }

            AudioStream? stream = ResourceLoader.Load<AudioStream>(path);
            if (stream == null)
            {
                BlackKnightLog.Warn($"黑骑士 BGM 加载失败：{path}");
                return;
            }

            // The custom stream is played by Godot rather than FMOD. Stop only
            // the native music event; ambience and ordinary combat SFX remain.
            StopNativeMusic();
            _ownsMusicOverride = true;

            _room = room;
            _owner = owner;
            int generation = ++_generation;

            var player = new AudioStreamPlayer
            {
                Name = "BlackKnightBossMusic",
                Stream = stream,
                VolumeDb = VolumeDb,
            };
            int bgmBus = AudioServer.GetBusIndex("BGM");
            if (bgmBus >= 0)
                player.Bus = "BGM";

            var watchdog = new Godot.Timer
            {
                Name = "BlackKnightMusicWatchdog",
                WaitTime = WatchdogSeconds,
                OneShot = false,
                Autostart = true,
            };

            _player = player;
            _watchdog = watchdog;
            room.AddChild(player);
            room.AddChild(watchdog);

            player.Finished += OnTrackFinished;
            watchdog.Timeout += OnWatchdog;
            room.TreeExiting += OnRoomTreeExiting;

            player.Play();
            BlackKnightLog.Info(
                $"黑骑士 BGM 已开始：{TrackFile}，音量 {VolumeDb} dB，循环间隔 {LoopGapSeconds:0.#} 秒，实例 {generation}。");
        }
        catch (Exception e)
        {
            BlackKnightLog.Warn($"黑骑士 BGM 启动失败，已安全回退到游戏音乐：{e}");
            StopInternal(restoreGameMusic: true);
        }
    }

    private static void OnTrackFinished()
    {
        AudioStreamPlayer? player = _player;
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;

        int generation = _generation;
        SceneTree? tree = player.GetTree();
        if (tree == null)
            return;

        SceneTreeTimer delay = tree.CreateTimer(LoopGapSeconds);
        delay.Timeout += () =>
        {
            if (generation != _generation || !ShouldPlay())
                return;

            AudioStreamPlayer? current = _player;
            if (current != null && GodotObject.IsInstanceValid(current))
                current.Play();
        };
    }

    private static void OnWatchdog()
    {
        try
        {
            if (!CombatManager.Instance.IsInProgress
                || _room == null
                || NCombatRoom.Instance != _room)
            {
                StopInternal(restoreGameMusic: true);
                return;
            }

            // A room reload, multiplayer resync, or phase transition may ask
            // the run music controller to start the default track again.
            // Keep the native FMOD music event silent for this encounter only.
            StopNativeMusic();

            // Stop the custom track as soon as the Black Knight encounter is
            // defeated, while retaining the watchdog until combat officially
            // finishes so the native post-combat music can be restored once.
            if (!HasLivingBlackKnight())
            {
                if (_player != null
                    && GodotObject.IsInstanceValid(_player)
                    && _player.Playing)
                {
                    _player.Stop();
                }
            }
        }
        catch (Exception e)
        {
            BlackKnightLog.Warn($"黑骑士 BGM 监控异常，已停止播放器：{e}");
            StopInternal(restoreGameMusic: true);
        }
    }

    private static bool ShouldPlay() =>
        CombatManager.Instance.IsInProgress
        && _room != null
        && NCombatRoom.Instance == _room
        && HasLivingBlackKnight();

    private static bool HasLivingBlackKnight()
    {
        try
        {
            return _owner?.CombatState?.Enemies.Any(
                creature => creature.IsAlive && creature.Monster is BlackKnightEnemy) == true;
        }
        catch
        {
            return false;
        }
    }

    private static void OnRoomTreeExiting() =>
        StopInternal(restoreGameMusic: true);

    /// <summary>
    /// The run soundtrack is played through NRunMusicController's own FMOD
    /// proxy, not NAudioManager's proxy. Stop both paths so an already-running
    /// combat track is silenced as well as any direct music event.
    /// This intentionally does not call NRunMusicController.StopMusic(),
    /// because that method also stops ambience and unloads the act music bank.
    /// </summary>
    private static void StopNativeMusic()
    {
        try
        {
            NRunMusicController? controller = NRunMusicController.Instance;
            if (controller != null && GodotObject.IsInstanceValid(controller))
            {
                Node? proxy = controller.GetNodeOrNull<Node>("Proxy");
                proxy?.Call("stop_music");
            }
        }
        catch (Exception e)
        {
            BlackKnightLog.Warn($"停止运行音乐 Proxy 失败：{e.Message}");
        }

        // Some screens and third-party encounters start music through the
        // global audio manager instead of the run controller.
        NAudioManager.Instance?.StopMusic();
    }

    private static void StopInternal(bool restoreGameMusic)
    {
        bool shouldRestore = restoreGameMusic && _ownsMusicOverride;
        _generation++;

        try
        {
            if (_player != null && GodotObject.IsInstanceValid(_player))
            {
                _player.Finished -= OnTrackFinished;
                _player.Stop();
                if (!_player.IsQueuedForDeletion())
                    _player.QueueFree();
            }

            if (_watchdog != null && GodotObject.IsInstanceValid(_watchdog))
            {
                _watchdog.Timeout -= OnWatchdog;
                _watchdog.Stop();
                if (!_watchdog.IsQueuedForDeletion())
                    _watchdog.QueueFree();
            }

            if (_room != null && GodotObject.IsInstanceValid(_room))
                _room.TreeExiting -= OnRoomTreeExiting;
        }
        catch
        {
            // Cleanup is best-effort; static references are cleared below even
            // if Godot is already tearing down the combat scene.
        }
        finally
        {
            _player = null;
            _watchdog = null;
            _room = null;
            _owner = null;
            _ownsMusicOverride = false;
        }

        if (!shouldRestore)
            return;

        try
        {
            // StopCustomMusic restarts the run controller's current FMOD track
            // at its combat-end section. It is safe even though our custom MP3
            // was played by Godot rather than by FMOD.
            NRunMusicController.Instance?.StopCustomMusic();
        }
        catch (Exception e)
        {
            BlackKnightLog.Warn($"恢复游戏 BGM 失败：{e}");
        }
    }
}
