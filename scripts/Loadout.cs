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
	/// picker. The picker is gone and the numbers are pinned to what Normal
	/// used to be; the escalation curve replaces this with a function of
	/// elapsed run time, at which point the hooks in Body and BodySpawner stay
	/// exactly where they are and only this source changes.
	/// </summary>
	public static Difficulties.Profile DifficultyProfile => Difficulties.Baseline;
}
