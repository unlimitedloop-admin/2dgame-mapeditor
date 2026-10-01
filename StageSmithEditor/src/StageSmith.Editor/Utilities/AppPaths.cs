namespace StageSmith.Editor.Utilities;

/// <summary>
/// アプリに同梱するファイル（Assets/・resource/ 配下）のパス解決。
/// 相対パスのまま読むと「起動時の作業フォルダー」基準になり、ショートカットの作業フォルダーが
/// exe の場所と違う場合などに見つからなくなる。常に exe の場所（AppContext.BaseDirectory）を基準にする。
/// </summary>
public static class AppPaths
{
    public static string Resolve(string relativePath)
    {
        return Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(AppContext.BaseDirectory, relativePath);
    }
}
