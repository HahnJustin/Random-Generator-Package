// Editor/TileObjectAssetWatcher.cs
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using Dalichrome.RandomGenerator.UserData;

sealed class TileObjectAssetWatcher : AssetPostprocessor
{
    // The one folder you care about (relative to the project root)
    const string kFolder = "/Resources/TileObjects/";

    static void OnPostprocessAllAssets(
        string[] imported, string[] deleted,
        string[] movedFrom, string[] movedTo)
    {
        bool touchesTileObject =
            // added / re-imported / moved-into folder  ¨ asset exists
            imported.Any(IsTileObjectPresent) ||
            movedTo.Any(IsTileObjectPresent) ||

            // removed / moved-out of folder            ¨ asset is already gone
            deleted.Any(IsTileObjectPath) ||
            movedFrom.Any(IsTileObjectPath);

        if (touchesTileObject)
        {
            TileDisplayHelper.Invalidate();        // non-Odin cache
            TileDropdownOdinUtility.Invalidate();  // Odin cache
        }
    }

    /* „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ helpers „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ */

    // Fast, type-safe check (asset still in DB)
    static bool IsTileObjectPresent(string path) =>
        path.Contains(kFolder) &&
        AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(TileObject);

    // Fallback heuristic for files that have just been deleted / moved
    static bool IsTileObjectPath(string path) =>
        path.Contains(kFolder) &&
        path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase);
}
#endif
