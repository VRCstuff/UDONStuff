
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using com.vrcstuff.udon;

public enum WheelAxis
{
    X, 
    Y, 
    Z
}

namespace com.vrcstuff.controls.wheel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Wheel : UdonSharpBehaviour
    {

        #region Settings
        [Header("References")]
        public GameObject wheel;
        public GameObject grabTarget;
        public GameObject grabResetTarget;
        [Header("Audio Settings")]
        //public AudioSource crankSoundSource;
        [Range(0, 1)]
        public float audioSourceVolume = 1f;
        public AudioClip crankAudioClip;

        [SerializeField] private AudioSource myAudioSource;
        [SerializeField] private AudioSource sharedAudioSource;
        [SerializeField] private float lowestPitch = 0.9f, highestPitch = 1.1f;
        public float soundVolume = 0.5f;

        [Header("Settings")]
        [Tooltip("Only allows players to turn the wheel between the below angles, the wheel will stop when it hits either end")]
        public bool clampRotation = true;
        [Tooltip("Where the wheel will rotate to when the script starts")]
        public float defaultRotation = 0f;
        [Tooltip("How far anti-clockwise the wheel can be turned")]
        public float minimumRotation = 0.0f;
        [Tooltip("How far clockwise the wheel can be turned")]
        public float maximumRotation = 16f;
        public WheelAxis wheelAxis = WheelAxis.Y;
        public KeyCode desktopLeftButton = KeyCode.Q;
        public KeyCode desktopRightButton = KeyCode.E;
        [Range(0.01f, 0.2f)]
        public float desktopSpeed = 0.1f;
        [Header("Send Data To Other Objects")]
        [Tooltip("The UdonBehaviour to update with the current angle of the wheel")]
        public UdonBehaviour udonBehaviourToUpdate;
        [Tooltip("The name of a variable in the UdonBehaviour to update with a float between 0-360")]
        public string udonBehaviourAngleVariableName;
        [Tooltip("The name of a variable in the UdonBehaviour to update with a float between 0-1. Reflects the position of the wheel between min/max rotations")]
        public string udonBehaviourFloatVariableName;
        [Tooltip("What function to call when a player lets go of the wheel")]
        public string udonBehaviourOnDropEventName;
        [Tooltip("What function to call when the wheel has been turned enough to play a crank noise")]
        public string udonBehaviourOnCrankEventName;
        [Header("Lever Mode")]
        [Tooltip("REQUIRES(Clamp Rotation) - When player lets go of the wheel it will animate to the closest limit and play a sound")]
        public bool actAsLever = false;
        [Tooltip("Useful for making a lever that needs to be held up then drops back when you let go")]
        public bool returnToDefaultPositionOnDrop = false;
        [Tooltip("A curve used to make the lever snap animation look nice")]
        public AnimationCurve wheelLeverAnimationCurve;
        public AudioClip wheelLeverSnapAudioClip;
        [Tooltip("The name of a variable in the UdonBehaviour to update with a 0 or 1 (left/right) when the lever is changed")]
        public string udonBehaviourLeverIntVariableName;
        [Tooltip("The name of a variable in the UdonBehaviour to update with a false/true (left/right) when the lever is changed")]
        public string udonBehaviourLeverBoolVariableName;
        #endregion

        #region Internal Vars
        public bool handleIsGrabbed = false;

        private bool hasLastWheelHandleDir = false;
        [UdonSynced] public float _currentWheelAngle = 0;
        private float lastWheelDir = 0.0f;
        private float lastClickTime = 0f;

        private VRC_Pickup.PickupHand currentHand;
        private PuttSync puttSync;

        private float wheelAnimationTime = -1f;
        private Quaternion wheelAnimationStartRotation;
        private Quaternion wheelAnimationEndRotation;
        public bool WheelIsAnimating
        {
            get => wheelAnimationTime >= 0f;
        }
        private bool wheelIsCurrentlyFollowingSync = false;
        private Rigidbody grabTargetRB;
        #endregion

        private void UpdateUdonBehaviour(bool isSnapping)
        {
            if (udonBehaviourToUpdate != null)
            {
                // Update the progress float variable between 0-1
                if (udonBehaviourFloatVariableName != null && udonBehaviourFloatVariableName.Length > 0)
                    udonBehaviourToUpdate.SetProgramVariable(udonBehaviourFloatVariableName, (_currentWheelAngle - minimumRotation) / (maximumRotation - minimumRotation));

                // Update the wheel angle variable 0-360
                if (udonBehaviourAngleVariableName != null && udonBehaviourAngleVariableName.Length > 0)
                    udonBehaviourToUpdate.SetProgramVariable(udonBehaviourAngleVariableName, wheel.transform.localEulerAngles.y);

                if (isSnapping && udonBehaviourOnDropEventName != null && udonBehaviourOnDropEventName.Length > 0)
                    udonBehaviourToUpdate.SendCustomEvent(udonBehaviourOnDropEventName);

                if (actAsLever && isSnapping)
                {
                    if (udonBehaviourLeverBoolVariableName != null && udonBehaviourLeverBoolVariableName.Length > 0)
                        udonBehaviourToUpdate.SetProgramVariable(udonBehaviourLeverBoolVariableName, _currentWheelAngle == maximumRotation);
                    if (udonBehaviourLeverIntVariableName != null && udonBehaviourLeverIntVariableName.Length > 0)
                        udonBehaviourToUpdate.SetProgramVariable(udonBehaviourLeverIntVariableName, _currentWheelAngle == maximumRotation ? 1 : 0);
                }
            }
        }

        void Start()
        {
            puttSync = GetComponent<PuttSync>();
            grabTargetRB = grabTarget.GetComponent<Rigidbody>();
            if (actAsLever)
            {
                clampRotation = true;
                float wheelMidpoint = (minimumRotation + maximumRotation) / 2;
                _currentWheelAngle = defaultRotation > wheelMidpoint ? maximumRotation : minimumRotation;
            }
            else
            {
                _currentWheelAngle = defaultRotation;
            }

            switch (wheelAxis)
            {
                case WheelAxis.X:
                    wheel.transform.localRotation = Quaternion.Euler(_currentWheelAngle * Mathf.Rad2Deg, 0.0f, 0.0f);
                    break;
                case WheelAxis.Y:
                    wheel.transform.localRotation = Quaternion.Euler(0.0f, _currentWheelAngle * Mathf.Rad2Deg, 0.0f);
                    break;
                case WheelAxis.Z:
                    wheel.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, _currentWheelAngle * Mathf.Rad2Deg);
                    break;
            }

            UpdateUdonBehaviour(true);

            ReattachHandleTimer();
        }

        /// <summary>
        /// Maybe stops grab handles from flying away??
        /// </summary>
        private void ReattachGrabHandle()
        {
            if (grabTargetRB != null)
            {
                grabTargetRB.velocity = Vector3.zero;
                grabTargetRB.angularVelocity = Vector3.zero;
                grabTargetRB.isKinematic = true;
            }
            grabTarget.transform.position = grabResetTarget.transform.position;
            grabTarget.transform.rotation = grabResetTarget.transform.rotation;
        }

        public void ReattachHandleTimer()
        {
            ReattachGrabHandle();

            SendCustomEventDelayedSeconds(nameof(ReattachHandleTimer), 1);
        }

        public void HandleSnap()
        {
            float lerpProgress = wheelLeverAnimationCurve.Evaluate(wheelAnimationTime / 0.1f);

            wheelAnimationTime += Time.deltaTime;

            wheel.transform.localRotation = Quaternion.Lerp(wheelAnimationStartRotation, wheelAnimationEndRotation, lerpProgress);

            ReattachGrabHandle();

            if (lerpProgress >= 1f)
            {
                wheelAnimationTime = -1f;
                //if (crankSoundSource != null && wheelLeverSnapAudioClip != null)
                //    crankSoundSource.PlayOneShot(wheelLeverSnapAudioClip, audioSourceVolume);
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlaySnapSound");
                return;
            }

            SendCustomEventDelayedFrames(nameof(HandleSnap), 0);
        }

        public void HandleLocalPlayerRotation()
        {
            if (!handleIsGrabbed)
            {
                ReattachGrabHandle();
                return;
            }

            var oldReelAngle = _currentWheelAngle;
            var deltaAngle = 0.0f;

            if (Input.GetKey(desktopLeftButton))
            {
                deltaAngle = -desktopSpeed;
                hasLastWheelHandleDir = false;
            }
            else if (Input.GetKey(desktopRightButton))
            {
                deltaAngle = desktopSpeed;
                hasLastWheelHandleDir = false;
            }
            else
            {
                // Work out the current angle of the wheel
                var inverseRot = Quaternion.Inverse(transform.rotation);

                var wheelHandleDelta = inverseRot * (grabTarget.transform.position - wheel.transform.position);
                var wheelHandleDir = Mathf.Atan2(wheelHandleDelta.x, wheelHandleDelta.z);
                switch (wheelAxis)
                {
                    case WheelAxis.X:
                        wheelHandleDir = Mathf.Atan2(wheelHandleDelta.z, wheelHandleDelta.y);
                        break;
                    case WheelAxis.Y:
                        wheelHandleDir = Mathf.Atan2(wheelHandleDelta.x, wheelHandleDelta.z);
                        break;
                    case WheelAxis.Z:
                        wheelHandleDir = Mathf.Atan2(wheelHandleDelta.y, wheelHandleDelta.z);
                        break;
                }

                if (hasLastWheelHandleDir)
                    deltaAngle = AngleDiff(wheelHandleDir - lastWheelDir);

                hasLastWheelHandleDir = true;
                lastWheelDir = wheelHandleDir;
            }

            if (deltaAngle != 0.0f)
            {
                // Clamp the angle so the player can't spin it past the limits
                if (clampRotation)
                    _currentWheelAngle = Mathf.Clamp(_currentWheelAngle + deltaAngle, minimumRotation, maximumRotation);
                else
                    _currentWheelAngle = _currentWheelAngle + deltaAngle;

                // Work out if we need to play a sound and haptics yet
                var hapticsInterval = Mathf.PI / 6.0f;
                if (Mathf.RoundToInt(oldReelAngle / hapticsInterval) != Mathf.RoundToInt(_currentWheelAngle / hapticsInterval))
                {
                    var now = Time.time;

                    if (now - lastClickTime > 0.1f)
                    {
                        if (udonBehaviourOnCrankEventName != null && udonBehaviourOnCrankEventName.Length > 0)
                            udonBehaviourToUpdate.SendCustomEvent(udonBehaviourOnCrankEventName);

                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayCrankSound");
                        lastClickTime = now;
                    }

                    if (Utils.LocalPlayerIsValid())
                        Networking.LocalPlayer.PlayHapticEventInHand(currentHand, 0.3f, 1f, 1f);
                }

                if (puttSync != null)
                    puttSync.RequestFastSync();

                UpdateUdonBehaviour(false);
            }

            // Rotate the wheel to match the total angle
            switch (wheelAxis)
            {
                case WheelAxis.X:
                    wheel.transform.localRotation = Quaternion.Euler(_currentWheelAngle * Mathf.Rad2Deg, 0.0f, 0.0f);
                    break;
                case WheelAxis.Y:
                    wheel.transform.localRotation = Quaternion.Euler(0.0f, _currentWheelAngle * Mathf.Rad2Deg, 0.0f);
                    break;
                case WheelAxis.Z:
                    wheel.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, _currentWheelAngle * Mathf.Rad2Deg);
                    break;
            }

            ReattachGrabHandle();

            SendCustomEventDelayedFrames(nameof(HandleLocalPlayerRotation), 0);
        }

        public override void OnDeserialization()
        {
            if (!wheelIsCurrentlyFollowingSync)
                SendCustomEventDelayedFrames(nameof(SmoothToTargetRotation), 0);

            UpdateUdonBehaviour(true);

            ReattachGrabHandle();
        }

        public void SmoothToTargetRotation()
        {
            ReattachGrabHandle();

            wheelIsCurrentlyFollowingSync = true;
            Quaternion targetRotation = Quaternion.identity;
            switch (wheelAxis)
            {
                case WheelAxis.X:
                    targetRotation = Quaternion.Euler(_currentWheelAngle * Mathf.Rad2Deg, 0.0f, 0.0f);
                    break;
                case WheelAxis.Y:
                    targetRotation = Quaternion.Euler(0.0f, _currentWheelAngle * Mathf.Rad2Deg, 0.0f);
                    break;
                case WheelAxis.Z:
                    targetRotation = Quaternion.Euler(0.0f, 0.0f, _currentWheelAngle * Mathf.Rad2Deg);
                    break;
            }
            // Try to smooth out the lerps
            Quaternion newRotation = Quaternion.Lerp(wheel.transform.localRotation, targetRotation, 1.0f - Mathf.Pow(0.001f, Time.deltaTime));

            if (Quaternion.Angle(wheel.transform.localRotation, newRotation) > 0.001f)
            {
                // Rotate the wheel to match the total angle
                wheel.transform.localRotation = newRotation;

                SendCustomEventDelayedFrames(nameof(SmoothToTargetRotation), 0);
            }
            else
            {
                // Rotate the wheel to match the total angle
                wheel.transform.localRotation = targetRotation;

                wheelIsCurrentlyFollowingSync = false;
            }
        }

        public void InteractStart()
        {
            Utils.Log(this, "Picked up");
            Utils.SetOwner(Networking.LocalPlayer, gameObject);
            if (udonBehaviourToUpdate != null)
                Utils.SetOwner(Networking.LocalPlayer, udonBehaviourToUpdate.gameObject);

            handleIsGrabbed = true;

            // Work out which hand the VR player is holding the dial with
            var leftPickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            var rightPickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
            if (leftPickup != null && leftPickup.gameObject == grabTarget)
                currentHand = VRC_Pickup.PickupHand.Left;
            else if (rightPickup != null && rightPickup.gameObject == grabTarget)
                currentHand = VRC_Pickup.PickupHand.Right;
            else
                currentHand = VRC_Pickup.PickupHand.None;

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "OnOtherUserGrabWheel");

            SendCustomEventDelayedFrames(nameof(HandleLocalPlayerRotation), 0);
        }

        public void InteractEnd()
        {
            handleIsGrabbed = false;
            hasLastWheelHandleDir = false;

            currentHand = VRC_Pickup.PickupHand.None;

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "OnOtherUserReleaseWheel");

            if (actAsLever)
            {
                float wheelMidpoint = (minimumRotation + maximumRotation) / 2;

                if (returnToDefaultPositionOnDrop)
                    _currentWheelAngle = defaultRotation;

                _currentWheelAngle = _currentWheelAngle > wheelMidpoint ? maximumRotation : minimumRotation;

                wheelAnimationStartRotation = wheel.transform.localRotation;
                switch (wheelAxis)
                {
                    case WheelAxis.X:
                        wheelAnimationEndRotation = Quaternion.Euler(_currentWheelAngle * Mathf.Rad2Deg, 0.0f, 0.0f);
                        break;
                    case WheelAxis.Y:
                        wheelAnimationEndRotation = Quaternion.Euler(0.0f, _currentWheelAngle * Mathf.Rad2Deg, 0.0f);
                        break;
                    case WheelAxis.Z:
                        wheelAnimationEndRotation = Quaternion.Euler(0.0f, 0.0f, _currentWheelAngle * Mathf.Rad2Deg);
                        break;
                }
                wheelAnimationTime = 0f;
                HandleSnap();
            }

            UpdateUdonBehaviour(true);

            ReattachGrabHandle();

            if (Networking.IsOwner(Networking.LocalPlayer, gameObject))
                RequestSerialization();
        }

        /// <summary>
        /// Plays the crank sound to all nearby players so people know something is happening
        /// </summary>
        public void PlayCrankSound()
        {
            if (sharedAudioSource != null)
                sharedAudioSource.PlaySound(crankAudioClip, this.transform.position, .96f, 1.04f);
            else if (myAudioSource != null)
                myAudioSource.PlaySound(crankAudioClip, Vector3.zero, .96f, 1.04f);
        }

        /// <summary>
        /// A network event that is called when another player has grabbed the wheel. Used to disable pickups so only 1 player can use them at the same time.
        /// </summary>
        public void OnOtherUserGrabWheel()
        {
            if (Networking.LocalPlayer != null && !Networking.IsOwner(Networking.LocalPlayer, gameObject))
            {
                grabTarget.GetComponent<VRC_Pickup>().pickupable = false;
                grabTarget.GetComponent<VRC_Pickup>().Drop();
            }

            ReattachGrabHandle();
        }

        /// <summary>
        /// A network event that is called when another player has released the wheel
        /// </summary>
        public void OnOtherUserReleaseWheel()
        {
            grabTarget.GetComponent<VRC_Pickup>().pickupable = true;

            ReattachGrabHandle();
        }

        /// <summary>
        /// Magic function that makes wheel spin
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        private float AngleDiff(float angle)
        {
            if (angle < 0)
            {
                return -(-angle + Mathf.PI) % (2.0f * Mathf.PI) + Mathf.PI;
            }
            else
            {
                return (angle + Mathf.PI) % (2.0f * Mathf.PI) - Mathf.PI;
            }
        }

        public void UpdateVolume()
        {
            if (sharedAudioSource != null)
                sharedAudioSource.volume = soundVolume;
            else if (myAudioSource != null)
                myAudioSource.volume = soundVolume;
        }

        public void PlaySnapSound()
        {
            if (sharedAudioSource != null)
                sharedAudioSource.PlaySound(wheelLeverSnapAudioClip, transform.position, .96f, 1.04f);
            else if (myAudioSource != null)
                myAudioSource.PlaySound(wheelLeverSnapAudioClip, Vector3.zero, .96f, 1.04f);
        }
    }
}