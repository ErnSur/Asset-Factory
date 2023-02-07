using System;
using UnityEditor;
using UnityEngine;

namespace QuickEye.AssetFactory.Editor
{
    public class CreateScriptableObjectStrategy<T> : CreateScriptableObjectStrategy where T : ScriptableObject
    {
        public CreateScriptableObjectStrategy() : base(typeof(T)) { }
        public CreateScriptableObjectStrategy(string menuPath) : base(typeof(T), menuPath) { }
    }

    public class CreateScriptableObjectStrategy : CreateAssetStrategy
    {
        public CreateScriptableObjectStrategy(Type type) : this(type, ObjectNames.NicifyVariableName(type.Name)) { }

        public CreateScriptableObjectStrategy(Type type, string menuPath) : base(type,FallbackTypeNamePath(type, menuPath))
        {
            FileExtension = ".asset";
            Icon = GetIcon(type);
        }

        public override void Execute(string path)
        {
            var asset = ScriptableObject.CreateInstance(AssetType);
            ProjectWindowUtil.CreateAsset(asset, path);
        }

        private static string FallbackTypeNamePath(Type type, string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? ObjectNames.NicifyVariableName(type.Name)
                : path;
        }

        private static Texture2D GetIcon(Type type)
        {
            var obj = ScriptableObject.CreateInstance(type);
            var c = EditorGUIUtility.ObjectContent(obj, type).image as Texture2D;
            ScriptableObject.DestroyImmediate(obj);
            return c;
        }
    }
}