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

        public static Rect LastPosition { get; private set; }

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

        private static GUIContent CreateWindowTitle()
        {
            var name = "New item...";
            var icon = EditorGUIUtility.IconContent("Project").image;
            return new GUIContent(name, icon);
        }

        private AssetFactoryController _controller = new AssetFactoryController();

        private List<CreateAssetStrategy> _itemEntries;

        #region VisualElements
        [UQuery("details")]
        private VisualElement _details;

        [UQuery("entry-list")]
        private ListView _entryListView;

        [UQuery("category-list")]
        private ListView _categoryListView;

        [UQuery("description")]
        private Label _description;

        [UQuery("type-label")]
        private Label _typeName;

        [UQuery]
        private ToolbarSearchField _searchField;
        [UQuery]
        private FileLocationPanel _fileLocationPanel;
        [UQuery]
        private FileNameField _fileNameField;
        #endregion

        private string _fileName;

        private void Update() => LastPosition = position;

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Select"), false, () => {
                Debug.Log($"select ");
                _searchField.Q<TextField>().SelectAll();
            });

            menu.AddItem(new GUIContent("Focus"), false, () => {
                Debug.Log($"Focus ");
                _searchField.Q<TextField>().Q("unity-text-input").Focus();
            });
        }

        private void OnEnable()
        {
            LoadVisualTree();
            _itemEntries = _controller.GetCreateActions();

            SetupEntryListView();
            SetupCategoryView();
            InitSearchField();
            SetupFileLocationPanel();

            _entryListView.selectedIndex = 0;
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(evt=>
            {
                FocusSearchField();

            });
        }

        private void FocusSearchField() => _searchField.Q<TextField>().Q("unity-text-input").Focus();

        private void SetupFileLocationPanel()
        {
            _fileLocationPanel.CancelClicked += Close;
            _fileLocationPanel.AddClicked += SubmitEntry;
            _fileLocationPanel.FileName = _fileName;
            SetDirectory();

            _fileNameField.RegisterValueChangedCallback(evt =>
            {
                if (evt.target != _fileNameField) return;
                _fileName = evt.newValue;
            });
        }

        private void SetDirectory()
        {
            var defaultDirectory = string.Empty;

            if (Selection.assetGUIDs.Length > 0)
                defaultDirectory = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            else if (FileLocationPanel.PreviousDirectories.Count > 0)
                defaultDirectory = FileLocationPanel.PreviousDirectories.FirstOrDefault();

            _fileLocationPanel.Directory = defaultDirectory;
        }

        private void LoadVisualTree()
        {
            var visualTree = Resources.Load<VisualTreeAsset>(_uxmlPath);

            visualTree.CloneTree(rootVisualElement);
            rootVisualElement.AssignQueryableMembers(this);
        }

        private void SubmitEntry()
        {
            var entry = _itemEntries[_entryListView.selectedIndex];
            var name = Path.ChangeExtension(_fileLocationPanel.FileName, entry.FileExtension);
            entry.Execute(Path.Combine(_fileLocationPanel.Directory, name));
        }

        //Make this a TreeView when its UI Toolkit conterpart becomes public
        private void SetupCategoryView()
        {
            var categories = _controller.CreateCategories(_itemEntries);

            _categoryListView.makeItem = () =>
            {
                var item = new Label();
                item.AddToClassList("category-item");
                return item;
            };

            _categoryListView.bindItem = (VisualElement element, int index) =>
            {
                var category = _categoryListView.itemsSource[index] as NewItemCategory;
                (element as Label).text = category.DisplayName;
            };

            _categoryListView.onSelectionChange += (IEnumerable<object> selectedItems) =>
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

            _entryListView.makeItem = MakeListItem;
            _entryListView.bindItem = BindListItem;
            _entryListView.onItemsChosen += _ => SubmitEntry();
            _entryListView.onSelectionChange += items => OnEntrySelected(items.FirstOrDefault() as CreateAssetStrategy);

            _entryListView.itemsSource = _itemEntries;

            VisualElement MakeListItem() => itemPrototype.CloneTree();

            void BindListItem(VisualElement element, int index)
            {
                var entry = _entryListView.itemsSource[index] as CreateAssetStrategy;
                element.Q<Label>().text = entry.ItemName;
                element.Q("icon").style.backgroundImage = entry.Icon;
            }
        }

        private void OnEntrySelected(CreateAssetStrategy entry)
        {
            UpdateDetailsSection(entry);
            UpdateNameField(entry);
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

        private void InitSearchField()
        {
            _searchField = rootVisualElement.Q<ToolbarSearchField>("search-field");

            _searchField.RegisterValueChangedCallback(OnSearchUpdate);


            void OnSearchUpdate(ChangeEvent<string> evt)
            {
                _categoryListView.selectedIndex = 0;

                if (string.IsNullOrWhiteSpace(evt.newValue))
                    _entryListView.itemsSource = _itemEntries;
                else
                    _entryListView.itemsSource =
                        _itemEntries
                        .Where(e => e.ItemName.ToLower().Contains(evt.newValue.ToLower()))
                        .ToList();
            }
        }
    }
}