using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.SceneManagement;

namespace Zafir
{
    [InitializeOnLoad]
    public static class ScreenshotButtonEditor
    {
        private static readonly Type _toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static readonly Type _guiViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");
        private static readonly PropertyInfo _viewVisualTree = _guiViewType.GetProperty("visualTree",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static ScriptableObject _currentToolbar;
        private static GameObject _productionLight;
        private static GameObject _workLight;

        static ScreenshotButtonEditor()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (_currentToolbar == null)
            {
                // Find toolbar
                var toolbars = Resources.FindObjectsOfTypeAll(_toolbarType);
                _currentToolbar = toolbars.Length > 0 ? (ScriptableObject) toolbars[0] : null;
                if (_currentToolbar != null)
                {
                    // Get it's visual tree
                    var visualTree = (VisualElement) _viewVisualTree.GetValue(_currentToolbar, null);

                    // Get first child which 'happens' to be toolbar IMGUIContainer
                    var container = (IMGUIContainer) visualTree[0];

                    // (Re)attach handler
                    var handler = (Action) _imguiContainerOnGui.GetValue(container);
                    handler -= OnGUI;
                    handler += OnGUI;
                    _imguiContainerOnGui.SetValue(container, handler);
                }
            }

            if (_workLight == null)
            {
                _workLight = GameObject.FindWithTag("WorkLight");
            }

            if (_productionLight == null)
            {
                _productionLight = GameObject.FindWithTag("ProductionLight");
            }
        }

        private static void OnGUI()
        {
            var commandButtonStyle = new GUIStyle("Command")
            {
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageAbove,
            };

            const int offset = 150;
            var screenWidth = EditorGUIUtility.currentViewWidth;
            float playButtonsPosition = (screenWidth - offset) / 2;

            Rect leftRect = new Rect
            {
                y = 5,
                height = 24,
                xMin = playButtonsPosition - offset,
                xMax = playButtonsPosition
            };

            GUILayout.BeginArea(leftRect);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_TimelineEditModeRippleON", "Save Screenshot"), commandButtonStyle))
            {
                string path = Application.persistentDataPath + "/" + SceneManager.GetActiveScene().name + ".png";
                ScreenCapture.CaptureScreenshot(path, 4);
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_LookDevObjRotation", "Open Screenshot Folder"), commandButtonStyle))
            {
                string path = Application.persistentDataPath.Replace("/", "\\");
                System.Diagnostics.Process.Start("explorer.exe", path);
            }

            var productionLightingOn = EditorPrefs.GetBool("ZafirProductionLighting");
            productionLightingOn = GUILayout.Toggle(productionLightingOn, EditorGUIUtility.IconContent("d_LookDevShadow", "Toggle production lighting"), commandButtonStyle);
            EditorPrefs.SetBool("ZafirProductionLighting", productionLightingOn);
            bool workLightingOn = !productionLightingOn;
            if (_workLight != null && _workLight.activeSelf != workLightingOn)
            {
                _workLight.SetActive(workLightingOn);
            }

            if (_productionLight != null && _productionLight.activeSelf != productionLightingOn)
            {
                _productionLight.SetActive(productionLightingOn);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}