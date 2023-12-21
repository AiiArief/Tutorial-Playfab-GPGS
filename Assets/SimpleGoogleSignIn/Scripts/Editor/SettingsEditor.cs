using UnityEditor;
using UnityEngine;

namespace Assets.SimpleGoogleSignIn.Scripts.Editor
{
    [CustomEditor(typeof(GoogleAuthSettings))]
    public class SettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("- Get `Client ID` and `Client Secret` in Google Cloud / Credentials.\n- `Custom URI Scheme` (protocol) should be the same as `Bundle ID` in Google Cloud.\n- Refer to `Settings for Windows` section on wiki for setup options.", MessageType.None);

            DrawDefaultInspector();

            var settings = (GoogleAuthSettings) target;
            
            if (!settings.Redefined())
            {
                EditorGUILayout.HelpBox("Test settings are in use. They are for test purposes only and may be disabled or blocked. Please set your own settings obtained from Google Cloud / Credentials.", MessageType.Warning);
            }

            if (GUILayout.Button("Google Cloud / Credentials"))
            {
                Application.OpenURL("https://console.cloud.google.com/apis/credentials");
            }

            if (GUILayout.Button("Wiki"))
            {
                Application.OpenURL("https://github.com/hippogamesunity/SimpleGoogleSignIn/wiki");
            }
        }
    }
}