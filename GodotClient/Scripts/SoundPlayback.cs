using System.Collections.Generic;
using System.IO;
using Godot;
using Library;

namespace ZirconClient.Scripts;

/// <summary>共享场景音效播放器，供登录/选角等没有 GameScene 的阶段使用。</summary>
public static class SoundPlayback
{
    private static readonly Dictionary<SoundIndex, AudioStream> Cache = new();
    private static readonly Dictionary<SoundIndex, AudioStreamPlayer> Loops = new();

    public static void Play(Node owner, SoundIndex sound)
    {
        if (owner == null || sound == SoundIndex.None || !SoundCatalog.TryGet(sound, out var entry)) return;
        if (entry.Loop && Loops.TryGetValue(sound, out var existing) && GodotObject.IsInstanceValid(existing)) return;

        if (!Cache.TryGetValue(sound, out var stream))
        {
            var path = ProjectSettings.GlobalizePath("res://../Debug/Client/Sound/" + entry.FileName);
            stream = File.Exists(path) ? AudioStreamWav.LoadFromFile(path) : null;
            if (stream == null)
            {
                GD.PrintErr($"[Sound] 场景音效缺失: {sound} -> {path}");
                return;
            }
            Cache[sound] = stream;
        }

        if (entry.Loop && stream is AudioStreamWav wav)
            wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        // 按音效分类走对应总线（设置页 5 类音量/静音的消费端）
        var player = new AudioStreamPlayer { Stream = stream, Bus = ClientSettings.BusFor(entry.Category) };
        owner.AddChild(player);
        if (entry.Loop)
        {
            Loops[sound] = player;
            player.Finished += () =>
            {
                Loops.Remove(sound);
                if (GodotObject.IsInstanceValid(player)) player.QueueFree();
            };
        }
        else player.Finished += player.QueueFree;
        player.Play();
    }

    public static void Stop(SoundIndex sound)
    {
        if (!Loops.Remove(sound, out var player)) return;
        if (GodotObject.IsInstanceValid(player)) player.QueueFree();
    }
}
