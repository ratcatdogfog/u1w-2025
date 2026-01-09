using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("AudioSources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("Volume Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    [Header("Slider SE")]
    [SerializeField] private AudioClip sliderSE;
    [SerializeField] private float sliderSEDelay = 0.1f; // 値の変化が止まってからSEを鳴らすまでの待機時間（秒）

    [Header("UI SE")]
    [SerializeField] private AudioClip decisionSE; // UI選択時の決定SE
    [SerializeField] private AudioClip clickableClickSE; // クリッカブル時のクリックSE
    [Range(0f, 1f)] [SerializeField] private float clickableClickSEVolumeScale = 0.5f; // クリッカブルSEの音量スケール（決定SEを基準とした相対音量）

    [Header("Defaults")]
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float seVolume  = 1.0f;

    private Coroutine seSliderSECoroutine;

    private void Awake()
    {
        // シングルトン（シーンをまたいで1つだけにする）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 最低限の安全設定
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = bgmVolume;
        }
        if (seSource != null)
        {
            seSource.loop = false;
            seSource.playOnAwake = false;
            seSource.volume = seVolume;
        }
    }

    private void Start()
    {
        // Sliderの初期値を設定し、イベントに接続
        if (bgmSlider != null)
        {
            bgmSlider.value = bgmVolume;
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (seSlider != null)
        {
            seSlider.value = seVolume;
            seSlider.onValueChanged.AddListener(OnSEVolumeChanged);
        }
    }

    private void OnDestroy()
    {
        // イベントの解除（メモリリーク防止）
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        }

        if (seSlider != null)
        {
            seSlider.onValueChanged.RemoveListener(OnSEVolumeChanged);
        }

        // コルーチンの停止
        if (seSliderSECoroutine != null)
        {
            StopCoroutine(seSliderSECoroutine);
        }
    }

    // ---------- BGM ----------
    public void PlayBGM(AudioClip clip, bool restartIfSame = false)
    {
        if (bgmSource == null || clip == null) return;

        // 同じ曲が鳴ってるなら再スタートしない（必要ならオプションで）
        if (!restartIfSame && bgmSource.isPlaying && bgmSource.clip == clip)
            return;

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource == null) return;
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void SetBGMVolume(float volume01)
    {
        bgmVolume = Mathf.Clamp01(volume01);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        
        // Sliderの値も同期（外部からSetBGMVolumeが呼ばれた場合）
        if (bgmSlider != null && Mathf.Abs(bgmSlider.value - bgmVolume) > 0.001f)
        {
            bgmSlider.value = bgmVolume;
        }
    }

    // ---------- SE ----------
    public void PlaySE(AudioClip clip, float volumeScale = 1f)
    {
        if (seSource == null || clip == null) return;

        // seSource.volume は「基本音量」、volumeScale は「鳴らすときの補正」
        seSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * seVolume);
    }

    public void SetSEVolume(float volume01)
    {
        seVolume = Mathf.Clamp01(volume01);
        if (seSource != null) seSource.volume = seVolume;
        
        // Sliderの値も同期（外部からSetSEVolumeが呼ばれた場合）
        if (seSlider != null && Mathf.Abs(seSlider.value - seVolume) > 0.001f)
        {
            seSlider.value = seVolume;
        }
    }

    /// <summary>
    /// UI選択時の決定SEを再生する
    /// </summary>
    public void PlayDecisionSE()
    {
        if (decisionSE != null)
        {
            PlaySE(decisionSE);
        }
    }

    /// <summary>
    /// クリッカブル時のクリックSEを再生する
    /// </summary>
    public void PlayClickableClickSE()
    {
        if (clickableClickSE != null)
        {
            PlaySE(clickableClickSE, clickableClickSEVolumeScale);
        }
    }

    // ---------- Slider Callbacks ----------
    private void OnBGMVolumeChanged(float value)
    {
        SetBGMVolume(value);
    }

    private void OnSEVolumeChanged(float value)
    {
        SetSEVolume(value);
        TriggerSliderSE(ref seSliderSECoroutine);
    }

    // ---------- Slider SE ----------
    private void TriggerSliderSE(ref Coroutine coroutine)
    {
        // 既存のコルーチンを停止
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        // 新しいコルーチンを開始
        coroutine = StartCoroutine(PlaySliderSEAfterDelay());
    }

    private IEnumerator PlaySliderSEAfterDelay()
    {
        // 指定時間待機（この間に値が変更されればコルーチンが停止される）
        yield return new WaitForSeconds(sliderSEDelay);

        // 待機時間が経過したらSEを再生
        if (sliderSE != null)
        {
            PlaySE(sliderSE);
        }
    }
}
