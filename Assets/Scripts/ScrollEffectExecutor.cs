using UnityEngine;

public static class ScrollEffectExecutor
{
    public static void ExecuteScrollEffect(string scrollName, bool isAI)
    {
        switch (scrollName)
        {
            case "Earthbound’s Step":
                PieceMover.lastStickValue = 1;
                PieceMover.lastMoveWasRethrow = false; // Prevent rethrow
                if (isAI)
                {
                    AiMover.StartAITurn(); // AI continues with new move
                }
                else
                {
                    PieceMover.Instance.Invoke("UpdateThrowButtonState", 0.1f); // Player can move
                }
                break;

            // You can add more scrolls here
            default:
                Debug.LogWarning("Unknown scroll effect: " + scrollName);
                break;
        }
    }

}