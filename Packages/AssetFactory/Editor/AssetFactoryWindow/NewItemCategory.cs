using System;

namespace QuickEye.AssetFactory.Editor
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