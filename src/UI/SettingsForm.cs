namespace HushLittlePC;

using System.Reflection;
using System.Drawing.Drawing2D;

class SettingsForm : Form
{
    static bool _darkTheme = ReadSystemTheme();
    static Color Accent => Color.FromArgb(0, 103, 192);
    static Color Surface => _darkTheme ? Color.FromArgb(38, 38, 38) : Color.White;
    static Color Page => _darkTheme ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 247, 251);
    static Color Muted => _darkTheme ? Color.FromArgb(190, 190, 190) : Color.FromArgb(75, 85, 105);
    static Color PrimaryText => _darkTheme ? Color.FromArgb(245, 245, 245) : Color.FromArgb(20, 30, 48);
    static Color SecondaryText => _darkTheme ? Color.FromArgb(205, 205, 205) : Color.FromArgb(50, 62, 86);
    static Color Sidebar => _darkTheme ? Color.FromArgb(35, 35, 35) : Color.FromArgb(239, 244, 252);
    static Color Border => _darkTheme ? Color.FromArgb(65, 65, 65) : Color.FromArgb(224, 230, 240);
    static Color Selected => _darkTheme ? Color.FromArgb(55, 75, 100) : Color.FromArgb(216, 231, 252);
    static Color IconSurface => _darkTheme ? Color.FromArgb(48, 58, 70) : Color.FromArgb(234, 243, 253);
    internal static Color ToggleOff => _darkTheme ? Color.FromArgb(100, 100, 100) : Color.FromArgb(190, 197, 207);

    readonly Settings _settings;
    readonly Action _settingsChanged;
    readonly Action _exit;
    readonly Panel _content;
    readonly Panel _sidebar;
    readonly Panel _shell;
    readonly Panel _main;
    readonly Button _settingsNav;
    readonly Button _aboutNav;

    public SettingsForm(Settings settings, Action settingsChanged, Action exit)
    {
        _settings = settings;
        _settingsChanged = settingsChanged;
        _exit = exit;

        Text = "Hush Little PC";
        Icon = TrayApp.MakeIcon(true);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 650);
        Size = new Size(1120, 760);
        Font = new Font("Segoe UI", 10F);
        BackColor = Page;
        AutoScaleMode = AutoScaleMode.Dpi;

        _shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Page, Padding = new Padding(0) };
        _shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 236));
        _shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(_shell);

        _sidebar = new Panel { Dock = DockStyle.Fill, BackColor = Sidebar, Padding = new Padding(18, 24, 14, 18) };
        var sidebar = _sidebar;
        _shell.Controls.Add(sidebar, 0, 0);

        var brand = new Label { Text = "◔  Hush Little PC", Font = new Font("Segoe UI Semibold", 15F), ForeColor = PrimaryText, AutoSize = true, Location = new Point(10, 8) };
        sidebar.Controls.Add(brand);
        var tagline = new Label { Text = "Keep the peace. Automatically.", Font = new Font("Segoe UI", 9.5F), ForeColor = Muted, AutoSize = true, Location = new Point(10, 48) };
        sidebar.Controls.Add(tagline);

        _settingsNav = NavButton("⚙   Settings", true);
        _settingsNav.Location = new Point(0, 118);
        _settingsNav.Click += (_, _) => ShowSettingsPage();
        sidebar.Controls.Add(_settingsNav);

        _aboutNav = NavButton("ⓘ   About", false);
        _aboutNav.Location = new Point(0, 166);
        _aboutNav.Click += (_, _) => ShowAboutPage();
        sidebar.Controls.Add(_aboutNav);

        var exitButton = NavButton("⏻   Exit", false);
        exitButton.Location = new Point(0, 214);
        exitButton.Click += (_, _) => _exit();
        sidebar.Controls.Add(exitButton);

        var statusLine = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = Color.Transparent };
        var statusDot = new Label { Text = "●", ForeColor = Color.FromArgb(34, 197, 94), AutoSize = true, Location = new Point(2, 11) };
        var status = new Label { Text = "Monitoring audio devices", ForeColor = PrimaryText, AutoSize = true, Location = new Point(24, 10), Font = new Font("Segoe UI", 9.5F) };
        statusLine.Controls.Add(statusDot);
        statusLine.Controls.Add(status);
        sidebar.Controls.Add(statusLine);

        _main = new Panel { Dock = DockStyle.Fill, BackColor = Page, Padding = new Padding(28, 24, 28, 20) };
        var main = _main;
        _shell.Controls.Add(main, 1, 0);
        _content = new Panel { Dock = DockStyle.Fill, BackColor = Page, AutoScroll = true };
        main.Controls.Add(_content);

        ShowSettingsPage();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ShowSettingsPage();
    }

    static bool ReadSystemTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    protected override void WndProc(ref Message message)
    {
        const int WmSettingChange = 0x001A;
        base.WndProc(ref message);
        if (message.Msg == WmSettingChange && ReadSystemTheme() != _darkTheme)
        {
            bool settingsPage = _aboutNav.BackColor == Color.Transparent;
            _darkTheme = ReadSystemTheme();
            BackColor = Page;
            _shell.BackColor = Page;
            _sidebar.BackColor = Sidebar;
            _main.BackColor = Page;
            _content.BackColor = Page;
            ShowSettingsPage();
        }
    }

    void ShowSettingsPage()
    {
        SelectNavigation(true);
        _content.SuspendLayout();
        _content.Controls.Clear();

        var page = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Page,
            Padding = new Padding(0, 0, 0, 8), Margin = new Padding(0)
        };
        page.Size = new Size(Math.Max(620, _content.ClientSize.Width - 24), 10);
        _content.Controls.Add(page);

        page.Controls.Add(PageHeading("Settings", "Configure how Hush Little PC manages your audio.", page.Width - 4));

        AddSwitchCard(page, "Enable Hush Little PC", "Automatically mute your audio when you're idle.", _settings.Enabled, value => { _settings.Enabled = value; Changed(); });
        AddIdleCard(page);
        AddSwitchCard(page, "Unmute on activity", "Unmute when you use your keyboard or mouse.", _settings.UnmuteOnActivity, value => { _settings.UnmuteOnActivity = value; Changed(); });
        AddSwitchCard(page, "Stay unmuted while audio is playing", "Avoid muting while music, videos, or other audio are playing.", _settings.SkipWhileAudioPlaying, value => { _settings.SkipWhileAudioPlaying = value; Changed(); });
        AddSwitchCard(page, "Mute on output change", "Mute when the default audio device changes.", _settings.MuteOnDeviceChange, value => { _settings.MuteOnDeviceChange = value; Changed(); });
        AddSwitchCard(page, "Notifications", "Show a notification when audio is muted.", _settings.ShowNotification, value => { _settings.ShowNotification = value; Changed(); });
        AddSwitchCard(page, "Run at startup", "Start Hush Little PC when you sign in to Windows.", _settings.RunOnStartup, value => { _settings.RunOnStartup = value; StartupManager.Set(value); Changed(); });

        var footer = new Panel { Width = page.Width - 4, Height = 54, BackColor = Page, Margin = new Padding(0, 4, 0, 0) };
        var rule = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Border };
        footer.Controls.Add(rule);
        var info = new Label { Text = "ⓘ   Changes are applied automatically.", AutoSize = true, ForeColor = PrimaryText, Location = new Point(10, 19) };
        footer.Controls.Add(info);
        var learn = new LinkLabel { Text = "Learn more", AutoSize = true, LinkColor = Accent, Location = new Point(Math.Max(footer.Width - 100, 20), 19), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        learn.LinkClicked += (_, _) => ShowAboutPage();
        footer.Controls.Add(learn);
        page.Controls.Add(footer);

        _content.ResumeLayout(true);
    }

    void AddIdleCard(FlowLayoutPanel page)
    {
        var card = NewCard(page.Width - 4, 104);
        var icon = IconBubble("◷");
        icon.Location = new Point(16, 22);
        card.Controls.Add(icon);
        AddCardText(card, "Idle time", "Mute after a period of inactivity.", 110, 24);

        bool synchronizingIdleControls = false;
        var minutes = new NumericUpDown
        {
            Minimum = Settings.MinIdleMinutes, Maximum = Settings.MaxIdleMinutes,
            Value = _settings.IdleMinutes, Width = 68, Height = 34,
            AccessibleName = "Idle time in minutes",
            Font = new Font("Segoe UI", 11F), TextAlign = HorizontalAlignment.Center,
            Location = new Point(card.Width - 132, 20), Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        var unit = new Label { Text = "min", AutoSize = true, ForeColor = PrimaryText, Location = new Point(card.Width - 56, 27), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        var slider = new TrackBar { Minimum = Settings.MinIdleMinutes, Maximum = Settings.MaxIdleMinutes, TickStyle = TickStyle.None, Value = _settings.IdleMinutes, Width = 290, Height = 42, Location = new Point(card.Width - 430, 12), Anchor = AnchorStyles.Top | AnchorStyles.Right, AccessibleName = "Idle time slider" };
        var range = new Label { Text = "1 – 1,440 minutes", AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI", 9F), Location = new Point(slider.Left, 59), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        slider.Scroll += (_, _) =>
        {
            if (synchronizingIdleControls) return;
            synchronizingIdleControls = true;
            minutes.Value = slider.Value;
            synchronizingIdleControls = false;
            _settings.IdleMinutes = slider.Value;
            Changed();
        };
        minutes.ValueChanged += (_, _) =>
        {
            if (synchronizingIdleControls) return;
            synchronizingIdleControls = true;
            slider.Value = (int)minutes.Value;
            synchronizingIdleControls = false;
            _settings.IdleMinutes = (int)minutes.Value;
            Changed();
        };
        card.Controls.Add(slider);
        card.Controls.Add(minutes);
        card.Controls.Add(unit);
        card.Controls.Add(range);
        page.Controls.Add(card);
    }

    void AddSwitchCard(FlowLayoutPanel page, string title, string description, bool initial, Action<bool> changed)
    {
        var card = NewCard(page.Width - 4, 82);
        var iconText = title switch
        {
            "Enable Hush Little PC" => "◖",
            "Unmute on activity" => "↗",
            "Stay unmuted while audio is playing" => "♫",
            "Mute on output change" => "↻",
            "Notifications" => "♧",
            "Run at startup" => "⊞",
            _ => "•"
        };
        var icon = IconBubble(iconText);
        icon.Location = new Point(16, 15);
        card.Controls.Add(icon);
        AddCardText(card, title, description, 110, 20);

        var toggle = new SwitchControl { Checked = initial, AccessibleName = title, Location = new Point(card.Width - 112, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        var state = new Label { Text = initial ? "On" : "Off", AutoSize = true, ForeColor = PrimaryText, Font = new Font("Segoe UI", 10F), Location = new Point(card.Width - 52, 27), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        toggle.CheckedChanged += (_, _) => { state.Text = toggle.Checked ? "On" : "Off"; changed(toggle.Checked); };
        card.Controls.Add(toggle);
        card.Controls.Add(state);
        page.Controls.Add(card);
    }

    void ShowAboutPage()
    {
        SelectNavigation(false);
        _content.Controls.Clear();
        var page = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Page, Padding = new Padding(0) };
        _content.Controls.Add(page);
        page.Controls.Add(PageHeading("About", "A quieter PC, without the extra effort.", Math.Max(620, _content.ClientSize.Width - 24)));
        var card = NewCard(Math.Max(620, _content.ClientSize.Width - 24), 190);
        var logo = IconBubble("◔");
        logo.Size = new Size(72, 72);
        logo.Font = new Font("Segoe UI", 30F, FontStyle.Bold);
        logo.Location = new Point(22, 24);
        card.Controls.Add(logo);
        var name = new Label { Text = "Hush Little PC", Font = new Font("Segoe UI Semibold", 18F), AutoSize = true, Location = new Point(112, 28), ForeColor = PrimaryText };
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Development";
        var details = new Label { Text = $"Version {version}\nAutomatically manages audio muting based on your activity.", Font = new Font("Segoe UI", 10F), AutoSize = true, Location = new Point(114, 64), ForeColor = Muted };
        var repo = new LinkLabel { Text = "View project on GitHub", AutoSize = true, Location = new Point(114, 128), LinkColor = Accent };
        repo.LinkClicked += (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/wi-ry/HushLittlePC") { UseShellExecute = true });
        card.Controls.Add(name); card.Controls.Add(details); card.Controls.Add(repo);
        page.Controls.Add(card);
    }

    void Changed()
    {
        SettingsStore.Save(_settings);
        _settingsChanged();
    }

    void SelectNavigation(bool settings)
    {
        _settingsNav.BackColor = settings ? Selected : Color.Transparent;
        _aboutNav.BackColor = settings ? Color.Transparent : Selected;
        _settingsNav.ForeColor = PrimaryText;
        _aboutNav.ForeColor = PrimaryText;
        _settingsNav.ForeColor = PrimaryText;
        _aboutNav.ForeColor = PrimaryText;
        _settingsNav.Font = new Font("Segoe UI", 10F, settings ? FontStyle.Bold : FontStyle.Regular);
        _aboutNav.Font = new Font("Segoe UI", 10F, settings ? FontStyle.Regular : FontStyle.Bold);
    }

    static Button NavButton(string text, bool selected) => new()
    {
        Text = text, Size = new Size(190, 42), FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 0 }, TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(12, 0, 0, 0), Font = new Font("Segoe UI", 10F),
        ForeColor = PrimaryText, BackColor = selected ? Selected : Color.Transparent,
        Cursor = Cursors.Hand
    };

    static Control PageHeading(string title, string subtitle, int width)
    {
        // A two-line heading is hosted by a fixed-height panel so the page can flow naturally.
        var host = new Panel { Width = width, Height = 88, BackColor = Page, Margin = new Padding(0, 0, 0, 12) };
        host.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI Semibold", 25F), AutoSize = true, ForeColor = PrimaryText, Location = new Point(0, 0) });
        host.Controls.Add(new Label { Text = subtitle, Font = new Font("Segoe UI", 11F), AutoSize = true, ForeColor = Muted, Location = new Point(2, 48) });
        return host;
    }

    static Panel NewCard(int width, int height)
    {
        var card = new Panel { Width = width, Height = height, BackColor = Surface, Margin = new Padding(0, 0, 0, 8), Padding = new Padding(0) };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Border);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
        };
        return card;
    }

    static Label IconBubble(string glyph)
    {
        var label = new Label { Text = glyph, Size = new Size(58, 58), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Symbol", 22F, FontStyle.Bold), ForeColor = Accent, BackColor = IconSurface };
        label.Paint += (_, e) => { using var brush = new SolidBrush(label.BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.FillEllipse(brush, 0, 0, label.Width - 1, label.Height - 1); TextRenderer.DrawText(e.Graphics, label.Text, label.Font, label.ClientRectangle, label.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); };
        return label;
    }

    static void AddCardText(Control parent, string title, string description, int left, int top)
    {
        parent.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 11F), ForeColor = PrimaryText, Location = new Point(left, top) });
        parent.Controls.Add(new Label { Text = description, AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = SecondaryText, Location = new Point(left, top + 27) });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}

sealed class SwitchControl : CheckBox
{
    public SwitchControl()
    {
        Appearance = Appearance.Button;
        AutoSize = false;
        Size = new Size(52, 30);
        Text = string.Empty;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.CheckButton;
        AccessibleName = "Toggle setting";
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new Rectangle(0, 2, Width - 1, Height - 5);
        using var trackBrush = new SolidBrush(Checked ? Color.FromArgb(0, 103, 220) : SettingsForm.ToggleOff);
        using var path = Rounded(track, track.Height / 2);
        e.Graphics.FillPath(trackBrush, path);
        int diameter = track.Height - 6;
        int x = Checked ? track.Right - diameter - 3 : track.Left + 3;
        using var thumb = new SolidBrush(Color.FromArgb(250, 250, 250));
        e.Graphics.FillEllipse(thumb, x, track.Top + 3, diameter, diameter);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(track, -2, -2));
    }

    static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

static class GraphicsExtensions
{
    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        graphics.DrawPath(pen, path);
    }
}
