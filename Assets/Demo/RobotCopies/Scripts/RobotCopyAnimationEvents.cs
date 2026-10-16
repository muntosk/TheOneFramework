using UnityEngine;

// The StarterAssets walk/run/land clips fire OnFootstep/OnLand animation events, which the player's
// controller handles. The robot copies behind the observation window only replay the motion, so
// this swallows those events (silently - a dozen synced footsteps through glass would be noise).
public class RobotCopyAnimationEvents : MonoBehaviour
{
    private void OnFootstep(AnimationEvent animationEvent) { }

    private void OnLand(AnimationEvent animationEvent) { }
}
