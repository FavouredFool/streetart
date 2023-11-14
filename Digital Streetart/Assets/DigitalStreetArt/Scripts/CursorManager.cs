using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.InputSystem.XR;

public class CursorManager : MonoBehaviour
{
    [SerializeField] Transform cursor;
    [SerializeField] TrackedPoseDriver stabilizedController;

    [SerializeField] Transform topRightVirtualReferenceTransform;
    [SerializeField] Transform bottomLeftVirtualReferenceTransform;

    // Die RealReferencePoints sollten während des Spiels werden mit zwei initialen Button-Presses o.ä. berechnet werden.
    // Das wäre dann ein Recalibration-Process.
    // Da die technisch gesehen sich nur ändern wenn die Lighthouses sich bewegen,
    // könnte man die auch in einer Datei speichern damit man die nur einmal pro Session neu berechnen muss.
    Vector2 topRightRealReferencePoint = new Vector2(0.3f, 2.0f);
    Vector2 bottomLeftRealReferencePoint = new Vector2(-0.6f, 1.4f);

    void Start()
    {
    }

    void Update()
    {
        Vector2 positionOnWall = ProjectCursorPositionOnWall(stabilizedController.transform.position);

        cursor.position = DistortPositionBasedOnReferencePoints(positionOnWall);
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

    Vector3 ProjectCursorPositionOnWall(Vector3 controllerPosition)
    {
        return new Vector3(controllerPosition.x, controllerPosition.y, 0);
    }
}
