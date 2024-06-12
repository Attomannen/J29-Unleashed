using UnityEngine;

public interface IAIFlightInputHandler
{
	float GetPitch();
	float GetYaw();
	float GetRoll();
	float GetThrottle();
	float GetWeaponSwitch();
	float GetFire();

	void SetPitch(float value);
	void SetYaw(float value);
	void SetRoll(float value);
	void SetThrottle(float value);
	void SetWeaponSwitch(float value);
	void SetFire(float value);
}

public class AIInputHandler : MonoBehaviour, IAIFlightInputHandler
{
	private AIController aiController;

	public float Pitch { get; private set; }
	public float Yaw { get; private set; }
	public float Roll { get; private set; }
	public float Throttle { get; private set; }
	public float WeaponSwitch { get; private set; }
	public float Fire { get; private set; }

	public AIInputHandler(AIController aiController)
	{
		this.aiController = aiController;
	}

	public float GetPitch()        => Pitch;
	public float GetYaw()          => Yaw;
	public float GetRoll()         => Roll;
	public float GetThrottle()     => Throttle;
	public float GetWeaponSwitch() => WeaponSwitch;
	public float GetFire()         => Fire;

	public void SetPitch(float value) { Pitch = value; }
	public void SetYaw(float value) { Yaw = value; }
	public void SetRoll(float value) { Roll = value; }
	public void SetThrottle(float value) { Throttle = value; }
	public void SetWeaponSwitch(float value) { WeaponSwitch = value; }
	public void SetFire(float value) { Fire = value; }
}
