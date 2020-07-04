using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Plugins.PluginManager
{
    public class PluginManager : EditorWindow
    {
        string _workingDirectory;
        string _manifestPath;
        PluginData _pluginData;
        PluginData _filteredPluginData;
        VisualTreeAsset _pluginListItemTemplate;

        List<string> _installedSubmodules;

        [MenuItem("Plugins/Plugin Manager")]
        public static void ShowExample()
        {
            PluginManager wnd = GetWindow<PluginManager>();
            wnd.titleContent = new GUIContent("Plugin Manager");
            wnd.minSize = new Vector2(600, 300);
        }

        public void OnEnable()
        {
            _workingDirectory = GetWorkingDirectory();

            VisualElement root = rootVisualElement;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Path.Combine(_workingDirectory, "PluginManager.uxml"));
            VisualElement uxml = visualTree.Instantiate();
            root.Add(uxml);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(Path.Combine(_workingDirectory, "PluginManager.uss"));
            root.styleSheets.Add(styleSheet);

            _pluginListItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Path.Combine(_workingDirectory, "PluginListItem.uxml"));

            var listView = root.Q<ListView>("list-view");
            listView.makeItem = () => _pluginListItemTemplate.Instantiate();
            listView.onSelectionChange += _ => SelectPlugin(listView.selectedIndex);

            var updateButton = root.Q<Button>("plugin-update-button");
            updateButton.clicked += () =>
            {
                ShowSpinner();
                GitHelper.UpdateSubmoduleAsync(Path.GetDirectoryName(_manifestPath), _pluginData.Data[listView.selectedIndex].path, () =>
                {
                    HideSpinner();
                    GetPluginList();
                });
            };

            var removeButton = root.Q<Button>("plugin-remove-button");
            removeButton.clicked += () =>
            {
                ShowSpinner();
                GitHelper.RemoveSubmodule(Path.GetDirectoryName(_manifestPath), _pluginData.Data[listView.selectedIndex].path);
                HideSpinner();
                GetPluginList();
            };

            root.Q<ToolbarButton>("refresh-button").clicked += GetPluginList;

            var searchBar = root.Q<ToolbarSearchField>("search-bar");
            searchBar.RegisterValueChangedCallback(e => FilterPlugins(e.newValue));

            GetPluginList();

            var dataContext = new SerializedObject(_filteredPluginData);
            root.Bind(dataContext);

            listView.selectedIndex = 0; //Auto-select first element
        }

        private void OnGUI()
        {
            rootVisualElement.Q<VisualElement>("container").style.height = position.height;
        }

        string GetWorkingDirectory()
        {
            var script = MonoScript.FromScriptableObject(this);
            return Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
        }

        void GetPluginList()
        {
            _manifestPath = GetManifestPath();

            if (!File.Exists(_manifestPath))
            {
                Debug.LogError("manifest.json couldn't be located.", this);
                return;
            }

            if (_pluginData == null) _pluginData = CreateInstance<PluginData>();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(_manifestPath), _pluginData);

            FilterPlugins(string.Empty);

            ShowSpinner();
            GitHelper.GetInstalledSubmodulesAsync(Path.GetDirectoryName(_manifestPath), installedSubmodules =>
            {
                HideSpinner();
                _installedSubmodules = installedSubmodules;
                SelectPlugin(rootVisualElement.Q<ListView>("list-view").selectedIndex);
            });
        }

        string GetManifestPath()
        {
            var directoryPath = _workingDirectory;
            string manifestPath;

            do
            {
                directoryPath = Path.GetDirectoryName(directoryPath);
                manifestPath = Path.Combine(directoryPath, "manifest.json");
            } while (!File.Exists(manifestPath) && new DirectoryInfo(directoryPath).Name.ToLower() != "assets");

            return manifestPath;
        }

        void SelectPlugin(int selectedIndex)
        {
            var selectedPlugin = _pluginData.Data[selectedIndex];

            var pluginTitle = rootVisualElement.Q<Label>("plugin-title");
            pluginTitle.text = selectedPlugin.name;
            
            var pluginDescription = rootVisualElement.Q<TextElement>("plugin-description");
            pluginDescription.text = selectedPlugin.description;

            var linkContainer = rootVisualElement.Q<VisualElement>("plugin-link-container");
            var allElements = linkContainer.Children().ToList();
            foreach (var element in allElements) linkContainer.Remove(element);

            foreach (var link in selectedPlugin.links)
            {
                var linkButton = new Button();
                linkButton.clicked += () => Application.OpenURL(link.link);
                linkButton.text = link.name;
                linkButton.AddToClassList("link");
                linkContainer.Add(linkButton);
            }

            if (_installedSubmodules != null)
            {
                var pluginInstalled = _installedSubmodules.Contains(selectedPlugin.path);

                var updateButton = rootVisualElement.Q<Button>("plugin-update-button");
                var removeButton = rootVisualElement.Q<Button>("plugin-remove-button");

                updateButton.text = pluginInstalled ? "Update" : "Install";
                removeButton.visible = pluginInstalled;
            }
        }

        void FilterPlugins(string filter)
        {
            if (_pluginData == null) return;

            if (_filteredPluginData == null) _filteredPluginData = CreateInstance<PluginData>();

            if (filter.Length == 0)
            {
                _filteredPluginData.Data = _pluginData.Data.OrderBy(x => x.name).ToList();
                return;
            }

            _filteredPluginData.Data = _pluginData.Data.Where(x => x.name.Split().Any(y => y.StartsWith(filter)) || x.name.StartsWith(filter)).OrderBy(x => x.name).ToList();
        }

        void ShowSpinner()
        {
            EditorApplication.update += RotateSpinner;
            rootVisualElement.Q<VisualElement>("loading-spinner-container").visible = true;
        }

        void HideSpinner()
        {
            EditorApplication.update -= RotateSpinner;
            rootVisualElement.Q<VisualElement>("loading-spinner-container").visible = false;
        }

        void RotateSpinner()
        {
            var loadingSpinner = rootVisualElement.Q<VisualElement>("loading-spinner-container");
            loadingSpinner.transform.rotation = Quaternion.Euler(0, 0, ((float)EditorApplication.timeSinceStartup % 360) * 360);
        }

        [Serializable]
        public class PluginData : ScriptableObject
        {
            public List<Plugin> Data = new List<Plugin>();
        }

        [Serializable]
        public struct Plugin
        {
            public string name;
            public string description;
            public string path;
            public Link[] links;

            [Serializable]
            public struct Link
            {
                public string name;
                public string link;
            }
        }
    }
}