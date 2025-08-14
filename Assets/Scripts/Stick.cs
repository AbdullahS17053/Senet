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

        // X-axis face-up/down rotation in local space
        float xRotation = shouldBeFaceUp ? 0f : 180f;

        // Optional: tiny random tilt for realism
        float randomY = Random.Range(-5f, 5f);
        float randomZ = Random.Range(-5f, 5f);

        transform.localRotation = Quaternion.Euler(xRotation, 90, 0);
    }

    /// <summary>
    /// Determines if the stick is face-up (based on snapped rotation).
    /// </summary>
    public bool IsFaceUp()
    {
        float x = transform.localEulerAngles.x;
        return Mathf.Approximately(x % 360f, 0f);
    }

    void Update()
    {
        if (isFlipping)
        {
            // Rotate only on local X axis
            transform.Rotate(Vector3.right * 720f * Time.deltaTime, Space.Self);
        }
    }
}