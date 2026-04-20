using UnityEngine;

public class RotationHandler : MonoBehaviour
{
    public LayerMask layerMask;
    private PlayerMovement player;
    private Rigidbody rb;
    private float amountRotateRight = -90;
    private float amountRotateLeft = 90;

    void Start()
    {
        player = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
    }

    public void SetRotation()
    {
        Vector3 velocity = transform.InverseTransformDirection(rb.linearVelocity);

        if (velocity.x < 0)
            rb.MoveRotation(Quaternion.Euler(0, rb.rotation.eulerAngles.y + amountRotateLeft, 0));
        else if (velocity.x > 0)
            rb.MoveRotation(Quaternion.Euler(0, rb.rotation.eulerAngles.y + amountRotateRight, 0));

        player.setPlayerXToZ = !player.setPlayerXToZ;
        gameObject.GetComponent<PlayerCollision>().ChangeAxis();

        FixAxis();
    }

    public void FixAxis()
    {
        bool currXIsNearHalf = IsNearHalf(transform.position.x);
        bool currZIsNearHalf = IsNearHalf(transform.position.z);

        if (player.setPlayerXToZ)
        {
            if (currXIsNearHalf)
            {
                transform.position = new Vector3
                (
                    GetNearestHalf(transform.position.x), 
                    transform.position.y, 
                    transform.position.z
                );
            }
            else
            {
                transform.position = new Vector3
                (
                    Mathf.Round(transform.position.x),
                    transform.position.y,
                    transform.position.z
                );
            }
        }
        else if (!player.setPlayerXToZ)
        {
            if (currZIsNearHalf)
            {
                transform.position = new Vector3
                (
                    transform.position.x,
                    transform.position.y,
                    GetNearestHalf(transform.position.z)
                );
            }
            else
            {
                transform.position = new Vector3
                (
                    transform.position.x,
                    transform.position.y,
                    Mathf.Round(transform.position.z)
                );
            }
        }
    }

    private bool IsNearHalf(float number, float tolerance = 0.25f)
    {
        float fractionalPart = Mathf.Abs(number) % 1.0f;

        return Mathf.Abs(fractionalPart - 0.5f) <= tolerance;
    }

    private float GetNearestHalf(float number)
    {
        float difference = Mathf.Abs(number) % 1.0f;
        float amountToHalf = difference - 0.5f;


        return number - amountToHalf;
    }

    public bool CheckAxis(CameraTriggerVolume.Axis axis)
    {
        if (player.setPlayerXToZ && axis == CameraTriggerVolume.Axis.Z)
        {
            return true;
        }

        if (!player.setPlayerXToZ && axis == CameraTriggerVolume.Axis.X)
        {
            return true;
        }

         return false;
    }
}
