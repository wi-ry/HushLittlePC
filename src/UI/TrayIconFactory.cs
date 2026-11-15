using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace HushLittlePC;

static class TrayIconFactory
{
    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr handle);

    public static Icon MakeIcon(bool enabled)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var color = enabled ? Color.FromArgb(46, 160, 67) : Color.FromArgb(140, 140, 140);
            using var brush = new SolidBrush(color);
            using var pen = new Pen(color, 3f);

            graphics.FillPolygon(brush, new[]
            {
                new Point(3, 12), new Point(10, 12), new Point(18, 5),
                new Point(18, 27), new Point(10, 20), new Point(3, 20)
            });

            if (enabled)
            {
                graphics.DrawArc(pen, 14, 9, 10, 14, -60, 120);
                graphics.DrawArc(pen, 12, 4, 18, 24, -60, 120);
            }
            else
            {
                using var red = new Pen(Color.FromArgb(200, 50, 50), 3.5f);
                graphics.DrawLine(red, 22, 11, 30, 21);
                graphics.DrawLine(red, 30, 11, 22, 21);
            }
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
