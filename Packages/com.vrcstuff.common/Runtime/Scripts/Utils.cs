
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
        /// Gives you the local rotation velocity between this frame and the last. This is using world positions and not local positions.
        /// </summary>
        /// <returns>Vector3 containing the velocity since last frame.</returns>
        /// <param name="objectTransform">This is the current position of the transform. You should pass in "transform" or similar here.</param>
        /// <param name="previousPosition">The previous frames transform. Remember to save this frames rotation with "previousPosition = transform.position" or similar</param>
        public static Vector3 CalculateVelocity(Transform objectTransform, Vector3 previousPosition)
        {
            // Calculate the displacement in world space
            Vector3 currentPosition = objectTransform.position;
            Vector3 displacement = currentPosition - previousPosition;
            // Convert the displacement to local space
            Vector3 localDisplacement = objectTransform.InverseTransformDirection(displacement);
            // Calculate the local velocity
            Vector3 localVelocity = localDisplacement / Time.deltaTime;
            //Return the velocity
            return localVelocity;
        }
        /// <summary>
        /// Gives you the local rotation velocity between this frame and the last. This is using world rotation and not local rotation.
        /// </summary>
        /// <returns>Vector3 containing the rotation velocity since last frame.</returns>
        /// <param name="currentRotation">This is the current position of the transform. You should pass in "transform.rotation" here.</param>
        /// <param name="previousRotation">The previous frames transform. Remember to save this frames rotation with "previousRotation = transform.rotation" or similar</param>
        public static Vector3 CalculateRotationVelocity(Quaternion currentRotation, Quaternion previousRotation)
        {
            // Calculate the change in rotation
            Quaternion deltaRotation = currentRotation * Quaternion.Inverse(previousRotation);
            // Convert the change in rotation to angular velocity
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            Vector3 angularVelocity = axis * angle * Mathf.Deg2Rad / Time.deltaTime;
            return angularVelocity;
        }

        /// <summary>
        /// This function checks what connected player is closest to the given transform.
        /// </summary>
        /// <param name="targetTransform">This transform is what it's compared agains</param>
        /// <returns>closest vrcplayer.</returns>
        public static VRCPlayerApi GetClosestPlayer(Transform targetTransform)
        {
            VRCPlayerApi[] players = new VRCPlayerApi[100];
            VRCPlayerApi closestPlayer = null;
            float closestDistance = Mathf.Infinity;
            VRCPlayerApi.GetPlayers(players);
            foreach (VRCPlayerApi player in players)
            {
                if (player == null) continue; //Skip empty slots.
                float distance = Vector3.Distance(player.GetPosition(), targetTransform.position); //Check the distance
                if (distance < closestDistance) //Checks who is the closest
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }
            return closestPlayer;
        }
        /// <summary>
        /// This function checks what connected player is closest to the given transform.
        /// </summary>
        /// <param name="targetPosition">This position is what it's compared agains</param>
        /// <returns>closest vrcplayer.</returns>
        public static VRCPlayerApi GetClosestPlayer(Vector3 targetPosition)
        {
            VRCPlayerApi[] players = new VRCPlayerApi[100];
            VRCPlayerApi closestPlayer = null;
            float closestDistance = Mathf.Infinity;
            VRCPlayerApi.GetPlayers(players);
            foreach (VRCPlayerApi player in players)
            {
                if (player == null) continue; //Skip empty slots.
                float distance = Vector3.Distance(player.GetPosition(), targetPosition); //Check the distance
                if (distance < closestDistance) //Checks who is the closest
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }
            return closestPlayer;
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

    public static class GizmoExtensions
    {
        /// <summary>
        /// This will add functionality to draw a arrow from the center of a transform in the forward direction.
        /// </summary>
        /// <param name="context">This is the transform you want to send the arrow from</param>
        /// <param name="arrowLength">Length of the arrow</param>
        /// <param name="arrowHeadLength">Length of the arrowhead</param>
        /// <param name="arrowHeadAngle">Angle of the arrowhead</param>
        public static void DrawForwardArrow(this UnityEngine.Transform context, float arrowLength = 1.2f, float arrowHeadLength = 0.2f, float arrowHeadAngle = 20f, Color? color = null)
        {

            Gizmos.color = color ?? Color.red;

            Gizmos.DrawLine(context.position, (context.position + context.forward * arrowLength));
            Vector3 right = Quaternion.LookRotation(context.forward) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(context.forward) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;

            Gizmos.DrawLine(context.position + context.forward * arrowLength, context.position + context.forward * arrowLength + right * arrowHeadLength);
            Gizmos.DrawLine(context.position + context.forward * arrowLength, context.position + context.forward * arrowLength + left * arrowHeadLength);
            Gizmos.DrawLine((context.position + context.forward * arrowLength + left * arrowHeadLength), (context.position + context.forward * arrowLength + right * arrowHeadLength));

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