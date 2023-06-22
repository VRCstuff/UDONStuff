
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

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
        if (player == null || !player.IsValid()) return;
        if (!Networking.IsOwner(player, nowOwns))
            Networking.SetOwner(player, nowOwns);
    }

    /// <summary>
    /// This will check if there are any players in the selected objects box collider.
    /// </summary>
    /// <param name="objectWithBoxCollider">This is the gameObject that you wanna check. It needs a box collider.</param>
    /// <returns>true if a person is in it. False if there is no box collider or no person in it.</returns>
    public static bool PlayerPresentInBoxCollider(GameObject objectWithBoxCollider)
    {
        if (objectWithBoxCollider == null)
        {
            LogError(tag: "Utils", message: nameof(PlayerPresentInBoxCollider) + " tried to run but had no box collider.", tagColor: "red");
            return false;
        }
        VRCPlayerApi[] vrcPlayers = new VRCPlayerApi[100]; //This is to check if players are present.
        VRCPlayerApi.GetPlayers(vrcPlayers);
        foreach (VRCPlayerApi player in vrcPlayers)
        {
            if (player == null) continue;
            if (objectWithBoxCollider.GetComponent<BoxCollider>().bounds.Contains(player.GetPosition()))
            {
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// This will check if there are any players in the selected collider.
    /// </summary>
    /// <param name="Collider">The collider you want to check for a player.</param>
    /// <returns>true if a person is in it. False if there is no box collider or no person in it.</returns>
    public static bool PlayerPresentInBoxCollider(Collider Collider)
    {
        if (Collider == null)
        {
            LogError(tag: "Utils", message: nameof(PlayerPresentInBoxCollider) + " tried to run but had no box collider.", tagColor: "red");
            return false;
        }
        VRCPlayerApi[] vrcPlayers = new VRCPlayerApi[100]; //This is to check if players are present.
        VRCPlayerApi.GetPlayers(vrcPlayers);
        foreach (VRCPlayerApi player in vrcPlayers)
        {
            if (player == null) continue;
            if (Collider.bounds.Contains(player.GetPosition()))
            {
                return true;
            }
        }
        return false;
    }

    public static float GetUnixTimestamp()
    {
        System.DateTime offsetDateTime = new System.DateTime(2022, 6, 13, 0, 0, 0, System.DateTimeKind.Utc);
        return (float)(System.DateTime.UtcNow - offsetDateTime).TotalSeconds;
    }

    public static bool LocalPlayerIsValid()
    {
        return Networking.LocalPlayer != null && Networking.LocalPlayer.IsValid();
    }

    /// <summary>
    /// Returns a vector position that describes the closest point attached to the line based on another position
    /// </summary>
    /// <param name="vA">The start of the line</param>
    /// <param name="vB">The end of the line</param>
    /// <param name="vPoint">The position to run the comparison on</param>
    /// <returns></returns>
    public static Vector3 ClosestPointOnLine(Vector3 vA, Vector3 vB, Vector3 vPoint)
    {
        var vVector1 = vPoint - vA;
        var vVector2 = (vB - vA).normalized;

        var d = Vector3.Distance(vA, vB);
        var t = Vector3.Dot(vVector2, vVector1);

        if (t <= 0)
            return vA;

        if (t >= d)
            return vB;

        var vVector3 = vVector2 * t;

        var vClosestPoint = vA + vVector3;

        return vClosestPoint;
    }
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

    /// <summary>
    /// This updates a soundsources volume at scale. We might just be able to remove this one. Who knows. It makes it a bit easier to standardise some stuff at least.
    /// </summary>
    /// <param name="context">Just write "this" please</param>
    /// <param name="sharedSoundObject">This is the gameobject with the souurce on it.</param>
    /// <param name="volume">Volume.... 0 to 1.</param>
    public static void UpdateSoundSourceVolume(UdonSharpBehaviour context, GameObject sharedSoundObject, float volume = 0.5f)
    {
        if (sharedSoundObject != null && sharedSoundObject.GetComponent<AudioSource>() != null)
        {
            sharedSoundObject.GetComponent<AudioSource>().volume = volume;
            return;
        }
        else if (context.gameObject.GetComponent<AudioSource>() != null)
        {
            context.gameObject.GetComponent<AudioSource>().volume = volume;

            return;
        }
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

}

#region Extensions
public static class Extensions
{
    public static bool LocalPlayerOwnsThisObject(this UdonSharpBehaviour behaviour)
    {
        return Utils.LocalPlayerIsValid() && Networking.LocalPlayer.IsOwner(behaviour.gameObject);
    }

    public static bool IsPointWithin(this Collider collider, Vector3 point)
    {
        return (collider.ClosestPoint(point) - point).sqrMagnitude < Mathf.Epsilon * Mathf.Epsilon;
    }
}

static class RendererExtensions
{
    public static void SetMaterial(this Renderer renderer, int index, Material newMaterial)
    {
        Material[] materials = renderer.materials;
        materials[index] = newMaterial;
        renderer.materials = materials;
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