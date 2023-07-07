﻿
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace com.vrcstuff.udon
{
    public class Utils : UdonSharpBehaviour
    {
        void Start()
        {

        }

        public static void Log(string tag, string message, string tagColor = "green")
        {
#if UNITY_STANDALONE_WIN
            Debug.Log($"[<color={tagColor}>{tag}</color>] {message}");
#elif UNITY_EDITOR
            Debug.Log($"[<color={tagColor}>{tag}</color>] {message}");
#endif
        }

        public static void LogWarning(string tag, string message, string tagColor = "green")
        {
#if UNITY_STANDALONE_WIN
            Debug.LogWarning($"[<color={tagColor}>{tag}</color>] {message}");
#elif UNITY_EDITOR
            Debug.LogWarning($"[<color={tagColor}>{tag}</color>] {message}");
#endif
        }

        public static void LogError(string tag, string message, string tagColor = "green")
        {
#if UNITY_STANDALONE_WIN
            Debug.LogError($"[<color={tagColor}>{tag}</color>] {message}");
#elif UNITY_EDITOR
            Debug.LogError($"[<color={tagColor}>{tag}</color>] {message}");
#endif
        }

        public static void Log(UdonSharpBehaviour context, string message, string tagColor = "green")
        {
            if (context == null)
            {
                Debug.LogError("Log context is missing!");
                return;
            }

#if UNITY_STANDALONE_WIN
            Debug.Log($"[{context.gameObject.name} (<color={tagColor}>{context.GetUdonTypeName()}</color>)] {message}", context.gameObject);
#elif UNITY_EDITOR
            Debug.Log($"[{context.gameObject.name} (<color={tagColor}>{context.GetUdonTypeName()}</color>)] {message}", context.gameObject);
#endif
        }

        public static void LogWarning(UdonSharpBehaviour context, string message, string tagColor = "green")
        {
            if (context == null)
            {
                Debug.LogError("Log context is missing!");
                return;
            }

#if UNITY_STANDALONE_WIN
            Debug.LogWarning($"[{context.gameObject.name} (<color={tagColor}>{context.GetUdonTypeName()}</color>)] {message}", context.gameObject);
#elif UNITY_EDITOR
            Debug.LogWarning($"[{context.gameObject.name} (<color={tagColor}>{context.GetUdonTypeName()}</color>)] {message}", context.gameObject);
#endif
        }

        public static void LogError(UdonSharpBehaviour context, string message, string tagColor = "green")
        {
            if (context == null)
            {
                Debug.LogError("Log context is missing!");
                return;
            }

#if UNITY_STANDALONE_WIN
            Debug.LogError($"[{context.gameObject.name} (<color={tagColor}>{context.GetUdonTypeName()}</color>)] {message}", context.gameObject);
#elif UNITY_EDITOR
            Debug.LogError($"[{context.gameObject.name} (<color={tagColor}>{context.GetUdonTypeName()}</color>)] {message}", context.gameObject);
#endif
        }

        /// <summary>
        /// Checks if the player isn't already the owner of the object, if not it applies ownership of the object
        /// </summary>
        /// <param name="player">The player who wants ownership</param>
        /// <param name="nowOwns">The object to set ownership on</param>
        public static void SetOwner(VRCPlayerApi player, GameObject nowOwns)
        {
            if (LocalPlayerIsValid() && !Networking.IsOwner(player, nowOwns))
                Networking.SetOwner(player, nowOwns);
        }

        /// <summary>
        /// Scripts that use FakeUpdate can use this to determine how often they should update based on player distance to script source.
        /// </summary>
        //public static float DistanceFakeUpdateSpeed(UdonSharpBehaviour scriptlocation, float clamplow = 0f, float clampHigh = 5f)
        //{
        //    Vector3 playerpos = Networking.LocalPlayer.GetPosition();
        //    Vector3.Distance(playerpos, scriptlocation.transform.position);
        //    float returnTime;

        //    return returnTime;
        //}
        public static bool LocalPlayerIsValid() => Utilities.IsValid(Networking.LocalPlayer);

        /// <summary>
        /// Gets a UNIX-like timestamp but starts at 2023-01-01 00:00:00 UTC so float precision is better (maybe?)
        /// </summary>
        /// <returns>Number of seconds since 2023-01-01 00:00:00 UTC</returns>
        public static float GetUnixTimestamp() => (float)(System.DateTime.UtcNow - new System.DateTime(2023, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;

        /// <summary>
        /// This plays a sound. Usually used for when you have a speaker that is moving around to keep the speaker count down. But can also be used on a local speaker to provide a small random pitch shift.
        /// </summary>
        /// <param name="context">Just write "this" please</param>
        /// <param name="aClip">The audio clip the speaker should play</param>
        /// <param name="sharedSoundObject">Provide this if you'll use a game obbject with a speaker moving around.</param>
        /// <param name="lowestPitch">The lowest pitch the sound will have</param>
        /// <param name="highestPitch">The highest pitch the sound will have</param>
        public static void PlaySound(UdonSharpBehaviour context, AudioClip aClip, GameObject sharedSoundObject, float lowestPitch = 1, float highestPitch = 1)
        {
            if (context == null)
            {
                LogError("[unknown]", nameof(PlaySound) + " was called without context.");

            }
            if (aClip == null)
            {
                LogError(context.gameObject.name, "I'm calling the " + nameof(PlaySound) + " function with a missing audio clip.");
                return;
            }
            if (sharedSoundObject != null && sharedSoundObject.GetComponent<AudioSource>() != null)
            {
                AudioSource Asrc = sharedSoundObject.GetComponent<AudioSource>();
                sharedSoundObject.transform.position = context.gameObject.transform.position;
                Asrc.clip = aClip;
                Asrc.pitch = UnityEngine.Random.Range(lowestPitch, highestPitch);
                Asrc.Play();
                return;
            }
            else if (context.gameObject.GetComponent<AudioSource>() != null)
            {
                AudioSource Asrc = context.gameObject.GetComponent<AudioSource>();
                Asrc.clip = aClip;
                Asrc.pitch = UnityEngine.Random.Range(lowestPitch, highestPitch);
                Asrc.Play();
                return;
            }
            LogError(context.gameObject.name, "I have no sound source attaced when I'm trying to play a sound.");
            return;
        }


    }

    #region Extensions
    public static class Extensions
    {
        public static bool LocalPlayerOwnsThisObject(this UdonSharpBehaviour behaviour) => behaviour.gameObject.LocalPlayerOwnsThisObject();
        public static bool LocalPlayerOwnsThisObject(this GameObject gameObject) => Utils.LocalPlayerIsValid() && Networking.LocalPlayer.IsOwner(gameObject);

        /// <summary>
        /// Pushes an element into an array and pops an element off the other side of the array (For queues/stacks)
        /// <para>
        /// Based on: <see href="https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.insert?view=net-6.0">List&lt;T&gt;.Insert(Int32, T)</see>
        /// </para>
        /// </summary>
        /// <returns>Modified T[]</returns>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">Source T[] to modify.</param>
        /// <param name="item">The object to insert.</param>
        /// <param name="atStart">True=push onto index 0, false=push onto end of array</param>
        public static T[] Push<T>(this T[] array, T item, bool atStart = true)
        {
            int length = array.Length;

            T[] newArray = new T[length];

            newArray.SetValue(item, atStart ? 0 : array.Length - 1);

            if (atStart)
            {
                Array.Copy(array, 0, newArray, 1, length - 1);
            }
            else
            {
                Array.Copy(array, 1, newArray, 0, length - 1);
            }

            return newArray;
        }
    }

    public static class BoxColliderExtensions
    {
        /// <summary>
        /// This will check if there are any players in the selected collider.<br/><br/>
        /// <b>Note: This can fail in the box collider is above the ground that players walk on!</b><br/>
        /// Consider using OnPlayerTriggerEnter and OnPlayerTriggerExit on the actual GameObject to keep track of this instead!
        /// </summary>
        /// <returns>True if at least 1 player is within the box colliders bounds</returns>
        public static bool ContainsAtLeastOnePlayer(this BoxCollider boxCollider)
        {
            // Get all players
            VRCPlayerApi[] vrcPlayers = new VRCPlayerApi[100];
            VRCPlayerApi.GetPlayers(vrcPlayers);

            foreach (VRCPlayerApi player in vrcPlayers)
                if (boxCollider.ContainsPlayer(player))
                    return true;

            return false;
        }

        /// <summary>
        /// Checks if the player is within the bounds of the box collider<br/><br/>
        /// <b>Note: This can fail in the box collider is above the ground that players walk on!</b><br/>
        /// Consider using OnPlayerTriggerEnter and OnPlayerTriggerExit on the actual GameObject to keep track of this instead!
        /// </summary>
        /// <param name="boxCollider"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool ContainsPlayer(this BoxCollider boxCollider, VRCPlayerApi player)
        {
            return Utilities.IsValid(player) && boxCollider.bounds.Contains(player.GetPosition());
        }
    }

    public static class VectorExtensions
    {
        /// <summary>
        /// Works out the closest position on a line relative this world position vector. Useful for making an object follow a particular path.
        /// </summary>
        /// <param name="lineStartPosition">World position vector that represents the start of the line</param>
        /// <param name="lineEndPosition">World position vector that represents the end of the line</param>
        /// <returns>A position vector in world space that represents the closest point on the line</returns>
        public static Vector3 ClosestPointOnLine(this Vector3 worldPosition, Vector3 lineStartPosition, Vector3 lineEndPosition)
        {
            var vVector1 = worldPosition - lineStartPosition;
            var vVector2 = (lineEndPosition - lineStartPosition).normalized;

            var d = Vector3.Distance(lineStartPosition, lineEndPosition);
            var t = Vector3.Dot(vVector2, vVector1);

            if (t <= 0)
                return lineStartPosition;

            if (t >= d)
                return lineEndPosition;

            var vVector3 = vVector2 * t;

            return lineStartPosition + vVector3;
        }
    }

    public static class RendererExtensions
    {
        public static void SetMaterial(this Renderer renderer, int index, Material newMaterial)
        {
            Material[] materials = renderer.materials;
            materials[index] = newMaterial;
            renderer.materials = materials;
        }
    }

    public static class AudioSourceExtensions
    {
        /// <summary>
        /// Plays a sound either on the attached AudioSource component or (if specified) a shared AudioSource.<br/>
        /// Also applies a random pitch shift between the lowestPitch and highestPitch variables
        /// </summary>
        /// <param name="context">The GameObject to play a sound from</param>
        /// <param name="aClip">The AudioClip to play</param>
        /// <param name="sharedSoundObject">The shared AudioSource GameObject</param>
        /// <param name="lowestPitch">The lowest random pitch value</param>
        /// <param name="highestPitch">The highest random pitch value</param>
        public static void PlaySound(this AudioSource context, AudioClip aClip, Vector3 moveToWorldPosition, float lowestPitch = 1, float highestPitch = 1)
        {
            if (aClip == null)
            {
                Utils.LogError(context.gameObject.name, "I'm calling the " + nameof(PlaySound) + " function with a missing audio clip.");
                return;
            }

            if (context != null)
            {
                context.clip = aClip;
                context.pitch = UnityEngine.Random.Range(lowestPitch, highestPitch);
                context.Play();
            }

            if (moveToWorldPosition != Vector3.zero)
                context.transform.position = moveToWorldPosition;
        }
    }
    #endregion


    #region Useful Enums
    public enum MoveState : int
    {
        AtSource = 0,
        MovingToTarget = 1,
        AtTarget = 2,
        MovingToSource = 3,
        Grabbed = 4,
        MovingToClosest = 5
    }
    #endregion
}