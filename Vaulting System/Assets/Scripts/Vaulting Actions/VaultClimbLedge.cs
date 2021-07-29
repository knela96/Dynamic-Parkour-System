using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class VaultClimbLedge : VaultAction
    {
        ClimbController climbController;

        public VaultClimbLedge(VaultingController _vaultingController) : base(_vaultingController)
        {
            climbController = vaultingController.controller.GetComponent<ClimbController>();
        }

        public override bool CheckAction()
        {
            return climbController.ClimbCheck();
        }

        public override bool ExecuteAction()
        {
            return climbController.ClimbUpdate();
        }

        public override void OnAnimatorIK(int layerIndex)
        {
            climbController.onAnimatorIK(layerIndex);
        }

        public override void DrawGizmos()
        {
            climbController.onDrawGizmos();
        }
    }
}
