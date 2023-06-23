
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace com.vrcstuff.udon
{
    public class InteractForwarder : UdonSharpBehaviour
    {
        public UdonSharpBehaviour listener;
        public string onPickupFunctionName = "OnPlayerPickup";
        public string onDropFunctionName = "OnPlayerDrop";

        public override void OnPickup()
        {
            listener.SendCustomEvent(onPickupFunctionName);
        }

        public override void OnDrop()
        {
            listener.SendCustomEvent(onDropFunctionName);
        }
    }
}