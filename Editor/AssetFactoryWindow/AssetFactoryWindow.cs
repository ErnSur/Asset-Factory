using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using QuickEye.UIToolkit;
#if UNITY_2019_1_OR_NEWER
using UnityEditor.ShortcutManagement;
#endif

namespace QuickEye.Scaffolding
{
    public class AssetFactoryWindow : EditorWindow, IHasCustomMenu
    {
        public const string ContextMenuPath = "Assets/Create/NewItem...";
        private const string _uxmlPath = "QuickEye/AssetFactory/AssetFactory";
        private const string _listItemUxmlPath = _uxmlPath + "-item";

#if UNITY_2019_1_OR_NEWER
        [Shortcut(ContextMenuPath, KeyCode.X, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
#endif
        private static void OpenWindow()
        {
            var wnd = GetWindow<AssetFactoryWindow>();

            wnd.titleContent = CreateWindowTitle();

            var defaultSize = new Vector2(574, 446);
            wnd.minSize = defaultSize;
            wnd.position = new Rect(Screen.width / 2, Screen.height / 2, defaultSize.x, defaultSize.y);
            wnd.Show();
        }

        private static GUIContent CreateWindowTitle() => new GUIContent
        {
            text = "New item...",
            image = EditorGUIUtility.IconContent("Project").image
        };
        
        private AssetFactoryDataSource _dataSource = new AssetFactoryDataSource();

        private List<CreateAssetStrategy> _itemEntries;

        #region VisualElements
        [Q("details")]
        private VisualElement _details;

        [Q("entry-list")]
        private ListView _entryListView;

        [Q("category-list")]
        private ListView _categoryListView;

        [Q("description")]
        private Label _description;

        [Q("type-label")]
        private Label _typeName;

        [Q] private ToolbarSearchField _searchField;
        [Q] private FileLocationPanel _fileLocationPanel;
        [Q] private FileNameField _fileNameField;
        #endregion

        private string _fileName;

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Select"), false, () =>
            {
                Debug.Log($"select ");
                _searchField.Q<TextField>().SelectAll();
            });

            menu.AddItem(new GUIContent("Focus"), false, () =>
            {
                Debug.Log($"Focus ");
                _searchField.Q<TextField>().Q("unity-text-input").Focus();
            });
        }

        private void OnEnable()
        {
            LoadVisualTree();
            _itemEntries = _dataSource.GetCreateStrategies();

            SetupEntryListView();
            SetupCategoryView();
            SetupSearchField();
            SetupFileLocationPanel();

            _entryListView.selectedIndex = 0;
        }

        private void LoadVisualTree()
        {
            var visualTree = Resources.Load<VisualTreeAsset>(_uxmlPath);

            visualTree.CloneTree(rootVisualElement);
            rootVisualElement.AssignQueryResults(this);
        }

        private void SetupFileLocationPanel()
        {
            _fileLocationPanel.CancelClicked += Close;
            _fileLocationPanel.AddClicked += SubmitEntry;
            _fileLocationPanel.FileName = _fileName;
            SetDirectory();

            _fileNameField.RegisterThisValueChangedCallback(evt => _fileName = evt.newValue);
        }

        private void SetDirectory()
        {
            var defaultDirectory = string.Empty;

            if (Selection.assetGUIDs.Length > 0)
                defaultDirectory = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
            else if (FileLocationPanel.PreviousDirectories.Count > 0)
                defaultDirectory = FileLocationPanel.PreviousDirectories.FirstOrDefault();

            _fileLocationPanel.Directory = defaultDirectory;
        }

        private void SubmitEntry()
        {
            var entry = _entryListView.selectedItem as CreateAssetStrategy;
            var name = Path.ChangeExtension(_fileLocationPanel.FileName, entry.FileExtension);
            entry.Execute(Path.Combine(_fileLocationPanel.Directory, name));
        }

        //Make this a TreeView when its UI Toolkit conterpart becomes public
        private void SetupCategoryView()
        {
            var categories = _dataSource.CreateCategories(_itemEntries);

            _categoryListView.makeItem = () => new Label().Class("category-item");

            _categoryListView.bindItem = (VisualElement element, int index) =>
            {
                var category = _categoryListView.itemsSource[index] as NewItemCategory;
                (element as Label).text = category.DisplayName;
            };

            _categoryListView.onSelectionChanged += selectedItems =>
            {
                var category = selectedItems.FirstOrDefault() as NewItemCategory;
                _entryListView.itemsSource = _itemEntries.Where(category.Predicate).ToList();
                if (_entryListView.selectedIndex == -1)
                    _entryListView.selectedIndex = 0;
            };

            _categoryListView.itemsSource = categories;
        }

        private void SetupEntryListView()
        {
            var itemPrototype = Resources.Load<VisualTreeAsset>(_listItemUxmlPath);

            _entryListView.makeItem = itemPrototype.CloneTree;
            _entryListView.bindItem = BindListItem;
            _entryListView.onItemChosen += _ => SubmitEntry();
            _entryListView.onSelectionChanged += s => UpdateView(s.FirstOrDefault() as CreateAssetStrategy);

            _entryListView.itemsSource = _itemEntries;

            void BindListItem(VisualElement element, int index)
            {
                var entry = _entryListView.itemsSource[index] as CreateAssetStrategy;
                element.Q<Label>().text = entry.ItemName;
                element.Q("icon").style.backgroundImage = entry.Icon;
            }

            void UpdateView(CreateAssetStrategy selectedEntry)
            {
                UpdateDetailsSection(selectedEntry);
                UpdateNameField(selectedEntry);
            }
        }

        private void UpdateNameField(CreateAssetStrategy entry)
        {
            var defaultFileName = entry != null
                ? Path.ChangeExtension(entry.DefaultFileName, entry.FileExtension)
                : "";

            _fileNameField.SetValueWithoutNotify(
                string.IsNullOrWhiteSpace(_fileName) ? defaultFileName : _fileName);
        }

        private void UpdateDetailsSection(CreateAssetStrategy entry)
        {
            _details.ToggleDisplayStyle(entry != null);
            _description.text = entry?.Description;
            _typeName.text = entry?.AssetType?.Name ?? "";
        }

        private void SetupSearchField()
        {
            _searchField.RegisterThisValueChangedCallback(FilterEntryList);

            rootVisualElement.RegisterCallback<AttachToPanelEvent>(_ => FocusSearchField());

            void FocusSearchField() => _searchField.Q<TextField>().Q("unity-text-input").Focus();

            void FilterEntryList(ChangeEvent<string> evt)
            {
                _categoryListView.selectedIndex = 0;

                if (string.IsNullOrWhiteSpace(evt.newValue))
                    _entryListView.itemsSource = _itemEntries;
                else
                    _entryListView.itemsSource = _itemEntries
                      .Where(e => e.ItemName.ToUpperInvariant().Contains(evt.newValue.ToUpperInvariant()))
                      .ToList();

                if(_entryListView.itemsSource.Count > 0)
                    _entryListView.selectedIndex = 0;
            }
        }
    }
}