using Godot;

/// <summary>
/// One shared ShaderMaterial for the high-contrast outline, since every sprite
/// that wears it uses identical uniforms — no reason to load the shader or
/// allocate a material more than once.
/// </summary>
public static class OutlineMaterial
{
	private static ShaderMaterial material;

	public static ShaderMaterial Get()
	{
		if (material != null)
			return material;

		var shader = GD.Load<Shader>("res://scenes/outline.gdshader");
		material = new ShaderMaterial { Shader = shader };
		material.SetShaderParameter("line_color", new Color(1.0f, 1.0f, 1.0f, 1.0f));
		material.SetShaderParameter("line_thickness", 1.5f);
		return material;
	}
}
