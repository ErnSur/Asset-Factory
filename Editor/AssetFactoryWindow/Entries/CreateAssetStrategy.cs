using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace QuickEye.Scaffolding
{
    //Strategy for asset creation
    // descryption andect in attribute? no because StrategyFactory need to provide this too?

    public class CreateAssetStrategy<T> : CreateAssetStrategy
    {
        public CreateAssetStrategy(string menuPath) : base(typeof(T), menuPath) { }
        public CreateAssetStrategy(string menuPath, Action<string> addHandler) : base(typeof(T), menuPath, addHandler) { }
    }

    public class CreateAssetStrategy
    {
        public virtual Texture2D Icon { get; set; }
        public virtual string Description { get; set; }
        public virtual string MenuPath { get; }

        private readonly Action<string> _addHandler;

        public Type AssetType { get; }

        public string ItemName { get; }
        public string DefaultFileName { get; set; }
        public string FileExtension { get; set; }

        public CreateAssetStrategy(Type assetType, string menuPath)
        {
            if (string.IsNullOrWhiteSpace(menuPath))
            {
                Debug.LogError($"Item entry with no path, omited.");
                return;
            }
            AssetType = assetType;
            MenuPath = menuPath;
            ItemName = Path.GetFileName(menuPath);
            DefaultFileName = ObjectNames.NicifyVariableName(ItemName);
            Icon = AssetPreview.GetMiniTypeThumbnail(AssetType);
        }

        public CreateAssetStrategy(Type assetType, string menuPath, Action<string> addHandler) : this(assetType, menuPath)
        {
            _addHandler = addHandler;
        }

        public virtual bool CanExecute(string path) => true;

        public virtual void Execute(string path) // Open ScaffoldingWindow/CreateAsset
        {
            _addHandler?.Invoke(path);
        }
    }

    //Base Asset Entries
    public class CreateSceneAssetStrategy : CreateAssetStrategy<SceneAsset>
    {
        [CreateAssetEntry]
        public static CreateAssetStrategy CreateEntry() => new CreateSceneAssetStrategy();

        private string _doCreateSceneTypeName = "UnityEditor.ProjectWindowCallback.DoCreateScene, UnityEditor";

        public override Texture2D Icon => EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;

        public CreateSceneAssetStrategy() : base("Scene") { FileExtension = ".unity"; }

        public override void Execute(string path)
        {
            //path += ".unity";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var action = ScriptableObject.CreateInstance(Type.GetType(_doCreateSceneTypeName)) as EndNameEditAction;
            ProjectWindowUtil.
            StartNameEditingIfProjectWindowExists(0, action, path, Icon, null);
        }
    }

    public class MenuItemEntry : CreateAssetStrategy
    {
        private const string _executionPrefix = "Assets/Create/";
        private const string _standardSurfaceShader = "Shader/Standard Surface Shader";
        private const string _unlitShader = "Shader/Unlit Shader";

        [CreateAssetEntry]
        public static CreateAssetStrategy CreateSurfaceShader() =>
            new MenuItemEntry(typeof(Shader), _executionPrefix + _standardSurfaceShader, _standardSurfaceShader);
        [CreateAssetEntry]
        public static CreateAssetStrategy CreateUnlitShader() =>
            new MenuItemEntry(typeof(Shader), _executionPrefix + _unlitShader, _unlitShader);

        private string _absoluteMenuItemPath;
        public MenuItemEntry(Type assetType, string menuItemPath, string entryPath) : base(assetType, entryPath)
         => _absoluteMenuItemPath = menuItemPath;

        public override void Execute(string path)
        {
            EditorApplication.ExecuteMenuItem(_absoluteMenuItemPath);
        }
    }

    public class CreateMaterialAssetEntry : CreateAssetStrategy<SceneAsset>
    {
        private readonly static Material _defaultMaterial;

        static CreateMaterialAssetEntry()
        {
            _defaultMaterial = GetDefaultMaterial();
        }

        [CreateAssetEntry]
        public static CreateAssetStrategy CreateEntry() => new CreateMaterialAssetEntry();

        public override Texture2D Icon => EditorGUIUtility.IconContent("Material Icon").image as Texture2D;

        public CreateMaterialAssetEntry() : base("Material") { }

        public override void Execute(string path)
        {
            path += ".mat";
            var defaultMat = _defaultMaterial;

            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp != null)
            {
                defaultMat = rp.defaultMaterial;
            }

            ProjectWindowUtil.CreateAsset(new Material(defaultMat), path);
        }

        private static Material GetDefaultMaterial()
        {
            //The fact that this is internal makes me cry
            var methodInfo = typeof(Material).GetMethod("GetDefaultMaterial", BindingFlags.Static | BindingFlags.NonPublic);
            return methodInfo.Invoke(null, null) as Material;
        }
    }
}