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
	private static int world = 1;

	public static WeaponId Weapon
	{
		get => weapon;
		set
		{
			weapon = value;
			GameSettings.Instance?.SetWeapon(value);
		}
	}

	/// <summary>The chosen world's id (1..12). Falls back to World 1 if the saved choice is no longer unlocked.</summary>
	public static int World
	{
		get => world;
		set
		{
			world = PlayerProfile.IsWorldUnlocked(value) ? value : 1;
			GameSettings.Instance?.SetWorld(world);
		}
	}

	/// <summary>Restores the last weapon without writing it back out again.</summary>
	public static void Restore(WeaponId saved)
	{
		weapon = saved;
	}

	/// <summary>Restores the last world without writing it back out again.</summary>
	public static void RestoreWorld(int saved)
	{
		world = PlayerProfile.IsWorldUnlocked(saved) ? saved : 1;
	}

	public static WeaponProfile Profile => WeaponProfile.Get(weapon);
	public static Worlds.Profile WorldProfile => Worlds.Get(world);
}
