/*
Dynamic Parkour System grants parkour capabilities to any humanoid character model.
Copyright (C) 2021  Èric Canela Sol
Contact: knela96@gmail.com or @knela96 twitter

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

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
