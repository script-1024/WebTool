using Windows.Foundation;
using System.Runtime.InteropServices;

namespace Windows.Win32.Foundation;

[StructLayout(LayoutKind.Sequential)]
public struct POINT(int x, int y)
{
    public int X = x;

    public int Y = y;

    public static implicit operator Point(POINT p) => new(p.X, p.Y);

    public static implicit operator POINT(Point p) => new((int)p.X, (int)p.Y);

    public override string ToString() => $"X: {X}, Y: {Y}";
}
