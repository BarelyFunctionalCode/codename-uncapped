using UnityEngine;

public class CharacterTelemetry
{
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

    public CharacterTelemetry(DevVectorRenderer devVectorRenderer)
    {
        this.devVectorRenderer = devVectorRenderer;
        this.position = Vector3.zero;
        this.velocity = Vector3.zero;
        this.movementDirection = Vector3.zero;
        this.surfaceNormal = Vector3.zero;
        this.surfacePoint = Vector3.zero;
        this.distanceToSurface = 0.0f;
        this.isSkiing = false;
        this.isUpJetting = false;
        this.isDownJetting = false;
        this.isGrounded = false;
    }

    public void Update()
    {
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