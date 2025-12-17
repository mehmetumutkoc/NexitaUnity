#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RemoveCollidersTool
{
    [MenuItem("Tools/Remove Colliders/From Selected Objects")]
    static void RemoveCollidersFromSelected()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("No object selected.");
            return;
        }

        int removedCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);

            foreach (Collider col in colliders)
            {
                Undo.DestroyObjectImmediate(col);
                removedCount++;
            }
        }

        Debug.Log($"Removed {removedCount} colliders from selected objects.");
    }
}
#endif
