// Created by Sérgio Murillo da Costa Faria
// Date: 19/02/2023

using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public bool isMoving;
    public float speed;
    public Vector3 movement;

    [SerializeField, ReadOnly] private Vector2 direction;
    [SerializeField, ReadOnly] private Rigidbody rb;
    [SerializeField, ReadOnly] private BoxCollider boxCollider;
    [SerializeField, ReadOnly] private GameControls controls;

    private void OnEnable()
    {
        controls = new GameControls();
        controls.Enable();
    }

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        boxCollider = gameObject.GetComponent<BoxCollider>();
        if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider>();
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Movement()
    {
        direction = controls.Default.Movement.ReadValue<Vector2>();
        movement = Vector3.zero;
        movement = new Vector3(direction.x, 0, direction.y).normalized;

        if (movement == Vector3.zero) return;
        rb.MovePosition(transform.position + movement * speed * Time.fixedDeltaTime);
    }

    private void OnDisable()
    {
        controls.Disable();
    }
}