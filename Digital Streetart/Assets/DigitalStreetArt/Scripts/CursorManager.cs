using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.InputSystem.XR;
using TMPro;
using UnityEngine.InputSystem;
using Unity.Mathematics;
using System;

public class CursorManager : MonoBehaviour
{
    [SerializeField] Transform cursorTranslatedController;
    [SerializeField] Transform raycastCursor;
    [SerializeField] TrackedPoseDriver stabilizedController;
    [SerializeField] Vector3 planePosition;
    [SerializeField] Vector3 planeNormal;
    [SerializeField] TMP_Text tutorialText;

    [Header("Input")]
    [SerializeField] InputActionReference triggerInput;

    [Header("Spray")]
    [SerializeField][Range(0.1f, 5)] float maxSprayAngle = 0.5f;
    [SerializeField][Range(0.1f, 5)] float fadeOutAngle = 0.4f;
    [SerializeField][Range(1, 10)] int minPaintSploshRadius = 2;
    [SerializeField][Range(1, 10)] int maxPaintSploshRadius = 7;
    [SerializeField][Range(1, 100)] int raycastsPerFrame = 5;

    private Plane plane;
    private int calibrationState = 0;

    [SerializeField] Transform topRightVirtualReferenceTransform;
    [SerializeField] Transform bottomLeftVirtualReferenceTransform;

    // Die RealReferencePoints sollten während des Spiels werden mit zwei initialen Button-Presses o.ä. berechnet werden.
    // Das wäre dann ein Recalibration-Process.
    // Da die technisch gesehen sich nur ändern wenn die Lighthouses sich bewegen,
    // könnte man die auch in einer Datei speichern damit man die nur einmal pro Session neu berechnen muss.
    Vector3 topRightRealReferencePoint = new Vector2(0, 0);
    Vector3 bottomLeftRealReferencePoint = new Vector2(0, 0);
    Vector3 localForwardVector = Vector3.forward;

    Quaternion rotationOffset = Quaternion.identity;

    const int kalmanFilterPositionElements = 10;
    const int movingAverageRotationElements = 5;

    List<Vector3> positionValueList = new(kalmanFilterPositionElements);
    List<Quaternion> rotationValueList = new(movingAverageRotationElements);

    KalmanFilterVector3 kalmanFilter3D = new();

    private void Awake()
    {
        for (int i = 0; i < kalmanFilterPositionElements; i++)
        {
            positionValueList.Add(Vector3.zero);
        }

        for (int i = 0; i < movingAverageRotationElements; i++)
        {
            rotationValueList.Add(Quaternion.identity);
        }
    }

    void Start()
    {
        plane = new Plane(planeNormal, planePosition);
    }

    void Update()
    {
        if (RaycastPositionOnWall(out var pos))
        {
            raycastCursor.position = pos;
        }       
    }

    private void FixedUpdate()
    {
        CursorRotation();
        CursorPosition();
        SprayInput();
    }

    void CursorRotation()
    {
        Quaternion latestValue = stabilizedController.transform.rotation * rotationOffset;
        
        rotationValueList.Add(latestValue);
        rotationValueList.RemoveAt(0);

        cursorTranslatedController.rotation = CalcAvg(0, rotationValueList);

        //Debug.Log($"latest: {latestValue}, cursorRotation: {cursorTranslatedController.rotation}");
    }

    private Quaternion CalcAvg(int iterator, List<Quaternion> values)
    {
        if (iterator < values.Count-1)
        {
            return Quaternion.Lerp(values[iterator], CalcAvg(++iterator, values), 0.5f);
        }
        return values[iterator];
    }

    /*
    private Quaternion CalcAvg(List<Quaternion> rotationlist)
    {
        if (rotationlist.Count == 0)
            throw new ArgumentException();

        float x = 0, y = 0, z = 0, w = 0;
        foreach (var go in rotationlist)
        {
            var q = go;
            x += q.x; y += q.y; z += q.z; w += q.w;
        }
        float k = 1.0f / Mathf.Sqrt(x * x + y * y + z * z + w * w);
        return new Quaternion(x * k, y * k, z * k, w * k);
    }
    */

    Quaternion CalculateAverageQuaternion(List<Quaternion> values)
    {
        // https://gamedev.stackexchange.com/questions/119688/calculate-average-of-arbitrary-amount-of-quaternions-recursion
        float x = 0, y = 0, z = 0, w = 0;

        foreach (Quaternion quat in values)
        {
            x += quat.x;
            y += quat.y;
            z += quat.z;
            w += quat.w;
        }


        float k = 1.0f / Mathf.Sqrt(x * x + y * y + z * z + w * w);

        if (k > 1)
        {
            return values[values.Count-1];
        }
        

        return new Quaternion(x * k, y * k, z * k, w * k);
    }

    void CursorPosition()
    {
        Vector3 latestValue = DistortPositionBasedOnReferencePoints(stabilizedController.transform.position);

        positionValueList.Add(latestValue);
        positionValueList.RemoveAt(0);

        // Apply kalman filter for last X values
        // Info about Q and R here: https://stackoverflow.com/questions/21245167/kalman-filter-in-computer-vision-the-choice-of-q-and-r-noise-covariances
        // in Short:
        // R is: "how much noise is in my measurements / to what extend can I trust my measurement?"
        // Q is: "how much does my measured Element move?"
        cursorTranslatedController.position = kalmanFilter3D.Update(positionValueList, false, 1, 10);

        kalmanFilter3D.Reset();
    }

    bool RaycastPositionOnWall(out Vector3 position)
    {
        position = Vector2.zero;
        float distance;
        Vector3 controllerPosition = cursorTranslatedController.position;
        Vector3 forwardDirection = cursorTranslatedController.forward;
        if (plane.Raycast(new Ray(controllerPosition, forwardDirection), out distance))
        {
            position = controllerPosition + distance * forwardDirection;
            return true;
        }
        return false;
    }

