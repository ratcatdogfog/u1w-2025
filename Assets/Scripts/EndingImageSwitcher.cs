using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// エンディング用の画像切り替えコンポーネント。
/// 左クリックで次の画像に切り替え、最後の画像をクリックしたら最初の画面へ戻る。
/// </summary>
public class EndingImageSwitcher : MonoBehaviour
{
    [System.Serializable]
    private class EndingSpriteSet
    {
        public Sprite[] sprites;
    }

    [Header("Image Settings")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] endingSprites;
    [SerializeField] private EndingSpriteSet[] alternativeEndingSprites; // choice2の選択回数に応じて使用する代替エンディング画像（配列のインデックスが選択回数に対応）
    [SerializeField] private GameObject endingImagePanel; // エンディング画像を表示するパネル（未設定の場合はtargetImageのGameObjectを使用）
    
    [Header("Ending Branch Settings")]
    [SerializeField] private bool useAlternativeEndingForZeroChoice2 = true; // choice2が0回のときも代替画像を使うかどうか（trueの場合、0回と1回は同じ分岐）
    [SerializeField] private int spriteIndexForZeroChoice2 = 0; // choice2が0回のときに使用するalternativeEndingSpritesのインデックス
    [SerializeField] private int spriteIndexForOneChoice2 = 0; // choice2が1回のときに使用するalternativeEndingSpritesのインデックス
    [SerializeField] private int spriteIndexForTwoChoice2 = 1; // choice2が2回のときに使用するalternativeEndingSpritesのインデックス
    
    [Header("UI Manager")]
    [SerializeField] private GameUIManager gameUIManager; // 最初の画面へ戻るための参照
    
    [Header("Clickable Indicator")]
    [SerializeField] private ClickableIndicator clickableIndicator; // クリッカブルであることを示すマーク
    [SerializeField] private float clickableIndicatorDelay = 2f; // 画像切り替え後のクールダウン時間（秒）
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.5f; // フェードインの時間（秒）
    
    private int currentSpriteIndex = -1; // 現在表示中のSpriteのインデックス（-1は未初期化状態）
    private Coroutine clickableIndicatorDelayCoroutine = null; // クリッカブルインジケーターの遅延表示用コルーチン
    private Coroutine fadeInCoroutine = null; // フェードイン用のコルーチン
    private float switchImageCooldownElapsed = 0f; // SwitchImage()のクールダウン経過時間（秒）
    private bool wasCanSwitchImage = false; // 前回のcanSwitchImageの値
    private bool isActive = false; // エンディングがアクティブかどうか
    private Sprite[] currentEndingSprites; // 現在使用中のエンディング画像配列

    private void Awake()
    {
        // 必須参照のチェック
        if (gameUIManager == null)
        {
            Debug.LogError("[EndingImageSwitcher] gameUIManagerが設定されていません。InspectorでGameUIManagerコンポーネントを割り当ててください。", this);
        }

        // エンディング画像パネルを非表示にする
        InitializeEndingObjects();
    }

    /// <summary>
    /// エンディング関連のオブジェクトの初期状態を設定する
    /// </summary>
    private void InitializeEndingObjects()
    {
        // endingImagePanelが設定されている場合はそれを使用、未設定の場合はtargetImageのGameObjectを使用
        GameObject panelToHide = endingImagePanel != null ? endingImagePanel : (targetImage != null ? targetImage.gameObject : null);
        
        if (panelToHide != null && panelToHide.activeSelf)
        {
            panelToHide.SetActive(false);
        }

        // targetImageのalphaを0に設定
        if (targetImage != null)
        {
            Color color = targetImage.color;
            color.a = 0f;
            targetImage.color = color;
        }
    }

    private void Update()
    {
        if (!isActive) return;

        // クールダウンが完了したかどうかを判定
        bool cooldownComplete = switchImageCooldownElapsed >= clickableIndicatorDelay;
        bool canSwitchImage = cooldownComplete;
        
        // canSwitchImageの値が変化した時だけクリッカブル状態を更新
        if (canSwitchImage != wasCanSwitchImage)
        {
            wasCanSwitchImage = canSwitchImage;
            NotifyClickableState();
        }
        
        // 左クリックで次の画像へ切り替え
        if (canSwitchImage && Input.GetMouseButtonDown(0))
        {
            SwitchImage();
        }
        
        // クールダウン経過時間を更新
        switchImageCooldownElapsed += Time.deltaTime;
    }

