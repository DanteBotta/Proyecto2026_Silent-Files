using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FieldOfView))]

public class FieldOfViewEditor : Editor
{

    private void OnSceneGUI()
    {
        FieldOfView fov = (FieldOfView)target;
        CameraVisual visual = fov.GetComponent<CameraVisual>();
        Color coneColor = visual != null
            ? (fov.canSeePlayer ? visual.DetectedColor : visual.NormalColor)
            : (fov.canSeePlayer ? Color.red : Color.yellow);
        coneColor.a = 1f;

        Handles.color = Color.white;
        Handles.DrawWireArc(fov.transform.position, fov.transform.up, fov.transform.forward, 360, fov.radius);

        Vector3 viewAngle01 = fov.transform.rotation * Quaternion.Euler(0, -fov.angle / 2, 0) * Vector3.forward;
        Vector3 viewAngle02 = fov.transform.rotation * Quaternion.Euler(0, fov.angle / 2, 0) * Vector3.forward;

        Handles.color = coneColor;
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngle01 * fov.radius);
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngle02 * fov.radius);
        if (fov.canSeePlayer && fov.playerRef != null)
        {
            Handles.DrawLine(fov.transform.position, fov.playerRef.transform.position);
        }
    }

}
