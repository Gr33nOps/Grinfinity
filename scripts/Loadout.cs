/// <summary>
/// What the world is carrying into the next orbit.
///
/// There is one mode and one curve now, so this no longer holds a player's
/// pre-run choices — there are none left to make. What remains is the starting
/// weapon, kept static so it survives the scene change into the game and
/// mirrored into settings.cfg so a restart keeps it.
///
/// The weapon is expected to stop living here once in-run weapon progression
/// lands: a run's identity is meant to come from what it grew into, not from
/// what it started as.
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

	/// <summary>Restores the last weapon without writing it back out again.</summary>
	public static void Restore(WeaponId saved)
	{
		weapon = saved;
	}

	public static WeaponProfile Profile => WeaponProfile.Get(weapon);

	/// <summary>
	/// The body-scaling knobs that used to come from the Easy/Normal/Hard
	/// picker. Now a function of how long the current orbit has lasted, read at
	/// the moment a body is born. Falls back to neutral outside a run, so a body
	/// spawned by a tool or a test has nothing to trip over.
	/// </summary>
	public static Difficulties.Profile DifficultyProfile =>
		Difficulties.At(RunState.ElapsedSeconds);
}
