using UnityEngine;

namespace QuickEye.AssetFactory.Editor
{
    //Example
    public class Ability : ScriptableObject
    {
        [CreateAssetEntry] // Create the same automaticly form CreateAssetMenuAttribute
        private static CreateAssetStrategy CreateEntry() => new CreateScriptableObjectStrategy<Ability>();
    }
}