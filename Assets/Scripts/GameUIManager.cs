using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("Scene Management")]
    [SerializeField] string[] sceneNames; // ロードするシーン名の配列（複数の同じシーンを用意する場合）
    [SerializeField] bool useSceneRotation = true; // シーンを順番にローテーションするか（falseの場合は常に最初のシーンをロード）
    [SerializeField] bool loadSceneOnEnding = true; // エンディング後にシーンをロードするか

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

    /// <summary>
    /// 最初の画像のフェードイン演出を実行する（ImageSwitcherから呼び出される）
    /// mainMenuToGameTransitionを使用し、1→0への遷移を実行
    /// </summary>
    public void PlayFirstImageFadeIn()
    {
        if (mainMenuToGameTransition != null)
        {
            // SceneTransitionで1→0への遷移を実行（もともと黒だったものから0へ）
            mainMenuToGameTransition.PlayToBlack();
        }
        else
        {
            Debug.LogWarning("[GameUIManager] mainMenuToGameTransitionが設定されていません。");
        }
    }

    /// <summary>
    /// illustrationPanelの画面遷移を実行する（ImageSwitcherから呼び出される）
    /// illustrationPanelTransitionを使用し、1→0への遷移を実行
    /// 遷移前に_Progressを1に設定（暗転状態からスタート）
    /// </summary>
    /// <param name="onComplete">完了時のコールバック</param>
    public void PlayIllustrationPanelTransition(System.Action onComplete = null)
    {
        if (illustrationPanelTransition == null)
        {
            Debug.LogWarning("[GameUIManager] illustrationPanelTransitionが設定されていません。");
            onComplete?.Invoke();
            return;
        }

        // TransitionControllerのGameObjectがアクティブであることを確認
        if (!illustrationPanelTransition.gameObject.activeInHierarchy)
        {
            illustrationPanelTransition.gameObject.SetActive(true);
        }

        // 画面遷移開始前にテロップ用UIを表示
        ShowTelopPanel();

        // _Progressを1に設定（暗転状態からスタート）
        SetIllustrationPanelTransitionProgress(1f);

        // illustrationPanelTransitionで1→0への遷移を実行
        // 完了後に初期値にリセット
        illustrationPanelTransition.PlayToBlack(() =>
        {
            // 遷移完了後に初期値にリセット
            illustrationPanelTransition.ResetState();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// illustrationPanelTransitionの_Progressを直接設定する
    /// </summary>
    /// <param name="progress">0（明るい）から1（暗い）の値</param>
    private void SetIllustrationPanelTransitionProgress(float progress)
    {
        if (illustrationPanelTransition == null)
        {
            Debug.LogWarning("[GameUIManager] illustrationPanelTransitionが設定されていません。");
            return;
        }

        // TransitionControllerからMaterialを取得して_Progressを設定
        // UI要素（Image/RawImage）と3D要素（Renderer）の両方に対応
        Material material = GetMaterialFromTransition(illustrationPanelTransition);
        if (material != null)
        {
            material.SetFloat("_Progress", Mathf.Clamp01(progress));
        }
        else
        {
            Debug.LogWarning("[GameUIManager] illustrationPanelTransitionのRenderer、Image、またはRawImageからMaterialが取得できませんでした。");
        }
    }

    /// <summary>
    /// タイトル画面に戻る（エンディングから呼び出される）
    /// シーンをロードするか、同じシーン内でタイトル画面を表示する
    /// </summary>
    public void BackToTitleScreen()
    {
        // シーンをロードする場合
        if (loadSceneOnEnding && sceneNames != null && sceneNames.Length > 0)
        {
            LoadNextScene();
            return;
        }

        // 同じシーン内でタイトル画面を表示する場合
        // titlePanelを表示
        SetOnly(titlePanel);

        // titleToMainMenuTransitionを初期値にリセット（元の状態に戻す）
        if (titleToMainMenuTransition != null)
        {
            titleToMainMenuTransition.ResetState();
        }
        else
        {
            Debug.LogWarning("[GameUIManager] titleToMainMenuTransitionが設定されていません。");
        }
    }

    /// <summary>
    /// 次のシーンをロードする
    /// </summary>
    private void LoadNextScene()
    {
        if (sceneNames == null || sceneNames.Length == 0)
        {
            Debug.LogWarning("[GameUIManager] シーン名が設定されていません。同じシーン内でタイトル画面を表示します。");
            SetOnly(titlePanel);
            return;
        }

        string sceneNameToLoad;

        if (useSceneRotation)
        {
            // 現在のシーン名を取得
            string currentSceneName = SceneManager.GetActiveScene().name;
            
            // 現在のシーンが配列内にあるか確認
            int currentIndex = System.Array.IndexOf(sceneNames, currentSceneName);
            
            if (currentIndex >= 0)
            {
                // 次のシーンを選択（最後のシーンの場合は最初に戻る）
                int nextIndex = (currentIndex + 1) % sceneNames.Length;
                sceneNameToLoad = sceneNames[nextIndex];
            }
            else
            {
                // 現在のシーンが配列内にない場合は最初のシーンをロード
                sceneNameToLoad = sceneNames[0];
            }
        }
        else
        {
            // 常に最初のシーンをロード
            sceneNameToLoad = sceneNames[0];
        }

        // トランジションを実行してからシーンをロード
        if (mainMenuToGameTransition != null)
        {
            // 暗転してからシーンをロード
            mainMenuToGameTransition.PlayFromBlack(() =>
            {
                SceneManager.LoadScene(sceneNameToLoad);
            });
        }
        else
        {
            // トランジションがない場合は直接ロード
            SceneManager.LoadScene(sceneNameToLoad);
        }
    }

    /// <summary>
    /// titleToMainMenuTransitionの_Progressを直接設定する
    /// </summary>
    /// <param name="progress">0（明るい）から1（暗い）の値</param>
    private void SetTitleToMainMenuTransitionProgress(float progress)
    {
        if (titleToMainMenuTransition == null)
        {
            Debug.LogWarning("[GameUIManager] titleToMainMenuTransitionが設定されていません。");
            return;
        }

        // TransitionControllerからMaterialを取得して_Progressを設定
        // UI要素（Image/RawImage）と3D要素（Renderer）の両方に対応
        Material material = GetMaterialFromTransition(titleToMainMenuTransition);
        if (material != null)
        {
            material.SetFloat("_Progress", Mathf.Clamp01(progress));
        }
        else
        {
            Debug.LogWarning("[GameUIManager] titleToMainMenuTransitionのRenderer、Image、またはRawImageからMaterialが取得できませんでした。");
        }
    }

    /// <summary>
    /// TransitionControllerからMaterialを取得する（UI要素と3D要素の両方に対応）
    /// </summary>
    /// <param name="transition">TransitionControllerコンポーネント</param>
    /// <returns>取得したMaterial、取得できない場合はnull</returns>
    private Material GetMaterialFromTransition(TransitionController transition)
    {
        if (transition == null)
        {
            return null;
        }

        // UI要素（Image）をチェック
        Image image = transition.GetComponent<Image>();
        if (image != null && image.material != null)
        {
            return image.material;
        }

        // UI要素（RawImage）をチェック
        RawImage rawImage = transition.GetComponent<RawImage>();
        if (rawImage != null && rawImage.material != null)
        {
            return rawImage.material;
        }

        // 3D要素（Renderer）をチェック
        Renderer renderer = transition.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            return renderer.material;
        }

        return null;
    }
}