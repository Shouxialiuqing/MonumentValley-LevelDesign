using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioSource> loopingAudioSources = new Dictionary<string, AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        PlayLooping("Background");
    }

    // 从Audio文件夹加载音频
    private AudioClip LoadAudio(string audioName)
    {
        if (audioClips.TryGetValue(audioName, out AudioClip clip))
            return clip;

        clip = Resources.Load<AudioClip>($"Audio/{audioName}");

        if (clip != null)
            audioClips.Add(audioName, clip);
        else
            Debug.LogError($"无法加载音频: {audioName}");

        return clip;
    }

    // 一次性播放音频
    public AudioSource PlayOneShot(string audioName, float volume = 1f, float pitch = 1f)
    {
        AudioClip clip = LoadAudio(audioName);
        if (clip == null) return null;

        GameObject oneShotGO = new GameObject($"OneShot_{audioName}");
        oneShotGO.transform.SetParent(transform);
        AudioSource source = oneShotGO.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();

        Destroy(oneShotGO, clip.length);
        return source;
    }

    // 循环播放音频
    public AudioSource PlayLooping(string audioName, float volume = 1f, float pitch = 1f)
    {
        AudioClip clip = LoadAudio(audioName);
        if (clip == null) return null;

        if (loopingAudioSources.TryGetValue(audioName, out AudioSource existingSource))
        {
            StopLooping(audioName);
        }

        GameObject loopingGO = new GameObject($"Looping_{audioName}");
        loopingGO.transform.SetParent(transform);
        AudioSource source = loopingGO.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.loop = true;
        source.Play();

        loopingAudioSources[audioName] = source;
        return source;
    }

    // 按时长循环播放
    public void PlayLoopingForDuration(string audioName, float duration, float volume = 1f, float pitch = 1f)
    {
        AudioSource source = PlayLooping(audioName, volume, pitch);
        if (source != null)
            Invoke(nameof(StopLooping), duration);
    }

    // 停止循环播放
    public void StopLooping(string audioName)
    {
        if (loopingAudioSources.TryGetValue(audioName, out AudioSource source))
        {
            Destroy(source.gameObject);
            loopingAudioSources.Remove(audioName);
        }
    }

    // 停止所有循环音频
    public void StopAllLooping()
    {
        foreach (var source in loopingAudioSources.Values)
            Destroy(source.gameObject);

        loopingAudioSources.Clear();
    }

    // 检查音频是否正在循环播放
    public bool IsLooping(string audioName)
    {
        return loopingAudioSources.TryGetValue(audioName, out AudioSource source) && source.isPlaying;
    }
}