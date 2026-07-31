/// <summary>
/// What the world is carrying into the next orbit.
///
/// Static so it survives the scene change from the select screen into the game,
/// and mirrored into settings.cfg so a restart — the "one more orbit" pillar —
/// keeps the weapon without asking again.
/// </summary>
public static class Loadout
{
	private static WeaponId weapon = WeaponId.Comet;

	public static WeaponId Weapon
	{
		get => weapon;
		set
		{
			weapon = value;
			GameSettings.Instance?.SetWeapon(value);
		}
	}

	/// <summary>Restores the last choice without writing it back out again.</summary>
	public static void Restore(WeaponId saved)
	{
		weapon = saved;
	}

	public static WeaponProfile Profile => WeaponProfile.Get(weapon);
}
