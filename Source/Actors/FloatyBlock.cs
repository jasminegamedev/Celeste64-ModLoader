namespace Celeste64;

public sealed class FloatyBlock : Solid
{
	public Vec3 Origin;
	public Vec3 Offset;
	public float Friction = 300;
	public float FrictionThreshold = 50;
	public float Frequency = 1.2f;
	public float Halflife = 3.0f;
	public Player? PlayerRiderLocal;

	public override void Added()
	{
		base.Added();
		Origin = Position;
		Offset = Vec3.Zero;
	}

	public override void Update()
	{
		base.Update();

		if (PlayerRiderLocal is null && HasPlayerRider())
		{
			PlayerRiderLocal = GetPlayerRider();
			Velocity += GetPlayerRider()!.PreviousVelocity * .8f;
		}
		else if (PlayerRiderLocal is not null && !HasPlayerRider())
		{
			if (PlayerRiderLocal is { } player)
				Velocity -= player.Velocity * .8f;
			PlayerRiderLocal = null;
		}

		// friction
		Friction = 200;
		FrictionThreshold = 1;
		if (Friction > 0 && Velocity.LengthSquared() > FrictionThreshold.Squared())
			Velocity = Utils.Approach(Velocity, Velocity.Normalized() * FrictionThreshold, Friction * Time.Delta);

		// spring!
		var diff = Position - (Origin + Offset);
		var normal = diff.Normalized();
		float vel = Vec3.Dot(Velocity, normal);
		float old_vel = vel;
		vel = SpringPhysics.Calculate(diff.Length(), vel, 0, 0, Frequency, Halflife);
		Velocity += normal * (vel - old_vel);
	}
}
