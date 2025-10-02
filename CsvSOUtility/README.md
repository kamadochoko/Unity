# CsvSOUtility

Unityエディタで、CSVデータからScriptableObjectのクラスとアセットを自動生成・同期できるエディタツールです。

---

## ✅ 特長

- CSVの1ファイルからScriptableObjectクラスとアセットを自動生成
- ScriptableObjectとCSVを相互変換（Import/Export）
- `IIdentifiableSO`インターフェース実装をオプション選択可能
- コメントもコードに反映（XMLコメント形式）
- 実行時にはエラー表示と自動フォルダ作成にも対応

---

## 🛠️ セットアップ方法

1. `CsvSOUtility.cs` を Unity プロジェクト内の `Assets/Editor/` に配置します。
2. Unity を再起動 or スクリプトの再コンパイルを待ちます。
3. メニューに `Tools > CSV → ScriptableObject Utility` が表示されます。

---

## 🧪 USAGE（使い方）

### ① ScriptableObject クラスの生成

1. CSVファイルを準備  
   - 1行目：列のコメント（`#`付き）
   - 2行目：フィールド名
   - 3行目：型名（`int`, `float`, `bool`, `string` のいずれか）

2. Unityメニューからツールを開く  
   `Tools > CSV → ScriptableObject Utility`

3. 設定項目を入力  
   - CSVファイル（TextAsset）
   - 出力フォルダ（例：`Assets/ScriptableObjects`）
   - 名前空間（空でもOK）
   - `IIdentifiableSO` 実装（任意）

4. 「Generate ScriptableObject Class」ボタンを押下  
   → 自動的に `.cs` ファイルが生成されます。

---

### ② CSV → ScriptableObject にインポート

- 「Import CSV to Single ScriptableObject」ボタンで、CSVを `.asset` に変換して保存。
- `entries` リストにデータが挿入されます。

---

### ③ ScriptableObject → CSV にエクスポート

- 「Export Single ScriptableObject to CSV」ボタンで、現在の `.asset` データをCSVに書き出せます。

---

## 💡 補足

- **文字コード**：CSVは **Shift_JIS** 推奨（日本語文字化け防止）
- **フィールド名の命名規則**：C#の変数名として使える形式にしてください（例：`name`, `age`）
- `IIdentifiableSO` を有効にすると `id` を返す `GetId()` メソッドが自動生成されます（※`id`列が必要）

---

## 📂 ディレクトリ構成例

```
Assets/
├─ Editor/
│   └─ CsvSOUtility.cs
├─ ScriptableObjects/
│   ├─ charactorsSO.cs       ← 自動生成されたクラス
│   └─ charactorsSO.asset    ← インポートされたアセット
├─ Resources/
│   └─ charactors.csv        ← 入力元CSV
```

---

## 📝 サンプルCSVフォーマット

```csv
# 名前, 年齢, 職業
name, age, job
string, int, string
じろう, 20, 愛犬
たろう, 25, 愛犬
```

---

## 🔢 `IIdentifiableSO` を使いたい場合（任意機能）

### 📌 有効にする方法

1. ユーティリティ画面の「Implement IIdentifiableSO」を **ON** にします。
2. CSVのヘッダーに `id` というフィールド名を含めてください。

```csv
# ID, 名前, レベル
id, name, level
int, string, int
1, じろう, 99
2, たろう, 45
```

### 🧩 自動生成される内容

クラスに以下のような `GetId()` 実装が付きます：

```csharp
public int GetId() => entries != null && entries.Count > 0 ? entries[0].id : -1;
```

> `IIdentifiableSO` を実装することで、ゲーム全体でScriptableObjectのIDによる識別・検索が可能になります。

### 🎯 活用例（IDで検索）

#### 例：リストの中から指定IDのデータを取得

```csharp
var target = mySO.entries.FirstOrDefault(e => e.id == 1);
if (target != null)
{
    Debug.Log($"見つかったキャラ: {target.name}");
}
```

#### 例：マネージャーや辞書に登録

```csharp
Dictionary<int, CharactorsSO.Entry> charaDict = new();
foreach (var e in mySO.entries)
{
    charaDict[e.id] = e;
}

// IDでアクセス
var target = charaDict[2];
Debug.Log(target.name);
```

### 🛠 必要な準備（インターフェース定義）

```csharp
// Assets/Scripts/IIdentifiableSO.cs
public interface IIdentifiableSO
{
    int GetId();
}
```

> 名前空間 `IIdentifiableNamespace` を使用する場合、ユーティリティで指定したnamespaceと一致させてください。

---

## 📄 ライセンス

このプロジェクトは [MITライセンス]の下で公開されています。

---

## Author: kamadochoko

