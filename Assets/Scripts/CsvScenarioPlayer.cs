using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class CsvScenarioPlayer : MonoBehaviour
{
    [Header("CSV (TextAsset) をInspectorで割り当て")]
    [SerializeField] private TextAsset csvFile;

    [Header("出力先")]
    [SerializeField] private TMP_Text nameTMP;   // 1列目
    [SerializeField] private TMP_Text lineTMP;   // 2列目

    [Header("再生設定")]
    [SerializeField] private bool inheritLastNameWhenEmpty = true; // 1列目が空なら前の名前を引き継ぐ
    [SerializeField] private bool convertBrToNewline = false;      // <br> を改行に変換したい場合にON
    [SerializeField] private KeyCode nextKey = KeyCode.Space;      // 次へ進むキー
    [SerializeField] private bool autoStart = false;               // Awake時に自動で表示を開始するか
    [SerializeField] private bool forceDelayZero = false;         // delayを無条件で0秒にするかどうか
    
    [Header("タイプライター効果")]
    [SerializeField] private bool enableTypewriter = true;         // タイプライター効果を有効にする
    [SerializeField] private float typewriterDelay = 0.05f;          // 1文字表示の間隔（秒）
    
    [Header("外部連携")]
    [SerializeField] private ImageSwitcher imageSwitcher;           // 画面遷移終了を検知するためのImageSwitcher参照
    [SerializeField] private GameObject telopPanel;                  // テロップパネル（アクティブ判定用）
    [SerializeField] private ClickableIndicator clickableIndicator; // クリッカブルであることを示すマーク
    [SerializeField] private TMPHoverColor nameTMPHoverColor;       // 名前テキストのホバー検知用（未設定の場合は自動取得）
    [SerializeField] private TMPHoverColor lineTMPHoverColor;        // セリフテキストのホバー検知用（未設定の場合は自動取得）
    [SerializeField] private GameUIManager gameUIManager;           // GameUIManagerへの参照（明転用）
    
    [Header("選択肢")]
    [SerializeField] private GameObject choice1;                     // 選択肢1のオブジェクト
    [SerializeField] private GameObject choice2;                     // 選択肢2のオブジェクト

    private readonly List<Row> rows = new();
    private int index = 0;
    private string lastName = "";
    private float lastDelay = 0f;                                     // 最後に指定された間隔（秒数）
    private float currentRowDelay = 0f;                               // 現在の行の間隔指定（秒数）
    private bool currentRowEnableDelay = true;                        // 現在の行の待機有効化フラグ
    private bool isInitialized = false;
    private Coroutine typewriterCoroutine = null;
    private Coroutine delayCoroutine = null;                          // 間隔待機用のコルーチン
    private bool isTypewriting = false;
    private string currentFullText = "";                             // 現在表示中の全文テキスト
    private bool wasTelopPanelActive = false;                        // 前回のtelopPanelのアクティブ状態
    private bool wasTypewriting = false;                              // 前回のタイプライター状態

    [Serializable]
    private class Row
    {
        public string name;
        public string line;
        public bool showChoices;      // 選択肢表示可否
        public float delay;           // 次の文章への間隔指定（秒数）
        public string action;         // 備考
        public bool disableClick;     // クリック禁止
    }

    private void Awake()
    {
        // 必須参照のチェック
        if (clickableIndicator == null)
        {
            Debug.LogError("[CsvScenarioPlayer] clickableIndicatorが設定されていません。InspectorでClickableIndicatorコンポーネントを割り当ててください。", this);
        }

        // TMPHoverColorの自動取得（未設定の場合）
        if (nameTMPHoverColor == null && nameTMP != null)
        {
            nameTMPHoverColor = nameTMP.GetComponent<TMPHoverColor>();
        }
        if (lineTMPHoverColor == null && lineTMP != null)
        {
            lineTMPHoverColor = lineTMP.GetComponent<TMPHoverColor>();
        }

        // TMPのテキストをクリア
        if (nameTMP != null) nameTMP.text = "";
        if (lineTMP != null) lineTMP.text = "";

        // 選択肢を非表示にする
        if (choice1 != null) choice1.SetActive(false);
        if (choice2 != null) choice2.SetActive(false);

        if (csvFile == null)
        {
            Debug.LogError("[CsvScenarioPlayer] csvFile が未設定です。TextAsset を割り当ててください。", this);
            enabled = false;
            return;
        }

        rows.Clear();
        rows.AddRange(ParseCsv(csvFile.text));

        // 先頭行がヘッダなら捨てる（あなたの例に合わせて判定）
        if (rows.Count > 0 && rows[0].name == "キャラクター名" && rows[0].line == "セリフ")
            rows.RemoveAt(0);

        index = 0;
        lastName = "";
        lastDelay = 0f;
        isInitialized = true;

        // autoStartがtrueの場合のみ最初の表示を行う
        if (autoStart)
        {
            ShowCurrentRow();
        }
    }

    private void Start()
    {
        if (imageSwitcher != null)
        {
            SubscribeToImageSwitcher();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromImageSwitcher();
    }

    /// <summary>
    /// ImageSwitcherの画面遷移終了イベントを購読する
    /// </summary>
    private void SubscribeToImageSwitcher()
    {
        if (imageSwitcher != null)
        {
            imageSwitcher.onTransitionCompleted.AddListener(StartScenario);
        }
    }

    /// <summary>
    /// ImageSwitcherのイベント購読を解除する
    /// </summary>
    private void UnsubscribeFromImageSwitcher()
    {
        if (imageSwitcher != null)
        {
            imageSwitcher.onTransitionCompleted.RemoveListener(StartScenario);
        }
    }

    private void Update()
    {
        // 初期化されていない、または表示が開始されていない場合は処理しない
        if (!isInitialized || index < 0) return;

        // telopPanelのアクティブ状態が変化した時だけクリッカブル状態を更新
        bool telopPanelActive = telopPanel != null && telopPanel.activeSelf;
        if (telopPanelActive != wasTelopPanelActive)
        {
            wasTelopPanelActive = telopPanelActive;
            if (!telopPanelActive)
            {
                // telopPanelが非アクティブになった時は非表示にする
                NotifyClickable(false);
            }
            else
            {
                // telopPanelがアクティブになった時は状態を更新
                // ただし、間隔待機中（delayCoroutine != null）の場合は更新しない
                if (delayCoroutine == null)
                {
                    NotifyClickableIfReady();
                }
            }
        }

        // タイプライター状態が変化した時だけクリッカブル状態を更新
        // ただし、間隔待機中（delayCoroutine != null）の場合は更新しない
        if (isTypewriting != wasTypewriting)
        {
            wasTypewriting = isTypewriting;
            if (!isTypewriting && telopPanelActive && delayCoroutine == null)
            {
                // タイプライターが終了した時は状態を更新（間隔待機中でない場合のみ）
                NotifyClickableIfReady();
            }
        }

        // telopPanelがアクティブな時の左クリック処理
        if (telopPanel != null && telopPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            // IaiReactionGameが稼働中の場合はクリック処理をスキップ
            if (IaiReactionGame.IsGameActive)
            {
                return;
            }
            
            // TMPにホバー中の場合、クリック処理をスキップ
            if (IsAnyTMPHovering())
            {
                return;
            }

            // タイプライター効果中の場合、即座に全文を表示
            if (isTypewriting)
            {
                StopTypewriterAndShowFullText();
                return;
            }

            // 間隔待機中の場合はクリックを無視
            if (delayCoroutine != null)
            {
                return;
            }

            // クリック禁止のチェック
            if (index < rows.Count && rows[index].disableClick)
            {
                return;
            }

            // クリック可能な条件をチェック（NotifyClickableIfReadyと同じ条件）
            bool telopActive = telopPanel != null && telopPanel.activeSelf;
            bool hasNextRow = index < rows.Count - 1;
            bool canProceed = telopActive && !isTypewriting && hasNextRow;

            if (!canProceed)
            {
                return;
            }

            // タイプライター効果が終了し、間隔待機も終了している場合、次の行へ進む
            index++;
            if (index >= rows.Count)
            {
                return;
            }
            ShowCurrentRow();
        }

        // 従来のキーボード操作（telopPanelが非アクティブまたは未設定の場合も有効）
        if (Input.GetKeyDown(nextKey))
        {
            // タイプライター効果中の場合、即座に全文を表示
            if (isTypewriting)
            {
                StopTypewriterAndShowFullText();
                return;
            }

            // 間隔待機中の場合はキー入力を無視
            if (delayCoroutine != null)
            {
                return;
            }

            // クリック禁止のチェック
            if (index < rows.Count && rows[index].disableClick)
            {
                return;
            }
            index++;
            if (index >= rows.Count)
            {
                return;
            }
            ShowCurrentRow();
        }
    }

    /// <summary>
    /// シナリオの表示を開始する（ImageSwitcherの画面遷移終了時に自動的に呼び出される）
    /// </summary>
    private void StartScenario()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[CsvScenarioPlayer] 初期化が完了していません。");
            return;
        }

        // 最初の行を表示
        index = 0;
        ShowCurrentRow();
    }

    private void ShowCurrentRow()
    {
        var r = rows[index];

        string linePreview = string.IsNullOrEmpty(r.line) ? "" : (r.line.Length > 20 ? r.line.Substring(0, 20) + "..." : r.line);

        // 新しい行を表示するので、一旦クリック不可にする
        NotifyClickable(false);

        // 間隔指定の処理：空欄の場合は最後に指定された秒数を参照
        float delay = r.delay;
        if (delay < 0) // 空欄や無効な値の場合は前の値を保持
        {
            delay = lastDelay;
        }
        else
        {
            lastDelay = delay; // 有効な値の場合は更新
        }
        currentRowDelay = delay; // 現在の行の間隔指定を保持

        // 選択肢の表示/非表示制御
        if (r.showChoices)
        {
            if (choice1 != null) choice1.SetActive(true);
            if (choice2 != null) choice2.SetActive(true);
        }
        else
        {
            if (choice1 != null) choice1.SetActive(false);
            if (choice2 != null) choice2.SetActive(false);
        }

        // 即座に表示（間隔待機はタイプライター終了後に行う）
        ShowCurrentRowImmediate(r);
    }

    /// <summary>
    /// 行を即座に表示する（間隔待機後の処理）
    /// </summary>
    private void ShowCurrentRowImmediate(Row r)
    {
        // 1列目が空のときに引き継ぐ（あなたのCSV例で便利）
        string name = r.name;
        if (inheritLastNameWhenEmpty)
        {
            if (string.IsNullOrWhiteSpace(name)) name = lastName;
            else lastName = name;
        }

        string line = r.line ?? "";
        if (convertBrToNewline)
        {
            line = line.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
        }

        // 名前の表示処理：「モノローグ」の場合は何も表示しない
        if (nameTMP != null)
        {
            string nameToDisplay = name ?? "";
            if (nameToDisplay == "モノローグ")
            {
                nameTMP.text = "";
            }
            else
            {
                nameTMP.text = nameToDisplay;
            }
        }
        
        // タイプライター効果が有効な場合はコルーチンで表示、無効な場合は即座に表示
        if (lineTMP != null)
        {
            currentFullText = line; // 全文テキストを保持
            if (enableTypewriter && !string.IsNullOrEmpty(line))
            {
                StartTypewriter(line);
            }
            else
            {
                lineTMP.text = line;
                // タイプライター効果が無効な場合は即座に表示完了なので、間隔待機後にクリック可能にする
                StartDelayAndNotifyClickable();
            }
        }
        else
        {
            // lineTMPがnullの場合も間隔待機後にクリック可能にする
            StartDelayAndNotifyClickable();
        }
    }

    /// <summary>
    /// タイプライター効果でテキストを1文字ずつ表示する
    /// </summary>
    private void StartTypewriter(string fullText)
    {
        // 既存のタイプライター効果を停止
        StopTypewriter();
        
        // 新しいタイプライター効果を開始
        typewriterCoroutine = StartCoroutine(TypewriterCoroutine(fullText));
    }

    /// <summary>
    /// タイプライター効果を停止する（全文表示は行わない）
    /// </summary>
    private void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        isTypewriting = false;
    }

    /// <summary>
    /// タイプライター効果を停止し、全文を即座に表示する
    /// </summary>
    private void StopTypewriterAndShowFullText()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        isTypewriting = false;
        
        // 全文を即座に表示
        if (lineTMP != null && !string.IsNullOrEmpty(currentFullText))
        {
            lineTMP.text = currentFullText;
        }
        
        // 全文表示完了なので、間隔待機後にクリック可能にする
        StartDelayAndNotifyClickable();
    }

    /// <summary>
    /// タイプライター効果のコルーチン（<br>タグを考慮）
    /// </summary>
    private IEnumerator TypewriterCoroutine(string fullText)
    {
        isTypewriting = true;
        
        if (lineTMP == null)
        {
            isTypewriting = false;
            yield break;
        }

        StringBuilder displayedText = new StringBuilder();
        int i = 0;

        while (i < fullText.Length)
        {
            // <br>タグの検出（<br>, <br/>, <br />に対応）
            if (fullText[i] == '<' && i + 3 < fullText.Length)
            {
                // <br> のチェック
                if (fullText.Substring(i, 4) == "<br>")
                {
                    displayedText.Append("<br>");
                    i += 4;
                    lineTMP.text = displayedText.ToString();
                    yield return new WaitForSeconds(typewriterDelay);
                    continue;
                }
                
                // <br/> のチェック
                if (i + 5 < fullText.Length && fullText.Substring(i, 5) == "<br/>")
                {
                    displayedText.Append("<br/>");
                    i += 5;
                    lineTMP.text = displayedText.ToString();
                    yield return new WaitForSeconds(typewriterDelay);
                    continue;
                }
                
                // <br /> のチェック（スペースあり）
                if (i + 6 < fullText.Length && fullText.Substring(i, 6) == "<br />")
                {
                    displayedText.Append("<br />");
                    i += 6;
                    lineTMP.text = displayedText.ToString();
                    yield return new WaitForSeconds(typewriterDelay);
                    continue;
                }
            }

            // 通常の文字を1文字追加
            displayedText.Append(fullText[i]);
            lineTMP.text = displayedText.ToString();
            i++;
            yield return new WaitForSeconds(typewriterDelay);
        }

        isTypewriting = false;
        typewriterCoroutine = null;
        
        // タイプライター効果が終了したので、間隔待機後にクリック可能にする
        StartDelayAndNotifyClickable();
    }

    /// <summary>
    /// 間隔指定の秒数待機してからクリッカブルを表示する
    /// </summary>
    private void StartDelayAndNotifyClickable()
    {
        // 既存の待機コルーチンを停止
        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
        }

        // forceDelayZeroがtrueの場合は、delayの値に関係なく待機しない
        if (forceDelayZero)
        {
            NotifyClickableIfReady();
            return;
        }

        // 待機が有効で、かつ間隔指定がある場合のみ待機する
        if (currentRowEnableDelay && currentRowDelay > 0)
        {
            // 間隔指定がある場合は待機してからクリッカブルを表示
            delayCoroutine = StartCoroutine(DelayAndNotifyClickableCoroutine());
        }
        else
        {
            // 待機が無効、または間隔指定がない場合は即座にクリッカブルを表示
            NotifyClickableIfReady();
        }
    }

    /// <summary>
    /// 間隔指定の秒数待機してからクリッカブルを表示するコルーチン
    /// </summary>
    private IEnumerator DelayAndNotifyClickableCoroutine()
    {
        yield return new WaitForSeconds(currentRowDelay);
        delayCoroutine = null;
        NotifyClickableIfReady();
    }

    /// <summary>
    /// 改行を含む "..." セルにも対応した簡易CSVパーサ
    /// - 区切り: ,（カンマ）
    /// - 囲み: "（ダブルクォート）
    /// - エスケープ: "" → "
    /// - 改行: \n / \r\n どちらも対応
    /// </summary>
    private static List<Row> ParseCsv(string csvText)
    {
        var table = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();

        bool inQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // "" → "
                    bool isEscapedQuote = (i + 1 < csvText.Length && csvText[i + 1] == '"');
                    if (isEscapedQuote)
                    {
                        cell.Append('"');
                        i++; // 次の " を消費
                    }
                    else
                    {
                        inQuotes = false; // クォート終了
                    }
                }
                else
                {
                    cell.Append(c); // クォート内はそのまま
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true; // クォート開始
                }
                else if (c == ',')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                }
                else if (c == '\r')
                {
                    // \r\n の \r は無視して次へ（\nで確定させる）
                    continue;
                }
                else if (c == '\n')
                {
                    row.Add(cell.ToString());
                    cell.Clear();

                    table.Add(row);
                    row = new List<string>();
                }
                else
                {
                    cell.Append(c);
                }
            }
        }

        // 最後のセル/行
        row.Add(cell.ToString());
        table.Add(row);

            // 6列に整形して Row 化
        var result = new List<Row>(table.Count);
        float lastDelayValue = 0f; // 最後に指定された間隔を保持
        
        foreach (var r in table)
        {
            string c0 = r.Count > 0 ? r[0] : "";  // 名前
            string c1 = r.Count > 1 ? r[1] : "";  // 台詞
            string c2 = r.Count > 2 ? r[2] : "";  // 選択肢表示可否
            string c3 = r.Count > 3 ? r[3] : "";  // 間隔指定
            string c4 = r.Count > 4 ? r[4] : "";  // 備考
            string c5 = r.Count > 5 ? r[5] : "";  // クリック禁止

            // 末尾の余計な空白だけ軽く整える
            c0 = c0?.Trim();
            c4 = c4?.Trim();

            // 選択肢表示可否の判定：記載なしはfalse、1はtrue
            bool showChoices = false;
            if (!string.IsNullOrWhiteSpace(c2))
            {
                showChoices = c2.Trim() == "1";
            }

            // 間隔指定の判定：空欄の場合は最後の値を参照、有効な値の場合は更新
            float delay = -1f; // デフォルトは無効値
            if (!string.IsNullOrWhiteSpace(c3))
            {
                if (float.TryParse(c3.Trim(), out float parsedDelay))
                {
                    delay = parsedDelay;
                    lastDelayValue = delay; // 有効な値の場合は更新
                }
            }
            else
            {
                // 空欄の場合は最後に指定された秒数を参照
                delay = lastDelayValue;
            }

            // クリック禁止の判定：記載なしはfalse、1はtrue
            bool disableClick = false;
            if (!string.IsNullOrWhiteSpace(c5))
            {
                disableClick = c5.Trim() == "1";
            }

            result.Add(new Row 
            { 
                name = c0, 
                line = c1, 
                showChoices = showChoices,
                delay = delay,
                action = c4,
                disableClick = disableClick
            });
        }

        return result;
    }


    /// <summary>
    /// クリック可能な状態をClickableIndicatorに伝達する（準備ができている場合のみ）
    /// </summary>
    private void NotifyClickableIfReady()
    {
        if (clickableIndicator == null)
        {
            return;
        }

        // ImageSwitcherが最後の画像を表示していて、かつtargetPanelがアクティブな場合は、CsvScenarioPlayerからの表示命令を無視する
        // targetPanelが非アクティブな時（画面遷移開始後）は、CsvScenarioPlayerが動作するので表示命令を許可する
        if (imageSwitcher != null && imageSwitcher.IsLastSprite() && imageSwitcher.IsPanelActive())
        {
            return;
        }

        // クリック可能な条件：
        // 1. telopPanelがアクティブ
        // 2. タイプライター効果が終了している（isTypewriting == false）
        // 3. 間隔待機が終了している（delayCoroutine == null）
        // 4. 次の行が存在する（index < rows.Count - 1）
        // 5. クリック禁止が設定されていない（rows[index].disableClick == false）
        bool telopActive = telopPanel != null && telopPanel.activeSelf;
        bool hasNextRow = index < rows.Count - 1;
        bool isWaitingDelay = delayCoroutine != null;
        bool isClickDisabled = index < rows.Count && rows[index].disableClick;
        bool isClickable = telopActive && !isTypewriting && !isWaitingDelay && hasNextRow && !isClickDisabled;
        
        clickableIndicator.SetClickable(isClickable);
    }

    /// <summary>
    /// クリック可能な状態をClickableIndicatorに直接伝達する
    /// </summary>
    private void NotifyClickable(bool clickable)
    {
        if (clickableIndicator != null)
        {
            clickableIndicator.SetClickable(clickable);
        }
    }

    /// <summary>
    /// nameTMPまたはlineTMPのいずれかがホバー中かどうかを判定する
    /// </summary>
    private bool IsAnyTMPHovering()
    {
        return (nameTMPHoverColor != null && nameTMPHoverColor.IsHovering) ||
               (lineTMPHoverColor != null && lineTMPHoverColor.IsHovering);
    }

    /// <summary>
    /// choice1が選択されたときに呼び出す（ButtonのOnClickから呼び出す想定）
    /// クリック禁止行が見つかるまで（空行も含めて）スキップし、そのクリック禁止行の次の行から表示を開始する
    /// </summary>
    public void OnChoice1Selected()
    {
        if (!isInitialized || index < 0 || index >= rows.Count)
        {
            return;
        }

        // 選択肢を非表示にする
        if (choice1 != null) choice1.SetActive(false);
        if (choice2 != null) choice2.SetActive(false);

        // タイプライター効果を停止
        StopTypewriter();

        // 間隔待機を停止
        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
            delayCoroutine = null;
        }

        // 次の行へ進む
        index++;
        if (index >= rows.Count)
        {
            return;
        }

        // クリック禁止行が見つかるまでスキップ（空行も含めて）
        while (index < rows.Count && !rows[index].disableClick)
        {
            index++;
        }

        // クリック禁止行が見つかった場合、その次の行へ進む
        if (index < rows.Count && rows[index].disableClick)
        {
            index++;
        }

        // 表示開始行が存在するか確認
        if (index >= rows.Count)
        {
            return;
        }

        ShowCurrentRow();
    }

    /// <summary>
    /// 明転トランジションを実行する（IaiReactionGameから呼び出される）
    /// </summary>
    public void PlayFadeInTransition()
    {
        if (gameUIManager == null)
        {
            Debug.LogWarning("[CsvScenarioPlayer] GameUIManagerが設定されていません。明転を実行できません。");
            return;
        }

        TransitionController transition = gameUIManager.GetMainTransition();
        if (transition != null)
        {
            // 明転（1から0への遷移）
            transition.PlayToBlack(() =>
            {
                // 明転完了後の処理は必要に応じて追加
            });
        }
        else
        {
            Debug.LogWarning("[CsvScenarioPlayer] TransitionControllerが取得できませんでした。");
        }
    }
}
