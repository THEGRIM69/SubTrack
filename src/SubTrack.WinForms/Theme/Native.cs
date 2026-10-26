using System.Runtime.InteropServices;

namespace SubTrack.WinForms.Theme;

internal static class Native
{
    private const int EmSetCueBanner = 0x1501;
    private const int WmNcLButtonDown = 0xA1;
    private const int HtCaption = 0x2;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, string lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    public static void Cue(TextBox box, string text)
    {
        _ = box.Handle;
        _ = SendMessage(box.Handle, EmSetCueBanner, (IntPtr)1, text);
    }

    public static void Drag(Control control)
    {
        control.MouseDown += (_, args) =>
        {
            if (args.Button != MouseButtons.Left)
            {
                return;
            }

            var form = control.FindForm();

            if (form is null)
            {
                return;
            }

            _ = ReleaseCapture();
            _ = SendMessage(form.Handle, WmNcLButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
        };
    }
}
