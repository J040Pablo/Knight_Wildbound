using UnityEngine;
using Roguelite.Core;

namespace Roguelite.Player
{
    public enum HorseState
    {
        Idle,
        Walk,
        Trot,
        Gallop
    }

    [RequireComponent(typeof(CharacterController))]
    public class HorseController : MonoBehaviour
    {
        [Header("Speed Settings")]
        [SerializeField] private float walkSpeed = 6.0f;
        [SerializeField] private float trotSpeed = 11.0f;
        [SerializeField] private float gallopSpeed = 17.0f;
        [SerializeField] private float turnSpeed = 10.0f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 9.5f;

        [Header("Mount Socket")]
        [SerializeField] private Transform mountSocket;

        public Transform MountSocket => mountSocket != null ? mountSocket : transform;
        public HorseState CurrentState { get; private set; } = HorseState.Idle;
        public bool IsMounted { get; private set; } = false;

        private CharacterController characterController;
        private Vector3 verticalVelocity;
        private const float GRAVITY = -19.6f;

        // Visual leg animation nodes for movement feedback
        private Transform frontLeftLeg;
        private Transform frontRightLeg;
        private Transform backLeftLeg;
        private Transform backRightLeg;
        private float legCycle = 0f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            BuildPlaceholderHorseVisuals();
        }

        private void BuildPlaceholderHorseVisuals()
        {
            // Simple low-poly placeholder geometry
            // 1. Torso/Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "HorseBody_Visual";
            body.transform.parent = transform;
            body.transform.localPosition = new Vector3(0, 1.1f, 0);
            body.transform.localScale = new Vector3(1.0f, 1.0f, 2.2f);
            Collider bCol = body.GetComponent<Collider>();
            if (bCol != null) Destroy(bCol);
            Renderer bR = body.GetComponent<Renderer>();
            if (bR != null) bR.material.color = new Color(0.45f, 0.25f, 0.12f); // Chestnut brown

            // 2. Neck & Head
            GameObject neck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neck.name = "HorseHead_Visual";
            neck.transform.parent = transform;
            neck.transform.localPosition = new Vector3(0, 1.8f, 0.9f);
            neck.transform.localScale = new Vector3(0.6f, 1.1f, 0.7f);
            Quaternion nRot = Quaternion.Euler(25f, 0, 0);
            nRot.Normalize();
            neck.transform.localRotation = nRot;
            Collider nCol = neck.GetComponent<Collider>();
            if (nCol != null) Destroy(nCol);
            Renderer hR = neck.GetComponent<Renderer>();
            if (hR != null) hR.material.color = new Color(0.40f, 0.22f, 0.10f);

            // 3. Saddle (Mount Socket location indicator)
            GameObject saddle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            saddle.name = "HorseSaddle_Visual";
            saddle.transform.parent = transform;
            saddle.transform.localPosition = new Vector3(0, 1.65f, 0.1f);
            saddle.transform.localScale = new Vector3(0.85f, 0.2f, 0.85f);
            Collider sCol = saddle.GetComponent<Collider>();
            if (sCol != null) Destroy(sCol);
            Renderer sR = saddle.GetComponent<Renderer>();
            if (sR != null) sR.material.color = new Color(0.2f, 0.15f, 0.1f); // Dark leather

            // Socket transform for player mounting point
            GameObject socketObj = new GameObject("MountSocket");
            socketObj.transform.parent = transform;
            socketObj.transform.localPosition = new Vector3(0, 1.8f, 0.1f);
            mountSocket = socketObj.transform;

            // 4. Legs for walking animation
            frontLeftLeg = CreateLeg("Leg_FL", new Vector3(-0.4f, 0.5f, 0.8f));
            frontRightLeg = CreateLeg("Leg_FR", new Vector3(0.4f, 0.5f, 0.8f));
            backLeftLeg = CreateLeg("Leg_BL", new Vector3(-0.4f, 0.5f, -0.8f));
            backRightLeg = CreateLeg("Leg_BR", new Vector3(0.4f, 0.5f, -0.8f));
        }

