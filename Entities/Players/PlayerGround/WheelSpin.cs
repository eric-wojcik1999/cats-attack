using Godot;

public partial class WheelSpin : Node3D
{
    [ExportGroup("Wheel Nodes")]
    [Export] private Node3D _frontRightWheel;
    [Export] private Node3D _frontLeftWheel;
    [Export] private Node3D _backRightWheel;
    [Export] private Node3D _backLeftWheel;

    [ExportGroup("Spin Settings")]
    [Export] private Vector3 _localSpinAxis = Vector3.Right;

    [Export(PropertyHint.Range, "0.01, 10.0, 0.01")]
    private float _wheelRadius = 0.35f;

    [Export] private bool _reverseAllWheels = false;

    [Export] private bool _reverseLeftWheels = false;

    private PlayerGround  _playerBody;

    public override void _Ready() 
    {
        FindPlayerBody();

        if ( _localSpinAxis.LengthSquared() < 0.0001f)
        {
            GD.PushError("[WheelSpin] Local Spin Axis cannot be zero.");
            SetPhysicsProcess(false);
        }

        if (_playerBody == null) 
        {
            GD.PushError("[WheelSpin] Could not find a CharacterBody3D ancestor.");

            SetPhysicsProcess(false);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!GodotObject.IsInstanceValid(_playerBody))
        {
            return;
        }

        float spinRadians = CalculateSpinRadians((float)delta);

        if (_reverseAllWheels)
        {
            spinRadians = -spinRadians;
        }

        RotateWheel(_frontRightWheel, spinRadians);
        RotateWheel(_backRightWheel, spinRadians);

        float leftSpinRadians = _reverseLeftWheels ? -spinRadians : spinRadians;

        RotateWheel(_frontLeftWheel, leftSpinRadians);
        RotateWheel(_backLeftWheel, leftSpinRadians);
    }

    private float CalculateSpinRadians(float delta)
    {
        if (!GodotObject.IsInstanceValid(_playerBody))
        {
            return 0.0f;
        }

        Vector3 horizontalVelocity = new Vector3(_playerBody.Velocity.X, 0.0f, _playerBody.Velocity.Z);

        // Speed specifically along the player's forward direction.
        float forwardSpeed = Mathf.Abs(horizontalVelocity.Dot(_playerBody.ForwardDir));

        // Includes forward and lateral movement.
        float totalHorizontalSpeed = horizontalVelocity.Length();

        // Ensure normal auto-forward always provides a baseline.
        float intendedAutoForwardSpeed = _playerBody.CurrentAutoForwardSpeed;

        forwardSpeed = Mathf.Max(forwardSpeed, intendedAutoForwardSpeed);

        // Blend between forward-only movement and complete horizontal movement.
        float calculatedMovementSpeed = Mathf.Lerp(forwardSpeed, totalHorizontalSpeed, 0.4f);

        // Increase the result for better-looking visual rotation.
        float visualMovementSpeed = calculatedMovementSpeed * 2.5f;

        float safeRadius = Mathf.Max(_wheelRadius, 0.01f);

        return (visualMovementSpeed / safeRadius) * delta;
    }

    private void RotateWheel(Node3D wheel, float radians)
    {
        if (!GodotObject.IsInstanceValid(wheel))
        {
            return;
        }

        wheel.RotateObjectLocal(_localSpinAxis.Normalized(), radians);
    }

    private void FindPlayerBody()
    {
        Node currentNode = this;

        while (currentNode != null)
        {
            if (currentNode is PlayerGround  player)
            {
                _playerBody = player;
                return;
            }

            currentNode = currentNode.GetParent();
        }
    }
}