using RollerEnemy;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace Roller
{
    class WallSensor:MonoBehaviour
    {
        public RollerAI roller;

        //check if wall was hit
        private void OnTriggerStay(Collider other)
        {            
            //not sure if both are needed
            if (other.gameObject.layer == LayerMask.NameToLayer("Room")
                ||
                other.gameObject.layer == LayerMask.NameToLayer("Colliders"))
            {                
                roller.WallSignal();
            }
        }
    }
}
