using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Climbing/Vaulting Reach Action")]
public class ActionVaultReach : Action
{
    public float midHeight;
    public float maxHeight;

    public string HandAnimVariableName;
}
