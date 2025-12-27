using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// クリッカブルであることを示すマークを上下にアニメーションさせるコンポーネント
/// </summary>
public class ClickableIndicator : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Image indicatorImage;
    [SerializeField] private float moveDistance = 10f; // 上下に動く距離
    [SerializeField] private float animationDuration = 0.5f; // アニメーションの時間
    [SerializeField] private Ease easeType = Ease.InOutSine; // イージングタイプ

    private Vector3 originalPosition;
    private Sequence animationSequence;
    private bool isAnimating = false;
    private bool isClickable = false; // クリック可能な状態
    private Color originalColor; // 元の色（alpha以外）

    private void Awake()
    {
        // 必須参照のチェック
        if (indicatorImage == null)
        {
            Debug.LogError("[ClickableIndicator] indicatorImageが設定されていません。InspectorでImageコンポーネントを割り当ててください。", this);
            enabled = false;
            return;
        }

        // 初期位置を保存
        originalPosition = indicatorImage.rectTransform.anchoredPosition;

        // 元の色を保存（alpha以外）
        originalColor = indicatorImage.color;

        // 初期状態では非表示にする（alphaを0に設定）
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        // OnEnable時はアニメーションを開始しない（alphaが0の場合は表示されていないため）
        // 表示状態に応じてアニメーションはShow()で開始される
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    private void OnDestroy()
    {
        StopAnimation();
    }

    /// <summary>
    /// アニメーションを開始する
    /// </summary>
    public void StartAnimation()
    {
        if (indicatorImage == null || isAnimating)
        {
            return;
        }

        isAnimating = true;
        indicatorImage.rectTransform.anchoredPosition = originalPosition;

        // 既存のアニメーションを停止
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Kill();
        }

        // 上下に動くアニメーションを作成
        animationSequence = DOTween.Sequence();
        animationSequence.Append(indicatorImage.rectTransform.DOAnchorPosY(
            originalPosition.y + moveDistance, 
            animationDuration
        ).SetEase(easeType));
        animationSequence.Append(indicatorImage.rectTransform.DOAnchorPosY(
            originalPosition.y, 
            animationDuration
        ).SetEase(easeType));
        animationSequence.SetLoops(-1); // 無限ループ
    }

    /// <summary>
    /// アニメーションを停止する
    /// </summary>
    public void StopAnimation()
    {
        isAnimating = false;

        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Kill();
            animationSequence = null;
        }

        // 位置を初期位置に戻す
        if (indicatorImage != null)
        {
            indicatorImage.rectTransform.anchoredPosition = originalPosition;
        }
    }

    /// <summary>
    /// クリック可能な状態を設定する（外部から呼び出される）
    /// </summary>
    public void SetClickable(bool clickable)
    {
        isClickable = clickable;
        UpdateVisibility();
    }

    /// <summary>
    /// クリック可能な状態に基づいて表示/非表示を更新する（内部処理）
    /// </summary>
    private void UpdateVisibility()
    {
        if (isClickable)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    /// <summary>
    /// マークを表示する（内部処理）
    /// </summary>
    private void Show()
    {
        if (indicatorImage != null)
        {
            SetAlpha(1f);
            StartAnimation();
        }
    }

    /// <summary>
    /// マークを非表示にする（内部処理）
    /// </summary>
    private void Hide()
    {
        StopAnimation();
        if (indicatorImage != null)
        {
            SetAlpha(0f);
        }
    }

    /// <summary>
    /// Imageのalpha値を設定する
    /// </summary>
    private void SetAlpha(float alpha)
    {
        if (indicatorImage != null)
        {
            Color color = indicatorImage.color;
            color.a = alpha;
            indicatorImage.color = color;
        }
    }
}

