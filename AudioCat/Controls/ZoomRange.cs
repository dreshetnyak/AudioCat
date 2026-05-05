namespace AudioCat.Controls;

public readonly struct ZoomRange(int start, int end) : IEquatable<ZoomRange>
{
    public int Start { get; } = start;
    public int End   { get; } = end;

    /// <summary>
    /// The sentinel value {0, 0} instructs the control to auto-follow Capacity.
    /// </summary>
    public bool IsSentinel => Start == 0 && End == 0;

    public static ZoomRange Sentinel { get; } = new(0, 0);

    public bool Equals(ZoomRange other) => Start == other.Start && End == other.End;
    public override bool Equals(object? obj) => obj is ZoomRange other && Equals(other);
    public override int  GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(ZoomRange left, ZoomRange right) =>  left.Equals(right);
    public static bool operator !=(ZoomRange left, ZoomRange right) => !left.Equals(right);

    public override string ToString() => $"[{Start}, {End}]";
}