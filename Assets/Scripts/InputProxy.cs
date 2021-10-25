using UnityEngine;
using UnityEngine.InputSystem;

public class InputProxy : MonoBehaviour
{

    void OnMove(InputValue movement)
    {
        var movementVector = movement.Get<Vector2>();
        InputCapture.movementX = movementVector.x;
        InputCapture.movementY = movementVector.y;
    }

}