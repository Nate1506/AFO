using UnityEngine;

namespace SayItAgain
{
    /// <summary>All levels in display order. Loaded from Resources so menus and the match find the same list.</summary>
    [CreateAssetMenu(menuName = "Say It Again/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public LevelData[] levels;

        static LevelCatalog instance;
        public static LevelCatalog Instance => instance != null ? instance : instance = Resources.Load<LevelCatalog>("LevelCatalog");

        public LevelData Get(int index) =>
            levels != null && levels.Length > 0 ? levels[Mathf.Clamp(index, 0, levels.Length - 1)] : null;
    }
}
