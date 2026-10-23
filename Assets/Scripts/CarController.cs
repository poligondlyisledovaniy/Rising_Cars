using UnityEngine;

public class CarController : MonoBehaviour
{
    public float speed = 20f;
    public float reverseSpeed = 10f;
    public float turnSpeed = 100f;

    void Update()
    {
        float turn = 0f;
        if (Input.GetKey(KeyCode.A)) turn -= 1f;
        if (Input.GetKey(KeyCode.D)) turn += 1f;

        bool moving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S);
        if (moving) transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);

        float drive = 0f;
        if (Input.GetKey(KeyCode.W)) drive = 1f;
        if (Input.GetKey(KeyCode.S)) drive = -1f;

        float currentSpeed = (drive > 0) ? speed : reverseSpeed;

        if (drive != 0)
        {
            transform.position += transform.forward * drive * currentSpeed * Time.deltaTime;
        }
    }
}