using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 画像を順番に切り替えるコンポーネント。
/// 最初の画像表示時にフェードイン演出を行い、最後の画像後に画面遷移を実行する。
/// </summary>
public class ImageSwitcher : MonoBehaviour
{
    [Header("Image Settings")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] sprites;
    
    [Header("Panel Settings")]
    [SerializeField] private GameObject targetPanel;
    
    [Header("Transition Settings")]
    [SerializeField] private GameObject activateOnTransition;
    [SerializeField] private GameObject[] deactivateOnTransition;
    
    [Header("UI Manager")]
    [SerializeField] private GameUIManager gameUIManager; // テロップ用UI表示用、トランジション管理用
    
    [Header("Clickable Indicator")]
    [SerializeField] private ClickableIndicator clickableIndicator; // クリッカブルであることを示すマーク
    [SerializeField] private float clickableIndicatorDelay = 2f; // 画像切り替え後の表示遅延時間（秒）
    
    [Header("Events")]
    /// <summary>
    /// 画面遷移終了時に発火するイベント（外部から購読可能）
    /// </summary>
    public UnityEvent onTransitionCompleted;

    private int currentSpriteIndex = -1; // 現在表示中のSpriteのインデックス（-1は未初期化状態）
    private bool hasPerformedFirstSwitch = false; // 最初のフェードイン演出が実行済みかどうか
    private bool hasCheckedPanelActivation = false; // targetPanelのアクティブ状態をチェック済みかどうか
    private Coroutine clickableIndicatorDelayCoroutine = null; // クリッカブルインジケーターの遅延表示用コルーチン
    private bool wasPanelActive = false; // 前回のPanelのアクティブ状態
    private float switchImageCooldownElapsed = 0f; // SwitchImage()のクールダウン経過時間（秒）
    private bool wasCanSwitchImage = false; // 前回のcanSwitchImageの値

    private void Awake()
    {
        // 必須参照のチェック
        if (clickableIndicator == null)
        {
            Debug.LogError("[ImageSwitcher] clickableIndicatorが設定されていません。InspectorでClickableIndicatorコンポーネントを割り当ててください。", this);
        }

        InitializeTransitionObjects();
    }

    /// <summary>
    /// 遷移関連のオブジェクトの初期状態を設定する
    /// </summary>
    private void InitializeTransitionObjects()
    {
        // 遷移時にアクティブにするオブジェクトは、初期状態では非アクティブにする
        if (activateOnTransition != null && activateOnTransition.activeSelf)
        {
            activateOnTransition.SetActive(false);
        }

        // 遷移時に非アクティブにするオブジェクトは、初期状態ではアクティブにする
        if (deactivateOnTransition != null)
        {
            foreach (GameObject obj in deactivateOnTransition)
            {
                if (obj != null && !obj.activeSelf)
                {
                    obj.SetActive(true);
                }
            }
        }
    }

    private void Start()
    {
        InitializeFirstImage();
    }

    /// <summary>
    /// 最初の画像を表示する
    /// </summary>
    private void InitializeFirstImage()
    {
        if (sprites == null || sprites.Length == 0 || targetImage == null)
        {
            return;
        }

        currentSpriteIndex = 0;
        targetImage.sprite = sprites[currentSpriteIndex];
    }

