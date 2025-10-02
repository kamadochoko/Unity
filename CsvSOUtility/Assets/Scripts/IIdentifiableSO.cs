// Author: kamadochoko
namespace IIdentifiableNamespace
{
    /// <summary>
    /// ScriptableObjectに一意なID取得メソッドを必須化するためのインターフェース。
    /// </summary>
    /// <remarks>
    /// USAGE:
    /// 1. エントリIDでアクセスしたいScriptableObjectに本インターフェースを実装する。
    /// 2. `GetId` で返却する値は、CSVや外部データと突き合わせる識別子を返すように統一する。
    /// 3. `FlexibleRelationResolver` や `CsvSOUtility` からID参照を行いたい場合に、共通契約として利用する。
    /// </remarks>
    public interface IIdentifiableSO
    {
        /// <summary>
        /// ScriptableObjectに紐づく代表IDを返す。存在しない場合は負数等を返して明示する。
        /// </summary>
        int GetId();
    }
}
