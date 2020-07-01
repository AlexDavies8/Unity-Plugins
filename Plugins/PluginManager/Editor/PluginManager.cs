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
        VisualTreeAsset _pluginListItemTemplate;

        List<string> _installedSubmodules;

        [MenuItem("Window/UIElements/PluginManager")]
        public static void ShowExample()
        {
            PluginManager wnd = GetWindow<PluginManager>();
            wnd.titleContent = new GUIContent("Plugin Manager");
            wnd.minSize = new Vector2(600, 450);
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

            var updateButton = rootVisualElement.Q<Button>("plugin-update-button");
            updateButton.clicked += () => GitHelper.UpdateSubmoduleAsync(Path.GetDirectoryName(_manifestPath), _pluginData.Data[listView.selectedIndex].path, GetPluginList);

            var removeButton = rootVisualElement.Q<Button>("plugin-remove-button");
            removeButton.clicked += () =>
            {
                GitHelper.RemoveSubmodule(Path.GetDirectoryName(_manifestPath), _pluginData.Data[listView.selectedIndex].path);
                GetPluginList();
            };

            GetPluginList();

            var dataContext = new SerializedObject(_pluginData);
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

            GitHelper.GetInstalledSubmodulesAsync(Path.GetDirectoryName(_manifestPath), installedSubmodules =>
            {
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

            if (_installedSubmodules != null)
            {
                var pluginInstalled = _installedSubmodules.Contains(selectedPlugin.path);

                var updateButton = rootVisualElement.Q<Button>("plugin-update-button");
                var removeButton = rootVisualElement.Q<Button>("plugin-remove-button");

                updateButton.text = pluginInstalled ? "Update" : "Install";
                removeButton.visible = pluginInstalled;
            }
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
        }
    }
}