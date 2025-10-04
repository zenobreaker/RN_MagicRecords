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


    public GameObject CreatePopUpUI(GameObject target)
    {
        return Instantiate(target, this.gameObject.transform); 
    }
}
