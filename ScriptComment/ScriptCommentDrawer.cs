using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ScriptCommentDrawer : UnityEditor.AssetModificationProcessor
{
    static ScriptCommentDrawer()
    {
        EditorApplication.projectWindowItemOnGUI += DisplayCommentInProjectView;
    }

    private static void DisplayCommentInProjectView(string guid, Rect selectionRect)
    {
        // スクリプトのアセットパスを取得
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);

        // スクリプトファイルであるかを確認
        if (!assetPath.EndsWith(".cs"))
            return;

        // スクリプトの内容をUTF-8形式で読み込む
        string scriptContents;
        using (System.IO.StreamReader reader = new System.IO.StreamReader(assetPath, System.Text.Encoding.UTF8))
        {
            scriptContents = reader.ReadToEnd();
        }

        // コメントを検索して最初のコメントを取得
        string comment = ExtractFirstComment(scriptContents);

        // コメントを表示
        if (!string.IsNullOrEmpty(comment))
        {
            GUIStyle style = EditorStyles.label;
            style.normal.textColor = Color.gray;
            EditorGUI.LabelField(new Rect(selectionRect.xMax - 200, selectionRect.y, 200, selectionRect.height), comment, style);
        }
    }

    private static string ExtractFirstComment(string scriptContents)
    {
        int commentStart = scriptContents.IndexOf("/*");
        int commentEnd = scriptContents.IndexOf("*/");
        if (commentStart >= 0 && commentEnd > commentStart)
        {
            return scriptContents.Substring(commentStart + 2, commentEnd - commentStart - 2).Trim();
        }
        return string.Empty;
    }
}
