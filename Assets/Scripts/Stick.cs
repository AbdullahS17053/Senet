using UnityEngine;

public class Stick : MonoBehaviour
{
    private bool isFlipping = false;

    public void StartFlip()
    {
        isFlipping = true;
    }

    public void StopAndSnapRotation()
    {
        isFlipping = false;

        // Randomly choose 0 or 180 for X-axis
        float xRotation = Random.value < 0.5f ? 0f : 180f;

        // Preserve Y and Z rotations
        Vector3 currentEuler = transform.eulerAngles;
        transform.eulerAngles = new Vector3(xRotation, currentEuler.y, currentEuler.z);
    }

    public bool IsFaceUp()
    {
        float x = transform.eulerAngles.x;
        return Mathf.Approximately(x, 0f);
    }

    void Update()
    {
        if (isFlipping)
        {
            transform.Rotate(Vector3.right * 720 * Time.deltaTime); // Fast flip on X-axis
        }
    }
}