using UnityEngine;

public static class CommonTool
{
    /// <summary>
    /// 加载Item
    /// </summary>
    public static GameObject LoadItem(ItemEnum itemEnum, string info)
    {
        GameObject go = Object.Instantiate(Resources.Load<GameObject>("Prefab/" + itemEnum.ToString()));
        TextMesh textmesh = go.transform.Find("id").GetComponent<TextMesh>();
        textmesh.text = info;
        return go;
    }

    /// <summary>
    /// 设置Item的颜色
    /// </summary>
    public static void SetMaterialColor(GameObject go, MaterialEnum materialEnum)
    {
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        Material material = Resources.Load<Material>("Mat/" + materialEnum.ToString());
        mr.material = material;
    }

    public static int index;
    /// <summary>
    /// 设置Cell的颜色
    /// </summary>
    public static void SetCellColor(GameObject gameObject)
    {
        index++;
        if (index >= 9)
            index = 1;

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        Material material = Resources.Load<Material>("Mat/CellMats/Cell" + (index % 10).ToString());
        meshRenderer.material = material;
    }
}

public enum ItemEnum
{
    CellItem,
    EntityItem
}

public enum MaterialEnum
{
    red,
    green,
    blue,
    cyan,
    magenta,
    yellow,
    black,
    white,
    gray
}
