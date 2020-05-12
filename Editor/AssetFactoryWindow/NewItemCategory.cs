using System;
#if UNITY_2019_1_OR_NEWER
#endif

namespace QuickEye.Scaffolding
{
    public class NewItemCategory
    {
        public string DisplayName { get; }
        public Func<CreateAssetStrategy, bool> Predicate { get; }

        public NewItemCategory(string displayName, Func<CreateAssetStrategy, bool> predicate)
        {
            DisplayName = displayName;
            Predicate = predicate;
        }
    }
}