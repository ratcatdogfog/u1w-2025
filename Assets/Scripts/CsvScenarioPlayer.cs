using System;
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

    private readonly List<Row> rows = new();
    private int index = 0;
    private string lastName = "";

    [Serializable]
    private class Row
    {
        public string name;
        public string line;
        public string action;
    }

    private void Awake()
    {
        if (csvFile == null)
        {
            Debug.LogError("csvFile が未設定です。TextAsset を割り当ててください。");
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
        ShowCurrentRow(); // 最初の表示
    }

    private void Update()
    {
        if (Input.GetKeyDown(nextKey))
        {
            index++;
            if (index >= rows.Count)
            {
                Debug.Log("シナリオ終了");
                return;
            }
            ShowCurrentRow();
        }
    }

    private void ShowCurrentRow()
    {
        var r = rows[index];

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

        if (nameTMP != null) nameTMP.text = name ?? "";
        if (lineTMP != null) lineTMP.text = line;

        // 3列目は DebugLog
        if (!string.IsNullOrWhiteSpace(r.action))
        {
            Debug.Log($"[行動指示] {r.action}");
        }
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

        // 3列に整形して Row 化
        var result = new List<Row>(table.Count);
        foreach (var r in table)
        {
            string c0 = r.Count > 0 ? r[0] : "";
            string c1 = r.Count > 1 ? r[1] : "";
            string c2 = r.Count > 2 ? r[2] : "";

            // 末尾の余計な空白だけ軽く整える（必要なければ削除OK）
            c0 = c0?.Trim();
            c2 = c2?.Trim();

            result.Add(new Row { name = c0, line = c1, action = c2 });
        }

        return result;
    }
}
