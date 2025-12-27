using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorRandomize : MonoBehaviour
{
    [SerializeField]
    private string stateName = "Idle";

    [SerializeField]
    private Vector2 speedRange = new Vector2(0.95f, 1.05f);

    void Start()
    {
        Animator animator = GetComponent<Animator>();

        // 再生位置をランダム化（0〜1）
        float randomTime = Random.value;
        animator.Play(stateName, 0, randomTime);

        // 再生速度をランダム化
        animator.speed = Random.Range(speedRange.x, speedRange.y);
    }
}
