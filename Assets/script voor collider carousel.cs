using UnityEngine;
using System.IO;

public class scriptvoorcollidercarousel : MonoBehaviour
{
    [SerializeField] private carouselmovingsecond hoofdScript;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private Transform carouselParent;
    
    [Header("Camera Settings")]
    public Camera cameraLeft;
    public Camera cameraRight;
    public int resolutionWidth = 1024;
    public int resolutionHeight = 1024;
    public string savePath = "Assets/CapturedImages/";

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogWarning($"Geen Rigidbody gevonden voor object {other.gameObject.name}. Zorg ervoor dat het object een Rigidbody heeft.");
            return;
        }
        

        if ((targetLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            GameObject geraaktObject = rb.gameObject;
            Transform targetParent = carouselParent;
            
            if (targetParent == null && hoofdScript != null)
            {
                targetParent = hoofdScript.basisTransform;
            }

            if (targetParent == null) return;

            BlokjeTracker tracker = geraaktObject.GetComponent<BlokjeTracker>();
            if (tracker == null)
            {
                tracker = geraaktObject.AddComponent<BlokjeTracker>();
                tracker.targetParent = targetParent;
                tracker.hoofdScript = hoofdScript;
                tracker.cameraLeft = cameraLeft;
                tracker.cameraRight = cameraRight;
                tracker.resolutionWidth = resolutionWidth;
                tracker.resolutionHeight = resolutionHeight;
                tracker.savePath = savePath;
                tracker.Initialize(rb);
            }
        }
    }
}

public class BlokjeTracker : MonoBehaviour
{
    public Transform targetParent;
    public carouselmovingsecond hoofdScript;
    public Camera cameraLeft;
    public Camera cameraRight;
    public int resolutionWidth;
    public int resolutionHeight;
    public string savePath;
    
    private int positieCount = 0;
    private bool isInitialized = false;
    private bool isSettled = false;
    private Rigidbody myRigidbody;
    private float lastRotationY = 0f;
    private float timer = 0f;
    private float draaiInterval = 2f;
    private float delayTimer = 0f;
    public float tijdblokjeslatenvallen = 1;


    public void Initialize(Rigidbody incomingRigidbody)
    {
        if (incomingRigidbody == null) return;
        
        myRigidbody = incomingRigidbody;

        myRigidbody.isKinematic = true;
        transform.SetParent(null, true);

        isInitialized = true;
        isSettled = false;
        delayTimer = 0f;
        timer = 0f;

        tijdblokjeslatenvallen = Mathf.Max(0, UImanagersim.blokjelatenvallen);
        Debug.Log("Blokje " + gameObject.name + " geïnitialiseerd en geparent aan " + targetParent.name);




    
       
        
    }

    private void Update()
    {
        if (!isInitialized || hoofdScript == null || targetParent == null) return;

        

        if (!isSettled)
        {
            delayTimer += Time.deltaTime;
            tijdblokjeslatenvallen = Mathf.Max(0, UImanagersim.blokjelatenvallen);
            if (delayTimer >= tijdblokjeslatenvallen)
            {
                isSettled = true;
                
                transform.SetParent(targetParent, true);

                if (targetParent != null)
                {
                    lastRotationY = targetParent.eulerAngles.y;
                }
                timer = 0f;
                Debug.Log("Blokje " + gameObject.name + " is natuurlijk stilgevallen en nu geparent");

                if (hoofdScript != null)
                {
                    hoofdScript.ActiveerDraaiVanafTrigger();
                }
            }
            return;
        }
        timer += Time.deltaTime;

        if (timer >= draaiInterval)
        {
            timer = 0f;
            
            float currentRotationY = targetParent.eulerAngles.y;
            float rotationDifference = Mathf.Abs(Mathf.DeltaAngle(lastRotationY, currentRotationY));

            hoofdScript.ActiveerDraaiVanafTrigger();
            
            if (rotationDifference > 50f)
            {
                positieCount++;
                lastRotationY = currentRotationY;
                
                Debug.Log("Blokje " + gameObject.name + " is nu op positie " + positieCount + " (rotatie verschil: " + rotationDifference + ")");
                
                string[] photoPaths = TakeSnapshot();
                string detectedPart = TriggerPythonScript(photoPaths);
                
                Debug.Log("Gedetecteerd LEGO blokje: " + detectedPart);

                if (positieCount < 3)
                {
                    hoofdScript.ActiveerDraaiVanafTrigger();
                }
                else
                {
                    Debug.Log("Blokje " + gameObject.name + " heeft 3 posities gehad, wordt verwijderd");
                    Destroy(gameObject);
                }
            }
            else
            {
                if (positieCount < 3)
                {
                    hoofdScript.ActiveerDraaiVanafTrigger();
                }
            }
        }
    }

    private string[] TakeSnapshot()
    {
        if (cameraLeft == null || cameraRight == null)
        {
            Debug.LogWarning("Camera's niet ingesteld voor foto's");
            return new string[0];
        }

        Directory.CreateDirectory(savePath);
        
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        float angle = Vector3.Angle(transform.up, Vector3.up);
        string chosenPath = "";

        if (angle < 90f)
        {
            chosenPath = Path.Combine(savePath, $"cam_left_{gameObject.name}_{timestamp}.png");
            TakeSnapshot(cameraLeft, chosenPath);
        }
        else
        {
            chosenPath = Path.Combine(savePath, $"cam_right_{gameObject.name}_{timestamp}.png");
            TakeSnapshot(cameraRight, chosenPath);
        }

        return new string[] { chosenPath };
    }

    private void TakeSnapshot(Camera camera, string filePath)
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

    private string TriggerPythonScript(string[] imagePaths)
    {
        System.Diagnostics.Process p = new System.Diagnostics.Process();
        p.StartInfo.FileName = "python";

        string scriptPath = Path.Combine(Application.dataPath, "sorter v2", "scripts", "brickverwerken.py");
        
        string args = $"\"{scriptPath}\"";
        foreach (string path in imagePaths)
        {
            args += $" \"{path}\"";
        }
        
        p.StartInfo.Arguments = args;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;

        p.Start();

        string output = p.StandardOutput.ReadToEnd();
        string error = p.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(output)) 
        {
            Debug.Log("Python Output: " + output);
            
            if (output.Contains("Detected LEGO part:"))
            {
                int startIndex = output.IndexOf("Detected LEGO part:") + "Detected LEGO part:".Length;
                int endIndex = output.IndexOf("-", startIndex);
                if (endIndex > startIndex)
                {
                    string partNum = output.Substring(startIndex, endIndex - startIndex).Trim();
                    return partNum;
                }
            }
        }
        
        if (!string.IsNullOrEmpty(error)) Debug.LogError("Python Error: " + error);
        
        return "unknown";
    }
}