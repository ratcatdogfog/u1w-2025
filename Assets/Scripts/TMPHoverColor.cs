using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TMPHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private bool enableHover = true;

    /// <summary>
    /// 現在ホバー中かどうかを取得する
    /// </summary>
    public bool IsHovering { get; private set; } = false;

    /// <summary>
    /// ホバー機能の有効/無効を取得・設定する
    /// </summary>
    public bool EnableHover
    {
        get => enableHover;
        set
        {
            enableHover = value;
            // 無効にされた場合、現在の状態をリセット
            if (!enableHover && target != null)
            {
                IsHovering = false;
                target.color = normalColor;
            }
        }
    }

    private void Reset()
    {
        // 付けた瞬間に子から自動取得したい場合
        target = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        if (target != null) target.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableHover) return;
        
        IsHovering = true;
        if (target != null) target.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enableHover) return;
        
        IsHovering = false;
        if (target != null) target.color = normalColor;
    }

    private void OnDisable()
    {
        // パネル切替で非表示になったとき色が残るのを防ぐ
        IsHovering = false;
        if (target != null) target.color = normalColor;
    }
}
