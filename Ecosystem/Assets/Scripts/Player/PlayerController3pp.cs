using UnityEngine;
using System;
using Input;
using UnityEngine.Serialization;

namespace Player
{
    public class PlayerController3pp : MonoBehaviourPlus
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Vector2 lookSpeed = Vector2.one;
        [SerializeField] private bool invertMouseY = true;
        [SerializeField] private float stableMovementSharpness = 18f;
        [SerializeField] private float dragFactorStatValue = 0.5f;
        [SerializeField] private float orientationSharpness = 18f;

        private Vector3 currentVelocity;
        private Vector3 moveInputVector;
        private Vector3 lookInputVector;
        private float moveLookAngularDifference = 0f;
        

        // Start is called before the first frame update
        void Start()
        {
            InputManager.Singleton.OnMove += Move;
            InputManager.Singleton.OnLook += Look;
            InputManager.Singleton.OnJump += Jump;
        }

        private void OnDestroy()
        {
            InputManager.Singleton.OnMove -= Move;
            InputManager.Singleton.OnLook -= Look;
            InputManager.Singleton.OnJump -= Jump;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            UpdatePosition();
            UpdateRotation();
        }

        private void Move(Vector2 _inputValue)
        {
            moveInputVector = new Vector3(_inputValue.x, 0, _inputValue.y).normalized;
        }

        private void Look(Vector2 _inputValue)
        {
            lookInputVector = (Vector3) _inputValue;
        }

        private void Jump(bool _value)
        {
            
        }
        