    Vector3 DistortPositionBasedOnReferencePoints(Vector3 controllerPosition)
    {
        float t_x = Mathf.InverseLerp(bottomLeftRealReferencePoint.x, topRightRealReferencePoint.x, controllerPosition.x);
        float t_y = Mathf.InverseLerp(bottomLeftRealReferencePoint.y, topRightRealReferencePoint.y, controllerPosition.y);
        float t_z = Mathf.InverseLerp(bottomLeftRealReferencePoint.z, topRightRealReferencePoint.z, controllerPosition.z);

        Vector3 virtualPosition;
        virtualPosition.x = Mathf.Lerp(bottomLeftVirtualReferenceTransform.position.x, topRightVirtualReferenceTransform.position.x, t_x);
        virtualPosition.y = Mathf.Lerp(bottomLeftVirtualReferenceTransform.position.y, topRightVirtualReferenceTransform.position.y, t_y);
        virtualPosition.z = Mathf.Lerp(bottomLeftVirtualReferenceTransform.position.z, topRightVirtualReferenceTransform.position.z, t_z);

        return virtualPosition;
    }

    public void OnCalibrationButton(InputAction.CallbackContext context)
    {
        if (!context.action.WasPerformedThisFrame()) return;
        switch(calibrationState)
        {
            case 0:
                tutorialText.text = "Press menu to calibrate lower left corner";
                calibrationState++;
                break;
            case 1:
                bottomLeftRealReferencePoint = stabilizedController.transform.position;
                tutorialText.text = "Press menu to calibrate upper right corner";
                calibrationState++;
                break;
            case 2:
                topRightRealReferencePoint = stabilizedController.transform.position;
                tutorialText.text = "Hold your Controller upright and press menu to finish calibration";
                calibrationState++;
                break;
            case 3:
                rotationOffset = Quaternion.Inverse(Quaternion.LookRotation(stabilizedController.transform.forward));
                tutorialText.text = "";
                calibrationState = 0;
                break;
        }
    }

    public void SprayInput()
    {
        //if (!triggerInput.action.WasPerformedThisFrame()) return;

        float inputValue = triggerInput.action.ReadValue<float>();

        if (inputValue < 0.95f) return;

        Texture2D tex = FindTexture();

        if (tex == null) return;

        for (int i = 0; i < raycastsPerFrame; i++)
        {
            // Shoot a hundred slightly differently arranged raycasts

            Quaternion offset;
            // Create small offset
            // Randomize max angle to create fade-out effect
            float randomMaxAngle = UnityEngine.Random.Range(maxSprayAngle, maxSprayAngle + fadeOutAngle);
            offset = Quaternion.AngleAxis(UnityEngine.Random.Range(-randomMaxAngle, randomMaxAngle), Vector3.up) * Quaternion.AngleAxis(UnityEngine.Random.Range(-randomMaxAngle, randomMaxAngle), Vector3.right) * Quaternion.AngleAxis(UnityEngine.Random.Range(-randomMaxAngle, randomMaxAngle), Vector3.forward);

            ShootRaycast(offset, randomMaxAngle);
        }
        
        tex.Apply();
    }

    public Texture2D FindTexture()
    {
        if (!Physics.Raycast(new Ray(cursorTranslatedController.position, cursorTranslatedController.TransformDirection(localForwardVector)), out RaycastHit hit)) return null;

        Renderer rend = hit.transform.GetComponent<Renderer>();
        MeshCollider meshCollider = hit.collider as MeshCollider;

        if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null) return null;

        return rend.material.mainTexture as Texture2D;
    }

    public void ShootRaycast(Quaternion offset, float randomMaxAngle)
    {
        if (!Physics.Raycast(new Ray(cursorTranslatedController.position, offset * cursorTranslatedController.TransformDirection(localForwardVector)), out RaycastHit hit)) return;

        Renderer rend = hit.transform.GetComponent<Renderer>();
        MeshCollider meshCollider = hit.collider as MeshCollider;

        if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null) return;

        Texture2D tex = rend.material.mainTexture as Texture2D;

        Vector2 pixelUV = hit.textureCoord;

        int centerX = Mathf.Clamp((int)(pixelUV.x * tex.width), 0, tex.width);
        int centerY = Mathf.Clamp((int)(pixelUV.y * tex.height), 0, tex.height);

        // radius based on offset distance from center
        float maxAngle = Quaternion.Angle(Quaternion.identity, Quaternion.AngleAxis(randomMaxAngle, Vector3.right));
        float currentAngle = Quaternion.Angle(offset, Quaternion.AngleAxis(randomMaxAngle, Vector3.right));

        int dynamicRadius = (int)Map(currentAngle, 0, maxAngle, minPaintSploshRadius, maxPaintSploshRadius);     

        DrawCircle(tex, Color.red, centerX, centerY, dynamicRadius);
    }

    float Map(float s, float a1, float a2, float b1, float b2)
    {
        // https://forum.unity.com/threads/re-map-a-number-from-one-range-to-another.119437/
        return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
    }

    public Texture2D DrawCircle(Texture2D tex, Color color, int x, int y, int radius = 3)
    {
        // from: https://stackoverflow.com/questions/30410317/how-to-draw-circle-on-texture-in-unity

        float rSquared = radius * radius;

        for (int u = x - radius; u < x + radius + 1; u++)
            for (int v = y - radius; v < y + radius + 1; v++)
                if ((x - u) * (x - u) + (y - v) * (y - v) < rSquared)
                    tex.SetPixel(u, v, color);

        return tex;
    }
}
