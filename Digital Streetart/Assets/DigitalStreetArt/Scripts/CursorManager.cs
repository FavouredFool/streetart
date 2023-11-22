using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.InputSystem.XR;
using TMPro;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    [SerializeField] Transform cursor;
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
    Vector3 topRightRealReferencePoint = new Vector2(0.3f, 2.0f);
    Vector3 bottomLeftRealReferencePoint = new Vector2(-0.6f, 1.4f);
    Vector3 bottomRightRealReferencePoint = new Vector2(0, 0);
    Vector3 localForwardVector = Vector3.forward;

    void Start()
    {
        plane = new Plane(planeNormal, planePosition);
    }

    void Update()
    {
        Vector2 positionOnWall = ProjectCursorPositionOnWall(stabilizedController.transform.position);

        cursor.position = DistortPositionBasedOnReferencePoints(positionOnWall);

        if(RaycastPositionOnWall(out var pos))
        {
            raycastCursor.position = DistortPositionBasedOnReferencePoints(pos);
        }
    }

    Vector2 DistortPositionBasedOnReferencePoints(Vector2 positionOnWall)
    {
        float t_x = Mathf.InverseLerp(bottomLeftRealReferencePoint.x, topRightRealReferencePoint.x, positionOnWall.x);
        float t_y = Mathf.InverseLerp(bottomLeftRealReferencePoint.y, topRightRealReferencePoint.y, positionOnWall.y);

        Vector2 virtualPosition;
        virtualPosition.x = Mathf.Lerp(bottomLeftVirtualReferenceTransform.position.x, topRightVirtualReferenceTransform.position.x, t_x);
        virtualPosition.y = Mathf.Lerp(bottomLeftVirtualReferenceTransform.position.y, topRightVirtualReferenceTransform.position.y, t_y);

        return virtualPosition;
    }

    bool RaycastPositionOnWall(out Vector2 position)
    {
        position = Vector2.zero;
        float distance;
        Vector3 controllerPosition = stabilizedController.transform.position;
        Vector3 forwardDirection = stabilizedController.transform.TransformDirection(localForwardVector);
        if (plane.Raycast(new Ray(controllerPosition, forwardDirection), out distance))
        {
            Vector3 hitLocation = controllerPosition + distance * forwardDirection;
            position = new Vector2(hitLocation.x, hitLocation.y);
            return true;
        }
        return false;
    }

    Vector3 ProjectCursorPositionOnWall(Vector3 controllerPosition)
    {
        return new Vector3(controllerPosition.x, controllerPosition.y, 0);
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
                tutorialText.text = "Press menu to calibrate lower right corner";
                calibrationState++;
                break;
            case 3:
                bottomRightRealReferencePoint = stabilizedController.transform.position;
                tutorialText.text = "Hold your Controller upright and press menu to finish calibration";
                calibrationState++;
                break;
            case 4:
                plane = new Plane(bottomLeftRealReferencePoint, topRightRealReferencePoint, bottomRightRealReferencePoint);
                localForwardVector = stabilizedController.transform.InverseTransformDirection(-plane.normal);
                tutorialText.text = "";
                calibrationState = 0;
                break;
        }
    }
}
