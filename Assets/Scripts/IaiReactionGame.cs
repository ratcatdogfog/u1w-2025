using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 刹那の見切り（居合反応）ゲーム
/// 二つのアバターが居合のポーズをとり、エフェクトが出たタイミングで素早くクリックする反応速度ゲーム
/// </summary>
public class IaiReactionGame : MonoBehaviour
{
    [Header("アバター設定")]
    [SerializeField] private GameObject playerAvatar;      // プレイヤーのアバター
    [SerializeField] private GameObject[] enemyAvatars;   // 敵のアバター（順番に使用）
    
    [Header("アニメーター設定")]
    [SerializeField] private Animator playerAnimator;      // プレイヤーのアニメーター
    [SerializeField] private Animator[] enemyAnimators;    // 敵のアニメーター（順番に使用）
    
    [Header("エフェクト設定")]
    [SerializeField] private GameObject reactionEffect;    // 反応すべきエフェクト（クリックタイミング）
    [SerializeField] private float effectDelayMin = 1.0f;   // エフェクト表示までの最小遅延時間（秒）
    [SerializeField] private float effectDelayMax = 3.0f;   // エフェクト表示までの最大遅延時間（秒）
    
    [Header("ゲーム設定")]
    [SerializeField] private float successThreshold = 0.5f; // 成功判定の反応速度閾値（秒）
    [SerializeField] private float roundInterval = 1.5f;    // ラウンド間のインターバル（秒）
    [SerializeField] private float clickTimeout = 2.0f;     // クリック制限時間（秒）
    [SerializeField] private bool alwaysWin = false;            // 必勝モード（ONの場合は常に勝利）
    
    [Header("インゲーム非表示設定")]
    [SerializeField] private List<GameObject> objectsToHideOnGameStart = new List<GameObject>();  // インゲーム開始時に非表示にするオブジェクト
    
    [Header("トランジション設定")]
    [SerializeField] private GameUIManager gameUIManager;  // GameUIManagerへの参照（トランジション用）
    [SerializeField] private CsvScenarioPlayer csvScenarioPlayer;  // CsvScenarioPlayerへの参照（明転委譲用）
    
    [Header("フラッシュ効果")]
    [SerializeField] private Image flashImage;             // 画面全体のフラッシュ用画像（白）
    [SerializeField] private float flashDuration = 0.1f;   // フラッシュの表示時間（秒）
    
    [Header("音效設定")]
    [SerializeField] private AudioClip iaiStartSE;         // 居合開始時のSE
    [SerializeField] private AudioClip effectSE;          // エフェクト表示時のSE
    [SerializeField] private AudioClip flashSE;            // フラッシュ発生時のSE
    
    [Header("アニメーション設定")]
    private const string idleTrigger = "Idle";           // 待機モーションのトリガー名
    private const string pauseTrigger = "Pause";         // 居合ポーズのトリガー名
    private const string yarareTrigger = "Yarare";       // 負けた姿勢のトリガー名
    private const string finishTrigger = "Finish";       // 勝った姿勢のトリガー名
    
    private bool isGameActive = false;                     // ゲームがアクティブかどうか
    private bool isWaitingForClick = false;                // クリック待ち状態かどうか
    private float effectShowTime = 0f;                     // エフェクトが表示された時刻
    private Coroutine gameCoroutine = null;                // ゲームループ用コルーチン
    private Coroutine resultCoroutine = null;              // 結果処理用コルーチン
    private bool roundSuccess = false;                     // ラウンドが成功したかどうか
    private int currentRound = 0;                          // 現在の勝負回数
    private GameObject currentEnemyAvatar;                 // 現在使用中の敵アバター
    private Animator currentEnemyAnimator;                 // 現在使用中の敵アニメーター
    
    /// <summary>
    /// ゲームがアクティブかどうかを外部から確認するための静的プロパティ
    /// </summary>
    public static bool IsGameActive { get; private set; } = false;
    