        private Transform CreateLeg(string legName, Vector3 localPos)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = legName;
            leg.transform.parent = transform;
            leg.transform.localPosition = localPos;
            leg.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f);
            Collider lCol = leg.GetComponent<Collider>();
            if (lCol != null) Destroy(lCol);
            Renderer r = leg.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.35f, 0.20f, 0.10f);
            return leg.transform;
        }

        public void SetMountedState(bool mounted)
        {
            IsMounted = mounted;
        }

        private void Update()
        {
            if (UI.MasteryScreenUI.IsAnyMenuOpen)
            {
                return;
            }

            if (!IsMounted)
            {
                if (characterController.isGrounded && verticalVelocity.y < 0)
                {
                    verticalVelocity.y = -2f;
                }
                verticalVelocity.y += GRAVITY * Time.deltaTime;

                if (SafeCanMove())
                {
                    characterController.Move(verticalVelocity * Time.deltaTime);
                }

                CurrentState = HorseState.Idle;
                ResetLegs();
            }
        }

        public void TryJump()
        {
            if (SafeCanMove() && characterController.isGrounded)
            {
                verticalVelocity.y = jumpForce;
            }
        }

        public void ProcessMovementInput(Vector3 inputDir, bool wantsSprint, Camera mainCam)
        {
            Vector3 horizontalMove = Vector3.zero;
            float speed = 0f;

            if (inputDir.magnitude > 0.1f)
            {
                if (wantsSprint)
                {
                    speed = gallopSpeed;
                    CurrentState = HorseState.Gallop;
                }
                else if (inputDir.magnitude > 0.6f)
                {
                    speed = trotSpeed;
                    CurrentState = HorseState.Trot;
                }
                else
                {
                    speed = walkSpeed;
                    CurrentState = HorseState.Walk;
                }

                // Camera relative direction
                Vector3 forward = mainCam != null ? mainCam.transform.forward : transform.forward;
                Vector3 right = mainCam != null ? mainCam.transform.right : transform.right;
                forward.y = 0; right.y = 0;
                forward.Normalize(); right.Normalize();

                Vector3 moveDir = (forward * inputDir.z + right * inputDir.x).normalized;

                if (moveDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
                    targetRot.Normalize();
                    Quaternion slerped = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
                    slerped.Normalize();
                    transform.rotation = slerped;
                }

                horizontalMove = moveDir * speed;
                AnimateLegs(speed);
            }
            else
            {
                CurrentState = HorseState.Idle;
                ResetLegs();
            }

            // Ground snapping & gravity calculation
            if (SafeCanMove() && characterController.isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2f;
            }
            verticalVelocity.y += GRAVITY * Time.deltaTime;

            // SINGLE CharacterController.Move call per frame to avoid PhysX double-call issues
            Vector3 totalVelocity = horizontalMove + verticalVelocity;
            if (SafeCanMove())
            {
                characterController.Move(totalVelocity * Time.deltaTime);
            }
        }

        private bool SafeCanMove()
        {
            return characterController != null && characterController.enabled && characterController.gameObject.activeInHierarchy;
        }

        private void AnimateLegs(float speed)
        {
            legCycle += Time.deltaTime * speed * 2.5f;
            float swingAngle = Mathf.Sin(legCycle) * 20f;

            if (frontLeftLeg != null) { Quaternion r = Quaternion.Euler(swingAngle, 0, 0); r.Normalize(); frontLeftLeg.localRotation = r; }
            if (backRightLeg != null) { Quaternion r = Quaternion.Euler(swingAngle, 0, 0); r.Normalize(); backRightLeg.localRotation = r; }
            if (frontRightLeg != null) { Quaternion r = Quaternion.Euler(-swingAngle, 0, 0); r.Normalize(); frontRightLeg.localRotation = r; }
            if (backLeftLeg != null) { Quaternion r = Quaternion.Euler(-swingAngle, 0, 0); r.Normalize(); backLeftLeg.localRotation = r; }
        }

        private void ResetLegs()
        {
            if (frontLeftLeg != null) frontLeftLeg.localRotation = Quaternion.identity;
            if (frontRightLeg != null) frontRightLeg.localRotation = Quaternion.identity;
            if (backLeftLeg != null) backLeftLeg.localRotation = Quaternion.identity;
            if (backRightLeg != null) backRightLeg.localRotation = Quaternion.identity;
        }
    }
}
