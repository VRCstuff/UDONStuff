
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using com.vrcstuff.udon;


namespace com.vrcstuff.controls.Dial
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DialAutoOff : UdonSharpBehaviour
    {
        #region Public Settings
        [Tooltip("The Dial that controls this automatic toggle")]
        public Dial controller = null;

        [Tooltip("If the player leaves the collider area the switch will be set to the default position automatically. If left empty it will look for a Box Collider on this Object.")]
        public BoxCollider autoOffCollider = null;

        [Range(0, 10)]
        [Tooltip("Amount of seconds between checking if the player is inside the collider")]
        public float autoOffCheckDelay = 0.5f;

        [Tooltip("What position the Dial should be set to when the player leaves the area")]
        public int dialOffPosition = 0;
        #endregion

        #region Internal DialAutoOff Variables
        int currentDialPosition = 0;

        int playerInside = -1;

        float timeSinceLastLocationCheck = 0f;
        #endregion

        void Start()
        {
            if (controller != null)
                currentDialPosition = controller.GetCurrentPosition();

            if (autoOffCollider == null)
                autoOffCollider = GetComponent<BoxCollider>();

            if (controller != null && controller.syncDialPosition)
            {
                this.gameObject.SetActive(false);
                Utils.Log(this, "Disabling auto-off script as the Dial is synced and this isn't supported yet.");
                return;
            }

            if (autoOffCollider.gameObject == this.gameObject)
            {
                return;
            }

            if (autoOffCollider != null)
            {
                SendCustomEventDelayedSeconds(nameof(_CheckColliderState), autoOffCheckDelay);
            }
        }

        public void _CheckColliderState()
        {
            if (autoOffCollider == null)
                return;

            if (Networking.LocalPlayer == null || !Networking.LocalPlayer.IsValid())
            {
                SendCustomEventDelayedSeconds(nameof(_CheckColliderState), autoOffCheckDelay);
                return;
            }

            timeSinceLastLocationCheck += Time.deltaTime;

            if (timeSinceLastLocationCheck > autoOffCheckDelay)
            {
                int playerInside = autoOffCollider.bounds.Contains(Networking.LocalPlayer.GetPosition()) ? 1 : 0;

                if (this.playerInside != playerInside)
                {
                    this.OnPlayerStateChange(Networking.LocalPlayer, playerInside == 1);

                    this.playerInside = playerInside;
                }

                timeSinceLastLocationCheck = 0;
            }

            SendCustomEventDelayedSeconds(nameof(_CheckColliderState), autoOffCheckDelay);
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer && autoOffCollider == null)
                this.OnPlayerStateChange(player, true);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer && autoOffCollider == null)
                this.OnPlayerStateChange(player, false);
        }

        /// <summary>
        /// Triggered when we detect the player has entered/left the collider area. We do it this way because the OnEnter/OnExit events don't always work if you teleport into the area.
        /// </summary>
        /// <param name="player">The player that entered the area (Should only be the local player)</param>
        /// <param name="inside">Whether or not the player is now inside the area</param>
        private void OnPlayerStateChange(VRCPlayerApi player, bool inside)
        {
            if (controller != null && player.isLocal)
            {
                if (inside)
                {
                    controller.SetDialPosition(currentDialPosition);
                }
                else
                {
                    // Store what position the Dial is in
                    currentDialPosition = controller.GetCurrentPosition();
                    // Set the Dial to it's default position
                    controller.SetDialPosition(dialOffPosition);
                }
            }
        }
    }
}