    /// <summary>
    /// エンディングを開始する
    /// </summary>
    /// <param name="choice2Count">choice2の選択回数（0または1のときはalternativeEndingSprites[0]、2のときはalternativeEndingSprites[1]を使用）</param>
    public void StartEnding(int choice2Count = 0)
    {
        // choice2の選択回数に応じて使用するエンディング画像を決定
        Sprite[] spritesToUse = GetEndingSpritesForChoice2Count(choice2Count);
        
        if (spritesToUse == null || spritesToUse.Length == 0 || targetImage == null)
        {
            Debug.LogError("[EndingImageSwitcher] endingSpritesまたはtargetImageが設定されていません。", this);
            return;
        }

        // 現在使用中のエンディング画像を保存
        currentEndingSprites = spritesToUse;

        // 既存のフェードインコルーチンを停止
        if (fadeInCoroutine != null)
        {
            StopCoroutine(fadeInCoroutine);
            fadeInCoroutine = null;
        }

        // エンディング画像パネルを表示する
        GameObject panelToShow = endingImagePanel != null ? endingImagePanel : (targetImage != null ? targetImage.gameObject : null);
        if (panelToShow != null && !panelToShow.activeSelf)
        {
            panelToShow.SetActive(true);
        }

        isActive = true;
        currentSpriteIndex = 0;
        targetImage.sprite = currentEndingSprites[currentSpriteIndex];
        
        // targetImageのalphaを0に設定（フェードイン開始前）
        Color color = targetImage.color;
        color.a = 0f;
        targetImage.color = color;
        
        switchImageCooldownElapsed = 0f;
        wasCanSwitchImage = false;
        
        // 最初の画像表示時はクリッカブルマークを非表示にする
        if (clickableIndicator != null)
        {
            clickableIndicator.SetClickable(false);
        }

        // フェードインを開始
        fadeInCoroutine = StartCoroutine(FadeInImageCoroutine());
    }

    /// <summary>
    /// choice2の選択回数に応じて使用するエンディング画像を取得する
    /// Inspectorで設定されたspriteIndexを使用して分岐する
    /// </summary>
    /// <param name="choice2Count">choice2の選択回数</param>
    /// <returns>使用するエンディング画像配列</returns>
    private Sprite[] GetEndingSpritesForChoice2Count(int choice2Count)
    {
        // alternativeEndingSpritesが設定されていない場合はデフォルトを使用
        if (alternativeEndingSprites == null || alternativeEndingSprites.Length == 0)
        {
            return endingSprites;
        }

        // choice2Countに応じて使用するspriteIndexを決定
        int spriteIndex;
        if (choice2Count == 0)
        {
            // choice2が0回のときの処理
            if (useAlternativeEndingForZeroChoice2)
            {
                spriteIndex = spriteIndexForZeroChoice2;
            }
            else
            {
                // 代替画像を使わない場合はデフォルトを使用
                return endingSprites;
            }
        }
        else if (choice2Count == 1)
        {
            spriteIndex = spriteIndexForOneChoice2;
        }
        else if (choice2Count == 2)
        {
            spriteIndex = spriteIndexForTwoChoice2;
        }
        else
        {
            // choice2が3回以上の場合はデフォルトを使用
            return endingSprites;
        }

        // spriteIndexが有効な範囲内かチェック
        if (spriteIndex >= 0 && spriteIndex < alternativeEndingSprites.Length)
        {
            EndingSpriteSet spriteSet = alternativeEndingSprites[spriteIndex];
            if (spriteSet != null && spriteSet.sprites != null && spriteSet.sprites.Length > 0)
            {
                Debug.Log($"[EndingImageSwitcher] choice2の選択回数 {choice2Count} に応じてエンディング画像を変更しました（spriteIndex={spriteIndex}）。");
                return spriteSet.sprites;
            }
        }

        // 代替画像が設定されていない場合はデフォルトを使用
        return endingSprites;
    }