    private void Awake()
    {
        // 初期状態の設定
        if (reactionEffect != null)
        {
            reactionEffect.SetActive(false);
        }
        
        // フラッシュ画像を非表示
        if (flashImage != null)
        {
            flashImage.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// ゲームを開始する（choice1のbuttonのOnClickから呼び出す）
    /// 最初の勝負、または次の勝負を開始する
    /// </summary>
    public void StartGame()
    {
        if (isGameActive)
        {
            Debug.LogWarning("[IaiReactionGame] ゲームは既に開始されています。");
            return;
        }
        
        // 最初の勝負の場合は勝負回数をリセット
        if (currentRound == 0)
        {
            // 最初の敵キャラを設定
            SetEnemyAvatar(0);
        }
        // 次の勝負の敵キャラは既に暗転中に設定済み
        
        // アバターを居合ポーズ（Pause）に設定
        SetPausePose();
        
        // インゲーム開始時に非表示にするオブジェクトを非表示
        HideObjectsOnGameStart();
        
        // ゲームループを開始
        isGameActive = true;
        IsGameActive = true; // 静的プロパティも更新
        gameCoroutine = StartCoroutine(GameLoop());
    }
    
    /// <summary>
    /// ゲームループ（1回の勝負を実行）
    /// 複数回の勝負は、このメソッドが外部から繰り返し呼ばれることで実現される
    /// </summary>
    private IEnumerator GameLoop()
    {
        
        // 1回のラウンドを実行
        yield return StartCoroutine(PlayRound());
        
        // 勝敗に関わらず、次のラウンドに進む
        currentRound++;
        
        // 次の敵がいるかどうかを判定
        bool hasNextEnemy = currentRound < enemyAvatars.Length;
        
        if (hasNextEnemy)
        {
            // 次の勝負の準備（暗転中に敵キャラを入れ替える）
            // ここでは一旦ゲームを終了し、次の勝負は外部から再開される想定
            // （シナリオパートを挟むため）
            EndGame(roundSuccess, true); // 1つ目の引数は勝敗、2つ目の引数は「次の勝負がある」を示す
        }
        else
        {
            // 全ての勝負が終了
            EndGame(roundSuccess, false);
        }
    }
    
    /// <summary>
    /// 1ラウンドの処理
    /// </summary>
    private IEnumerator PlayRound()
    {
        
        roundSuccess = false;
        
        // アバターを居合ポーズ（Pause）に設定
        SetPausePose();
        
        // ランダムな遅延後にエフェクトを表示
        float delay = Random.Range(effectDelayMin, effectDelayMax);
        yield return new WaitForSeconds(delay);
        
        // エフェクトを表示
        ShowEffect();
        
        // クリック待ち状態にする
        isWaitingForClick = true;
        effectShowTime = Time.time;
        
        // 一定時間経過してもクリックされなかった場合のタイムアウト処理
        float timeoutTime = Time.time + clickTimeout;
        
        while (isWaitingForClick && Time.time < timeoutTime)
        {
            yield return null;
        }
        
        // タイムアウトした場合（クリックされなかった）
        if (isWaitingForClick)
        {
            // タイムアウト時の処理（フラッシュを焚いて負け判定）
            resultCoroutine = StartCoroutine(OnClickTimeoutCoroutine());
            yield return resultCoroutine;
            resultCoroutine = null;
        }
        else if (resultCoroutine != null)
        {
            // 結果処理のコルーチンが完了するまで待つ
            yield return resultCoroutine;
            resultCoroutine = null;
        }
    }
    
    /// <summary>
    /// 居合ポーズ（Pause）に設定
    /// </summary>
    private void SetPausePose()
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(pauseTrigger))
        {
            playerAnimator.SetTrigger(pauseTrigger);
        }
        
        if (currentEnemyAnimator != null && !string.IsNullOrEmpty(pauseTrigger))
        {
            currentEnemyAnimator.SetTrigger(pauseTrigger);
        }
        
        // 居合開始時のSEを再生
        if (AudioManager.Instance != null && iaiStartSE != null)
        {
            AudioManager.Instance.PlaySE(iaiStartSE);
        }
        
    }
    
