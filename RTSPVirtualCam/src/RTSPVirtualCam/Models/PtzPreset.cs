namespace RTSPVirtualCam.Models;

/// <summary>
/// PTZ preset configuration
/// </summary>
public class PtzPreset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    
    public override string ToString() => $"{Id}: {Name}";
}
