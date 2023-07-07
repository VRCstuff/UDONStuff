
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using com.vrcstuff.udon;


namespace com.vrcstuff.controls.Dial
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DialVR : UdonSharpBehaviour
    {

        public Dial controller;

        public override void OnPickup()
        {
            controller.VRInteractStart();
        }

        public override void OnDrop()
        {
            controller.VRInteractEnd();
        }

    }
}