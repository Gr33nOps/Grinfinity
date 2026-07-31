using Godot;

/// <summary>
/// Anything the world's shots can damage.
///
/// Bodies and bosses have almost nothing else in common — a boss is not in the
/// "bodies" group, does not count toward the spawn cap, is not absorbed by a
/// nova and sheds no debris — so this is the whole of their shared surface.
/// </summary>
public interface IShootable
{
	/// <param name="impactDirection">Travel direction of whatever hit it.</param>
	/// <returns>True if this hit destroyed it.</returns>
	bool TakeDamage(int amount, Vector2 impactDirection = default);
}