    private void Update()
    {
        bool panelActive = IsPanelActive();
        
        // Panelのアクティブ状態が変化した時だけクリッカブル状態を更新
        if (panelActive != wasPanelActive)
        {
            wasPanelActive = panelActive;
            if (panelActive)
            {
                // Panelがアクティブになった時、クールダウンカウントを0にリセット
                switchImageCooldownElapsed = 0f;
                
                if (!hasCheckedPanelActivation && !hasPerformedFirstSwitch)
                {
                    hasCheckedPanelActivation = true;
                    HandleFirstSwitch();
                }
                else
                {
                    // 既に初期化済みの場合はクリッカブル状態を更新
                    NotifyClickableState();
                }
            }
            else
            {
                // Panelが非アクティブになった時は非表示にしてクールダウンをリセット
                switchImageCooldownElapsed = 0f;
                if (clickableIndicator != null)
                {
                    clickableIndicator.SetClickable(false);
                }
            }
        }
        
        // Panelがアクティブな時、毎フレームdeltaTimeを加算
        if (panelActive)
        {
            switchImageCooldownElapsed += Time.deltaTime;
        }
        
        // クールダウンが完了したかどうかを判定
        bool cooldownComplete = switchImageCooldownElapsed >= clickableIndicatorDelay;
        bool canSwitchImage = panelActive && cooldownComplete;
        
        // canSwitchImageの値が変化した時だけログ出力
        if (canSwitchImage != wasCanSwitchImage)
        {
            wasCanSwitchImage = canSwitchImage;
            
            // canSwitchImageの値が変化した時にクリッカブル状態を更新
            NotifyClickableState();
        }
        
        if (canSwitchImage && Input.GetButtonDown("Submit"))
        {
            SwitchImageInternal();
        }
    }

    /// <summary>
    /// targetPanelがアクティブかどうかを判定
    /// </summary>
    public bool IsPanelActive()
    {
        return targetPanel != null && targetPanel.activeSelf;
    }

    /// <summary>
    /// 画像を次のSpriteに切り替える（外部から呼ばれる場合、クールダウンチェックを行う）
    /// 最初の呼び出し時はフェードイン演出を実行し、最後のSprite後は画面遷移を実行する
    /// </summary>
    public void SwitchImage()
    {
        if (!ValidateSprites()) return;

        // 最初の呼び出し時はフェードイン演出を実行（クールダウンチェックをスキップ）
        if (!hasPerformedFirstSwitch)
        {
            HandleFirstSwitch();
            // 最初のフェードイン時はクールダウンをリセットしない（Panelがアクティブになった時点で既に開始済み）
            return;
        }

        // クールダウンチェック（外部から直接呼ばれた場合でもチェック）
        bool panelActive = IsPanelActive();
        bool cooldownComplete = switchImageCooldownElapsed >= clickableIndicatorDelay;
        bool canSwitchImage = panelActive && cooldownComplete;
        
        if (!canSwitchImage)
        {
            return;
        }

        // 内部メソッドを呼び出し
        SwitchImageInternal();
    }

    /// <summary>
    /// 画像を次のSpriteに切り替える（内部処理、クールダウンチェック済み）
    /// </summary>
    private void SwitchImageInternal()
    {
        // 最後のSprite表示中に再度クリックされた場合は画面遷移を実行
        if (IsLastSprite())
        {
            HandleLastSpriteClick();
            return;
        }

        // 次の画像に切り替え
        SwitchToNextSprite();
        
        // SwitchImage()が実行された時、クールダウンカウントを0にリセット
        switchImageCooldownElapsed = 0f;
    }

    private bool ValidateSprites()
    {
        return sprites != null && sprites.Length > 0;
    }

    /// <summary>
    /// 最初の画像切り替え時の処理（SceneTransitionでフェードイン演出を開始）
    /// GameUIManagerに移譲
    /// </summary>
    private void HandleFirstSwitch()
    {
        hasPerformedFirstSwitch = true;
        
        // GameUIManagerに移譲
        gameUIManager?.PlayFirstImageFadeIn();

        // クリッカブル状態の更新は、Update()内でクールダウンが完了した時に行う
        // （最初の画像表示時はクールダウンが完了するまでクリッカブルマークを表示しない）
    }

    /// <summary>
    /// 現在表示中のSpriteが最後のものかどうかを判定
    /// </summary>
    public bool IsLastSprite()
    {
        return currentSpriteIndex == sprites.Length - 1;
    }

    /// <summary>
    /// 最後のSprite表示中にクリックされた時の処理（画面遷移を実行）
    /// </summary>
    private void HandleLastSpriteClick()
    {
        // 遅延表示コルーチンを停止（最後の画像なので表示しない）
        if (clickableIndicatorDelayCoroutine != null)
        {
            StopCoroutine(clickableIndicatorDelayCoroutine);
            clickableIndicatorDelayCoroutine = null;
        }

        // 最後のSpriteなので非表示にする
        if (clickableIndicator != null)
        {
            clickableIndicator.SetClickable(false);
        }

        DeactivateObjectsOnTransition();
        ActivateTransitionObject();
        ExecuteTransition();
    }

