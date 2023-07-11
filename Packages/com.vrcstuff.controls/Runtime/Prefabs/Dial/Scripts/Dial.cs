
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using com.vrcstuff.udon;



namespace com.vrcstuff.controls.Dial
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Dial : UdonSharpBehaviour
    {
        #region Dial Object References
        [Header("Object References")]
        public GameObject knobVR;
        public GameObject knobPC;
        [Header("Audio")]
        [Tooltip("The default sound soource for this Dial. If you specify a shared source below this audio source will not be used")]
        public AudioSource mySoundSource;
        /// <summary>
        /// If you declare this. It will play the sound on a shared sound source. This is only a game object with a sound source attached to it that will move.
        /// </summary>
        [Tooltip("If you declare this. It will play the sound on the shared speaker.")]
        public AudioSource sharedSoundSource;
        [SerializeField] private AudioClip KnobSoundClip;
        public float soundVolume = 0.5f;
        [SerializeField, Range(0.7f, 1.2f)] 
        private float lowestPitch = 0.95f, highestPitch = 1.05f;

        [Tooltip("Reference to an object that represents each position on the Dial")]
        public GameObject newPositionMarker;
        #endregion

        #region Public Dial Settings
        [Header("Settings")]
        [Tooltip("Toggles Syncing for the Dial position between players")]
        public bool syncDialPosition = false;
        [Tooltip("Toggles Syncing for the Dial Sound when other players move a synced Dial")]
        public bool syncDialClick = false;
        [Tooltip("Toggles whether the dial will snap to the nearest position when the player lets go")]
        public bool snapToClosestPosition = true;
        [Tooltip("Toggles whether the Dial can be turned smoothly by VR players")]
        public bool smoothTurning = true;
        [Range(0, 60)]
        [Tooltip("Controls how many positions the dial has")]
        public int numberOfPositions = 3;
        [Range(0, 60)]
        [Tooltip("Defines which state the dial starts at")]
        public int defaultPosition = 0;
        [Header("Data Setup")]
        [Tooltip("Rotates the labels so the text surrounds the Dial radially")]
        public bool radialLabelText = false;
        [Tooltip("Rotates the labels so the text is easier to read")]
        public bool keepLabelTextUpright = true;
        [Tooltip("Set up the labels for each position")]
        public string[] labelText;
        [Tooltip("Set up the objects that will be enabled at each position")]
        public GameObject[] objectsToToggle;
        #endregion

        #region UDON Calls
        [Header("Udon Behaviour Trigger For Every Position")]
        [Tooltip("The UdonBehaviour to trigger every time the Dial is changed")]
        public UdonBehaviour udonBehaviour;
        [Tooltip("The function that will be triggered when the Dial is moved")]
        public string remoteDialEventName = "OnDialChanged";
        [Tooltip("The variable that will be updated when the Dial is moved")]
        public string remotePositionVariableName = "dialCurrentPosition";
        [Tooltip("The variable will be updated with the current angle of the Dial")]
        public string remoteAngleVariableName = "dialCurrentAngle";

        [Header("Position Based Udon Behaviour Trigger")]
        [Tooltip("The UdonBehaviour to trigger when the Dial is at each position in the array")]
        public UdonBehaviour[] udonBehaviours;
        public string[] remoteDialSelectedEventNames;
        public string[] remoteDialDeselectedEventNames;
        public string[] remotePositionVariableNames;
        #endregion

        #region Internal Dial Variables
        /// <summary>
        /// Stores where the Dial is in space so we can lock it. Prevents player from ripping the Dial off!
        /// </summary>
        private Vector3 basePos;

        /// <summary>
        /// Stores whether the dial has checked if the player is in VR or not yet
        /// </summary>
        private bool initVR;
        private float angleSpacing;
        private float positionSpacing;

        [Header("Some debug numbers")]
        private int oldPosition = 0;
        [SerializeField] private int currentPosition = 0;
        [UdonSynced] private int syncedCurrentPosition = 0;
        private float currentAngle = 0;

        /// <summary>
        /// The current knob that is active (PC/VR)
        /// </summary>
        private GameObject activeKnob;

        private bool firstSync = true;

        private float lastTrackedRot;

        private VRC_Pickup.PickupHand currentHand = VRC_Pickup.PickupHand.None;
        #endregion

        private void Start()
        {
            // initVR checks if the script have checked for VR
            initVR = false;
            activeKnob = knobPC;
            knobPC.SetActive(true);
            knobVR.SetActive(false);

            if (mySoundSource == null)
                mySoundSource = GetComponent<AudioSource>();

            // Grab where the knob current is so we can fix it there
            basePos = activeKnob.transform.localPosition;

            // sanitize the default position
            if (defaultPosition < 0)
                defaultPosition = 0;
            if (defaultPosition > numberOfPositions - 1)
                defaultPosition = numberOfPositions - 1;

            // Set the current position of the Dial to the default
            currentPosition = defaultPosition;
            oldPosition = currentPosition;

            if (syncDialPosition)
                syncedCurrentPosition = currentPosition;

            // Setup the current state of the collider
            //DrawDialPositions();
        }

        public int GetCurrentPosition()
        {
            return syncDialPosition ? syncedCurrentPosition : currentPosition;
        }

        [ExecuteInEditMode]
        public void _RefreshDialInEditor()
        {
            DrawDialPositions();
        }

        private void DrawDialPositions()
        {
            // Enable line that we copy from
            if (newPositionMarker != null)
                newPositionMarker.SetActive(true);


#if UNITY_EDITOR
            for (int i = newPositionMarker.transform.parent.childCount - 1; i >= 1; i--)
                Object.DestroyImmediate(newPositionMarker.transform.parent.GetChild(i).gameObject);
#else
            // Clean up old lines
            for (int i = newPositionMarker.transform.parent.childCount - 1; i >= 1; i--)
                Object.Destroy(newPositionMarker.transform.parent.GetChild(i).gameObject);
#endif

            if (numberOfPositions == 0)
            {
                angleSpacing = 0;
                positionSpacing = 0;
                snapToClosestPosition = false;
            }
            else if (newPositionMarker != null)
            {
                angleSpacing = 360f / (numberOfPositions * 2);
                positionSpacing = (360f / numberOfPositions);

                for (int i = 0; i < numberOfPositions; i++)
                {
                    //This creates all the lines
                    var newObject = Object.Instantiate(newPositionMarker, newPositionMarker.transform.parent);
                    if (labelText.Length > i)
                        newObject.name = labelText[i];
                    else
                        newObject.name = "Line " + i;

                    // Rotate the line
                    newObject.transform.localEulerAngles = new Vector3(0, 0, (positionSpacing * i));
                    newObject.transform.position = newPositionMarker.transform.position;

                    // Set up the label
                    if (newObject.transform.childCount > 0)
                    {
                        float newTextRotation = 0f;
                        if (keepLabelTextUpright)
                            newTextRotation = positionSpacing * (float)i;
                        else if (radialLabelText)
                            newTextRotation = 90f;
                        else if ((positionSpacing * (float)i) == 180)
                            newTextRotation = positionSpacing * (float)i;

                        string textAlignment = "Center";

                        if (radialLabelText)
                            textAlignment = "Left";
                        else if (newTextRotation > 0 && newTextRotation < 180)
                            textAlignment = "Left";
                        else if (newTextRotation > 180 && newTextRotation < 360)
                            textAlignment = "Right";

                        TextMeshPro textObject = null;

                        // Align the text either Left, Right or Centered
                        for (int t = 0; t < newObject.transform.childCount; t++)
                        {
                            if (newObject.transform.GetChild(t).name == textAlignment)
                            {
                                textObject = newObject.transform.GetChild(t).GetComponent<TextMeshPro>();
                            }
                            else
                            {
                                newObject.transform.GetChild(t).gameObject.SetActive(false);
                            }
                        }

                        if (textObject != null)
                        {
                            textObject.gameObject.SetActive(true);

                            if (labelText.Length > i && labelText[i] != null && labelText[i].Trim().Length > 0)
                                textObject.text = labelText[i]; // Set the label text
                            else
                                textObject.gameObject.SetActive(false); // Or disable the label object if we have no text for it

                            // Rotate the label so the text is always upright
                            Vector3 textPos = textObject.gameObject.transform.position;
                            Vector3 textRot = textObject.gameObject.transform.localEulerAngles;

                            textObject.gameObject.transform.localEulerAngles = new Vector3(textRot.x, textRot.y, newTextRotation);
                            textObject.gameObject.transform.position = textPos;
                        }
                    }
                }

                SetDialPosition(GetCurrentPosition());
            }

            // Hide the base position marker
            if (newPositionMarker != null)
                newPositionMarker.SetActive(false);
        }

        void Update()
        {
            // Update Dial Positions if the number of positions has changed
            if (newPositionMarker != null && newPositionMarker.transform.parent.childCount != numberOfPositions + 1)
                DrawDialPositions();

            if (!initVR)
            {
                if (Utils.LocalPlayerIsValid())
                {
                    if (Networking.LocalPlayer.IsUserInVR())
                    {
                        initVR = true;
                        activeKnob = knobVR;
                        knobPC.SetActive(false);
                        knobVR.SetActive(true);

                        // Sync VR Dial position
                        SetDialPosition(GetCurrentPosition());
                    }
                }
            }

            SetDialAngle(activeKnob.transform.localEulerAngles.z);

            // Either this will work or we need to turn the mesh renderer off on the VR Dial and enable to PC Dial and snap that between positions
            if (initVR)
            {
                if (smoothTurning)
                {
                    /* if (currentHand != VRC_Pickup.PickupHand.None)
                     {
                         // If player has rotated the dial enough, run a small vibration
                         var relRot = Mathf.Abs(this.lastTrackedRot - activeKnob.transform.localEulerAngles.z);
                         if (relRot > 1.8f)
                             Networking.LocalPlayer.PlayHapticEventInHand(currentHand, 0.1f, 0.9f, 0.9f);
                         // TODO: check if we crossed a position and do a bigger vibration
                     }

                     lastTrackedRot = activeKnob.transform.localEulerAngles.z;*/
                }
                else
                {
                    // If we snap to a new position vibrate controller
                    int oldPosition = GetCurrentPosition();

                    // Snap the dial to the nearest position
                    SnapDialToNearestPosition();

                    // If the dial actually changed positions
                    if (GetCurrentPosition() != oldPosition)
                    {
                        bool canPlaySound = false;

                        // Can play click sound if smooth turning is off and player is still holding the dial
                        if (!smoothTurning && currentHand != VRC_Pickup.PickupHand.None)
                            canPlaySound = true;

                        // Play the sound if we can
                        if (canPlaySound)
                        {
                            if (sharedSoundSource != null)
                                sharedSoundSource.PlaySound(KnobSoundClip, transform.position, lowestPitch, highestPitch);
                            else if(mySoundSource != null)
                                mySoundSource.PlaySound(KnobSoundClip, Vector3.zero, lowestPitch, highestPitch);
                        }


                        // Vibrate the controller for each click round
                        Networking.LocalPlayer.PlayHapticEventInHand(currentHand, 0.2f, 1f, 1f);
                    }
                }
            }
        }

        /// <summary>
        /// The user is in VR and just grabbed the Dial
        /// </summary>
        public void VRInteractStart()
        {
            if (syncDialClick)
                Utils.SetOwner(Networking.LocalPlayer, gameObject);

            // Work out which hand the VR player is holding the dial with
            var leftPickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            var rightPickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
            if (leftPickup != null && leftPickup.gameObject == knobVR)
                currentHand = VRC_Pickup.PickupHand.Left;
            else if (rightPickup != null && rightPickup.gameObject == knobVR)
                currentHand = VRC_Pickup.PickupHand.Right;
            else
                currentHand = VRC_Pickup.PickupHand.None;
        }

        /// <summary>
        /// The user is in VR and just let go of the Dial
        /// </summary>
        public void VRInteractEnd()
        {
            if (smoothTurning)
                Networking.LocalPlayer.PlayHapticEventInHand(currentHand, 0.3f, 1f, 1f);

            currentHand = VRC_Pickup.PickupHand.None;

            if (snapToClosestPosition || !smoothTurning)
                SnapDialToNearestPosition();

            // Tell other players to sync the Dial position
            if (syncDialPosition)
            {
                Utils.SetOwner(Networking.LocalPlayer, gameObject);
                RequestSerialization();
            }
        }

        public void DesktopInteract()
        {
            if (numberOfPositions > 0)
            {
                SetDialPosition(GetCurrentPosition() + 1);
            }
            else
            {
                currentAngle += 5;
                SetDialAngle(currentAngle %= 360);

                UpdateUdonBehaviours(0, 0);
            }

            if (sharedSoundSource != null)
                sharedSoundSource.PlaySound(KnobSoundClip, transform.position, lowestPitch, highestPitch);
            else if (mySoundSource != null)
                mySoundSource.PlaySound(KnobSoundClip, Vector3.zero, lowestPitch, highestPitch);

            // Tell other players to sync the Dial position
            if (syncDialPosition)
            {
                Utils.SetOwner(Networking.LocalPlayer, gameObject);
                RequestSerialization();
            }
        }

        public override void OnDeserialization()
        {
            if (syncDialPosition)
            {
                // We received a sync from another player.. update the Dial locally
                bool changedPosition = SetDialPosition(GetCurrentPosition());

                if (!firstSync && syncDialClick && changedPosition)
                {
                    if (sharedSoundSource != null)
                        sharedSoundSource.PlaySound(KnobSoundClip, transform.position, lowestPitch, highestPitch);
                    else if (mySoundSource != null)
                        mySoundSource.PlaySound(KnobSoundClip, Vector3.zero, lowestPitch, highestPitch);
                }

                firstSync = false;
            }
        }

        /// <summary>
        /// Snaps the Dial to the nearest position when a VR user lets go
        /// </summary>
        private void SnapDialToNearestPosition()
        {
            currentAngle = (currentAngle %= 360);

            if (numberOfPositions == 0)
            {
                UpdateUdonBehaviours(0, 0);
            }
            else
            {
                for (int i = 1; i < numberOfPositions; i++)
                {
                    if (currentAngle < angleSpacing)
                    {
                        SetDialPosition(0);
                    }
                    else if (currentAngle > ((positionSpacing * i) - angleSpacing) && currentAngle < ((positionSpacing * i) + angleSpacing))
                    {
                        SetDialPosition(i);
                    }
                    else if (currentAngle > (angleSpacing + ((numberOfPositions - 1) * positionSpacing)))
                    {
                        SetDialPosition(0);
                        i = numberOfPositions;
                    }
                }
            }


            bool canPlaySound = false;
            if (smoothTurning && currentHand == VRC_Pickup.PickupHand.None)
                canPlaySound = true;

            if (canPlaySound)
            {
                if (sharedSoundSource != null)
                    sharedSoundSource.PlaySound(KnobSoundClip, transform.position, lowestPitch, highestPitch);
                else if (mySoundSource != null)
                    mySoundSource.PlaySound(KnobSoundClip, Vector3.zero, lowestPitch, highestPitch);
            }
        }

        /// <summary>
        /// Sets the current position of the Dial to point at one of the lines
        /// </summary>
        /// <param name="newPosition"></param>
        public bool SetDialPosition(int newPosition)
        {
            if (Networking.LocalPlayer == null || !Networking.LocalPlayer.IsValid())
                return false;

            if (activeKnob == null || !snapToClosestPosition) return false;

            if (newPosition < 0 || newPosition >= numberOfPositions)
                newPosition = 0;

            // Store the current position of the Dial as its previous position
            oldPosition = GetCurrentPosition();

            // Set current position before the variable sync (just in case it's on)
            currentPosition = newPosition;
            if (syncDialPosition) syncedCurrentPosition = currentPosition;

            // Set the rotation of the Dial to point to the nearest marker
            SetDialAngle(positionSpacing * (float)newPosition);

            // Check for objects to toggle on/off
            UpdateObjectToggles();

            // Update Udon Behaviours
            UpdateUdonBehaviours(oldPosition, newPosition);

            return oldPosition != newPosition;
        }

        /// <summary>
        /// Locks the position of the Dial and sets the rotation
        /// </summary>
        private void SetDialAngle(float newAngle)
        {
            if (activeKnob == null) return;

            activeKnob.transform.localPosition = basePos;
            activeKnob.transform.localEulerAngles = new Vector3(0, 0, newAngle);

            currentAngle = newAngle;
        }

        /// <summary>
        /// Loops all attached objects and enables/disables them depending on what position the dial is currently at
        /// </summary>
        private void UpdateObjectToggles()
        {
            if (objectsToToggle != null)
            {
                for (int i = 0; i < objectsToToggle.Length; i++)
                {
                    if (objectsToToggle[i] != null)
                    {
                        objectsToToggle[i].SetActive(i == GetCurrentPosition());
                    }
                }
            }
        }

        public void UpdateVolume() // I don't think we need this one anymore.
        {
            //Utils.UpdateSoundSourceVolume(this, sharedSoundObject, soundVolume);

        }

        /// <summary>
        /// Triggers any local udon behaviours for this player when the Dial is moved
        /// </summary>
        /// <param name="oldPosition">The previous position of the Dial locally</param>
        /// <param name="newPosition">The new position of the Dial</param>
        private void UpdateUdonBehaviours(int oldPosition, int newPosition)
        {
            // Called at every position if it's set
            if (this.udonBehaviour != null)
            {
                if (this.remotePositionVariableName != null && this.remotePositionVariableName.Trim().Length > 0)
                    this.udonBehaviour.SetProgramVariable(this.remotePositionVariableName, newPosition);
                if (this.remoteAngleVariableName != null && this.remoteAngleVariableName.Trim().Length > 0)
                    this.udonBehaviour.SetProgramVariable(this.remoteAngleVariableName, currentAngle);
                if (this.remoteDialEventName != null && this.remoteDialEventName.Trim().Length > 0)
                    this.udonBehaviour.SendCustomEvent(this.remoteDialEventName);
            }

            for (int i = 0; i < this.udonBehaviours.Length; i++)
            {
                UdonBehaviour udonBehaviour = this.udonBehaviours[i];


                // Always update a position variable
                string remoteVarName = null;
                if (remotePositionVariableNames.Length > 0 && i < remotePositionVariableNames.Length)
                    remoteVarName = remotePositionVariableNames[i];
                if (remoteVarName != null && remoteVarName.Trim().Length > 0)
                    udonBehaviour.SetProgramVariable(remotePositionVariableName, newPosition);

                // Work out if we need to trigger an event in this behaviour
                string remoteEventName = null;

                if (i == newPosition)
                {
                    // Trigger the dial selected event on the currently selected behaviour
                    if (remoteDialSelectedEventNames.Length > 0 && i < remoteDialSelectedEventNames.Length)
                        remoteEventName = remoteDialSelectedEventNames[i];
                }
                else if (i == oldPosition)
                {
                    // Trigger the dial de-selected event on the previously selected behaviour
                    if (remoteDialDeselectedEventNames.Length > 0 && i < remoteDialDeselectedEventNames.Length)
                        remoteEventName = remoteDialDeselectedEventNames[i];
                }


                // Trigger an event on the behaviour if we have one
                if (remoteEventName != null && remoteEventName.Trim().Length > 0)
                    udonBehaviour.SendCustomEvent(remoteEventName);
            }
        }
    }
}