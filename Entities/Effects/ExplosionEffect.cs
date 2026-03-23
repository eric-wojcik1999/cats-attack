using Godot;
using System;
using System.Threading.Tasks;
public partial class ExplosionEffect : Node3D
{
	[ExportGroup("Node Paths")]
    [Export] private NodePath _debrisPath = "Debris";
    [Export] private NodePath _firePath = "Fire";
    [Export] private NodePath _smokePath = "Smoke";
    [Export] private NodePath _explosionSoundPath = "ExplosionSound";
	private GpuParticles3D  _debris;
    private GpuParticles3D  _fire;
    private GpuParticles3D  _smoke;
    private AudioStreamPlayer3D  _explosionSound;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        _debris = GetNodeOrNull<GpuParticles3D >(_debrisPath);
        _fire = GetNodeOrNull<GpuParticles3D >(_firePath);
        _smoke = GetNodeOrNull<GpuParticles3D >(_smokePath);
        _explosionSound = GetNodeOrNull<AudioStreamPlayer3D >(_explosionSoundPath);
	}

	public async Task Explode()
	{
		_debris.Emitting = true;
        _smoke.Emitting = true;
        _fire.Emitting = true;
        _explosionSound.Play();

        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);

        QueueFree();
	}
}