    /// <summary>
    /// 画面遷移時に非アクティブにするオブジェクトを処理
    /// </summary>
    private void DeactivateObjectsOnTransition()
    {
        if (deactivateOnTransition == null) return;

        foreach (GameObject obj in deactivateOnTransition)
        {
            if (obj != null && obj.activeSelf)
            {
                obj.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 画面遷移時にアクティブにするオブジェクトを処理
    /// </summary>
    private void ActivateTransitionObject()
    {
        activateOnTransition?.SetActive(true);
    }

    /// <summary>
    /// 画面遷移を実行し、完了後にtargetPanelを非アクティブにする
    /// GameUIManagerに移譲
    /// </summary>
    private void ExecuteTransition()
    {
        if (gameUIManager == null)
        {
            // GameUIManagerが設定されていない場合は即座に非アクティブ化
            DeactivateTargetPanel();
            onTransitionCompleted?.Invoke();
            return;
        }

        // GameUIManagerに移譲（_Progressを1に設定して暗転からスタートする処理も含まれる）
        gameUIManager.PlayIllustrationPanelTransition(() =>
        {
            DeactivateTargetPanel();
            onTransitionCompleted?.Invoke();
        });
    }

    /// <summary>
    /// targetPanelを非アクティブにする
    /// </summary>
    private void DeactivateTargetPanel()
    {
        targetPanel?.SetActive(false);
    }


    /// <summary>
    /// 次のSpriteに切り替える
    /// </summary>
    private void SwitchToNextSprite()
    {
        currentSpriteIndex++;
        if (targetImage != null && currentSpriteIndex < sprites.Length)
        {
            targetImage.sprite = sprites[currentSpriteIndex];
        }

        // 画像切り替え直後は一旦非表示にして、指定秒数後に表示する（最後の画像も含む）
        StartClickableIndicatorDelay();
    }

    /// <summary>
    /// クリッカブルインジケーターの遅延表示を開始する
    /// </summary>
    private void StartClickableIndicatorDelay()
    {
        // 既存のコルーチンを停止
        if (clickableIndicatorDelayCoroutine != null)
        {
            StopCoroutine(clickableIndicatorDelayCoroutine);
        }

        // 一旦非表示にする
        if (clickableIndicator != null)
        {
            clickableIndicator.SetClickable(false);
        }

        // 指定秒数後に表示するコルーチンを開始
        clickableIndicatorDelayCoroutine = StartCoroutine(ShowClickableIndicatorAfterDelay());
    }

    /// <summary>
    /// 指定秒数後にクリッカブルインジケーターを表示するコルーチン
    /// </summary>
    private IEnumerator ShowClickableIndicatorAfterDelay()
    {
        yield return new WaitForSeconds(clickableIndicatorDelay);

        // クリッカブルな状態を再評価して表示（最後の画像も含む）
        NotifyClickableState();

        clickableIndicatorDelayCoroutine = null;
    }

    /// <summary>
    /// クリッカブルな状態をClickableIndicatorに伝達する（状態が変化した時のみ呼ぶ）
    /// </summary>
    private void NotifyClickableState()
    {
        if (clickableIndicator == null)
        {
            return;
        }

        bool panelActive = IsPanelActive();

        // クリッカブルな条件：
        // 1. Panelがアクティブ
        // 2. 最初のフェードインが完了している
        // 3. クールダウンが完了している
        bool cooldownComplete = switchImageCooldownElapsed >= clickableIndicatorDelay;
        bool isClickable = panelActive && 
                          hasPerformedFirstSwitch &&
                          cooldownComplete;

        // クリック可能な状態を伝達するのみ（表示/非表示の判断はClickableIndicatorが行う）
        clickableIndicator.SetClickable(isClickable);
    }

}
