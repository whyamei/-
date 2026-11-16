using UnityEngine;

public class LockedHingeGate : MonoBehaviour
{
    [Header("铰链组件")]
    public HingeJoint hinge;
    [Header("开门参数")]
    public float motorSpeed = 60f;
    public float motorForce = 120f;
    public float targetAngle = 85f;

    private Rigidbody rb;
    private bool isUnlocked = false;
    private bool isOpened = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (hinge == null)
        {
            hinge = GetComponent<HingeJoint>();
        }
    }

    public void UnlockGate()
    {
        if (isUnlocked) return;
        isUnlocked = true;
        JointMotor motor = hinge.motor;
        motor.targetVelocity = motorSpeed;
        motor.force = motorForce;
        hinge.motor = motor;
        hinge.useMotor = true;
        Debug.Log("门解锁，马达启动");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            UnlockGate();
        }
    }

    void FixedUpdate()
    {
        if (hinge == null || rb == null) return;

        if (!isUnlocked)
        {
            rb.angularDrag = 10f;
            hinge.useMotor = false;
            return;
        }

        if (!isOpened)
        {
            float currentAngle = hinge.angle;
            Debug.Log("当前铰链角度：" + currentAngle);
            // 取绝对值，不管往正/负方向开，到85度就停
            if (Mathf.Abs(currentAngle) >= targetAngle)
            {
                isOpened = true;
                hinge.useMotor = false;
                rb.angularDrag = 5f;
                Debug.Log("门开到目标角度，停止马达");
            }
        }
    }

    public void LockGate()
    {
        isUnlocked = false;
        isOpened = false;
        hinge.useMotor = false;
        rb.angularDrag = 10f;
    }
}
