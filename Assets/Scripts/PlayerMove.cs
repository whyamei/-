using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 4f;
    public float jumpSpeed = 4.5f;
    public float rotateLerpSpeed = 10f;

    [Header("落地检查")]
    public float groundRayLength = 1.05f;
    public bool drawGroundRay = true;

    private Rigidbody rb;
    private float inputX;
    private float inputZ;
    private bool isGround;
    private bool jumpPressed;
    private Vector3 moveDir; // 保存移动方向，给Update旋转用

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.mass = 1.0f;
        rb.useGravity = true;
    }

    void Update()
    {
        // 输入放在Update
        inputX = Input.GetAxisRaw("Horizontal");
        inputZ = Input.GetAxisRaw("Vertical");
        moveDir = new Vector3(inputX, 0, inputZ);
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        if (Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
        }

        isGround = Physics.Raycast(transform.position, Vector3.down, groundRayLength, ~0, QueryTriggerInteraction.Ignore);

        // ========== 旋转放到 Update！！ ==========
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateLerpSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        Vector3 velocity = rb.velocity;
        if (jumpPressed && isGround)
        {
            rb.velocity = new Vector3(moveDir.x * moveSpeed, jumpSpeed, moveDir.z * moveSpeed);
            Debug.Log("玩家起跳，初始速度" + jumpSpeed + "m/s");
        }
        else
        {
            // 只控制水平速度，不碰旋转
            rb.velocity = new Vector3(moveDir.x * moveSpeed, rb.velocity.y, moveDir.z * moveSpeed);
        }
        jumpPressed = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("玩家" + collision.collider.name + ",撞击速度" + collision.relativeVelocity.magnitude.ToString("F1") + "m/s");
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGroundRay)
        {
            return;
        }
        Gizmos.color = isGround ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundRayLength);
    }
}
