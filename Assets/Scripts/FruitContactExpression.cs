using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class FruitContactExpression : MonoBehaviour
{
    [SerializeField] private Collider2D fruitCollider;
    [SerializeField] private Animator faceAnimator;

    private static readonly int IsTouchingParameter = Animator.StringToHash("IsTouching");

    // 벽과 과일에 동시에 닿아 있을 수 있으므로 모든 접촉점을 확인한다.
    private readonly List<ContactPoint2D> contacts = new List<ContactPoint2D>(8);

    public bool IsTouching { get; private set; }

    private void Reset()
    {
        FindReferences();
    }

    private void Awake()
    {
        FindReferences();
    }

    private void OnEnable()
    {
        SetExpression(false);
    }

    private void LateUpdate()
    {
        bool touching = HasFruitContact();

        if (touching != IsTouching)
            SetExpression(touching);
    }

    private bool HasFruitContact()
    {
        if (fruitCollider == null || !fruitCollider.enabled) return false;

        // Trigger 접촉은 GetContacts가 반환하는 물리 접촉점에 포함되지 않는다.
        int count = fruitCollider.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            var contact = contacts[i];
            var other = contact.collider == fruitCollider ? contact.otherCollider : contact.collider;
            if (other == null) continue;

            var otherFruit = other.GetComponentInParent<FruitContactExpression>();
            if (otherFruit != null && otherFruit != this)
                return true;
        }
        return false;
    }

    private void OnDisable()
    {
        SetExpression(false);
    }

    private void FindReferences()
    {
        if (fruitCollider == null)
            fruitCollider = GetComponent<Collider2D>();
        if (faceAnimator == null)
            faceAnimator = GetComponentInChildren<Animator>(true);
    }

    private void SetExpression(bool touching)
    {
        IsTouching = touching;
        if (faceAnimator != null && faceAnimator.runtimeAnimatorController != null)
            faceAnimator.SetBool(IsTouchingParameter, touching);
    }
}
