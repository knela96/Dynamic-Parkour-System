using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Climbing/Vaulting Obstacle Action")]
public class ActionVaultObstacle : Action
{
    public string HandAnimVariableName;

    [Range(0, 1f)]
    public float handToIKPositionSpeed = 0.0f;
}
