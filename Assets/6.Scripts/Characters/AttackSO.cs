using UnityEngine;
using UnityEngine.Playables;

[CreateAssetMenu(menuName = "Attacks/Normal Attack")]
public class AttackSO : ScriptableObject
{
    public AnimatorOverrideController animatorOv;
    public float damage;
}
