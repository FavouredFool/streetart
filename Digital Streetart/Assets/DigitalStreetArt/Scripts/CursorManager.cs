using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.InputSystem.XR;
using TMPro;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    [SerializeField] Transform cursorTranslatedController;
    [SerializeField] Transform raycastCursor;
    [SerializeField] TrackedPoseDriver stabilizedController;
    [SerializeField] Vector3 planePosition;
    [SerializeField] Vector3 planeNormal;
    [SerializeField] TMP_Text tutorialText;
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

    void Start()
    {
        plane = new Plane(planeNormal, planePosition);
    }

    void Update()
    {
        cursorTranslatedController.position = DistortPositionBasedOnReferencePoints(stabilizedController.transform.position);
        cursorTranslatedController.rotation = stabilizedController.transform.rotation * rotationOffset;

        if (RaycastPositionOnWall(out var pos))
        {
            raycastCursor.position = pos;
        }

        if (!Physics.Raycast(new Ray(cursorTranslatedController.position, cursorTranslatedController.forward), out RaycastHit hit)) return;

        Debug.Log("hit: " + hit);
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

    public void OnTriggerButton(InputAction.CallbackContext context)
    {
        
        if (!context.action.WasPerformedThisFrame()) return;

        if (!Physics.Raycast(new Ray(cursorTranslatedController.position, cursorTranslatedController.forward), out RaycastHit hit)) return;

        Renderer rend = hit.transform.GetComponent<Renderer>();
        MeshCollider meshCollider = hit.collider as MeshCollider;

        if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null) return;

        Texture2D tex = rend.material.mainTexture as Texture2D;
        Vector2 pixelUV = hit.textureCoord;
        pixelUV.x *= tex.width;
        pixelUV.y *= tex.height;

        tex.SetPixel((int)pixelUV.x, (int)pixelUV.y, Color.red);
        tex.Apply();

        Debug.Log(pixelUV);
        
    }
}
