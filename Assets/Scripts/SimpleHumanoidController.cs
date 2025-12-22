using UnityEngine;
using UnityEngine.InputSystem; // ★ 追加

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class SimpleHumanoidController : MonoBehaviour
{
    public float moveSpeed = 4f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        float h = 0f;
        float v = 0f;

        if (Keyboard.current.aKey.isPressed) h -= 1f;
        if (Keyboard.current.dKey.isPressed) h += 1f;
        if (Keyboard.current.sKey.isPressed) v -= 1f;
        if (Keyboard.current.wKey.isPressed) v += 1f;

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.2f);
        }

        Vector3 move = transform.forward * inputDir.magnitude;
        controller.Move(move * moveSpeed * Time.deltaTime);

        float speedParam = inputDir.magnitude;
        animator.SetFloat("Speed", speedParam);
    }
}
