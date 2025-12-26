using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
    [SerializeField] private TransitionController transitionController;
    [SerializeField] private GameObject activateOnTransition;
    [SerializeField] private GameObject[] deactivateOnTransition;
    [SerializeField] private float fadeInDuration = 0.5f;

    // 現在表示中のSpriteのインデックス（-1は未初期化状態）
    private int currentSpriteIndex = -1;
    
    // 最初のフェードイン演出が実行済みかどうか
    private bool hasPerformedFirstSwitch = false;
    
    // targetPanelのアクティブ状態をチェック済みかどうか
    private bool hasCheckedPanelActivation = false;

    private void Awake()
    {
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
    /// 最初の画像を表示し、フェードイン用にalphaを0に設定する
    /// </summary>
    private void InitializeFirstImage()
    {
        if (sprites == null || sprites.Length == 0 || targetImage == null)
        {
            Debug.LogWarning("[ImageSwitcher] InitializeFirstImage() - spritesまたはtargetImageが設定されていません。");
            return;
        }

        currentSpriteIndex = 0;
        targetImage.sprite = sprites[currentSpriteIndex];
        
        // フェードイン演出のために、最初の画像のalphaを0に設定
        Color imageColor = targetImage.color;
        imageColor.a = 0f;
        targetImage.color = imageColor;
    }

    private void Update()
    {
        bool panelActive = IsPanelActive();
        
        // targetPanelがアクティブになった時点で、最初のフェードインを自動実行
        if (!hasCheckedPanelActivation && panelActive && !hasPerformedFirstSwitch)
        {
            hasCheckedPanelActivation = true;
            HandleFirstSwitch();
            return;
        }
        
        // Panelがアクティブで、Submitボタンが押された時に画像を切り替え
        if (panelActive && Input.GetButtonDown("Submit"))
        {
            SwitchImage();
        }
    }

    private bool IsPanelActive()
    {
        return targetPanel != null && targetPanel.activeSelf;
    }

    /// <summary>
    /// 画像を次のSpriteに切り替える
    /// 最初の呼び出し時はフェードイン演出を実行し、最後のSprite後は画面遷移を実行する
    /// </summary>
    public void SwitchImage()
    {
        if (!ValidateSprites()) return;

        // 最初の呼び出し時はフェードイン演出を実行
        if (!hasPerformedFirstSwitch)
        {
            HandleFirstSwitch();
            return;
        }

        // 最後のSprite表示中に再度クリックされた場合は画面遷移を実行
        if (IsLastSprite())
        {
            HandleLastSpriteClick();
            return;
        }

        // 次の画像に切り替え
        SwitchToNextSprite();
    }

    private bool ValidateSprites()
    {
        return sprites != null && sprites.Length > 0;
    }

    /// <summary>
    /// 最初の画像切り替え時の処理（フェードイン演出を開始）
    /// </summary>
    private void HandleFirstSwitch()
    {
        if (targetImage == null)
        {
            Debug.LogWarning("[ImageSwitcher] HandleFirstSwitch() - targetImageがnullです。");
            return;
        }

        hasPerformedFirstSwitch = true;
        StartCoroutine(FadeInImage());
    }

    /// <summary>
    /// 現在表示中のSpriteが最後のものかどうかを判定
    /// </summary>
    private bool IsLastSprite()
    {
        return currentSpriteIndex == sprites.Length - 1;
    }

    /// <summary>
    /// 最後のSprite表示中にクリックされた時の処理（画面遷移を実行）
    /// </summary>
    private void HandleLastSpriteClick()
    {
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
        if (activateOnTransition != null)
        {
            activateOnTransition.SetActive(true);
        }
    }

    /// <summary>
    /// 画面遷移を実行し、完了後にtargetPanelを非アクティブにする
    /// </summary>
    private void ExecuteTransition()
    {
        if (transitionController != null)
        {
            transitionController.PlayToBlack(() => DeactivateTargetPanel());
        }
        else
        {
            // TransitionControllerが設定されていない場合は即座に非アクティブ化
            DeactivateTargetPanel();
        }
    }

    /// <summary>
    /// targetPanelを非アクティブにする
    /// </summary>
    private void DeactivateTargetPanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 次のSpriteに切り替える
    /// </summary>
    private void SwitchToNextSprite()
    {
        currentSpriteIndex++;
        if (targetImage != null)
        {
            targetImage.sprite = sprites[currentSpriteIndex];
        }
    }

    /// <summary>
    /// 画像のalpha値を0から1にフェードインさせるコルーチン
    /// </summary>
    private IEnumerator FadeInImage()
    {
        if (targetImage == null)
        {
            Debug.LogError("[ImageSwitcher] FadeInImage() - targetImageがnullです。");
            yield break;
        }

        if (fadeInDuration <= 0f)
        {
            Debug.LogWarning($"[ImageSwitcher] FadeInImage() - fadeInDurationが無効な値です: {fadeInDuration}");
        }

        Color imageColor = targetImage.color;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / fadeInDuration);
            imageColor.a = Mathf.Lerp(0f, 1f, normalizedTime);
            targetImage.color = imageColor;
            yield return null;
        }

        // 最終値を確実に1に設定
        imageColor.a = 1f;
        targetImage.color = imageColor;
    }
}
