using UnityEngine;
using UnityEngine.EventSystems;

public class OutGameUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject titlePanel;
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject creditsPanel;

    [Header("Transition")]
    [SerializeField] TransitionController transition;

    void Awake()
    {
        // EventSystemの存在確認（UIクリックに必要）
        EnsureEventSystem();
        
        // 初期状態：タイトルだけ表示
        SetOnly(titlePanel);
        // 遷移演出側も初期化（Progress=1、alpha=1など）
        if (transition) transition.ResetState();
    }

    void EnsureEventSystem()
    {
        // EventSystemが存在しない場合は自動生成
        if (EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.LogWarning("EventSystemが見つかりませんでした。自動生成しました。");
        }
    }

    // タイトル画面：クリック（透明フルスクリーンButtonのOnClickから呼ぶ想定）
    public void OnTitleScreenClicked()
    {
        if (transition != null)
        {
            // シームレスな遷移のため、先に下のPanel（mainMenuPanel）をactiveにする
            // sortorderが下なので、transitionの下に配置され、準備完了状態になる
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
            
            // transition開始（transition中はtitlePanelはactiveのまま）
            transition.PlayToBlack(() =>
            {
                // transition完了後にtitlePanelを非activeにする
                if (titlePanel) titlePanel.SetActive(false);
            });
        }
        else
        {
            Debug.LogWarning("TransitionControllerが設定されていません。");
            SetOnly(mainMenuPanel);
        }
    }

    // メインメニュー：各ボタン
    public void OpenSettings()
    {
        SetOnly(settingsPanel);
    }

    public void OpenCredits()
    {
        SetOnly(creditsPanel);
    }

    public void BackToMainMenu()
    {
        SetOnly(mainMenuPanel);
    }

    void SetOnly(GameObject activePanel)
    {
        if (titlePanel) titlePanel.SetActive(activePanel == titlePanel);
        if (mainMenuPanel) mainMenuPanel.SetActive(activePanel == mainMenuPanel);
        if (settingsPanel) settingsPanel.SetActive(activePanel == settingsPanel);
        if (creditsPanel) creditsPanel.SetActive(activePanel == creditsPanel);
    }
}
