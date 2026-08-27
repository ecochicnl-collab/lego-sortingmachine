using UnityEngine;
using System.IO;

public class brickscanner : MonoBehaviour
{
    [Header("camera's")]
    public Camera cameraLeft;
    public Camera cameraRight;
    

    [Header("instellingen")]
    public int resolutionWidth = 1024;
    public int resolutionHeight = 1024;
    public string savePath = "Assets/CapturedImages/";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Directory.CreateDirectory(savePath);

            // Genereer de unieke paden voor beide kanten
            string leftPath = Path.Combine(savePath, "cam_left.png");
            string rightPath = Path.Combine(savePath, "cam_right.png");

            // Maak beide foto's
            TakeSnapshot(cameraLeft, leftPath);
            TakeSnapshot(cameraRight, rightPath);

            Debug.Log("Beide foto's succesvol opgeslagen. Start Brickognize verwerking...");
            
            // Stuur beide paden mee naar het Python-script
            TriggerPythonScript(leftPath, rightPath);
        }
    }

    void TakeSnapshot(Camera camera, string filePath)
    {
        if (camera == null) return;

        RenderTexture rt = new RenderTexture(resolutionWidth, resolutionHeight, 24);
        camera.targetTexture = rt;

        Texture2D screenshot = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
        camera.Render();

        RenderTexture.active = rt;
        screenshot.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);
        screenshot.Apply();

        camera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
        Destroy(screenshot);
    }

    void TriggerPythonScript(string pathLeft, string pathRight)
    {
        System.Diagnostics.Process p = new System.Diagnostics.Process();
        p.StartInfo.FileName = "python";

        string scriptPath = Path.Combine(Application.dataPath, "sorter v2", "scripts", "brickverwerken.py");

        // We stoppen beide paden als losse argumenten tussen aanhalingstekens
        p.StartInfo.Arguments = $"\"{scriptPath}\" \"{pathLeft}\" \"{pathRight}\"";
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;

        p.Start();

        string output = p.StandardOutput.ReadToEnd();
        string error = p.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(output)) Debug.Log("Python Output:\n" + output);
        if (!string.IsNullOrEmpty(error)) Debug.LogError("Python Error:\n" + error);
    }
}