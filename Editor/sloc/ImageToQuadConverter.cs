using UnityEditor;
using UnityEngine;

namespace Editor.sloc
{
    public sealed class ImageToQuadsConverter : EditorWindow
    {
        [MenuItem("sloc/Generate Quads From Image")]
        public static void ShowWindow() => GetWindow<ImageToQuadsConverter>(true, "Convert image into primitive Quads");

        private Texture2D _image;
        private bool _compressImage;

        private const string _mergeQuadsDescription = "Should the multiple quads after each other with same color merge into single one?";

        private void OnGUI()
        {
            GUILayout.Label("Image", EditorStyles.boldLabel);
            _image = (Texture2D)EditorGUI.ObjectField(new Rect(5, 20, 100, 100), _image, typeof(Texture2D), true);
            GUILayout.Space(105);
            _compressImage = EditorGUILayout.Toggle(new GUIContent("Merge Quads*", _mergeQuadsDescription), _compressImage);
            GUILayout.Space(5);
            if (GUILayout.Button("Generate")) Generate();
        }

        private void Generate()
        {
            GameObject rootObject = new GameObject($"{_image.name}-container");
            if (_compressImage)
            {
                GenerateQuadsMerged(rootObject);
                return;
            }

            GenerateQuadsNotMerged(rootObject);
        }

        public void GenerateQuadsMerged(GameObject parent)
        {
            if (_image == null)
            {
                Debug.LogError("Image is null");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Colors"))
            {
                AssetDatabase.CreateFolder("Assets", "Colors");
            }

            for (int j = 0; j < _image.height; j++)
            {
                int i = 0;

                while (i < _image.width)
                {
                    Color color = _image.GetPixel(i, j);
                    int runLength = 1;
                    while (i + runLength < _image.width && _image.GetPixel(i + runLength, j) == color)
                    {
                        runLength++;
                    }

                    if (color.a > 0f)
                    {
                        CreatePrimitive(new Vector3(i + runLength / 2f, 1, j), color, runLength, parent);
                    }

                    i += runLength;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void GenerateQuadsNotMerged(GameObject parent)
        {
            if (_image == null)
            {
                Debug.LogError("Image is null");
                return;
            }

            for (int i = 0; i < _image.width; i++)
            {
                for (int j = 0; j < _image.height; j++)
                {
                    Color color = _image.GetPixel(i, j);
                    if (color.a > 0f) CreatePrimitive(new Vector3(i, 1, j), _image.GetPixel(i, j), 1, parent);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreatePrimitive(Vector3 position, Color color, int runLength, GameObject parent)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            gameObject.transform.position = position;
            gameObject.transform.localScale = new Vector3(runLength, 1, 1f);
            gameObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            string materialPath = $"Assets/Colors/Material-{color.ToString()}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                material.color = color;
                AssetDatabase.CreateAsset(material, materialPath);
            }

            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.material = material;

            DestroyImmediate(gameObject.GetComponent<MeshCollider>());

            gameObject.transform.SetParent(parent.transform);
        }
    }
}