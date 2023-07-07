
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using com.vrcstuff.udon;


namespace com.vrcstuff.controls.Dial
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DialPC : UdonSharpBehaviour
    {
        public Dial controller;

        public override void Interact()
        {
            controller.DesktopInteract();
        }
    }
}