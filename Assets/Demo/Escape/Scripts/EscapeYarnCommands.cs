using System.Collections;
using UnityEngine;
using Yarn.Unity;

// Small Yarn commands for the escape and ending scenes.
public static class EscapeYarnCommands
{
    // <<turn_to Wes Player>>: turns `who` (around the vertical axis) to face `target`, over a short moment.
    // Doesn't make the dialogue wait: if this conversation gets cut off by another one, a command that
    // finishes later would otherwise tell the new conversation to continue and skip one of its lines.
    [YarnCommand("turn_to")]
    public static void TurnTo(string who, string target, float duration = 0.6f)
    {
        GameObject mover = GameObject.Find(who);
        GameObject goal = GameObject.Find(target);
        if (mover == null || goal == null)
        {
            Debug.LogWarning($"[turn_to] '{who}' or '{target}' not found.");
            return;
        }

        CoroutineHost.Run(Turn(mover.transform, goal.transform, duration));
    }

    // <<look_at Unit_5>>: swings the player's camera round to look at `target` (e.g. something behind
    // them they must not miss). yawOffset (degrees) moves it off-centre so the player character
    // doesn't stand in front of it. Doesn't make the dialogue wait, same reason as turn_to.
    [YarnCommand("look_at")]
    public static void LookAt(string target, float duration = 1.2f, float yawOffset = 0.0f)
    {
        GameObject goal = GameObject.Find(target);
        var player = Object.FindAnyObjectByType<StarterAssets.ThirdPersonControllerFixed>();
        if (goal == null || player == null || Camera.main == null)
        {
            Debug.LogWarning($"[look_at] '{target}', the player or the camera not found.");
            return;
        }

        CoroutineHost.Run(Look(player, goal.transform, duration, yawOffset));
    }

    private static IEnumerator Look(StarterAssets.ThirdPersonControllerFixed player, Transform goal, float duration, float yawOffset)
    {
        float fromYaw = player.LookYaw, fromPitch = player.LookPitch;
        for (float t = 0.0f; t <= duration && player != null && goal != null; t += Time.deltaTime)
        {
            Vector3 to = goal.position + Vector3.up * 1.0f - Camera.main.transform.position;
            float yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg + yawOffset;
            float pitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
            float k = Mathf.SmoothStep(0.0f, 1.0f, t / duration);
            player.SetLook(Mathf.LerpAngle(fromYaw, yaw, k), Mathf.Lerp(fromPitch, pitch, k));
            yield return null;
        }
    }

    private static IEnumerator Turn(Transform mover, Transform goal, float duration)
    {
        Vector3 direction = goal.position - mover.position;
        direction.y = 0.0f;
        if (direction.sqrMagnitude < 0.0001f) yield break;

        Quaternion from = mover.rotation;
        Quaternion to = Quaternion.LookRotation(direction);
        for (float t = 0.0f; t < duration && mover != null; t += Time.deltaTime)
        {
            mover.rotation = Quaternion.Slerp(from, to, t / duration);
            yield return null;
        }
        if (mover != null) mover.rotation = to;
    }

    // Runs coroutines for static code; lives in the current scene and goes away with it.
    private class CoroutineHost : MonoBehaviour
    {
        private static CoroutineHost instance;

        public static void Run(IEnumerator routine)
        {
            if (instance == null) instance = new GameObject(nameof(CoroutineHost)).AddComponent<CoroutineHost>();
            instance.StartCoroutine(routine);
        }
    }
}
