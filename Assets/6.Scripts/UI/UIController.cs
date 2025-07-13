using UnityEngine;

public class UIController : MonoBehaviour
{
    private void Start()
    {
        
    }


    public void SetActiveTarget(GameObject target)
    {
        target.SetActive(!target.activeInHierarchy);
    }

}
