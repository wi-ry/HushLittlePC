using System.Runtime.InteropServices;

namespace HushLittlePC;

/// <summary>
/// A native Win32 popup menu. Because Windows draws it (not WinForms), it automatically
/// gets the Windows 11 look: rounded corners, modern spacing, and light/dark theming.
/// </summary>
sealed class NativeMenu
{
    const uint MF_STRING = 0x0000, MF_CHECKED = 0x0008, MF_SEPARATOR = 0x0800;
    const uint TPM_RIGHTBUTTON = 0x0002, TPM_NONOTIFY = 0x0080, TPM_BOTTOMALIGN = 0x0020, TPM_RETURNCMD = 0x0100;
    const uint WM_NULL = 0;

    static NativeWindow? s_owner;
    readonly List<(int Id, string Text, bool Checked)?> _items = new();

    public void Add(int id, string text, bool isChecked = false) => _items.Add((id, text, isChecked));
    public void AddSeparator() => _items.Add(null);

    /// <summary>Shows the menu at the cursor and returns the chosen item id (0 if dismissed).</summary>
    public int Show()
    {
        if (s_owner == null)
        {
            s_owner = new NativeWindow();
            s_owner.CreateHandle(new CreateParams());   // invisible owner window for the menu
        }

        IntPtr hMenu = CreatePopupMenu();
        try
        {
            foreach (var item in _items)
            {
                if (item is { } i)
                    AppendMenu(hMenu, MF_STRING | (i.Checked ? MF_CHECKED : 0), (nuint)i.Id, i.Text);
                else
                    AppendMenu(hMenu, MF_SEPARATOR, 0, null);
            }

            GetCursorPos(out var pt);
            SetForegroundWindow(s_owner.Handle);        // required so the menu closes when you click away
            int cmd = TrackPopupMenuEx(hMenu,
                TPM_RIGHTBUTTON | TPM_BOTTOMALIGN | TPM_RETURNCMD | TPM_NONOTIFY,
                pt.X, pt.Y, s_owner.Handle, IntPtr.Zero);
            PostMessage(s_owner.Handle, WM_NULL, IntPtr.Zero, IntPtr.Zero);
            return cmd;
        }
        finally
        {
            DestroyMenu(hMenu);
        }
    }

    /// <summary>Lets native menus follow the system light/dark setting (undocumented but widely used uxtheme calls).</summary>
    public static void FollowSystemTheme()
    {
        try { SetPreferredAppMode(1 /* AllowDark */); FlushMenuThemes(); }
        catch { /* older Windows: menus just stay light */ }
    }

    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }

    [DllImport("user32.dll")] static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll")] static extern bool DestroyMenu(IntPtr hMenu);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool AppendMenu(IntPtr hMenu, uint flags, nuint idNewItem, string? newItem);
    [DllImport("user32.dll")] static extern int TrackPopupMenuEx(IntPtr hMenu, uint flags, int x, int y, IntPtr hwnd, IntPtr tpm);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT pt);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("uxtheme.dll", EntryPoint = "#135")] static extern int SetPreferredAppMode(int mode);
    [DllImport("uxtheme.dll", EntryPoint = "#136")] static extern void FlushMenuThemes();
}
