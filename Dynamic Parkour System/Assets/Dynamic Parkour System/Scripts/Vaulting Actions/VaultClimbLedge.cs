using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    [RequireComponent(typeof(ClimbController))]
    public class VaultClimbLedge : VaultAction
    {
        ClimbController climbController;

        public VaultClimbLedge(ThirdPersonController _vaultingController) : base(_vaultingController)
        {
            climbController = controller.GetComponent<ClimbController>();
        }

        public override bool CheckAction()
        {
            if (controller.isVaulting)
                return false;

            return climbController.ClimbCheck();
        }

        public override bool Update()
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
