using UnityEngine;

public abstract class DataBase : MonoBehaviour
{
    [SerializeField] protected TextAsset jsonAsset;

    public abstract void Initialize();
}
