using UnityEngine;

public abstract class GameObjectBuilder : MonoBehaviour
{
    public abstract GameObject CreateInstance(GameObject source, Transform parent = null);
}
