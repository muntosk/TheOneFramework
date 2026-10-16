using System.Collections;
using StarterAssets;
using TheOneFramework.Audio;
using TheOneFramework.Portals;
using TheOneFramework.UI;
using UnityEngine;
using Yarn.Unity;

// "You are no longer required": a ceiling claw moves over the player, grabs them, lifts them up and -
// behind a fade to black - drops them at dropOff (the start of the disposal belt), which also becomes
// their respawn point. Started from Yarn with <<grab_player Claw>> (Claw = this object's name).
public class ClawGrab : MonoBehaviour
{
    [Tooltip("The part that moves (arm + hand).")]
    [SerializeField] private Transform claw;
    [Tooltip("Point between the fingers; the player's head ends up here.")]
    [SerializeField] private Transform gripPoint;
    [SerializeField] private Transform[] fingers;
    [SerializeField] private float fingerOpenAngle = -35.0f;
    [SerializeField] private float fingerClosedAngle = 15.0f;

    [Tooltip("Where the player is put down after the fade.")]
    [SerializeField] private Transform dropOff;

    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private float liftHeight = 6.0f;
    [SerializeField] private float headHeight = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip motorLoopClip;
    [SerializeField] private AudioClip grabClip;
    [SerializeField, Range(0.0f, 1.0f)] private float volume = 0.8f;

    private Vector3 restPosition;
    private AudioSource motor;
    private bool running;

    private void Awake()
    {
        restPosition = claw.position;
        SetFingers(fingerOpenAngle);

        if (motorLoopClip != null)
        {
            motor = claw.gameObject.AddComponent<AudioSource>();
            motor.clip = motorLoopClip;
            motor.loop = true;
            motor.spatialBlend = 1.0f;
            motor.volume = volume;
            GameAudio.Route(motor, AudioChannel.Sfx);
        }
    }

    [YarnCommand("grab_player")]
    public void Grab()
    {
        if (!running) StartCoroutine(GrabRoutine());
    }

    private IEnumerator GrabRoutine()
    {
        running = true;
        var cc = FindAnyObjectByType<CharacterController>();
        if (cc == null) yield break;
        Transform player = cc.transform;
        var respawn = player.GetComponent<RespawnBehaviour>();
        SetPlayerFree(player, false);

        if (motor != null) motor.Play();

        // Over the player at ceiling height, then down until the fingers are around their head.
        Vector3 above = new Vector3(player.position.x, restPosition.y, player.position.z) + (claw.position - gripPoint.position).WithY(0.0f);
        yield return MoveClaw(above);
        Vector3 offset = claw.position - gripPoint.position;
        yield return MoveClaw(player.position + Vector3.up * headHeight + offset);

        if (motor != null) motor.Pause();
        if (grabClip != null) GameAudio.PlayAt(grabClip, gripPoint.position, volume);
        for (float t = 0.0f; t < 0.3f; t += Time.deltaTime)
        {
            SetFingers(Mathf.Lerp(fingerOpenAngle, fingerClosedAngle, t / 0.3f));
            yield return null;
        }
        SetFingers(fingerClosedAngle);
        if (motor != null) motor.UnPause();

        // Up we go, player dangling from the claw.
        cc.enabled = false;
        Vector3 hold = player.position - gripPoint.position;
        Vector3 top = claw.position + Vector3.up * liftHeight;
        Coroutine fade = null;
        while (claw.position != top)
        {
            claw.position = Vector3.MoveTowards(claw.position, top, moveSpeed * 0.5f * Time.deltaTime);
            player.position = gripPoint.position + hold;
            if (respawn != null) respawn.MarkCarried();
            if (fade == null && Vector3.Distance(claw.position, top) < liftHeight * 0.4f) fade = ScreenFade.Out(1.0f);
            yield return null;
        }
        if (fade != null) yield return fade;

        // Put them down on the belt while it's black, and reset the claw for good measure.
        player.SetPositionAndRotation(dropOff.position, dropOff.rotation);
        cc.enabled = true;
        if (respawn != null) respawn.SetRespawnPoint(dropOff.gameObject);
        claw.position = restPosition;
        SetFingers(fingerOpenAngle);
        if (motor != null) motor.Stop();

        yield return new WaitForSeconds(0.5f);
        SetPlayerFree(player, true);
        yield return ScreenFade.In(1.0f);
        running = false;
    }

    private IEnumerator MoveClaw(Vector3 target)
    {
        while (claw.position != target)
        {
            claw.position = Vector3.MoveTowards(claw.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void SetFingers(float angle)
    {
        foreach (Transform finger in fingers)
        {
            if (finger == null) continue;
            Vector3 e = finger.localEulerAngles;
            finger.localEulerAngles = new Vector3(angle, e.y, e.z);
        }
    }

    // No walking, looking around keeps working; no portals either while dangling.
    private static void SetPlayerFree(Transform player, bool free)
    {
        var move = player.GetComponent<ThirdPersonControllerFixed>();
        if (move != null) move.enabled = free;
        var gun = player.GetComponent<PortalGun>();
        if (gun != null) gun.enabled = free;
    }
}

internal static class ClawGrabVectorExtensions
{
    public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
}
