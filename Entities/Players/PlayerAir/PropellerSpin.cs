using Godot;

public partial class PropellerSpin : Node3D
{
    [Export]
    private Node3D _propeller;

    [Export]
    private Vector3 _localSpinAxis = Vector3.Forward;

    [Export(PropertyHint.Range, "-3600.0,3600.0,10.0")]
    private float _degreesPerSecond = 900.0f;

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_propeller))
        {
            return;
        }

        float radians = Mathf.DegToRad(_degreesPerSecond) * (float)delta;

        _propeller.RotateObjectLocal(_localSpinAxis.Normalized(), radians);
    }
}