using UnityEngine;
using UnityEngine.EventSystems;

public class OutGameUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject titlePanel;
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject creditsPanel;
    [SerializeField] GameObject illustrationPanel; // 一枚絵を置く用のUI

    [Header("Transition")]
    [SerializeField] TransitionController titleToMainMenuTransition; // タイトルからメインメニューへの遷移用
    [SerializeField] TransitionController mainMenuToGameTransition; // メインメニューからインゲームへの遷移用

    void Awake()
    {
        // EventSystemの存在確認（UIクリックに必要）
        EnsureEventSystem();
        
        // 初期状態：タイトルだけ表示
        SetOnly(titlePanel);
        // 遷移演出側の初期化は必要に応じて明示的にResetState()を呼び出す
        // （現在のアルファ値を保持するため、Awake()では自動的にリセットしない）
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
        if (titleToMainMenuTransition != null)
        {
            // シームレスな遷移のため、先に下のPanel（mainMenuPanel）をactiveにする
            // sortorderが下なので、transitionの下に配置され、準備完了状態になる
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
            
            // transition開始（transition中はtitlePanelはactiveのまま）
            titleToMainMenuTransition.PlayToBlack(() =>
            {
                // transition完了後にtitlePanelを非activeにする
                if (titlePanel) titlePanel.SetActive(false);
            });
        }
        else
        {
            Debug.LogWarning("[OutGameUIManager] titleToMainMenuTransitionが設定されていません。");
            SetOnly(mainMenuPanel);
        }
    }

    // メインメニュー：各ボタン
    public void OpenSettings()
    {
        Debug.Log("OpenSettings called");
        SetOnly(settingsPanel);
    }

    public void OpenCredits()
    {
        Debug.Log("OpenCredits called");
        SetOnly(creditsPanel);
    }

    public void BackToMainMenu()
    {
        Debug.Log("BackToMainMenu called");
        SetOnly(mainMenuPanel);
    }

    // ゲーム開始ボタン：インゲームへ遷移（同じシーン内で完結）
    public void StartGame()
    {
        if (mainMenuToGameTransition != null)
        {
            // _Progressを0から1に遷移させるアニメーション
            mainMenuToGameTransition.PlayFromBlack(() =>
            {
                // 遷移完了後にillustrationPanelを表示
                SetOnly(illustrationPanel);
            });
        }
        else
        {
            Debug.LogWarning("[OutGameUIManager] mainMenuToGameTransitionが設定されていません。");
        }
    }

    void SetOnly(GameObject activePanel)
    {
        if (titlePanel) titlePanel.SetActive(activePanel == titlePanel);
        if (mainMenuPanel) mainMenuPanel.SetActive(activePanel == mainMenuPanel);
        if (settingsPanel) settingsPanel.SetActive(activePanel == settingsPanel);
        if (creditsPanel) creditsPanel.SetActive(activePanel == creditsPanel);
        if (illustrationPanel) illustrationPanel.SetActive(activePanel == illustrationPanel);
    }
}
