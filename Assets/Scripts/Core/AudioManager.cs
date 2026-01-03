using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 音楽と効果音を管理するクラス
/// </summary>
public class AudioManager : MonoBehaviour
{
    // シングルトンパターン
    public static AudioManager Instance { get; private set; }
    
    [System.Serializable]
    public class AudioClipInfo
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
    }
    
    [Header("音楽設定")]
    public List<AudioClipInfo> musicClips = new List<AudioClipInfo>();
    public AudioSource musicSource;
    
    [Header("効果音設定")]
    public List<AudioClipInfo> sfxClips = new List<AudioClipInfo>();
    public AudioSource sfxSource;
    
    // 効果音と音楽のディクショナリ
    private Dictionary<string, AudioClipInfo> musicDictionary = new Dictionary<string, AudioClipInfo>();
    private Dictionary<string, AudioClipInfo> sfxDictionary = new Dictionary<string, AudioClipInfo>();
    
    private void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初期化
            InitializeAudioSources();
            InitializeClipDictionaries();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// AudioSourceコンポーネントの初期化
    /// </summary>
    private void InitializeAudioSources()
    {
        // 音楽用AudioSource
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
        }
        
        // 効果音用AudioSource
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
        }
    }
    
    /// <summary>
    /// クリップディクショナリの初期化
    /// </summary>
    private void InitializeClipDictionaries()
    {
        // 音楽クリップをディクショナリに登録
        foreach (AudioClipInfo info in musicClips)
        {
            if (info.clip != null && !string.IsNullOrEmpty(info.name))
            {
                musicDictionary[info.name] = info;
            }
        }
        
        // 効果音クリップをディクショナリに登録
        foreach (AudioClipInfo info in sfxClips)
        {
            if (info.clip != null && !string.IsNullOrEmpty(info.name))
            {
                sfxDictionary[info.name] = info;
            }
        }
    }
    
    /// <summary>
    /// 音楽を再生
    /// </summary>
    public void PlayMusic(string musicName)
    {
        if (musicDictionary.TryGetValue(musicName, out AudioClipInfo info))
        {
            if (musicSource.isPlaying && musicSource.clip == info.clip)
                return;
                
            musicSource.clip = info.clip;
            musicSource.volume = info.volume;
            musicSource.Play();
            
            Debug.Log($"[AudioManager] 音楽再生: {musicName}");
        }
        else
        {
            Debug.LogWarning($"[AudioManager] 音楽が見つかりません: {musicName}");
        }
    }
    
    /// <summary>
    /// 効果音を再生
    /// </summary>
    public void PlaySFX(string sfxName)
    {
        if (sfxDictionary.TryGetValue(sfxName, out AudioClipInfo info))
        {
            sfxSource.PlayOneShot(info.clip, info.volume);
            Debug.Log($"[AudioManager] 効果音再生: {sfxName}");
        }
        else
        {
            Debug.LogWarning($"[AudioManager] 効果音が見つかりません: {sfxName}");
        }
    }
    
    /// <summary>
    /// 音楽を停止
    /// </summary>
    public void StopMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
            Debug.Log("[AudioManager] 音楽停止");
        }
    }
    
    /// <summary>
    /// ボリュームを調整
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        musicSource.volume = volume;
    }
    
    /// <summary>
    /// 効果音ボリュームを調整
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        sfxSource.volume = volume;
    }
}