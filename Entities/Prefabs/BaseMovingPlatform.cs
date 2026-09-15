using Godot;
using System;

[Tool]
public partial class BaseMovingPlatform : Node3D
{
	public enum EaseMode
	{
		Linear,
		SmoothStep
	}

	[Export]
	public NodePath BodyPath { get; set; } = "Body";
	[Export]
	public NodePath EndPointPath  { get; set; } = "Endpoint";

	[Export(PropertyHint.Range, "0.05,60,0.05")]
	public float TravelTimeSeconds { get; set; } = 2f;

	[Export(PropertyHint.Range,"0,10,0.5")]
	public float WaitAtStartSeconds { get; set; } = 0.25f;

	[Export]
	public float WaitAtEndSeconds { get; set; } = 0.25f;

	[Export]
	public EaseMode Easing { get; set; } = EaseMode.SmoothStep;

	[Export]
	public bool StartAtEnd { get; set; } = false;

	[Export(PropertyHint.Range,("0.1,1,0.01"))]
	public float EditorPreview { get; set; } = 0f;

	private AnimatableBody3D _body;
	private Marker3D _endPoint;

	private float _progress;
	private int _direction = 1;
	private float _waitTimer = 0f;

	public override void _Ready()
	{
		_body = GetNodeOrNull<AnimatableBody3D>(BodyPath);
		_endPoint = GetNodeOrNull<Marker3D>(EndPointPath);

		_progress = StartAtEnd ? 1f : 0f;
		_direction = StartAtEnd ? -1 : 1;

		UpdateBodyPosition(_progress);
	}
    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint())
        {
            return;
        }

		_body = GetNodeOrNull<AnimatableBody3D>(BodyPath);
		_endPoint = GetNodeOrNull<Marker3D>(EndPointPath);
        UpdateBodyPosition(EditorPreview);
    }


	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		if (_body == null || _endPoint == null)
		{
			return;
		}

		float dt = (float)delta;

		float safeTravelTime = Mathf.Max(TravelTimeSeconds, 0.05f);
		_progress = _direction * dt / safeTravelTime;

		if (_progress >= 1f)
		{
			_progress = 1f;
			_direction = -1;
			_waitTimer = WaitAtEndSeconds;

		} 
		else if (_progress <= 0f) 
		{
			_progress = 0f;
			_direction = 1;
			_waitTimer = WaitAtStartSeconds;
		}

		UpdateBodyPosition(_progress);
	}

	private void UpdateBodyPosition(float rawProgress)
	{
		if (_body == null || _endPoint == null)
		{
			return;
		}

		float progress = ApplyEase(rawProgress);

		Vector3 start = Vector3.Zero;
		Vector3 end = _endPoint.Position;

		_body.Position = start.Lerp(end, progress);
	}

	private float ApplyEase(float t)
	{
		t = Mathf.Clamp(t, 0f, 1f);

		// Look at the value of Easing via switch statement
		// If Easing is SmoothStep, return the smoothed value.
		// Otherwise, return t unchanged.
		return Easing switch
		{
			EaseMode.SmoothStep => t * t * (3f - 3f * t),
			_ => t
		};
	}
}
