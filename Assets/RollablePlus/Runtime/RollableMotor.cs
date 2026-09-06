using UnityEngine;
using UnityEngine.InputSystem;

namespace RollablePlus
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RollableMotor : MonoBehaviour
    {
        public ArenaGame game;
        public float acceleration = 28f;
        public float maxSpeed = 6.5f;
        public float dashCooldown = 2.2f;
        public float DashReady => Mathf.Clamp01(1f - cooldown / dashCooldown);
        public bool Dashing => dashTime > 0;
        public Rigidbody Body { get; private set; }
        Vector2 input;
        Vector3 heading = Vector3.forward;
        float cooldown, dashTime;
        bool dashQueued;
        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.linearDamping = 1.7f;
            Body.maxAngularVelocity = 24;
        }
        void Update()
        {
            if (game == null || !game.IsPlaying) { input = Vector2.zero; dashQueued = false; return; }
            var kb = Keyboard.current;
            input = Vector2.zero;
            if (kb != null)
            {
                input.x = ((kb.dKey.isPressed || kb.rightArrowKey.isPressed) ? 1 : 0) - ((kb.aKey.isPressed || kb.leftArrowKey.isPressed) ? 1 : 0);
                input.y = ((kb.wKey.isPressed || kb.upArrowKey.isPressed) ? 1 : 0) - ((kb.sKey.isPressed || kb.downArrowKey.isPressed) ? 1 : 0);
                dashQueued |= kb.spaceKey.wasPressedThisFrame;
            }
            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (stick.sqrMagnitude > .04f) input = stick;
                dashQueued |= Gamepad.current.buttonSouth.wasPressedThisFrame;
            }
            input = Vector2.ClampMagnitude(input, 1);
            cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
            dashTime = Mathf.Max(0, dashTime - Time.deltaTime);
        }
        void FixedUpdate()
        {
            if (game == null || !game.IsPlaying) return;
            Vector3 move = new Vector3(input.x, 0, input.y);
            if (move.sqrMagnitude > .02f) heading = move.normalized;
            Body.AddForce(move * acceleration, ForceMode.Acceleration);
            if (dashQueued && cooldown <= 0)
            {
                cooldown = dashCooldown; dashTime = .22f;
                Body.linearVelocity = heading * 13f + Vector3.up * Body.linearVelocity.y;
                game.Dash();
            }
            dashQueued = false;
            Vector3 velocity = Body.linearVelocity;
            Vector3 planar = Vector3.ClampMagnitude(new Vector3(velocity.x, 0, velocity.z), Dashing ? 13 : maxSpeed);
            Body.linearVelocity = planar + Vector3.up * velocity.y;
            if (transform.position.y < -3) game.Fell();
        }
        public void ResetAt(Vector3 point)
        {
            Body.position = point; Body.rotation = Quaternion.identity;
            Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            cooldown = 0; dashTime = 0; input = Vector2.zero; dashQueued = false;
        }
        void OnTriggerEnter(Collider other)
        {
            if (game == null || !game.IsPlaying) return;
            ArenaPickup pickup = other.GetComponentInParent<ArenaPickup>();
            if (pickup != null) game.Collect(pickup);
        }
        void OnCollisionEnter(Collision other) { Contact(other); }
        void OnCollisionStay(Collision other) { Contact(other); }
        void Contact(Collision other)
        {
            if (game != null && other.collider.GetComponentInParent<ArenaChaser>() != null)
                game.Hit(other.transform.position);
        }
    }
}
