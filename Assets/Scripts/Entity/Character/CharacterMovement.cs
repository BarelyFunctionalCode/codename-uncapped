using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class CharacterMovement : EntityComponent, IGravityModifiable
{
    [SerializeField] private PhysicsMaterial skiMaterial;
    [SerializeField] private PhysicsMaterial normalMaterial;
    public Vector3 SurfaceNormal { get; private set; } = Vector3.up;
    public Vector3 SurfacePoint { get; private set; } = Vector3.zero;
    public float DistanceToSurface { get; private set; } = Mathf.Infinity;



    private Transform characterTransform = null;
    private Collider characterCollider = null;
    private Rigidbody characterRb = null;


    float lastGroundedTime = 0;
    private NetworkVariable<float> gravityModifier = new();
    // Movement Parameters
    [SerializeField] private LayerMask groundeDetectionLayerMask;
    private readonly float hoverHeightMax = 0.02f;

    private readonly float upJetForce = 7031.25f; // TODO: This force value need to be moved to the elsewhere since they differ by class
    private readonly float downJetForce = 5156.25f; // TODO: This force value need to be moved to the elsewhere since they differ by class
    private readonly float jetAirMoveMinSpeed = 5f;
    private readonly float jetAirMoveMaxSpeed = 1000f;
    private readonly float jetAirMoveMaxAccelFactor = 1.5f;
    private readonly float jetDirectionalForceXY = 3125f; 
    private readonly float upJettingEnergyDrain = 22.5f;
    private readonly float downJettingEnergyDrain = 21.875f;
    private readonly float jetSkateEnergyDrain = 4f;

    private readonly float runForce = 10000f;
    private readonly float maxRunSpeed = 20f;
    private readonly float jumpForce = 2000f;
    private readonly float minJumpSpeed = 10f;
    private readonly float maxJumpSpeed = 15f;
    private readonly float jumpSurfaceAngle = 80f;
    private readonly float airControl = 1f;

    private readonly float horizontalJetResistance = 0.0017f;
    private readonly float horizontalJetResistanceFactor = 1.8f;
    private readonly float verticalJetResistance = 0.0006f;
    private readonly float verticalJetResistanceFactor = 1.8f;
    private readonly float horizResistFactor = 0.3f;
    private readonly float horizResistSpeed = 100f;
    private readonly float horizMaxSpeed = 120f;
    private readonly float upResistFactor = 0.3f;
    private readonly float upResistSpeed = 85f;
    private readonly float upMaxSpeed = 115f;
    private readonly float downResistFactor = 0.1f;
    private readonly float downResistSpeed = 1000f;
    private readonly float downMaxSpeed = 1000f;


    private readonly float drag = 0.004f;                        
    private readonly float airCushionDrag = 0.00275f;             
    private readonly float airCushionHeight = 10f;


    public override void Initialize(Entity entity)
    {
        base.Initialize(entity);

        if (IsServer) gravityModifier.Value = 1f;
        if (!IsServer || !IsOwner) return;

        if (characterTransform == null) characterTransform = entity.transform;
        if (characterCollider == null) characterCollider = entity.GetComponentInChildren<Collider>();
        if (characterRb == null) characterRb = entity.GetComponentInChildren<Rigidbody>();
    }

    public void ProcessUpdate(bool isSkiing)
    {
        if (characterRb == null || characterCollider == null) return;
        HandleGroundDetection();

        // Apply Drag and Friction
        characterRb.linearDamping = DistanceToSurface <= airCushionHeight ? airCushionDrag : drag;
        characterCollider.material = isSkiing ? skiMaterial : normalMaterial;
    }

    public void ProcessFixedUpdate(
        bool isRunning,
        bool isJumping,
        bool isSkiing,
        bool isJetting,
        bool isUpJetting,
        bool isDownJetting,
        Vector3 movementDirection = default,
        float rotationInputX = 0f
    )
    {
        HandleMovement(isRunning, isJumping, isSkiing, isJetting, isUpJetting, isDownJetting, movementDirection, rotationInputX);

        if (IsServer)
        {
            if (gravityModifier.Value != 1f)
            {
                gravityModifier.Value = Mathf.Lerp(gravityModifier.Value, 1f, Time.fixedDeltaTime * 5f);
                if (Mathf.Abs(gravityModifier.Value - 1f) < 0.01f) gravityModifier.Value = 1f;
            }
        }
    }

    public void UpdateCharacterData(Transform characterTransform, Collider characterCollider, Rigidbody characterRb)
    {
        if (characterTransform != null) this.characterTransform = characterTransform;
        if (characterCollider != null) this.characterCollider = characterCollider;
        if (characterRb != null) this.characterRb = characterRb;
    }

    public void SetGravityModifier(float modifier)
    {
        if (!IsSpawned || !IsServer) return;
        gravityModifier.Value = modifier;
    }

    public void Teleport(Vector3 destination, Quaternion rotation = default)
    {
        if (!IsServer) return;
        if (characterRb == null || characterCollider == null || characterTransform == null) return;

        characterRb.isKinematic = true;
        characterCollider.enabled = false;

        // localRb.position = destination;
        // if (rotation != default) localRb.rotation = rotation;
        // localRb.PublishTransform();
        GetComponent<NetworkTransform>().Teleport(destination, rotation, characterTransform.lossyScale);

        characterCollider.enabled = true;
        characterRb.isKinematic = false;
    }

    private void HandleGroundDetection()
    {
        lastGroundedTime += Time.deltaTime;
        entity.state.SetIsGrounded(false);

        DistanceToSurface = Mathf.Infinity;
        SurfaceNormal = Vector3.up;
        SurfacePoint = Vector3.zero;

        // Raycast down...
        Vector3 groundCheckPoint = characterCollider.bounds.center;
        RaycastHit hit;
        bool didHit = Physics.Raycast(
            new Ray(
                groundCheckPoint,
                Vector3.down
            ),
            out hit,
            DistanceToSurface,
            groundeDetectionLayerMask
        );
        if (didHit)
        {            
            // Surface too steep
            float slope = Vector3.Dot(hit.normal, Vector3.up);
            if (slope <= 0.1f) return;

            SurfacePoint = hit.point;
            DistanceToSurface = Mathf.Max(Vector3.Distance(SurfacePoint, groundCheckPoint) - characterCollider.bounds.extents.y - 0.2f, 0.0f);


            // Breakaway vertical speed check
            if (characterRb.linearVelocity.y > 20.0f) return;

            if (DistanceToSurface <= 0.6f)
            {
                entity.state.SetIsGrounded(true);
                lastGroundedTime = 0f;
            }
            else if (lastGroundedTime < 0.2f) entity.state.SetIsGrounded(true);
            else entity.state.SetIsGrounded(false);

            if (entity.state.IsGrounded) SurfaceNormal = hit.normal;
        }
    }

    private void HandleMovement(
        bool isRunning,
        bool isJumping,
        bool isSkiing,
        bool isJetting,
        bool isUpJetting,
        bool isDownJetting,
        Vector3 movementDirection,
        float rotationInputX
    )
    {
        if (characterRb == null || characterCollider == null || characterTransform == null) return;
        Vector3 currentVelocity = characterRb.linearVelocity;
        Vector3 desiredAcc = Vector3.zero;
        Vector3 groundImpulse = Vector3.zero;
        float desiredVerticalAcc = 0f;

        float gravityMagnitude = Physics.gravity.magnitude * gravityModifier.Value;

        // Air Control
        if (!entity.state.IsGrounded && !isJetting && !isSkiing)
        {
            Vector3 airDirection = movementDirection.normalized;
            Vector3 airControlAcc = airDirection * airControl;

            float maxAccel = runForce / characterRb.mass * Time.fixedDeltaTime * 0.3f;

            if (airControlAcc.magnitude > maxAccel)
            {
                airControlAcc = airControlAcc.normalized * maxAccel;
            }
            desiredAcc.x += airControlAcc.x;
            desiredAcc.z += airControlAcc.z;
        }

        // Jumping
        if (isJumping && entity.state.IsGrounded && currentVelocity.y <= maxJumpSpeed)
        {
            float jumpScale = 1.0f;

            if (currentVelocity.y < minJumpSpeed)
            {
                jumpScale = 1.0f - currentVelocity.y / minJumpSpeed / (maxJumpSpeed / minJumpSpeed);
            }

            Vector3 jumpDirection = movementDirection.normalized;

            float playerScaleFactor = characterTransform.localScale.y * 0.25f + 0.75f;
            float jumpForceFinal = jumpForce / characterRb.mass;

            float surfaceNormalDotJumpDirection = Vector3.Dot(jumpDirection, SurfaceNormal);

            if (surfaceNormalDotJumpDirection > 0.0f)
            {
                desiredAcc.x += SurfaceNormal.x * playerScaleFactor * jumpForceFinal;
                desiredAcc.z += SurfaceNormal.z * playerScaleFactor * jumpForceFinal;
            }

            Vector3 jumpSurfaceNormal = Vector3.Angle(SurfaceNormal, Vector3.up) <= jumpSurfaceAngle ?
                SurfaceNormal :
                Vector3.zero;
            desiredVerticalAcc = jumpSurfaceNormal.y * playerScaleFactor * jumpForceFinal * jumpScale;
            lastGroundedTime = 1f;
            
        }
        // Running Movement
        else if (isRunning)
        {
            groundImpulse = new(0f, -gravityMagnitude * Time.fixedDeltaTime, 0f);
            float slopeDot = -Vector3.Dot(groundImpulse, SurfaceNormal);

            if (slopeDot > 0.0f)
            {
                float modifiedSlopeDot = slopeDot + 0.002f;
                groundImpulse.y += SurfaceNormal.y * modifiedSlopeDot;
                groundImpulse.z += SurfaceNormal.z * modifiedSlopeDot;
                if (groundImpulse.magnitude < 0.0f) groundImpulse = Vector3.zero;
            }

            Vector3 targetVelocity = Vector3.zero;
            if (movementDirection.magnitude > 0.01f)
            {
                Vector3 runDirection = movementDirection;
                Vector3 forwardDirection = SurfaceNormal;
                Vector3 sideDirection = new(-runDirection.z * runDirection.magnitude, 0f, runDirection.x * runDirection.magnitude);
                float sideDot = Vector3.Dot(sideDirection, forwardDirection);
                forwardDirection -= sideDirection * sideDot;
                float moveDot = Vector3.Dot(runDirection, forwardDirection);
                runDirection -= forwardDirection * moveDot;
                targetVelocity = runDirection * (maxRunSpeed / runDirection.magnitude);
            }

            Vector3 velocityDiff = targetVelocity - (currentVelocity + groundImpulse);

            float maxRunAccel = runForce / characterRb.mass * Time.fixedDeltaTime;
            if (velocityDiff.magnitude > maxRunAccel)
                velocityDiff *= maxRunAccel / velocityDiff.magnitude;

            groundImpulse += velocityDiff;
        }

        // Skiing Movement
        if (isSkiing && entity.energy.CurrentEnergy > 0.0f)
        {
            // Hovering
            // More force the closer to the surface...
            float hoverFactor = Mathf.Clamp01(1.0f - (DistanceToSurface - hoverHeightMax) / hoverHeightMax) * 1.1f;

            Vector3 lateralVelocityDir = Vector3.ProjectOnPlane(currentVelocity, Vector3.up).normalized;
            float surfaceNormalDotLateralVelocityDirection = Vector3.Dot(SurfaceNormal, lateralVelocityDir);

            if (surfaceNormalDotLateralVelocityDirection > 0.0f)
            {
                // Going Downhill?
                // player is pushed fast downhill... easy
                desiredAcc = 2.0f * hoverFactor * gravityMagnitude * Time.fixedDeltaTime * Vector3.ProjectOnPlane(SurfaceNormal, Vector3.up);
            }
            else
            {
                // Going Uphill?
                Vector3 surfaceDirection = (SurfaceNormal - lateralVelocityDir * surfaceNormalDotLateralVelocityDirection).normalized;
                Vector3 sideDirection = -lateralVelocityDir;
                float sideDot = Vector3.Dot(surfaceDirection, sideDirection);
                
                desiredAcc = 0.5f * hoverFactor * gravityMagnitude * Time.fixedDeltaTime * (surfaceDirection - lateralVelocityDir * sideDot);
            }
            desiredAcc.y = 0.0f;
            Vector3 hoverVertAcc = hoverFactor * gravityMagnitude * Time.fixedDeltaTime * Vector3.up;
            currentVelocity += hoverVertAcc;
        }

        // Jetting Movement
        // TODO: I think there is suppose to be some kind of "Jet Activation Timeout" for when entity.energy depletes to prevent immediate re-jetting
        float speed = currentVelocity.magnitude;
        if (speed < jetAirMoveMaxSpeed)
        {
            float accelScale = 1.0f;
            if (speed < jetAirMoveMinSpeed && speed > 0.01f)
            {
                accelScale = Mathf.Min(
                    jetAirMoveMinSpeed / speed,
                    jetAirMoveMaxAccelFactor);
            }

            // Directional Control while Jetting/Skiing
            if (isSkiing && movementDirection.magnitude > 0.01f && entity.energy.CurrentEnergy > 0.0f)
            {
                float lateralForce = jetDirectionalForceXY / characterRb.mass * accelScale * Time.fixedDeltaTime;
                desiredAcc += movementDirection * lateralForce;
                entity.energy.ApplyEnergyDelta(-jetSkateEnergyDrain * accelScale * Time.fixedDeltaTime);
            }

            // Up Jetting
            if (isJetting && entity.energy.CurrentEnergy > 0.01f)
            {
                float force = 0f;
                if (isUpJetting)
                {
                    float cushion = 1.0f;
                    if (DistanceToSurface <= airCushionHeight)
                        cushion = (airCushionHeight - DistanceToSurface) / airCushionHeight;

                    force = upJetForce / characterRb.mass * accelScale * Time.fixedDeltaTime;
                    force += force * cushion * 0.5f;
                }
                else if (isDownJetting) // Down Jetting
                {
                    force = -downJetForce / characterRb.mass * accelScale * Time.fixedDeltaTime;
                }

                desiredVerticalAcc = force;
                entity.energy.ApplyEnergyDelta(- (isUpJetting ? upJettingEnergyDrain : downJettingEnergyDrain) * accelScale * Time.fixedDeltaTime);
            }
        }

        // Apply Jet Resistance
        currentVelocity += CalculateJetResistance(currentVelocity, desiredVerticalAcc, desiredAcc);

        // Apply desired acceleration, jetting accelration, walking acceleration, and gravity
        desiredAcc.y += desiredVerticalAcc;
        desiredAcc.y -= gravityMagnitude * Time.fixedDeltaTime;
        desiredAcc += groundImpulse;
        currentVelocity += desiredAcc;

        // Apply velocity caps
        currentVelocity += CalculateVelocityCaps(currentVelocity);
        // Debug.Log($"Current Velocity: {rb.linearVelocity:F2}\t Desired Acc: {desiredAcc:F2}\t Jet Resistance: {jetResistance:F2}\t Capped Excess: {velocityCappedExcess:F2}\t Final Velocity: {currentVelocity:F2}");
    
        // Calculate final change in velocity to apply
        Vector3 finalVelocityChange = currentVelocity - characterRb.linearVelocity;

        // Calculate rotation to apply
        Quaternion newRot = Quaternion.Euler(characterRb.rotation.eulerAngles + new Vector3(0f, rotationInputX, 0f));

        // Apply velocity and rotation updates to rigidbody
        characterRb.AddForce(finalVelocityChange, ForceMode.VelocityChange);
        characterRb.MoveRotation(newRot);
        // Debug.Log($"{(IsServer ? "Server" : "Client")} {OwnerClientId} Authoritative State: {anticipatedNetworkTransform.AuthoritativeState.Position}, {anticipatedNetworkTransform.AuthoritativeState.Rotation.eulerAngles} Should Reanticipate: {anticipatedNetworkTransform.ShouldReanticipate}");
    }

    private Vector3 CalculateJetResistance(Vector3 currentVelocity, float desiredVerticalAcc, Vector3 desiredAcc)
    {
        Vector3 newVelocity = currentVelocity;

        // Vertical
        float currentVerticalSpeed = Mathf.Abs(currentVelocity.y);
        float currentDesiredVerticalAccel = Mathf.Abs(desiredVerticalAcc);

        if (currentVerticalSpeed > 0.0f && currentDesiredVerticalAccel > 0.0f)
        {
            float resist = Mathf.Clamp(Mathf.Pow(currentVerticalSpeed, verticalJetResistanceFactor) * verticalJetResistance, 0.0f, 0.25f);
            newVelocity.y *= (currentVerticalSpeed - resist * currentDesiredVerticalAccel) / currentVerticalSpeed;
        }

        // Horizontal
        Vector3 currentHorizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
        float currentHorizontalSpeed = currentHorizontalVelocity.magnitude;
        float currentDesiredAccel = desiredAcc.magnitude;

        if (currentHorizontalSpeed > 0.0f && currentDesiredAccel > 0.0f)
        {
            float resist = Mathf.Clamp(Mathf.Pow(currentHorizontalSpeed, horizontalJetResistanceFactor) * horizontalJetResistance, 0.0f, 0.925f);
            float scale = (currentHorizontalSpeed - resist * currentDesiredAccel) / currentHorizontalSpeed;
            newVelocity.x *= scale;
            newVelocity.z *= scale;
        }

        return newVelocity - currentVelocity;
    }

    private Vector3 CalculateVelocityCaps(Vector3 currentVelocity)
    {
        // Horizontal Velocity Cap
        Vector3 cappedVelocity = currentVelocity;
        Vector3 horizVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
        float horizSpeed = horizVelocity.magnitude;
        if (horizSpeed > horizResistSpeed * 3.0f)
        {
            float maxSpeed = horizMaxSpeed * 3.0f;
            float targetSpeed = Mathf.Min(horizSpeed, maxSpeed);

            float scale = (targetSpeed - Time.fixedDeltaTime * horizResistFactor * (targetSpeed - horizResistSpeed)) / horizSpeed;
            cappedVelocity.x *= scale;
            cappedVelocity.z *= scale;
        }

        // Upward Velocity Cap
        if (cappedVelocity.y > upResistSpeed)
        {
            if (cappedVelocity.y > upMaxSpeed)
                cappedVelocity.y = upMaxSpeed;

            cappedVelocity.y -= Time.fixedDeltaTime * upResistFactor * (cappedVelocity.y - upResistSpeed);
        }

        // Downward Velocity Cap
        if (cappedVelocity.y < -downResistSpeed)
        {
            if (cappedVelocity.y < -downMaxSpeed)
                cappedVelocity.y = -downMaxSpeed;

            cappedVelocity.y += Time.fixedDeltaTime * downResistFactor * (cappedVelocity.y + downResistSpeed);
        }

        return cappedVelocity - currentVelocity;
    }
}
