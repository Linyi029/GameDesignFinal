using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("音訊源設定")]
    public AudioSource bgmSource;  // 專門放背景音樂的喇叭
    public AudioSource sfxSource;  // 專門放點擊音效的喇叭

    [Header("音效檔案 (Audio Clip)")]
    public AudioClip bgmMusic;     // 拖入妳的背景音樂檔案
    public AudioClip clickSound;   // 拖入妳的點擊音效檔案

    void Start()
    {
        // 1. 設定背景音樂並播放
        if (bgmSource != null && bgmMusic != null)
        {
            bgmSource.clip = bgmMusic;
            bgmSource.loop = true;        // 讓背景音樂無限循環
            bgmSource.playOnAwake = true; // 遊戲一打開就自動播放
            bgmSource.Play();
        }
    }

    /// <summary>
    /// 提供給點擊事件或其它腳本呼叫的「播放音效」功能
    /// </summary>
    public void PlayClickSFX()
    {
        if (sfxSource != null && clickSound != null)
        {
            // PlayOneShot 可以讓音效連續快速點擊時，聲音不會被中斷重頭播，而是重疊上去（打擊感更好！）
            sfxSource.PlayOneShot(clickSound);
        }
    }
}