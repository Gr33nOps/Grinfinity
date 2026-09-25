using System;
using System.IO;
using Godot;

/// <summary>Local persistence boundary. Complete writes replace the primary file atomically.</summary>
public static class SaveStore
{
    public static Error Load(ConfigFile config, string path)
    {
        Error result = config.Load(path);
        if (result == Error.Ok) return result;
        config.Clear();
        if (config.Load(path + ".bak") != Error.Ok) return result;
        // Restore the known-good primary, so the next save cannot back up damaged data.
        try { File.Copy(ProjectSettings.GlobalizePath(path + ".bak"), ProjectSettings.GlobalizePath(path), true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return Error.Ok;
    }

    public static Error Save(ConfigFile config, string path)
    {
        string target = ProjectSettings.GlobalizePath(path);
        string temporary = target + ".tmp";
        Error error = config.Save(temporary);
        if (error != Error.Ok) return error;
        try
        {
            if (File.Exists(target)) File.Replace(temporary, target, target + ".bak");
            else File.Move(temporary, target);
            return Error.Ok;
        }
        catch (IOException) { return Error.FileCantWrite; }
        catch (UnauthorizedAccessException) { return Error.FileNoPermission; }
    }

    // Reject wrong types and nonfinite numbers before Variant conversions reach gameplay.
    public static Variant Value(ConfigFile config, string section, string key, Variant fallback)
    {
        Variant value = config.GetValue(section, key, fallback);
        bool numeric = fallback.VariantType is Variant.Type.Int or Variant.Type.Float;
        if (numeric)
        {
            if (value.VariantType is not (Variant.Type.Int or Variant.Type.Float)) return fallback;
            double number = value.AsDouble();
            if (!double.IsFinite(number)) return fallback;
            if (fallback.VariantType == Variant.Type.Int && (number < int.MinValue || number > int.MaxValue)) return fallback;
        }
        else if (value.VariantType != fallback.VariantType) return fallback;
        return value;
    }
}
