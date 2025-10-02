using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

// Author: kamadochoko

/// <summary>
/// CSVデータからScriptableObjectクラス／インスタンスを生成・同期するためのエディタ専用ユーティリティウィンドウ。
/// </summary>
/// <remarks>
/// USAGE:
/// 1. Unityメニューの「Tools/CSV → ScriptableObject Utility」から本ウィンドウを開く。
/// 2. 対象となるCSVファイルを指定し、出力フォルダ・名前空間・IIdentifiableSO実装の要否を設定する。
/// 3. 必要に応じて「Generate」「Import」「Export」各ボタンを押下し、ScriptableObjectコード生成／CSV⇔SO同期処理を実行する。
/// </remarks>
public class CsvSOUtility : EditorWindow
{
    /// <summary>入力元CSVファイル。</summary>
    private TextAsset csvFile;
    /// <summary>入力CSVのアセットパスキャッシュ。</summary>
    private string csvPath;
    /// <summary>コードや.assetを生成する出力フォルダ。</summary>
    private string outputFolder = "Assets/ScriptableObjects";
    /// <summary>生成クラスに付与する名前空間。</summary>
    private string generateNamespace = string.Empty;
    /// <summary>IIdentifiableSOインターフェースを実装するかどうか。</summary>
    private bool implementIIdentifiable = false;

    /// <summary>エディタメニューの登録。ウィンドウを表示する。</summary>
    [MenuItem("Tools/CSV → ScriptableObject Utility")]
    public static void ShowWindow()
    {
        GetWindow<CsvSOUtility>("CSV → SO Utility");
    }

