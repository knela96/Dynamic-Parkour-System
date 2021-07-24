using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class VaultSlide : VaultAction
    {
        public VaultSlide(VaultingController _vaultingController, Action _actionInfo) : base(_vaultingController)
        {
            //Loads Action Info
            clip = _actionInfo.clip;
            kneeRaycastOrigin = _actionInfo.kneeRaycastOrigin;
            kneeRaycastLength = _actionInfo.kneeRaycastLength;
            landOffset = _actionInfo.landOffset;
        }

        public override bool CheckAction()
        {
            return false;
        }
        public override bool ExecuteAction()
        {
            return false;
        }

        public override void DrawGizmos()
        {

        }
    }
}
