using UnityEngine;
using UnityEngine.EventSystems;

public class GameUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject titlePanel;
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject creditsPanel;
    [SerializeField] GameObject illustrationPanel; // 一枚絵を置く用のUI
    [SerializeField] GameObject telopPanel; // テロップ用UI（他のUIと共存可能）

    [Header("Transition")]
    [SerializeField] TransitionController titleToMainMenuTransition; // タイトルからメインメニューへの遷移用
    [SerializeField] TransitionController mainMenuToGameTransition; // ゲーム全体で一貫して使われるシーン遷移用（titleToMainMenuTransitionとillustrationPanel専用以外はこれを使用）
    [SerializeField] TransitionController illustrationPanelTransition; // illustrationPanel専用のトランジション

    void Awake()
    {
        // 必須参照のチェック
        if (mainMenuToGameTransition == null)
        {
            Debug.LogError("[GameUIManager] mainMenuToGameTransitionが設定されていません。InspectorでTransitionControllerを割り当ててください。", this);
        }

        if (illustrationPanelTransition == null)
        {
            Debug.LogError("[GameUIManager] illustrationPanelTransitionが設定されていません。InspectorでTransitionControllerを割り当ててください。", this);
        }

        EnsureEventSystem();
        SetOnly(titlePanel);
        InitializeTelopPanel();
    }

    /// <summary>
    /// テロップパネルを初期化（他のUIと共存可能なのでSetOnlyとは独立）
    /// </summary>
    private void InitializeTelopPanel()
    {
        if (telopPanel != null)
        {
            telopPanel.SetActive(false);
        }
    }

    /// <summary>
    /// EventSystemの存在確認（存在しない場合は自動生成）
    /// </summary>
    private void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.LogWarning("EventSystemが見つかりませんでした。自動生成しました。");
        }
    }

    /// <summary>
    /// タイトル画面：クリック（透明フルスクリーンButtonのOnClickから呼ぶ想定）
    /// </summary>
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
            Debug.LogWarning("[GameUIManager] titleToMainMenuTransitionが設定されていません。");
            SetOnly(mainMenuPanel);
        }
    }

    /// <summary>
    /// 設定画面を開く
    /// </summary>
    public void OpenSettings()
    {
        SetOnly(settingsPanel);
    }

    /// <summary>
    /// クレジット画面を開く
    /// </summary>
    public void OpenCredits()
    {
        SetOnly(creditsPanel);
    }

    /// <summary>
    /// メインメニューに戻る
    /// </summary>
    public void BackToMainMenu()
    {
        SetOnly(mainMenuPanel);
    }

    /// <summary>
    /// ゲーム開始ボタン：インゲームへ遷移（同じシーン内で完結）
    /// </summary>
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
            Debug.LogWarning("[GameUIManager] mainMenuToGameTransitionが設定されていません。");
        }
    }

    /// <summary>
    /// 指定されたパネルのみをアクティブにし、他のパネルを非アクティブにする
    /// </summary>
    private void SetOnly(GameObject activePanel)
    {
        if (titlePanel) titlePanel.SetActive(activePanel == titlePanel);
        if (mainMenuPanel) mainMenuPanel.SetActive(activePanel == mainMenuPanel);
        if (settingsPanel) settingsPanel.SetActive(activePanel == settingsPanel);
        if (creditsPanel) creditsPanel.SetActive(activePanel == creditsPanel);
        if (illustrationPanel) illustrationPanel.SetActive(activePanel == illustrationPanel);
    }

    /// <summary>
    /// テロップ用UIを表示する（他のUIと共存可能）
    /// </summary>
    public void ShowTelopPanel()
    {
        if (telopPanel != null)
        {
            telopPanel.SetActive(true);
        }
    }

    /// <summary>
    /// ゲーム全体で一貫して使われるシーン遷移用のTransitionControllerを取得
    /// </summary>
    public TransitionController GetMainTransition()
    {
        return mainMenuToGameTransition;
    }

    /// <summary>
    /// illustrationPanel専用のTransitionControllerを取得
    /// </summary>
    public TransitionController GetIllustrationPanelTransition()
    {
        return illustrationPanelTransition;
    }
}