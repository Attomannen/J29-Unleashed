using UnityEngine.InputSystem;
using UnityEngine;
using static WeaponController;

public interface IPlayerInputHandler
{
	float GetPitch();
	float GetYaw();
	float GetRoll();
	float GetFire(); 
	float GetAim();

	float GetThrottle();
	float GetWeaponSwitch();
	float GetInputSwitch();
}

public class InputHandler : MonoBehaviour, IPlayerInputHandler
{
	static InputHandler _instance;
	public static InputHandler Instance { get { return _instance; } }

	private float pitch        = 0.0f;
	private float yaw          = 0.0f;
	private float roll         = 0.0f;
	private float fire         = 0.0f;
	private float aim          = 0.0f;
	private float throttle     = 0.0f;
	private float weaponSwitch = 0.0f;
	private float inputSwitch  = 0.0f;

	private void Awake()
	{
		_instance = this;
	}

	public void OnFire(InputAction.CallbackContext context)
	{
		fire = context.ReadValue<float>();
	}

	public void OnYaw(InputAction.CallbackContext context)
	{
		yaw = context.ReadValue<float>();
	}

	public void OnPitch(InputAction.CallbackContext context)
	{
		pitch = context.ReadValue<float>();
	}

	public void OnRoll(InputAction.CallbackContext context)
	{
		roll = context.ReadValue<float>();
	}

	public void OnThrottle(InputAction.CallbackContext context)
	{
		throttle = context.ReadValue<float>();
	}

	public void OnSwitchInput(InputAction.CallbackContext context)
	{
		inputSwitch = context.ReadValue<float>();
	}

	public void OnWeaponSwitch(InputAction.CallbackContext context)
	{
		if (context.performed)
		{
			GetComponent<WeaponController>().activeWeapon = GetComponent<WeaponController>().activeWeapon switch
			{
				Weapons.Cannon => Weapons.Missile,
				Weapons.Missile => Weapons.Bomb,
				Weapons.Bomb => Weapons.Cannon,
				_ => Weapons.Cannon,
			};
		}
	}
	public void OnAim(InputAction.CallbackContext context)
	{
		aim = context.ReadValue<float>();
	}


	public float GetPitch() { return pitch; }
	public float GetYaw() { return yaw; }
	public float GetAim() { return aim; }
	public float GetRoll() { return roll; }
	public float GetFire() { return fire; }
	public float GetThrottle() { return throttle; }
	public float GetWeaponSwitch() { return weaponSwitch; }
	public float GetInputSwitch() { return inputSwitch; }
}
