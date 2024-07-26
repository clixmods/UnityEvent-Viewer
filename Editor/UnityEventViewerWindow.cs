using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Plugins.UnityEventViewer
{
    /// <summary>
    /// Window to view all the assets with UnityEvent and their UnityEvent properties 
    /// </summary>
    public class UnityEventViewerWindow : EditorWindow
    {
        #region Fields

        private string[] _allAssetPaths;
    
        private Vector2 _scrollPosition;
    
        private List<SerializedObject> _scriptableObjectWithUnityEvent = new List<SerializedObject>();

        private List<SerializedObject> _prefabsWithUnityEvent = new List<SerializedObject>();
    
        private Dictionary<SerializedObject, bool> _foldoutStates = new Dictionary<SerializedObject, bool>();

        private GUIStyle _styleBold;

        private bool _showPrefabs = false;
    
        private bool _showScriptableObject = false;
    
        #endregion

        #region Properties
    
        /// <summary>
        /// Get all the ScriptableObject with UnityEvent
        /// </summary>
        private List<SerializedObject> ScriptableObjectWithUnityEvent
        {
            get => _scriptableObjectWithUnityEvent;
            set
            { 
                _scriptableObjectWithUnityEvent = value;
            } 
        }
    
        /// <summary>
        /// Get all the prefabs with UnityEvent
        /// </summary>
        private List<SerializedObject> PrefabsWithUnityEvent
        {
            get => _prefabsWithUnityEvent;
            set
            {
                _prefabsWithUnityEvent = value;
            } 
        }
    
        /// <summary>
        /// Get the number of assets with UnityEvent that have been modified
        /// </summary>
        private int CountDirtyAsset
        {
            get
            {
                return SerializedObjectsDirty.Length;
            }
        }
    
        /// <summary>
        /// Get all the assets with UnityEvent that have been modified
        /// </summary>
        private SerializedObject[] SerializedObjectsDirty
        {
            get
            {
                List<SerializedObject> serializedObjectsDirty = new List<SerializedObject>();
                // Check if there are any changes to the assets
                foreach (var serializedObject in _scriptableObjectWithUnityEvent)
                {
                    if(serializedObject.hasModifiedProperties)
                        serializedObjectsDirty.Add(serializedObject);
                }
        
                foreach (var serializedObject in _prefabsWithUnityEvent)
                {
                    if(serializedObject.hasModifiedProperties)
                        serializedObjectsDirty.Add(serializedObject);
                }

                return serializedObjectsDirty.ToArray();
            }
        }

        #endregion

        #region Init Methods

        [MenuItem("Window/Unity Event Viewer")]
        public static void ShowSaveViewer()
        {
            UnityEventViewerWindow wnd = GetWindow<UnityEventViewerWindow>();
            wnd.titleContent = new GUIContent("Unity Event Viewer");
        }

        private void OnEnable()
        {
            _styleBold = new GUIStyle(EditorStyles.boldLabel);
            _styleBold.fontSize = 20;
            _styleBold.alignment = TextAnchor.MiddleCenter;
        
            RefreshAssets();
        }
    
        #endregion

        #region Logic Methods
    
        private void OnGUI()
        {
            // Add a button to refresh the list of assets
            if (GUILayout.Button("Refresh"))
            {
                RefreshAssets();
            }
        
            if (CountDirtyAsset > 0 && GUILayout.Button("Save changes"))
            {
                foreach (SerializedObject serializedObject in SerializedObjectsDirty)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                }
                // Save the changes to the asset
                AssetDatabase.SaveAssets();
            }
        
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(" ScriptableObject ", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            if (_allAssetPaths == null || _allAssetPaths.Length == 0)
            {
                EditorGUILayout.HelpBox("No assets found", MessageType.Info);
                return;
            }
            // Draw a info box with the number of assets found
            EditorGUILayout.HelpBox($"Found {ScriptableObjectWithUnityEvent.Count} ScriptableObject with Unity Event", MessageType.Info);
        
            EditorGUILayout.HelpBox($"Found {PrefabsWithUnityEvent.Count} prefabs with Unity Event", MessageType.Info);

            EditorGUILayout.LabelField("Scriptable Object", _styleBold);
        
            _showScriptableObject = EditorGUILayout.Foldout(_showScriptableObject, "Scriptable Object with Unity Event");

            if (_showScriptableObject)
            {
                foreach (SerializedObject serializedObject in ScriptableObjectWithUnityEvent)
                {
                    DrawObjectWithUnityEvent<ScriptableObject>(serializedObject);
                }
            }
        
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
        
            EditorGUILayout.LabelField("Prefabs", _styleBold);

            _showPrefabs = EditorGUILayout.Foldout(_showPrefabs, "Prefabs with Unity Event");

            if (_showPrefabs)
            {
                foreach (SerializedObject serializedObject in _prefabsWithUnityEvent)
                {
                    DrawObjectWithUnityEvent<GameObject>(serializedObject);
                }
            }
        
            EditorGUILayout.EndScrollView();
        }
    
        /// <summary>
        /// Refresh the list of assets with UnityEvent and their foldout states 
        /// </summary>
        private void RefreshAssets()
        {
            // Check assets
            _allAssetPaths = AssetDatabase.GetAllAssetPaths();
        
            _scriptableObjectWithUnityEvent.Clear();
        
            _prefabsWithUnityEvent.Clear();
        
            _foldoutStates.Clear();
        
            // Check all assets of type ScriptableObject and keep only the ones with UnityEvent
            foreach (var assetPath in _allAssetPaths)
            {
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset != null)
                {
                    SerializedObject serializedObject = new SerializedObject(asset);

                    SerializedProperty serializedProperty = serializedObject.GetIterator();
                    while (serializedProperty.NextVisible(true))
                    {
                        if (serializedProperty.propertyType == SerializedPropertyType.Generic)
                        {
                            try
                            {
                                if (serializedProperty.type == typeof(UnityEvent).Name)
                                {
                                    ScriptableObjectWithUnityEvent.Add(serializedObject);
                                    _foldoutStates.TryAdd(serializedObject, false);
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.Log(e);
                            }
                        }
                    }
                }
            }
        
            // Check all prefabs and keep only the ones with UnityEvent
            foreach (var prefabPath in _allAssetPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    foreach (var component in prefab.GetComponents<Component>())
                    {
                        // A component can be null because the linked script is missing
                        if (component == null)
                        {
                            continue;
                        }
                        
                        SerializedObject serializedObject = new SerializedObject(component);
                        SerializedProperty serializedProperty = serializedObject.GetIterator();
                        while (serializedProperty.NextVisible(true))
                        {
                            if (serializedProperty.propertyType == SerializedPropertyType.Generic)
                            {
                                try
                                {
                                    if (serializedProperty.type == typeof(UnityEvent).Name)
                                    {
                                        PrefabsWithUnityEvent.Add(serializedObject);
                                        _foldoutStates.TryAdd(serializedObject, false);
                                        break;
                                    }
                                }
                                catch (Exception e)
                                {
                                    Debug.Log(e);
                                }
                            }
                        }
                    }
                }
            }
        }
    
        private void OnDestroy()
        {
            // If there are changes to the assets
            // Open Dialog to save changes
            if ( CountDirtyAsset > 0 && EditorUtility.DisplayDialog("Save changes with Unity Event", "Do you want to save the changes?", "Yes", "No"))
            {
                foreach (SerializedObject serializedObject in SerializedObjectsDirty)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                }
                // Save the changes to the asset
                AssetDatabase.SaveAssets();
            }
        }
    
        #region Draw Methods

        /// <summary>
        /// Draw the object with UnityEvent 
        /// </summary>
        /// <param name="serializedObject">The serialized object to draw</param>
        /// <typeparam name="T"> The type of the object to draw </typeparam>
        private void DrawObjectWithUnityEvent<T>(SerializedObject serializedObject)
        {
            GUI.enabled = false;
            EditorGUILayout.ObjectField(serializedObject.targetObject, typeof(T), false, GUILayout.Height(20));
            GUI.enabled = true;

            // Add a foldout for each prefab
            if (_foldoutStates.TryGetValue(serializedObject, out bool foldoutState))
            {
                foldoutState = EditorGUILayout.Foldout(foldoutState, "Component with Unity Event");
                _foldoutStates[serializedObject] = foldoutState;
            }

            if (foldoutState)
            {
                DrawComponentWithUnityEvent(serializedObject);
            }

            EditorGUILayout.Space(30);
        }

        /// <summary>
        /// Draw the component with UnityEvent
        /// </summary>
        /// <param name="serializedObject"> The serialized object to draw </param>
        private void DrawComponentWithUnityEvent(SerializedObject serializedObject)
        {
            SerializedProperty serializedProperty = serializedObject.GetIterator();
            while (serializedProperty.NextVisible(true))
            {
                if (serializedProperty.propertyType == SerializedPropertyType.Generic)
                {
                    try
                    {
                        if (serializedProperty.type == typeof(UnityEvent).Name)
                        {
                            EditorGUILayout.PropertyField(serializedProperty, true);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e);
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}