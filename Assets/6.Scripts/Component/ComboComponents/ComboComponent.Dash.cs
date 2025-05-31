using System;
using UnityEngine;

public partial class ComboComponent : MonoBehaviour
{
    private void TryProcess_Dash(InputCommand newInput)
    {
        ResetCombo();
        
        dash?.TryDash();
    }
}