    /// <summary>
    /// エンディングを停止する
    /// </summary>
    public void StopEnding()
    {
        isActive = false;
        
        // フェードインコルーチンを停止
        if (fadeInCoroutine != null)
        {
            StopCoroutine(fadeInCoroutine);
            fadeInCoroutine = null;
        }
        
        // 遅延表示コルーチンを停止
        if (clickableIndicatorDelayCoroutine != null)
        {
            StopCoroutine(clickableIndicatorDelayCoroutine);
            clickableIndicatorDelayCoroutine = null;
        }
        
        // クリッカブルマークを非表示にする
        if (clickableIndicator != null)
        {
            clickableIndicator.SetClickable(false);
        }

        // エンディング画像パネルを非表示にする
        GameObject panelToHide = endingImagePanel != null ? endingImagePanel : (targetImage != null ? targetImage.gameObject : null);
        if (panelToHide != null && panelToHide.activeSelf)
        {
            panelToHide.SetActive(false);
        }
    }

    /// <summary>
    /// 画像を次のSpriteに切り替える
    /// 最後のSprite表示中に再度クリックされた場合は最初の画面へ戻る
    /// </summary>
    private void SwitchImage()
    {
        if (!ValidateSprites()) return;

        // クリッカブルな状態でクリックしたので、クリッカブルクリックSEを再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickableClickSE();
        }

        // 最後のSprite表示中に再度クリックされた場合は最初の画面へ戻る
        if (IsLastSprite())
        {
            ReturnToMainMenu();
            return;
        }

        // 次の画像に切り替え
        SwitchToNextSprite();
        
        // SwitchImage()が実行された時、クールダウンカウントを0にリセット
        switchImageCooldownElapsed = 0f;
    }

    private bool ValidateSprites()
    {
        return currentEndingSprites != null && currentEndingSprites.Length > 0;
    }

    /// <summary>
    /// 現在表示中のSpriteが最後のものかどうかを判定
    /// </summary>
    private bool IsLastSprite()
    {
        return currentEndingSprites != null && currentSpriteIndex == currentEndingSprites.Length - 1;
    }

    /// <summary>
    /// 最初の画面（タイトル画面）へ戻る
    /// </summary>
    private void ReturnToMainMenu()
    {
        // エンディングを停止（isActiveをfalseにして、Update()の処理を停止）
        StopEnding();

        // GameUIManagerを使ってタイトル画面へ戻る
        if (gameUIManager != null)
        {
            gameUIManager.BackToTitleScreen();
        }
        else
        {
            Debug.LogWarning("[EndingImageSwitcher] GameUIManagerが設定されていません。最初の画面へ戻れません。");
        }
    }

    /// <summary>
    /// 次のSpriteに切り替える
    /// </summary>
    private void SwitchToNextSprite()
    {
        currentSpriteIndex++;
        if (targetImage != null && currentEndingSprites != null && currentSpriteIndex < currentEndingSprites.Length)
        {
            targetImage.sprite = currentEndingSprites[currentSpriteIndex];
        }

        // 画像切り替え直後は一旦非表示にして、指定秒数後に表示する
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

        // クリッカブルな状態を再評価して表示
        NotifyClickableState();

        clickableIndicatorDelayCoroutine = null;
    }

    /// <summary>
    /// クリッカブルな状態をClickableIndicatorに伝達する
    /// </summary>
    private void NotifyClickableState()
    {
        if (clickableIndicator == null)
        {
            return;
        }

        // クリッカブルな条件：
        // 1. エンディングがアクティブ
        // 2. クールダウンが完了している
        bool cooldownComplete = switchImageCooldownElapsed >= clickableIndicatorDelay;
        bool isClickable = isActive && cooldownComplete;

        // クリック可能な状態を伝達するのみ（表示/非表示の判断はClickableIndicatorが行う）
        clickableIndicator.SetClickable(isClickable);
    }

    /// <summary>
    /// Imageのalphaを0から1にフェードインするコルーチン
    /// </summary>
    private IEnumerator FadeInImageCoroutine()
    {
        if (targetImage == null)
        {
            fadeInCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        Color color = targetImage.color;
        float startAlpha = color.a;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            color.a = Mathf.Lerp(startAlpha, 1f, t);
            targetImage.color = color;
            yield return null;
        }

        // フェードイン完了後、alphaを1に設定
        color.a = 1f;
        targetImage.color = color;
        fadeInCoroutine = null;
        
        // フェードイン完了後にクールダウンを開始
        switchImageCooldownElapsed = 0f;
    }
}

