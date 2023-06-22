using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Screenshot : MonoBehaviour
{
    [SerializeField] private Camera cam;

    public List<GameObject> sceneObjects;
    public List<BlockTile> dataObjects;

    public string pathFolder;

    private void Awake() => cam = GetComponent<Camera>();

    [ContextMenu("Screenshot")]
    private void ProcessScreenshot() => StartCoroutine(CycleScreenshot());

    private IEnumerator CycleScreenshot()
    {
        for (int i = 0; i < sceneObjects.Count; i++) {
            GameObject obj = sceneObjects[i];
            BlockTile data = dataObjects[i];

            obj.gameObject.SetActive(true);
            yield return null;

            TakeScreenshot($"{Application.dataPath}/{pathFolder}/{data.id}_Icon.png");

            yield return null;
            obj.gameObject.SetActive(false);

            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"Asset/{pathFolder}/{data.id}_Icon.png");
            if (s != null) {
                data.icon = s;
                EditorUtility.SetDirty(data);
            }

            yield return null;
        }
    }

    private void TakeScreenshot(string fullPath)
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        RenderTexture rt = new RenderTexture(256, 256, 24);
        cam.targetTexture = rt;
        Texture2D screenshot = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        cam.Render();
        RenderTexture.active = rt;
        screenshot.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        cam.targetTexture = null;
        RenderTexture.active = null;

        if (Application.isEditor)
            DestroyImmediate(rt);
        else
            Destroy(rt);

        byte[] bytes = screenshot.EncodeToPNG();
        System.IO.File.WriteAllBytes(fullPath, bytes);
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }
}