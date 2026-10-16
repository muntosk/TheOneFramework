using System.Collections;

// A DialogueTrigger that can be used while another conversation is running (it cuts that one off),
// e.g. banging on a window while S.A.F.E.-T is still talking. The plain DialogueTrigger hides its
// "hold E" prompt for as long as any dialogue runs.
public class InterruptingDialogueTrigger : DialogueTrigger
{
    protected override bool IsActivated()
    {
        return false;
    }

    protected override void OnActivate()
    {
        StartCoroutine(StartAfterStopping());
    }

    // Starting straight after Stop() lets the old conversation's presenters skip the new one's first
    // lines, so give them a couple of frames to wind down.
    private IEnumerator StartAfterStopping()
    {
        if (dialogueRunner == null) yield break;
        if (dialogueRunner.IsDialogueRunning)
        {
            dialogueRunner.Stop();
            yield return null;
            yield return null;
        }
        dialogueRunner.StartDialogue(dialogueNodeName);
    }
}