        private void UpdatePosition()
        {
            var deltaTime = Time.deltaTime;
            var moveDirection = transform.forward * moveInputVector.z + transform.right * moveInputVector.x;
            moveDirection.Normalize();
            var targetMovementVelocity = moveDirection * moveSpeed;

            // Smooth movement Velocity
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-stableMovementSharpness * dragFactorStatValue * deltaTime));
            transform.position += currentVelocity;

            // Ground movement
            /*if (Motor.GroundingStatus.IsStableOnGround)
            {
                float currentVelocityMagnitude = currentVelocity.magnitude;

                Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
                if (currentVelocityMagnitude > 0f && Motor.GroundingStatus.SnappingPrevented)
                {
                    // Take the normal from where we're coming from
                    Vector3 groundPointToCharacter = Motor.TransientPosition - Motor.GroundingStatus.GroundPoint;
                    if (Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0f)
                    {
                        effectiveGroundNormal = Motor.GroundingStatus.OuterGroundNormal;
                    }
                    else
                    {
                        effectiveGroundNormal = Motor.GroundingStatus.InnerGroundNormal;
                    }
                }

                //Landed this update
                if (!previousGroundingState)
                {
                    onLand?.Invoke(Mathf.Abs(previousVelocity.y));
                }

                // Reorient velocity on slope
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                // Calculate target velocity
                Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                float sprintFactor = (_shouldBeSprinting?(sprintFactorStatValue):0);
                float speed = MaxStableMoveSpeed*speedStatValue;
                Vector3 targetMovementVelocity = reorientedInput * (speed + (speed * sprintFactor));

                // Smooth movement Velocity
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * dragFactorStatValue * deltaTime));
            }*/
            // Air movement
            /*else
            {
                // InAirDuration += Time.deltaTime;

                // Add move input
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    var addedVelocity = _moveInputVector * (AirAccelerationSpeed * deltaTime);

                    var currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                    float speed = MaxAirMoveSpeed*speedStatValue;

                    // Limit air velocity from inputs
                    if (currentVelocityOnInputsPlane.magnitude < speed)
                    {
                        // clamp addedVel to make total vel not exceed max vel on inputs plane
                        var newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, speed);
                        addedVelocity = newTotal - currentVelocityOnInputsPlane;
                    }
                    else
                    {
                        // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                        if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                        {
                            addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                        }
                    }

                    // Prevent air-climbing sloped walls
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                        {
                            var perpendicularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                            addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpendicularObstructionNormal);
                        }
                    }

                    // Apply added velocity
                    currentVelocity += addedVelocity;
                }

                var gravityDelta = Gravity * (Mass * deltaTime);

                // Decrease velocity if descending
                if (currentVelocity.y < 0)
                {
                    gravityDelta *= DescentFactor * glideFactorStatValue;
                }

                // Normal gravity
                currentVelocity += gravityDelta;

                // Drag
                // var dragDelta = currentVelocity * Drag;
                // currentVelocity -= dragDelta;

                // Check min jump velocity
                // if (currentVelocity.y > _minJumpVelocity) currentVelocity.y = _minJumpVelocity;
            }

            previousGroundingState = Motor.GroundingStatus.IsStableOnGround;

            // Handle jumping
            _jumpedThisFrame = false;
            _doubleJumpedThisFrame = false;
            _timeSinceJumpRequested += deltaTime;
            if (_jumpRequested)
            {
                var allowedToJump = _jumpCount==1
                                    && !_jumpConsumed
                                    && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) ||
                                        _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime);

                if(debugMessages) Debug.LogWarning($"PlayerKCC allowedToJump {allowedToJump} _jumpConsumed {_jumpConsumed}" +
                                                   $"grounded {(AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)}" +
                                                   $"_timeSinceLastAbleToJump {_timeSinceLastAbleToJump}");

                // See if we actually are allowed to jump
                if (allowedToJump)
                {
                    // Calculate jump direction before ungrounding
                    var jumpDirection = Motor.CharacterUp;
                    if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                    {
                        jumpDirection = Motor.GroundingStatus.GroundNormal;
                    }

                    // Makes the character skip ground probing/snapping on its next update.
                    // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                    Motor.ForceUnground();

                    var jumpVelocity = (1-(_jumptimer / JumpDuration)) * _maxJumpVelocity * jumpFactorStatValue;

                    var addedJumpVelocity = jumpDirection.normalized * jumpVelocity;
                    // var addedJumpVelocity = (jumpDirection * jumpVelocity) - Vector3.Project(currentVelocity, Motor.CharacterUp);

                    /*if(debugMessages) Debug.LogWarning($"PlayerKCC.UpdateVelocity jumpVelocity {jumpVelocity} " +
                                                       $"addedJumpVelocity.magnitude {addedJumpVelocity.magnitude} " +
                                                       $"currentVelocity.magnitude {currentVelocity.magnitude}");#1#

                    // Add to the return velocity and reset jump state
                    currentVelocity += addedJumpVelocity;

                    // Forward jump speed scale
                    // currentVelocity += (_moveInputVector * JumpScalableForwardSpeed);
                    // currentVelocity += _moveInputVector;

                    _jumpRequested = false;

                    _jumpedThisFrame = true;

                    if (_jumpStart)
                    {
                        onJumpDown?.Invoke();
                    }

                    if (currentVelocity.y <= 0 && !_jumpConsumed) _jumpConsumed = true;
                }

                var allowedToDoubleJump = _jumpStart && _jumpRequested && _doubleJumpEnabled && _jumpCount<=2 && !_jumpedThisFrame && !_doubleJumpConsumed;

                if(debugMessages) Debug.LogWarning($"PlayerKCC DoubleJump allowed {allowedToDoubleJump}/true, " +
                                                   $"start {_jumpStart}/true, requested {_jumpRequested}/true, " +
                                                   $"enabled {_doubleJumpEnabled}/true, jumpCount {_jumpCount}<=2, " +
                                                   $"jumpedThisFrame {_jumpedThisFrame}/false, doubleJumpConsumed {_doubleJumpConsumed}/false");

                if (allowedToDoubleJump)
                {
                    _doubleJumpedThisFrame = true;

                    // Calculate jump direction before ungrounding
                    Vector3 jumpDirection = Motor.CharacterUp;

                    var jumpVelocity = DoubleJumpPowerMultiplier * _maxJumpVelocity;

                    var verticalJumpVelocity = jumpDirection.normalized * jumpVelocity;

                    currentVelocity.y = verticalJumpVelocity.y;

                    _jumpRequested = false;

                    _jumpedThisFrame = true;
                    _doubleJumpConsumed = true;

                    if (debugMessages)
                    {
                        Debug.Log($"Jump added velocity {verticalJumpVelocity.magnitude} current velocity {currentVelocity}");
                        Debug.Log($"Jump states: {_jumpCount} _jumpRequested {_jumpRequested} _jumpConsumed {_jumpConsumed} _jumpedThisFrame {_jumpedThisFrame} _doubleJumpConsumed {_doubleJumpConsumed}");
                    }

                    onDoubleJump?.Invoke();
                }

                if(_jumpStart) _jumpStart = false;
            }

            // Take into account additive velocity
            if (_internalVelocityAdd.sqrMagnitude > 0f)
            {
                currentVelocity += _internalVelocityAdd;
                _internalVelocityAdd = Vector3.zero;
            }
            previousVelocity = currentVelocity;*/
        }
        
        private void UpdateRotation()
        {
            // Calculate the rotation amount based on the mouse input and rotation speed
            var rotationAmount = lookInputVector.x * lookSpeed.x * Time.deltaTime;

            // Apply the rotation to the transform around the Vector.up axis
            transform.Rotate(Vector3.up, rotationAmount);
        }
    }
}
