/// <summary>
/// The bestiary.
///
/// These are the names the design documents use — standing rule 8: nothing gets
/// a name that could belong to another genre. Drifter, Shard and Planetoid were
/// previously Chaser, Swarmer and Tank.
/// </summary>
public enum BodyKind
{
	/// <summary>Baseline. Falls straight in. Teaches nothing but the loop.</summary>
	Drifter,

	/// <summary>Fast, arrives in packs, dies to one hit. Teaches crowd control.</summary>
	Shard,

	/// <summary>Slow, four hits, huge contact radius. Teaches target priority.</summary>
	Planetoid,

	/// <summary>Dies into three smaller bodies. Teaches not to kill it point-blank.</summary>
	Fracture,

	/// <summary>What a Fracture leaves behind.</summary>
	Splinter,

	/// <summary>Holds a fixed orbit radius and fires inward. Punishes lazy aim.</summary>
	Satellite,

	/// <summary>Detonates in a radius on death. Teaches spacing.</summary>
	Flare,

	/// <summary>Armoured front arc. Teaches flanking.</summary>
	Bulwark
}