    /// <summary>
    /// 待機モーション（Idle）に戻す
    /// </summary>
    private void SetIdlePose()
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(idleTrigger))
        {
            playerAnimator.SetTrigger(idleTrigger);
        }
        
        if (currentEnemyAnimator != null && !string.IsNullOrEmpty(idleTrigger))
        {
            currentEnemyAnimator.SetTrigger(idleTrigger);
        }
        
    }
    
    /// <summary>
    /// インゲーム開始時に非表示にするオブジェクトを非表示にする
    /// </summary>
    private void HideObjectsOnGameStart()
    {
        if (objectsToHideOnGameStart == null)
        {
            return;
        }
        
        foreach (GameObject obj in objectsToHideOnGameStart)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        
    }
    
    /// <summary>
    /// インゲーム終了時に非表示にしたオブジェクトを表示する（外部からも呼び出し可能）
    /// </summary>
    public void ShowObjectsOnGameEnd()
    {
        if (objectsToHideOnGameStart == null)
        {
            return;
        }
        
        foreach (GameObject obj in objectsToHideOnGameStart)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        
    }
    
    /// <summary>
    /// エフェクトを表示
    /// </summary>
    private void ShowEffect()
    {
        if (reactionEffect != null)
        {
            reactionEffect.SetActive(true);
        }
        
        // エフェクト表示時のSEを再生
        if (AudioManager.Instance != null && effectSE != null)
        {
            AudioManager.Instance.PlaySE(effectSE);
        }
    }
    
    /// <summary>
    /// エフェクトを非表示
    /// </summary>
    private void HideEffect()
    {
        if (reactionEffect != null)
        {
            reactionEffect.SetActive(false);
        }
    }
    
    /// <summary>
    /// クリック処理（Updateで呼び出すか、UI ButtonのOnClickから呼び出す）
    /// </summary>
    public void OnReactionClick()
    {
        if (!isGameActive || !isWaitingForClick)
        {
            return;
        }
        
        // 反応速度を計算
        float reactionTime = Time.time - effectShowTime;
        
        // クリック待ち状態を解除
        isWaitingForClick = false;
        
        // エフェクトを非表示
        HideEffect();
        
        // フラッシュ効果を表示（同時に結果のポーズに遷移）
        StartCoroutine(ShowFlash());
        
        // 必勝モードがONの場合は常に成功判定
        bool isSuccess = alwaysWin || (reactionTime <= successThreshold);
        
        // フラッシュと同時に結果のポーズに遷移（1フレーム以内）
        if (isSuccess)
        {
            // 成功：プレイヤー→Finish、敵→Yarare
            SetResultPose(true);
            resultCoroutine = StartCoroutine(OnSuccessCoroutine(reactionTime));
        }
        else
        {
            // 失敗：プレイヤー→Yarare、敵→Finish
            SetResultPose(false);
            resultCoroutine = StartCoroutine(OnFailureCoroutine(reactionTime));
        }
    }
    
    /// <summary>
    /// 結果のポーズに遷移（勝敗に応じて）
    /// </summary>
    private void SetResultPose(bool playerWon)
    {
        if (playerWon)
        {
            // プレイヤーが勝った：プレイヤー→Finish、敵→Yarare
            if (playerAnimator != null && !string.IsNullOrEmpty(finishTrigger))
            {
                playerAnimator.SetTrigger(finishTrigger);
            }
            if (currentEnemyAnimator != null && !string.IsNullOrEmpty(yarareTrigger))
            {
                currentEnemyAnimator.SetTrigger(yarareTrigger);
            }
        }
        else
        {
            // プレイヤーが負けた：プレイヤー→Yarare、敵→Finish
            if (playerAnimator != null && !string.IsNullOrEmpty(yarareTrigger))
            {
                playerAnimator.SetTrigger(yarareTrigger);
            }
            if (currentEnemyAnimator != null && !string.IsNullOrEmpty(finishTrigger))
            {
                currentEnemyAnimator.SetTrigger(finishTrigger);
            }
        }
    }
    
    /// <summary>
    /// フラッシュ効果を表示するコルーチン
    /// </summary>
    private IEnumerator ShowFlash()
    {
        // フラッシュ発生時のSEを再生
        if (AudioManager.Instance != null && flashSE != null)
        {
            AudioManager.Instance.PlaySE(flashSE);
        }
        
        if (flashImage != null)
        {
            flashImage.gameObject.SetActive(true);
            yield return new WaitForSeconds(flashDuration);
            flashImage.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 成功時の処理（コルーチン）
    /// </summary>
    private IEnumerator OnSuccessCoroutine(float reactionTime)
    {
        
        // フラッシュ効果が終わるまで待つ（ポーズは既に遷移済み）
        yield return new WaitForSeconds(flashDuration);
        
        // モーションが再生される時間を待つ
        yield return new WaitForSeconds(1.0f);
        
        // 成功フラグを設定
        roundSuccess = true;
    }
    
    /// <summary>
    /// 失敗時の処理（コルーチン）
    /// </summary>
    private IEnumerator OnFailureCoroutine(float reactionTime)
    {
        
        // フラッシュ効果が終わるまで待つ（ポーズは既に遷移済み）
        yield return new WaitForSeconds(flashDuration);
        
        // モーションが再生される時間を待つ
        yield return new WaitForSeconds(1.0f);
    }
    
    /// <summary>
    /// タイムアウト時の処理（クリックされなかった場合）- コルーチン版
    /// フラッシュを焚いて負け判定にする（必勝モードがONの場合は勝ち判定）
    /// </summary>
    private IEnumerator OnClickTimeoutCoroutine()
    {
        
        // クリック待ち状態を解除
        isWaitingForClick = false;
        
        // エフェクトを非表示
        HideEffect();
        
        // フラッシュ効果を表示（同時に結果のポーズに遷移）
        StartCoroutine(ShowFlash());
        
        // 必勝モードがONの場合は勝ち判定、OFFの場合は負け判定
        if (alwaysWin)
        {
            // 成功のポーズに遷移（プレイヤー→Finish、敵→Yarare）
            SetResultPose(true);
            resultCoroutine = StartCoroutine(OnSuccessCoroutine(0f)); // タイムアウトなので反応時間は0として扱う
        }
        else
        {
            // 失敗のポーズに遷移（プレイヤー→Yarare、敵→Finish）
            SetResultPose(false);
        }
        
        // フラッシュ効果が終わるまで待つ
        yield return new WaitForSeconds(flashDuration);
        
        // モーションが再生される時間を待つ
        yield return new WaitForSeconds(1.0f);
        
        // 必勝モードで成功コルーチンが実行された場合は、その完了を待つ
        if (alwaysWin && resultCoroutine != null)
        {
            yield return resultCoroutine;
            resultCoroutine = null;
        }
    }
    
    /// <summary>
    /// 敵キャラを設定する（指定されたインデックスの敵を使用）
    /// </summary>
    private void SetEnemyAvatar(int enemyIndex)
    {
        // 範囲チェック
        if (enemyAvatars == null || enemyIndex < 0 || enemyIndex >= enemyAvatars.Length)
        {
            Debug.LogWarning($"[IaiReactionGame] 敵キャラのインデックスが無効です: {enemyIndex}");
            return;
        }
        
        // 現在の敵キャラを非表示
        if (currentEnemyAvatar != null)
        {
            currentEnemyAvatar.SetActive(false);
        }
        
        // 新しい敵キャラを設定
        currentEnemyAvatar = enemyAvatars[enemyIndex];
        if (currentEnemyAvatar != null)
        {
            currentEnemyAvatar.SetActive(true);
            
            // アニメーターを取得
            if (enemyAnimators != null && enemyIndex < enemyAnimators.Length)
            {
                currentEnemyAnimator = enemyAnimators[enemyIndex];
            }
            else
            {
                // アニメーター配列が設定されていない場合は、GameObjectから取得を試みる
                currentEnemyAnimator = currentEnemyAvatar.GetComponent<Animator>();
            }
        }
        
    }
    
    /// <summary>
    /// ゲーム終了処理
    /// </summary>
    /// <param name="won">勝利したかどうか</param>
    /// <param name="hasNextRound">次の勝負があるかどうか（勝った場合のみ）</param>
    private void EndGame(bool won, bool hasNextRound = false)
    {
        isGameActive = false;
        IsGameActive = false; // 静的プロパティも更新
        isWaitingForClick = false;
        
        // エフェクトを非表示
        HideEffect();
        
        // ゲームコルーチンを停止
        if (gameCoroutine != null)
        {
            StopCoroutine(gameCoroutine);
            gameCoroutine = null;
        }
        
        // トランジションを実行（0から1への遷移）
        // SetIdlePose()とShowObjectsOnGameEnd()はトランジション完了後に実行される
        StartCoroutine(PlayEndTransition(won, hasNextRound));
    }
    
    /// <summary>
    /// ゲーム終了時のトランジションを実行（暗転→敵入れ替え、明転はCsvScenarioPlayerに委譲）
    /// </summary>
    /// <param name="won">勝利したかどうか</param>
    /// <param name="hasNextRound">次の勝負があるかどうか</param>
    private IEnumerator PlayEndTransition(bool won, bool hasNextRound)
    {
        // 結果表示を少し待ってからトランジションを開始
        yield return new WaitForSeconds(1.0f);
        
        TransitionController transition = gameUIManager?.GetMainTransition();
        if (transition != null)
        {
            // 次の勝負がある場合、暗転中に敵キャラを入れ替える（勝敗に関わらず）
            if (hasNextRound && currentRound < enemyAvatars.Length)
            {
                // まず暗転（0から1への遷移）
                bool darkTransitionComplete = false;
                transition.PlayFromBlack(() =>
                {
                    darkTransitionComplete = true;
                });
                
                // 暗転完了を待つ
                yield return new WaitUntil(() => darkTransitionComplete);
                
                // 暗転中に敵キャラを入れ替える
                yield return StartCoroutine(SwitchEnemyDuringTransition());
                
                // 入れ替え完了後、非表示にしていたオブジェクトを表示
                ShowObjectsOnGameEnd();
                
                // アバターを待機モーション（Idle）に戻す
                SetIdlePose();
                
                // 明転はCsvScenarioPlayerに委譲
                if (csvScenarioPlayer != null)
                {
                    csvScenarioPlayer.PlayFadeInTransition();
                }
                else
                {
                    Debug.LogWarning("[IaiReactionGame] CsvScenarioPlayerが設定されていません。明転を実行できません。");
                }
            }
            else
            {
                // 次の勝負がない場合、通常のトランジション（0から1への遷移）
                transition.PlayFromBlack(() =>
                {
                    // トランジション完了後にアバターを待機モーション（Idle）に戻す
                    SetIdlePose();
                    
                    // トランジション完了後に非表示にしたオブジェクトを表示
                    ShowObjectsOnGameEnd();
                });
            }
        }
        else
        {
            Debug.LogWarning("[IaiReactionGame] GameUIManagerまたはmainMenuToGameTransitionが設定されていません。");
            // トランジションが設定されていない場合も、Idleポーズに戻し、オブジェクトを表示
            SetIdlePose();
            ShowObjectsOnGameEnd();
        }
    }
    
    /// <summary>
    /// トランジション中（暗転中）に敵キャラを入れ替える
    /// </summary>
    private IEnumerator SwitchEnemyDuringTransition()
    {
        
        // 次の敵キャラに切り替え
        SetEnemyAvatar(currentRound);
        
        // 入れ替え処理が完了するまで少し待つ（視覚的な安定のため）
        yield return new WaitForSeconds(0.1f);
        
    }
    
    /// <summary>
    /// ゲームを停止する（外部から呼び出し可能）
    /// </summary>
    public void StopGame()
    {
        if (!isGameActive)
        {
            return;
        }
        
        isGameActive = false;
        IsGameActive = false; // 静的プロパティも更新
        isWaitingForClick = false;
        
        // ゲームコルーチンを停止
        if (gameCoroutine != null)
        {
            StopCoroutine(gameCoroutine);
            gameCoroutine = null;
        }
        
        // エフェクトを非表示
        HideEffect();
        
        // アバターを待機モーション（Idle）に戻す
        SetIdlePose();
        
        // インゲーム終了時に非表示にしたオブジェクトを表示
        ShowObjectsOnGameEnd();
        
    }
    
    private void Update()
    {
        // ゲーム中でクリック待ち状態の時、マウスクリックを検知
        if (isGameActive && isWaitingForClick && Input.GetMouseButtonDown(0))
        {
            OnReactionClick();
        }
    }
}

