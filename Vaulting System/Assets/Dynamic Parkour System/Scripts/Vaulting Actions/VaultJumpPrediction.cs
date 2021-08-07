using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class VaultJumpPrediction : VaultAction
    {
        JumpPredictionController jumpController;


        public VaultJumpPrediction(ThirdPersonController _vaultingController) : base(_vaultingController)
        {
            jumpController = controller.GetComponent<JumpPredictionController>();
        }

        public override bool CheckAction()
        {
            if (controller.isVaulting)
                return false;

            //Ensures that curPoint is Pole type
            if (jumpController.curPoint != null)
            {
                if (jumpController.curPoint.transform.parent.parent.tag != "Pole")
                    jumpController.curPoint = null;
            }

            jumpController.CheckJump();

            return !jumpController.hasArrived();
        }

        public override bool FixedUpdate()
        {
            if (!jumpController.hasArrived())
            {
                jumpController.FollowParabola(0.7f);
            }

            return true;
        }

        public override bool Update()
        {
            bool ret = false;

            jumpController.hasEndedJump();

            if (!jumpController.hasArrived())
                ret = true;

            if (jumpController.hasArrived())
            {
                ret = jumpController.isMidPoint();
            }

            return ret;
        }
    }
}