    /// <summary>
    /// ウィンドウ描画と操作UIの構築。CSV指定・出力設定・操作ボタンをまとめて提供する。
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("CSV → ScriptableObject Utility", EditorStyles.boldLabel);
        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
        if (csvFile != null)
            csvPath = AssetDatabase.GetAssetPath(csvFile); // GUI描画ごとに最新アセットパスを保持。

        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        generateNamespace = EditorGUILayout.TextField("Namespace", generateNamespace);
        implementIIdentifiable = EditorGUILayout.Toggle("Implement IIdentifiableSO", implementIIdentifiable);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate ScriptableObject Class")) GenerateSOClass();
        if (GUILayout.Button("Import CSV to Single ScriptableObject")) ImportCsvToSingleSO();
        if (GUILayout.Button("Export Single ScriptableObject to CSV")) ExportSingleSOToCsv();
    }

    /// <summary>
    /// コメント列に含まれる制御文字やクォートをエスケープして、生成コードのXMLコメントに安全に埋め込む。
    /// </summary>
    private string EscapeComment(string str) =>
        str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "").Replace("\r", "");

    /// <summary>
    /// 指定CSVのヘッダ情報からScriptableObjectクラス定義コードを生成し、UTF-8(BOM付)で出力する。
    /// コメント行・ヘッダ行・型行の3行を前提に構造を組み立てる。
    /// </summary>
    private void GenerateSOClass()
    {
        if (string.IsNullOrEmpty(csvPath)) { EditorUtility.DisplayDialog("Error", "Please assign a CSV file.", "OK"); return; }
        var lines = File.ReadAllLines(csvPath, Encoding.GetEncoding("shift_jis"));
        if (lines.Length < 3) { EditorUtility.DisplayDialog("Error", "CSV must have at least 3 lines: comment, headers, and types.", "OK"); return; }

        var comments = SplitCsvLine(lines[0].TrimStart('#').Trim());
        var headers = SplitCsvLine(lines[1]);
        var types = SplitCsvLine(lines[2]);
        if (headers.Length != types.Length || headers.Length != comments.Length) { EditorUtility.DisplayDialog("Error", "Comment, header, and type counts must match.", "OK"); return; }

        var baseName = Path.GetFileNameWithoutExtension(csvPath).Replace(" ", "_");
        var className = baseName + "SO";
        var sb = new StringBuilder();
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        if (implementIIdentifiable)
            sb.AppendLine("using IIdentifiableNamespace; // Adjust namespace accordingly");
        if (!string.IsNullOrEmpty(generateNamespace)) { sb.AppendLine($"namespace {generateNamespace}"); sb.AppendLine("{"); }
        sb.AppendLine($"[CreateAssetMenu(fileName = \"{className}\", menuName = \"CSV SO/{className}\")] ");
        sb.AppendLine($"public class {className} : ScriptableObject{(implementIIdentifiable ? ", IIdentifiableSO" : "")}"); sb.AppendLine("{");
        sb.AppendLine("    [Serializable]"); sb.AppendLine("    public class Entry"); sb.AppendLine("    {");
        for (int i = 0; i < headers.Length; i++)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// {EscapeComment(comments[i])}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public {types[i]} {headers[i]};");
        }
        sb.AppendLine("    }");
        sb.AppendLine("    public List<Entry> entries = new List<Entry>();");

        if (implementIIdentifiable && headers.Contains("id"))
        {
            sb.AppendLine("    public int GetId() => entries != null && entries.Count > 0 ? entries[0].id : -1;");
        }

        sb.AppendLine("}");
        if (!string.IsNullOrEmpty(generateNamespace)) sb.AppendLine("}");

        Directory.CreateDirectory(outputFolder);
        var genPath = Path.Combine(outputFolder, className + ".cs");

        using (var writer = new StreamWriter(genPath, false, new UTF8Encoding(true)))
        {
            writer.Write(sb.ToString());
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"Generated {className}.cs at {outputFolder}", "OK");
    }

    /// <summary>
    /// CSVデータを既存のScriptableObject(.asset)へ取り込み、entries配列を書き換える。
    /// 型情報は3行目の宣言を参照し、SerializedObjectを介して安全に設定する。
    /// </summary>
    private void ImportCsvToSingleSO()
    {
        if (string.IsNullOrEmpty(csvPath)) { EditorUtility.DisplayDialog("Error", "Assign CSV first.", "OK"); return; }
        // var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        var lines = File.ReadAllLines(csvPath, Encoding.GetEncoding("shift_jis"));
        var headers = SplitCsvLine(lines[1]);
        var types = SplitCsvLine(lines[2]);
        var baseName = Path.GetFileNameWithoutExtension(csvPath).Replace(" ", "_");
        var className = baseName + "SO";
        var soType = GetTypeByName(className);
        if (soType == null) { EditorUtility.DisplayDialog("Error", $"Type {className} not found. Generate first.", "OK"); return; }

        var assetPath = Path.Combine(outputFolder, className + ".asset");
        var instance = AssetDatabase.LoadAssetAtPath(assetPath, soType) as ScriptableObject;
        if (instance == null)
        {
            Directory.CreateDirectory(outputFolder);
            instance = ScriptableObject.CreateInstance(soType);
            AssetDatabase.CreateAsset(instance, assetPath);
        }

        var so = new SerializedObject(instance);
        var entriesProp = so.FindProperty("entries");
        entriesProp.ClearArray();

        for (int r = 3; r < lines.Length; r++)
        {
            if (string.IsNullOrWhiteSpace(lines[r]) || lines[r].TrimStart().StartsWith("#")) continue;
            var values = SplitCsvLine(lines[r]);
            entriesProp.InsertArrayElementAtIndex(entriesProp.arraySize);
            var elem = entriesProp.GetArrayElementAtIndex(entriesProp.arraySize - 1);
            for (int c = 0; c < headers.Length && c < values.Length; c++)
            {
                var fieldProp = elem.FindPropertyRelative(headers[c]);
                if (fieldProp != null) SetSerializedProperty(fieldProp, values[c], types[c]);
            }
        }
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", "Imported CSV into single SO.", "OK");
    }

    /// <summary>
    /// ScriptableObjectで保持しているentries配列をCSV形式に書き出し、任意の保存先を選択してエクスポートする。
    /// </summary>
    private void ExportSingleSOToCsv()
    {
        if (string.IsNullOrEmpty(csvPath)) { EditorUtility.DisplayDialog("Error", "Assign CSV first.", "OK"); return; }
        var lines = new List<string>();
        var headers = new List<string>();
        var types = new List<string>();
        var baseName = Path.GetFileNameWithoutExtension(csvPath).Replace(" ", "_");
        var className = baseName + "SO";
        var soType = GetTypeByName(className);
        if (soType == null) { EditorUtility.DisplayDialog("Error", $"Type {className} not found.", "OK"); return; }

        var assetPath = Path.Combine(outputFolder, className + ".asset");
        var instance = AssetDatabase.LoadAssetAtPath(assetPath, soType) as ScriptableObject;
        if (instance == null) { EditorUtility.DisplayDialog("Error", "SO asset not found.", "OK"); return; }

        var so = new SerializedObject(instance);
        var entriesProp = so.FindProperty("entries");
        var entryType = soType.GetNestedType("Entry");
        var fields = entryType.GetFields();

        lines.Add("# Comment line. This file was exported from a ScriptableObject.");
        foreach (var f in fields) { headers.Add(f.Name); types.Add(f.FieldType.Name.ToLower()); }
        lines.Add(string.Join(",", headers));
        lines.Add(string.Join(",", types));

        for (int i = 0; i < entriesProp.arraySize; i++)
        {
            var elem = entriesProp.GetArrayElementAtIndex(i);
            var vals = new List<string>();
            foreach (var f in fields)
            {
                var prop = elem.FindPropertyRelative(f.Name);
                string strVal = f.FieldType == typeof(int) ? prop.intValue.ToString()
                                : f.FieldType == typeof(float) ? prop.floatValue.ToString()
                                : f.FieldType == typeof(bool) ? prop.boolValue.ToString()
                                : prop.stringValue;
                vals.Add(strVal);
            }
            lines.Add(string.Join(",", vals));
        }

        var outPath = EditorUtility.SaveFilePanel("Export CSV", string.Empty, baseName + ".csv", "csv");
        if (!string.IsNullOrEmpty(outPath)) File.WriteAllLines(outPath, lines, Encoding.UTF8);
        EditorUtility.DisplayDialog("Success", $"Exported CSV to {outPath}.", "OK");
    }

    /// <summary>
    /// 単純なカンマ区切り文字列を分解し、空白除去済みトークン配列を返す。
    /// </summary>
    private string[] SplitCsvLine(string line) => line.Split(',').Select(s => s.Trim()).ToArray();
    /// <summary>
    /// 現在ロードされているアセンブリから与えられた型名のTypeを探索する。
    /// </summary>
    private Type GetTypeByName(string name) => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.Name == name);

    /// <summary>
    /// 文字列で受け取ったセル値をSerializedPropertyへ型に応じて変換し反映するヘルパー。
    /// </summary>
    private static void SetSerializedProperty(SerializedProperty prop, string str, string typeName)
    {
        switch (typeName.ToLower())
        {
            case "int": prop.intValue = int.TryParse(str, out var i) ? i : 0; break;
            case "float": prop.floatValue = float.TryParse(str, out var f) ? f : 0f; break;
            case "bool": prop.boolValue = bool.TryParse(str, out var b) && b; break;
            default: prop.stringValue = str; break;
        }
    }
}
