using UnityEditor;
using UnityEngine;

public static class SecurityCameraCreation
{
    [MenuItem("GameObject/Silent Files/Security Camera", false, 10)]
    private static void Create(MenuCommand command)
    {
        GameObject cameraObject = new GameObject("Security Camera");
        GameObjectUtility.SetParentAndAlign(cameraObject, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(cameraObject, "Create Security Camera");

        FieldOfView fov = Undo.AddComponent<FieldOfView>(cameraObject);
        fov.angle = 90f;
        fov.targetMask = LayerMask.GetMask("Target");
        fov.obstructionMask = LayerMask.GetMask("Obstruction");
        Undo.AddComponent<SecurityCamera>(cameraObject);
        Undo.AddComponent<CameraRotation>(cameraObject);
        Undo.AddComponent<CameraVisual>(cameraObject);
        Selection.activeGameObject = cameraObject;
    }
}
