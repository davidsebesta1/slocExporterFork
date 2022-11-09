﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using slocExporter;
using slocExporter.Objects;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static slocExporter.ColliderModeSetter;

namespace Editor.sloc {

    public sealed class ExporterWindow : EditorWindow {

        private const string ProgressbarTitle = "slocExporter";
        private const string LossyColorDescription = "Uses a single 32-bit integer for colors instead of four 32-bit floats (16 bytes per color). This reduces file size but limits the RGB color range to 0-255 and therefore loses precision.";
        private const string Asterisk = "Hover over an item with an * for more information.";

        [MenuItem("Window/sloc/Export")]
        public static void ShowWindow() => GetWindow(typeof(ExporterWindow), true, "Export to sloc");

        private static string _filePath = @"%appdata%\EXILED\Plugins\sloc\Objects\MyObject";

        private static bool _debug;

        private static bool _lossyColor;

        private static PrimitiveObject.ColliderCreationMode _collider;

        private static readonly string[] OptionsArray = Enum.GetValues(typeof(PrimitiveObject.ColliderCreationMode))
            .Cast<PrimitiveObject.ColliderCreationMode>()
            .Select(ModeToString).ToArray();

        private static readonly List<string> Options = new(OptionsArray);

        private void OnGUI() {
            GUILayout.Label("File", EditorStyles.boldLabel);
            _filePath = EditorGUILayout.TextField("Path", _filePath);
            if (GUILayout.Button("Select File")) {
                var sceneName = SceneManager.GetActiveScene().name;
                var path = EditorUtility.SaveFilePanel("Save sloc file", Path.GetDirectoryName(_filePath.ToFullAppDataPath()), string.IsNullOrEmpty(sceneName) ? "MyObject" : sceneName, "sloc");
                if (!string.IsNullOrEmpty(path))
                    _filePath = path.ToShortAppDataPath();
            }

            GUILayout.Space(10);
            GUILayout.Label("Attributes", EditorStyles.boldLabel);
            _lossyColor = EditorGUILayout.Toggle(new GUIContent("Lossy Colors*", LossyColorDescription), _lossyColor);
            _collider = StringToMode(OptionsArray[EditorGUILayout.Popup(new GUIContent("Default Collider Mode*", "The default collider creation mode to use for primitive objects.\n" + GetModeDescription(_collider, true)), Options.IndexOf(ModeToString(_collider)), OptionsArray)]);
            GUILayout.Space(10);
            GUILayout.Label("Export", EditorStyles.boldLabel);
            _debug = EditorGUILayout.Toggle("Show Debug", _debug);
            if (GUILayout.Button("Export All"))
                Export(false);
            if (GUILayout.Button("Export Selected"))
                Export(true);
            GUILayout.Space(20);
            GUILayout.Label(Asterisk, EditorStyles.centeredGreyMiniLabel);
        }

        private static void Export(bool selectedOnly) {
            if (!ObjectExporter.Init(_debug, _filePath, CreateAttributes(), _collider)) {
                EditorUtility.DisplayDialog(ProgressbarTitle, "Export is already in progress", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar(ProgressbarTitle, "Starting export", -1f);
            ObjectExporter.TryExport(selectedOnly, ProgressbarUpdate);
            EditorUtility.ClearProgressBar();
        }

        private static slocAttributes CreateAttributes() {
            var attribute = slocAttributes.None;
            if (_lossyColor)
                attribute |= slocAttributes.LossyColors;
            if (_collider != PrimitiveObject.ColliderCreationMode.Unset)
                attribute |= slocAttributes.DefaultColliderMode;
            return attribute;
        }

        private static void ProgressbarUpdate(string info, float progress) => EditorUtility.DisplayProgressBar(ProgressbarTitle, info, progress);

    }

}
