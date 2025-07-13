using UnityEngine;

public class Stick : MonoBehaviour
{
    private bool isFlipping = false;
    private bool shouldBeFaceUp = false; // Desired state after flip

    /// <summary>
    /// Called by StickThrower to prepare final orientation.
    /// </summary>
    public void PrepareFaceUpState(bool isFaceUp)
    {
        shouldBeFaceUp = isFaceUp;
    }

    /// <summary>
    /// Starts stick flipping animation.
    /// </summary>
    public void StartFlip()
    {
        isFlipping = true;
    }

    /// <summary>
    /// Stops flipping and sets final rotation based on desired face-up state.
    /// </summary>
    public void StopAndSnapRotation()
    {
        isFlipping = false;

        float xRotation = shouldBeFaceUp ? 0f : 180f; // Face-up = 0, Face-down = 180

        // Optionally add slight random Y/Z variation for realism
        float randomY = Random.Range(-10f, 10f);
        float randomZ = Random.Range(-10f, 10f);

        transform.eulerAngles = new Vector3(xRotation, randomY, randomZ);
    }

    /// <summary>
    /// Determines if the stick is face-up (based on snapped rotation).
    /// </summary>
    public bool IsFaceUp()
    {
        float x = transform.eulerAngles.x;
        return Mathf.Approximately(x % 360f, 0f);
    }

    void Update()
    {
        if (isFlipping)
        {
            transform.Rotate(Vector3.right * 720f * Time.deltaTime); // Fast flip on X-axis
        }
    }
}