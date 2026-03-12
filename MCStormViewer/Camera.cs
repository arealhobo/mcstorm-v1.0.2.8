using System.Numerics;

namespace MCStormViewer;

public class Camera
{
    public Vector3 Position;
    public float Yaw = -90f;   // Degrees, -90 = looking along -Z initially
    public float Pitch = 0f;
    public float Speed = 20f;
    public float Sensitivity = 0.1f;

    private Vector3 _front;
    private Vector3 _right;
    private Vector3 _up;

    private static readonly Vector3 WorldUp = new(0, 1, 0);

    public Camera(Vector3 position)
    {
        Position = position;
        UpdateVectors();
    }

    public void ProcessMouseMovement(float deltaX, float deltaY)
    {
        Yaw += deltaX * Sensitivity;
        Pitch -= deltaY * Sensitivity;
        Pitch = Math.Clamp(Pitch, -89f, 89f);
        UpdateVectors();
    }

    public void ProcessMovement(bool forward, bool backward, bool left, bool right, bool up, bool down, float deltaTime)
    {
        float velocity = Speed * deltaTime;

        // Movement relative to horizontal facing direction
        var flatFront = Vector3.Normalize(new Vector3(_front.X, 0, _front.Z));
        var flatRight = _right;

        if (forward) Position += flatFront * velocity;
        if (backward) Position -= flatFront * velocity;
        if (left) Position -= flatRight * velocity;
        if (right) Position += flatRight * velocity;
        if (up) Position += WorldUp * velocity;
        if (down) Position -= WorldUp * velocity;
    }

    public void AdjustSpeed(float scrollDelta)
    {
        Speed = Math.Clamp(Speed + scrollDelta * 5f, 1f, 200f);
    }

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Position + _front, _up);
    }

    public static Matrix4x4 GetProjectionMatrix(float aspectRatio, float fov = 70f, float near = 0.1f, float far = 1000f)
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(
            fov * MathF.PI / 180f,
            aspectRatio,
            near,
            far
        );
    }

    private void UpdateVectors()
    {
        float yawRad = Yaw * MathF.PI / 180f;
        float pitchRad = Pitch * MathF.PI / 180f;

        _front = Vector3.Normalize(new Vector3(
            MathF.Cos(yawRad) * MathF.Cos(pitchRad),
            MathF.Sin(pitchRad),
            MathF.Sin(yawRad) * MathF.Cos(pitchRad)
        ));

        _right = Vector3.Normalize(Vector3.Cross(_front, WorldUp));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
    }
}
