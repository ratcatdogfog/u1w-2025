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
    [SerializeField] private GameObject playerAvatar;      // プレイヤーのアバター（最初のキャラ、シーンに配置）
    [SerializeField] private GameObject playerAvatarReplacement;   // プレイヤーのアバター（二回目以降の差し替え用、シーンに配置して非表示にしておく）
    [SerializeField] private GameObject[] enemyAvatars;   // 敵のアバター（順番に使用）
    
    [Header("アニメーター設定")]
    [SerializeField] private Animator playerAnimator;      // プレイヤーのアニメーター（最初のキャラ）
    [SerializeField] private Animator playerAnimatorReplacement;   // プレイヤーのアニメーター（二回目以降の差し替え用）
    [SerializeField] private Animator[] enemyAnimators;    // 敵のアニメーター（順番に使用）
    
    [Header("エフェクト設定")]
    [SerializeField] private GameObject reactionEffect;    // 反応すべきエフェクト（クリックタイミング）
    [SerializeField] private float effectDelayMin = 1.0f;   // エフェクト表示までの最小遅延時間（秒）
    [SerializeField] private float effectDelayMax = 3.0f;   // エフェクト表示までの最大遅延時間（秒）
    
    [Header("ゲーム設定")]
    [SerializeField] private float successThreshold = 0.5f; // 成功判定の反応速度閾値（秒）
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
    
    [Header("フラッシュ後の位置・角度設定（ローカル座標）")]
    [SerializeField] private Vector3 playerPositionAfterFlash;  // フラッシュ後のプレイヤー位置（ローカル座標）
    [SerializeField] private Vector3 playerRotationAfterFlash;  // フラッシュ後のプレイヤー角度（ローカル座標、Euler角）
    [SerializeField] private Vector3[] enemyPositionsAfterFlash;   // フラッシュ後の敵位置（ローカル座標、各敵キャラごと）
    [SerializeField] private Vector3[] enemyRotationsAfterFlash;  // フラッシュ後の敵角度（ローカル座標、Euler角、各敵キャラごと）
    
    [Header("音效設定")]
    [SerializeField] private AudioClip iaiStartSE;         // 居合開始時のSE
    [SerializeField] private AudioClip effectSE;          // エフェクト表示時のSE
    [SerializeField] private AudioClip flashSE;            // フラッシュ発生時のSE
    
    [Header("SE音量設定（0.0～1.0）")]
    [Range(0f, 1f)] [SerializeField] private float iaiStartSEVolume = 1.0f;  // 居合開始時SEの音量
    [Range(0f, 1f)] [SerializeField] private float effectSEVolume = 1.0f;    // エフェクト表示時SEの音量
    [Range(0f, 1f)] [SerializeField] private float flashSEVolume = 1.0f;     // フラッシュ発生時SEの音量
    
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
    private Coroutine endTransitionCoroutine = null;       // 終了トランジション用コルーチン
    private bool roundSuccess = false;                     // ラウンドが成功したかどうか
    private int currentRound = 0;                          // 現在の勝負回数
    private GameObject currentPlayerAvatar;                 // 現在使用中のプレイヤーアバター
    private Animator currentPlayerAnimator;                 // 現在使用中のプレイヤーアニメーター
    private GameObject currentEnemyAvatar;                 // 現在使用中の敵アバター
    private Animator currentEnemyAnimator;                 // 現在使用中の敵アニメーター
    
    // 初期位置・角度の保存用
    private Vector3 initialPlayerPosition;                 // プレイヤーの初期位置（最初のキャラ）
    private Quaternion initialPlayerRotation;               // プレイヤーの初期角度（最初のキャラ）
    private Vector3 initialPlayerReplacementPosition;       // プレイヤーの初期位置（差し替え用キャラ）
    private Quaternion initialPlayerReplacementRotation;     // プレイヤーの初期角度（差し替え用キャラ）
    private Vector3[] initialEnemyPositions;               // 敵の初期位置（配列）
    private Quaternion[] initialEnemyRotations;            // 敵の初期角度（配列）
    
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
        
        // 最初のプレイヤーキャラを設定
        if (playerAvatar != null)
        {
            currentPlayerAvatar = playerAvatar;
            currentPlayerAnimator = playerAnimator;
        }
        
        // 差し替え用プレイヤーキャラを初期状態で非表示にする（シーンに配置されている場合）
        if (playerAvatarReplacement != null)
        {
            playerAvatarReplacement.SetActive(false);
        }
        
        // 初期位置・角度を保存
        SaveInitialTransform();
    }
    
    /// <summary>
    /// 初期位置・角度を保存する（ローカル座標）
    /// </summary>
    private void SaveInitialTransform()
    {
        // プレイヤーの初期位置・角度を保存（ローカル座標）
        if (playerAvatar != null)
        {
            initialPlayerPosition = playerAvatar.transform.localPosition;
            initialPlayerRotation = playerAvatar.transform.localRotation;
        }
        
        // プレイヤーキャラ差し替え用の初期位置・角度を保存（ローカル座標）
        if (playerAvatarReplacement != null)
        {
            initialPlayerReplacementPosition = playerAvatarReplacement.transform.localPosition;
            initialPlayerReplacementRotation = playerAvatarReplacement.transform.localRotation;
        }
        
        // 敵の初期位置・角度を保存（ローカル座標）
        if (enemyAvatars != null && enemyAvatars.Length > 0)
        {
            initialEnemyPositions = new Vector3[enemyAvatars.Length];
            initialEnemyRotations = new Quaternion[enemyAvatars.Length];
            
            for (int i = 0; i < enemyAvatars.Length; i++)
            {
                if (enemyAvatars[i] != null)
                {
                    initialEnemyPositions[i] = enemyAvatars[i].transform.localPosition;
                    initialEnemyRotations[i] = enemyAvatars[i].transform.localRotation;
                }
            }
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
    /// 居合ゲームをスキップして終了処理のみを実行する（choice2のbuttonのOnClickから呼び出す）
    /// 暗転、敵キャラの入れ替え、明転などの終了処理を実行する
    /// </summary>
    public void SkipGameAndPlayTransition()
    {
        if (isGameActive)
        {
            return;
        }
        
        // 既に終了トランジションが実行中の場合はスキップ
        if (endTransitionCoroutine != null)
        {
            return;
        }
        
        // 最初の勝負の場合は勝負回数をリセットして最初の敵キャラを設定
        if (currentRound == 0)
        {
            Debug.Log($"[IaiReactionGame] SkipGameAndPlayTransition() 最初の勝負のため、敵キャラ0を設定");
            SetEnemyAvatar(0);
        }
        
        // インゲーム開始時に非表示にするオブジェクトを非表示
        HideObjectsOnGameStart();
        
        // ゲームをスキップしたので、負けとして扱う
        bool won = false;
        
        // 次の敵がいるかどうかを判定（currentRoundはまだインクリメントしていない）
        // GameLoop()と同じように、PlayEndTransition()内でcurrentRoundをインクリメントする
        bool hasNextRound = (currentRound + 1) < enemyAvatars.Length;
        Debug.Log($"[IaiReactionGame] SkipGameAndPlayTransition() currentRound={currentRound}, hasNextRound={hasNextRound}, 次の敵インデックス={(currentRound + 1)}");
        
        // 終了処理を実行（暗転、敵キャラの入れ替え、明転など）
        // PlayEndTransition()内でcurrentRoundをインクリメントする（incrementRound=true）
        endTransitionCoroutine = StartCoroutine(PlayEndTransition(won, hasNextRound, true));
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
        if (currentPlayerAnimator != null && currentPlayerAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(pauseTrigger))
        {
            currentPlayerAnimator.SetTrigger(pauseTrigger);
        }
        
        if (currentEnemyAnimator != null && currentEnemyAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(pauseTrigger))
        {
            currentEnemyAnimator.SetTrigger(pauseTrigger);
        }
        
        // 居合開始時のSEを再生
        if (AudioManager.Instance != null && iaiStartSE != null)
        {
            AudioManager.Instance.PlaySE(iaiStartSE, iaiStartSEVolume);
        }
        
    }
    
    /// <summary>
    /// 待機モーション（Idle）に戻す
    /// </summary>
    private void SetIdlePose()
    {
        if (currentPlayerAnimator != null && currentPlayerAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(idleTrigger))
        {
            currentPlayerAnimator.SetTrigger(idleTrigger);
        }
        
        if (currentEnemyAnimator != null && currentEnemyAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(idleTrigger))
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
            AudioManager.Instance.PlaySE(effectSE, effectSEVolume);
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
            if (currentPlayerAnimator != null && currentPlayerAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(finishTrigger))
            {
                currentPlayerAnimator.SetTrigger(finishTrigger);
            }
            if (currentEnemyAnimator != null && currentEnemyAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(yarareTrigger))
            {
                currentEnemyAnimator.SetTrigger(yarareTrigger);
            }
        }
        else
        {
            // プレイヤーが負けた：プレイヤー→Yarare、敵→Finish
            if (currentPlayerAnimator != null && currentPlayerAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(yarareTrigger))
            {
                currentPlayerAnimator.SetTrigger(yarareTrigger);
            }
            if (currentEnemyAnimator != null && currentEnemyAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(finishTrigger))
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
            AudioManager.Instance.PlaySE(flashSE, flashSEVolume);
        }
        
        if (flashImage != null)
        {
            flashImage.gameObject.SetActive(true);
            yield return new WaitForSeconds(flashDuration);
            flashImage.gameObject.SetActive(false);
        }
        
        // フラッシュ後に位置・角度を変更
        ChangePositionAfterFlash();
    }
    
    /// <summary>
    /// フラッシュ後にキャラクターの位置・角度を変更する（ローカル座標）
    /// </summary>
    private void ChangePositionAfterFlash()
    {
        // プレイヤーの位置・角度を変更（ローカル座標）
        if (currentPlayerAvatar != null)
        {
            currentPlayerAvatar.transform.localPosition = playerPositionAfterFlash;
            currentPlayerAvatar.transform.localRotation = Quaternion.Euler(playerRotationAfterFlash);
        }
        
        // 現在の敵の位置・角度を変更（ローカル座標）
        if (currentEnemyAvatar != null)
        {
            // 現在のラウンドに対応する敵キャラの位置・角度を取得
            int enemyIndex = currentRound;
            
            // 位置の設定（範囲外の場合は最後の要素を参照）
            if (enemyPositionsAfterFlash != null && enemyPositionsAfterFlash.Length > 0)
            {
                int positionIndex = enemyIndex;
                if (positionIndex < 0 || positionIndex >= enemyPositionsAfterFlash.Length)
                {
                    // 範囲外の場合は最後の要素を使用
                    positionIndex = enemyPositionsAfterFlash.Length - 1;
                }
                currentEnemyAvatar.transform.localPosition = enemyPositionsAfterFlash[positionIndex];
            }
            
            // 角度の設定（範囲外の場合は最後の要素を参照）
            if (enemyRotationsAfterFlash != null && enemyRotationsAfterFlash.Length > 0)
            {
                int rotationIndex = enemyIndex;
                if (rotationIndex < 0 || rotationIndex >= enemyRotationsAfterFlash.Length)
                {
                    // 範囲外の場合は最後の要素を使用
                    rotationIndex = enemyRotationsAfterFlash.Length - 1;
                }
                currentEnemyAvatar.transform.localRotation = Quaternion.Euler(enemyRotationsAfterFlash[rotationIndex]);
            }
        }
    }
    
    /// <summary>
    /// キャラクターを初期位置・角度に戻す（ローカル座標）
    /// </summary>
    private void ResetToInitialPosition()
    {
        // 現在のプレイヤーを初期位置・角度に戻す（ローカル座標）
        if (currentPlayerAvatar != null)
        {
            // 確実に表示されるようにする
            if (!currentPlayerAvatar.activeSelf)
            {
                currentPlayerAvatar.SetActive(true);
            }
            
            // 最初のキャラの場合は初期位置を使用
            if (currentPlayerAvatar == playerAvatar)
            {
                currentPlayerAvatar.transform.localPosition = initialPlayerPosition;
                currentPlayerAvatar.transform.localRotation = initialPlayerRotation;
            }
            else if (currentPlayerAvatar == playerAvatarReplacement)
            {
                // 差し替え用キャラの場合は、差し替え用の初期位置を使用
                currentPlayerAvatar.transform.localPosition = initialPlayerReplacementPosition;
                currentPlayerAvatar.transform.localRotation = initialPlayerReplacementRotation;
            }
        }
        
        // 現在の敵を初期位置・角度に戻す（ローカル座標）
        if (currentEnemyAvatar != null)
        {
            // 確実に表示されるようにする
            if (!currentEnemyAvatar.activeSelf)
            {
                currentEnemyAvatar.SetActive(true);
            }
            
            // 現在の敵キャラのインデックスを取得
            int enemyIndex = -1;
            if (enemyAvatars != null)
            {
                for (int i = 0; i < enemyAvatars.Length; i++)
                {
                    if (enemyAvatars[i] == currentEnemyAvatar)
                    {
                        enemyIndex = i;
                        break;
                    }
                }
            }
            
            // 初期位置・角度を設定
            if (enemyIndex >= 0 && initialEnemyPositions != null && initialEnemyRotations != null && 
                enemyIndex < initialEnemyPositions.Length && enemyIndex < initialEnemyRotations.Length)
            {
                currentEnemyAvatar.transform.localPosition = initialEnemyPositions[enemyIndex];
                currentEnemyAvatar.transform.localRotation = initialEnemyRotations[enemyIndex];
            }
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
    /// プレイヤーキャラを差し替え用キャラに設定する
    /// </summary>
    private void SetPlayerAvatar()
    {
        // 差し替え用キャラが設定されていない場合は何もしない
        if (playerAvatarReplacement == null)
        {
            return;
        }
        
        // 現在のプレイヤーキャラを非表示
        if (currentPlayerAvatar != null && currentPlayerAvatar != playerAvatarReplacement)
        {
            currentPlayerAvatar.SetActive(false);
        }
        
        // 新しいプレイヤーキャラを設定
        currentPlayerAvatar = playerAvatarReplacement;
        if (currentPlayerAvatar != null)
        {
            // 確実に表示されるようにする
            currentPlayerAvatar.SetActive(true);
            
            // 保存されている初期位置・角度を初期設定として使用（本当の初期位置）
            initialPlayerReplacementPosition = initialPlayerPosition;
            initialPlayerReplacementRotation = initialPlayerRotation;
            
            // 入れ替え後のGameObjectを初期位置・角度に設定
            currentPlayerAvatar.transform.localPosition = initialPlayerPosition;
            currentPlayerAvatar.transform.localRotation = initialPlayerRotation;
            
            // アニメーターを取得
            if (playerAnimatorReplacement != null)
            {
                currentPlayerAnimator = playerAnimatorReplacement;
            }
            else
            {
                // アニメーターが設定されていない場合は、GameObjectから取得を試みる
                currentPlayerAnimator = currentPlayerAvatar.GetComponent<Animator>();
            }
        }
    }
    
    /// <summary>
    /// 敵キャラを設定する（指定されたインデックスの敵を使用）
    /// </summary>
    private void SetEnemyAvatar(int enemyIndex)
    {
        Debug.Log($"[IaiReactionGame] SetEnemyAvatar() 開始: enemyIndex={enemyIndex}, enemyAvatars.Length={enemyAvatars?.Length ?? 0}");
        
        // 範囲チェック
        if (enemyAvatars == null || enemyIndex < 0 || enemyIndex >= enemyAvatars.Length)
        {
            Debug.LogWarning($"[IaiReactionGame] SetEnemyAvatar() 範囲外: enemyIndex={enemyIndex}, enemyAvatars.Length={enemyAvatars?.Length ?? 0}");
            return;
        }
        
        // 現在の敵キャラを非表示
        if (currentEnemyAvatar != null)
        {
            Debug.Log($"[IaiReactionGame] SetEnemyAvatar() 現在の敵キャラを非表示: {currentEnemyAvatar.name}");
            currentEnemyAvatar.SetActive(false);
        }
        
        // 新しい敵キャラを設定
        currentEnemyAvatar = enemyAvatars[enemyIndex];
        if (currentEnemyAvatar != null)
        {
            Debug.Log($"[IaiReactionGame] SetEnemyAvatar() 新しい敵キャラを設定: enemyIndex={enemyIndex}, name={currentEnemyAvatar.name}");
            currentEnemyAvatar.SetActive(true);
            
            // アニメーターを取得
            if (enemyAnimators != null && enemyIndex < enemyAnimators.Length)
            {
                currentEnemyAnimator = enemyAnimators[enemyIndex];
                Debug.Log($"[IaiReactionGame] SetEnemyAvatar() アニメーターを設定: enemyIndex={enemyIndex}");
            }
            else
            {
                // アニメーター配列が設定されていない場合は、GameObjectから取得を試みる
                currentEnemyAnimator = currentEnemyAvatar.GetComponent<Animator>();
                Debug.Log($"[IaiReactionGame] SetEnemyAvatar() アニメーターをGameObjectから取得: {currentEnemyAnimator != null}");
            }
        }
        else
        {
            Debug.LogWarning($"[IaiReactionGame] SetEnemyAvatar() 敵キャラがnull: enemyIndex={enemyIndex}");
        }
        
        Debug.Log($"[IaiReactionGame] SetEnemyAvatar() 完了: enemyIndex={enemyIndex}");
    }
    
    /// <summary>
    /// ゲーム終了処理
    /// </summary>
    /// <param name="won">勝利したかどうか</param>
    /// <param name="hasNextRound">次の勝負があるかどうか（勝った場合のみ）</param>
    private void EndGame(bool won, bool hasNextRound = false)
    {
        Debug.Log($"[IaiReactionGame] EndGame() 開始: won={won}, hasNextRound={hasNextRound}, endTransitionCoroutine={endTransitionCoroutine != null}");
        
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
        
        // 既に終了トランジションが実行中の場合はスキップ
        if (endTransitionCoroutine != null)
        {
            Debug.LogWarning($"[IaiReactionGame] EndGame() 既に終了トランジションが実行中のため、処理をスキップします");
            return;
        }
        
        // トランジションを実行（0から1への遷移）
        // SetIdlePose()とShowObjectsOnGameEnd()はトランジション完了後に実行される
        endTransitionCoroutine = StartCoroutine(PlayEndTransition(won, hasNextRound));
    }
    
    /// <summary>
    /// ゲーム終了時のトランジションを実行（暗転→敵入れ替え、明転はCsvScenarioPlayerに委譲）
    /// SkipGameAndPlayTransition()から呼び出される場合、currentRoundをインクリメントする
    /// </summary>
    /// <param name="won">勝利したかどうか</param>
    /// <param name="hasNextRound">次の勝負があるかどうか</param>
    /// <param name="incrementRound">currentRoundをインクリメントするかどうか（SkipGameAndPlayTransition()から呼び出される場合のみtrue）</param>
    private IEnumerator PlayEndTransition(bool won, bool hasNextRound, bool incrementRound = false)
    {
        Debug.Log($"[IaiReactionGame] PlayEndTransition() 開始: won={won}, hasNextRound={hasNextRound}, incrementRound={incrementRound}, currentRound={currentRound}");
        
        try
        {
            // SkipGameAndPlayTransition()から呼び出された場合、currentRoundをインクリメント
            if (incrementRound)
            {
                int oldRound = currentRound;
                currentRound++;
                Debug.Log($"[IaiReactionGame] PlayEndTransition() currentRoundをインクリメント: {oldRound} → {currentRound}");
            }
            
            // 結果表示を少し待ってからトランジションを開始
            yield return new WaitForSeconds(1.0f);
            
            TransitionController transition = gameUIManager?.GetMainTransition();
            if (transition != null)
            {
                // 次の勝負がある場合、暗転中に敵キャラを入れ替える（勝敗に関わらず）
                if (hasNextRound && currentRound < enemyAvatars.Length)
                {
                    Debug.Log($"[IaiReactionGame] PlayEndTransition() 次の勝負があるため、敵キャラを入れ替えます: currentRound={currentRound}, enemyAvatars.Length={enemyAvatars.Length}");
                    
                    // まず暗転（0から1への遷移）
                    bool darkTransitionComplete = false;
                    transition.PlayFromBlack(() =>
                    {
                        darkTransitionComplete = true;
                    });
                    
                    // 暗転完了を待つ
                    yield return new WaitUntil(() => darkTransitionComplete);
                    
                    // 二回目の居合が終わった時（currentRound == 2）にプレイヤーキャラを差し替え
                    // 差し替え用キャラが設定されている場合のみ実行
                    if (currentRound == 2 && playerAvatarReplacement != null)
                    {
                        Debug.Log($"[IaiReactionGame] PlayEndTransition() プレイヤーキャラを差し替えます: currentRound={currentRound}");
                        // プレイヤーキャラを差し替え
                        yield return StartCoroutine(SwitchPlayerDuringTransition());
                    }
                    
                    // 暗転中に敵キャラを入れ替える
                    Debug.Log($"[IaiReactionGame] PlayEndTransition() 敵キャラを入れ替えます: currentRound={currentRound}");
                    yield return StartCoroutine(SwitchEnemyDuringTransition());
                    
                    // キャラクター差し替え後、初期位置・角度に戻す
                    ResetToInitialPosition();
                    
                    // 入れ替え完了後、非表示にしていたオブジェクトを表示
                    ShowObjectsOnGameEnd();
                    
                    // アバターを待機モーション（Idle）に戻す
                    SetIdlePose();
                    
                    // 明転はCsvScenarioPlayerに委譲
                    if (csvScenarioPlayer != null)
                    {
                        csvScenarioPlayer.PlayFadeInTransition();
                    }
                }
                else
                {
                    // 次の勝負がない場合、通常のトランジション（0から1への遷移）
                    bool darkTransitionComplete = false;
                    transition.PlayFromBlack(() =>
                    {
                        darkTransitionComplete = true;
                    });
                    
                    // 暗転完了を待つ
                    yield return new WaitUntil(() => darkTransitionComplete);
                    
                    // 暗転後に初期位置・角度に戻す
                    ResetToInitialPosition();
                    
                    // トランジション完了後にアバターを待機モーション（Idle）に戻す
                    SetIdlePose();
                    
                    // トランジション完了後に非表示にしたオブジェクトを表示
                    ShowObjectsOnGameEnd();
                }
            }
            else
            {
                // トランジションが設定されていない場合も、初期位置に戻し、Idleポーズに戻し、オブジェクトを表示
                ResetToInitialPosition();
                SetIdlePose();
                ShowObjectsOnGameEnd();
            }
        }
        finally
        {
            // コルーチン終了時にフラグをリセット
            endTransitionCoroutine = null;
            Debug.Log($"[IaiReactionGame] PlayEndTransition() 完了: endTransitionCoroutineをリセット");
        }
    }
    
    /// <summary>
    /// トランジション中（暗転中）に敵キャラを入れ替える
    /// </summary>
    private IEnumerator SwitchEnemyDuringTransition()
    {
        Debug.Log($"[IaiReactionGame] SwitchEnemyDuringTransition() 開始: currentRound={currentRound}, enemyAvatars.Length={enemyAvatars?.Length ?? 0}");
        
        // 次の敵キャラに切り替え
        Debug.Log($"[IaiReactionGame] SwitchEnemyDuringTransition() SetEnemyAvatar({currentRound}) を呼び出します");
        SetEnemyAvatar(currentRound);
        
        // 入れ替え処理が完了するまで少し待つ（視覚的な安定のため）
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log($"[IaiReactionGame] SwitchEnemyDuringTransition() 完了");
    }
    
    /// <summary>
    /// トランジション中（暗転中）にプレイヤーキャラを入れ替える
    /// </summary>
    private IEnumerator SwitchPlayerDuringTransition()
    {
        // プレイヤーキャラを差し替え用キャラに切り替え
        SetPlayerAvatar();
        
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
        
        // 初期位置・角度に戻す
        ResetToInitialPosition();
        
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

