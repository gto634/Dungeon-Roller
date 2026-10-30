using UnityEngine;

internal static class DungeonObjectUtility
{
    internal static Transform CreateRoot(Transform parent)
    {
        var root = new GameObject("Generated Visuals").transform;
        root.SetParent(parent, false);
        return root;
    }

    internal static GameObject CreatePart(GameObject prefab, string name, Transform parent,
        Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject part = Object.Instantiate(prefab, parent, false);
        part.name = name;
        part.transform.localPosition = position;
        part.transform.localRotation = rotation * prefab.transform.localRotation;
        part.transform.localScale = Vector3.Scale(prefab.transform.localScale, scale);
        return part;
    }


    internal static GameObject CreateTile(GameObject prefab, string name, Transform parent,
        Rect area, Vector2 sourceSize)
        => CreatePart(prefab, name, parent, new Vector3(area.x, 0f, -area.y), Quaternion.identity,
            new Vector3(area.width / sourceSize.x, 1f, area.height / sourceSize.y));

    internal static void DestroyOwned(GameObject instance)
    {
        if (instance == null) return;
        if (Application.isPlaying)
        {
            instance.SetActive(false);

            instance.transform.SetParent(null, false);
            Object.Destroy(instance);
        }
        else Object.DestroyImmediate(instance);
    }
}
