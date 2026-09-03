using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 12f, -10f);
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) target = player.transform;
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}
