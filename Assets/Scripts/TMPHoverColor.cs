using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TMPHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color hoverColor = Color.white;

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
        if (target != null) target.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (target != null) target.color = normalColor;
    }

    private void OnDisable()
    {
        // パネル切替で非表示になったとき色が残るのを防ぐ
        if (target != null) target.color = normalColor;
    }
}
