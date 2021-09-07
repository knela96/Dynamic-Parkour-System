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
    [CreateAssetMenu(menuName = "Climbing/Vaulting Action")]
    public class Action : ScriptableObject
    {
        public AnimationClip clip;

        public Vector3 kneeRaycastOrigin;
        public float kneeRaycastLength = 1.0f;
        public float landOffset = 0.7f;
        public float startDelay = 0.0f;
        public LayerMask layer;
        public string tag;
    }
}
