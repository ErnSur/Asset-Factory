using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.IO;
using System.Text;
#if UNITY_2019_1_OR_NEWER
using UnityEditor.ShortcutManagement;
#endif

namespace QuickEye.AssetFactory.Editor
{
    public class AssetFactoryDataSource
    {
        private const string _createMenuPath = "Assets/Create/";
        private string NewItemWindowPath => AssetFactoryWindow.ContextMenuPath;

        public List<CreateAssetStrategy> GetCreateStrategies()
        {
            var menuItems = TypeCache.GetMethodsWithAttribute<MenuItem>();
            var scriptableObjects = TypeCache.GetTypesWithAttribute<CreateAssetMenuAttribute>();
            var entriesMethods = TypeCache.GetMethodsWithAttribute<CreateAssetEntryAttribute>();

            // var menuItemEntries =
            //     from method in menuItems
            //     let att = GetAttribute<MenuItem>(method)
            //     where !att.validate
            //     let menuPath = att.menuItem
            //     where menuPath.StartsWith(_createMenuPath)
            //     where menuPath != NewItemWindowPath
            //     select new CreateAssetStrategy(null, menuPath.Substring(_createMenuPath.Length),
            //     _ => EditorApplication.ExecuteMenuItem(menuPath));

            var soEntries =
                from so in scriptableObjects
                let att = GetAttribute<CreateAssetMenuAttribute>(so)
                select CreateItemEntryFromSO(so, att);

            var concredeEntries =
                from method in entriesMethods
                select method.Invoke(null, null) as CreateAssetStrategy;

            var allEntries =
                // menuItemEntries.Concat(soEntries)
                soEntries
                .Concat(concredeEntries)
                .ToList();

            allEntries.Sort((x, y) => EditorUtility.NaturalCompare(x.ItemName, y.ItemName));
            return allEntries;

            T GetAttribute<T>(MemberInfo memberInfo) where T : Attribute
            {
                return Attribute.GetCustomAttribute(memberInfo, typeof(T)) as T;
            }

            CreateAssetStrategy CreateItemEntryFromSO(Type type, CreateAssetMenuAttribute att)
            {
                var entry = new CreateScriptableObjectStrategy(type, att.menuName);
                if (!string.IsNullOrWhiteSpace(att.fileName))
                    entry.DefaultFileName = att.fileName;
                return entry;
            }
        }

        public List<NewItemCategory> CreateCategories(IEnumerable<CreateAssetStrategy> entries)
        {
            var categories = (
                from entry in entries
                let pathItems = entry.MenuPath.Split('/')
                let sb = new StringBuilder()
                from pathItem in pathItems.Take(pathItems.Count() - 1)
                select sb.Append($"/{pathItem}").ToString(1, sb.Length - 1)
                ).Distinct().Select(p => new NewItemCategory(p, e => e.MenuPath.StartsWith(p)));

            var specialCategories = new NewItemCategory[]
            {
                new NewItemCategory("All", _ => true),
            };

            return specialCategories.Concat(categories).ToList();
        }

        static CreateAssetStrategy[] ExtractCreateAssetMenuItems(System.Reflection.Assembly assembly)
        {
            var result = new List<CreateAssetStrategy>();

            foreach (var type in TypeCache.GetTypesWithAttribute<CreateAssetMenuAttribute>())
            {
                if (!(type.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false).FirstOrDefault() is CreateAssetMenuAttribute attr))
                    continue;

                if (!type.IsSubclassOf(typeof(ScriptableObject)))
                {
                    Debug.LogWarningFormat("CreateAssetMenu attribute on {0} will be ignored as {0} is not derived from ScriptableObject.", type.FullName);
                    continue;
                }

                var menuItemName = (string.IsNullOrEmpty(attr.menuName)) ? ObjectNames.NicifyVariableName(type.Name) : attr.menuName;
                var fileName = (string.IsNullOrEmpty(attr.fileName)) ? ("New " + ObjectNames.NicifyVariableName(type.Name) + ".asset") : attr.fileName;
                if (!Path.HasExtension(fileName))
                    fileName = fileName + ".asset";

                var item = new CreateScriptableObjectStrategy(type, menuItemName)
                { DefaultFileName = fileName };
                result.Add(item);
            }

            return result.ToArray();
        }
    }
}