using UnityEngine;

public class CharacterTelemetry
{
    private Character character;

    [PauseMenuDevOption("Surface Data")]
    public bool enableSurfaceDebug = false;

    [PauseMenuDevOption("Movement Data")]
    public bool enableMovementDebug = false;

    private DevVectorRenderer devVectorRenderer;

    public Vector3 position;
    public Vector3 velocity;

    public Vector3 movementDirection;

    public Vector3 surfaceNormal;
    public Vector3 surfacePoint;
    public float distanceToSurface;
    
    public bool isSkiing;
    public bool isUpJetting;
    public bool isDownJetting;
    public bool isGrounded;
    public bool previousIsGrounded;

    public CharacterTelemetry(DevVectorRenderer devVectorRenderer, Character character)
    {
        this.devVectorRenderer = devVectorRenderer;
        this.character = character;
        position = Vector3.zero;
        velocity = Vector3.zero;
        movementDirection = Vector3.zero;
        surfaceNormal = Vector3.zero;
        surfacePoint = Vector3.zero;
        distanceToSurface = 0.0f;
        isSkiing = false;
        isUpJetting = false;
        isDownJetting = false;
        isGrounded = false;
    }

    public void Update()
    {

        movementDirection = character.characterInputs.MovementDirection;
        isSkiing = character.characterInputs.IsSkiing;
        isUpJetting = character.characterInputs.IsUpJetting;
        isDownJetting = character.characterInputs.IsDownJetting;
        position = character.localRb.transform.position;
        velocity = character.localRb.linearVelocity;
        distanceToSurface = character.characterMovement.DistanceToSurface;
        surfacePoint = character.characterMovement.SurfacePoint;
        isGrounded = character.state.IsGrounded;
        surfaceNormal = character.characterMovement.SurfaceNormal;

        if (enableSurfaceDebug)
        {
            devVectorRenderer.AddDevVector(surfacePoint, surfaceNormal * 0.5f, new Color(0f, 1f, 0f, 0.2f), 5.0f, 0.1f);

            DebugWidgetManager.Instance.SetDebugText("Terrain Surface",
            $"Point: {surfacePoint:F2}\nNormal: {surfaceNormal:F2}\nDistance: {distanceToSurface:F2}",
            100, -200);
        }
        else
        {
            DebugWidgetManager.Instance.RemoveDebugText("Terrain Surface");
        }

        if (enableMovementDebug)
        {
            devVectorRenderer.AddDevVector(position, velocity.normalized, Color.blue, 5.0f);

            DebugWidgetManager.Instance.SetDebugText("Velocity",
            $"Speed: {velocity.magnitude:F2}\nDirection: {velocity.normalized:F1}\nDesired Direction: {movementDirection:F1}\nIs Grounded: {isGrounded}\nIs Skiing: {isSkiing}\nIs Up Jetting: {isUpJetting}\nIs Down Jetting: {isDownJetting}",
            100, -400);
        }
        else
        {
            DebugWidgetManager.Instance.RemoveDebugText("Velocity");
        }
    }
}