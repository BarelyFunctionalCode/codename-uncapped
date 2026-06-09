using Unity.Netcode;
using UnityEngine;

public class AIDummy : AI
{
    Character targetCharacter;

    CharacterMovement movementData;
    Transform aiCharacterTransform;

    bool targetInSight = false;
    float targetLostTime = 5f;
    float targetLostTimer = 0f;

    void Awake()
    {
        if (GameModeHandler.Instance != null)
        {
            GameModeHandler.Instance.currentPhase.OnValueChanged += CreateCharacter;
        }
    }

    void OnDestroy()
    {
        Character = null;
        if (GameModeHandler.Instance != null)
        {
            GameModeHandler.Instance.currentPhase.OnValueChanged -= CreateCharacter;
        }
    }

    void Update()
    {
        if (Character == null) return;
        if (!controlsEnabled) return;

        if (targetCharacter == null || targetCharacter.state.IsDead)
        {
            int randomTargetIndex = Random.Range(0, CharacterManager.Instance.characters.Count);
            targetCharacter = CharacterManager.Instance.characters[randomTargetIndex];
        }
        else
        {
            Transform povTransform = Character.localCharacterType.cameraLookAtTarget;
            Vector3 vectorToTarget = targetCharacter.localCharacterType.transform.position - povTransform.position;
            Vector3 localVectorToTarget = aiCharacterTransform.InverseTransformDirection(vectorToTarget);
            Vector2 moveVector = new(Mathf.Clamp(localVectorToTarget.x, -1f, 1f), Mathf.Clamp(localVectorToTarget.z, -1f, 1f));

            RaycastHit hit;
            // Move to get close to target
            bool doMove = !(
                vectorToTarget.magnitude < 50f &&
                Physics.Raycast(povTransform.position, vectorToTarget.normalized, out hit, 55f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                hit.transform == targetCharacter.transform
            );
            OnMove(doMove ? moveVector : Vector2.zero);
            OnSki(doMove);

            // Fire if target is in sight
            float verticalAim = (vectorToTarget.normalized.y - povTransform.forward.y) * 20f;
            float targetAlignment = Mathf.Clamp01(1.5f - Vector3.Dot(povTransform.forward, vectorToTarget.normalized));
            Vector2 lookVector = new(Mathf.Clamp(localVectorToTarget.x, -20f, 20f), Mathf.Clamp(verticalAim * targetAlignment, -15f, 15f));
            OnLook(lookVector);
            if (
                vectorToTarget.magnitude < 250f &&
                Physics.Raycast(povTransform.position, povTransform.forward, out hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                hit.transform == targetCharacter.transform
            )
            {
                targetInSight = true;
                targetLostTimer = 0f;
            }
            if (targetLostTimer > targetLostTime) targetInSight = false;
            else targetLostTimer += Time.deltaTime;
            OnPrimaryFire(targetInSight);

            // Jet if going uphill and close to the ground
            float goingUphill = Vector3.Dot(aiCharacterTransform.forward, movementData.SurfaceNormal);
            bool doJet = goingUphill < -0.4f && movementData.DistanceToSurface < 10f;
            if (doJet) OnJumpJet(true);
            else OnJumpJet(false);
        }
    }

    private void CreateCharacter(Phase _, Phase current)
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsHost) return;
        if (current == Phase.WARMUP)
        {
            CharacterManager.Instance.RegisterAI(this);
        }
    }

    protected sealed override void OnInitialized()
    {
        movementData = Character.characterMovement;
        aiCharacterTransform = Character.localCharacterType.transform;
    }

    #region Input Callbacks
    public void OnLook(Vector2 lookInput)
    {
        if (!controlsEnabled) return;
        Character.characterInputs.LookInput(lookInput);
    }

    public void OnMove(Vector2 moveInput)
    {
        if (!controlsEnabled) return;
        Character.characterInputs.MoveInput(moveInput);
    }

    public void OnPrimaryFire(bool isPressed)
    {
        if (!controlsEnabled) return;
        if (isPressed) Character.characterLoadout.OnPrimaryFireStartedRpc();
        else Character.characterLoadout.OnPrimaryFireCanceledRpc();
    }

    public void OnThrowable(bool isPressed)
    {
        if (!controlsEnabled) return;
        if (isPressed) Character.characterInputs.ThrowableStarted();
        else Character.characterInputs.ThrowableReleased();
    }

    public void OnActivateDrive(bool isPressed)
    {
        if (!controlsEnabled) return;
        if (isPressed) Character.characterLoadout.ActivateDriveRpc();
    }

    public void OnUseGear(bool isPressed)
    {
        if (!controlsEnabled) return;
        Character.characterLoadout.UseGearRpc(isPressed);
    }

    public void OnPreviousWeapon(bool isPressed)
    {
        if (!controlsEnabled) return;
        if (isPressed) Character.characterLoadout.PreviousWeaponRpc();
    }

    public void OnNextWeapon(bool isPressed)
    {
        if (!controlsEnabled) return;
        if (isPressed) Character.characterLoadout.NextWeaponRpc();
    }

    public void OnSki(bool isPressed)
    {
        if (!controlsEnabled) return;
        Character.characterInputs.SkiInput(isPressed ? 1f : 0f);
    }

    public void OnJumpJet(bool isPressed)
    {
        if (!controlsEnabled) return;

        Character.characterInputs.JetInput(isPressed ? 1f : 0f);
        if (isPressed) Character.characterInputs.JumpInput();
    }

    public void OnDownJet(bool isPressed)
    {
        if (!controlsEnabled) return;
        Character.characterInputs.DownJetInput(isPressed ? 1f : 0f);
    }
    #endregion
}
