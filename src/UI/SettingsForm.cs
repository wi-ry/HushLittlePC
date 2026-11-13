namespace HushLittlePC;

using System.Drawing.Drawing2D;
using System.Reflection;

class SettingsForm : Form
{
    static bool _darkTheme = ReadSystemTheme();

    static Color Page => _darkTheme ? Color.FromArgb(32, 32, 32) : Color.FromArgb(246, 248, 252);
    static Color Surface => _darkTheme ? Color.FromArgb(43, 43, 43) : Color.White;
    static Color Sidebar => _darkTheme ? Color.FromArgb(37, 37, 37) : Color.FromArgb(239, 244, 252);
    static Color PrimaryText => _darkTheme ? Color.FromArgb(245, 245, 245) : Color.FromArgb(20, 30, 48);
    static Color SecondaryText => _darkTheme ? Color.FromArgb(205, 205, 205) : Color.FromArgb(50, 62, 86);
    static Color Muted => _darkTheme ? Color.FromArgb(165, 165, 165) : Color.FromArgb(100, 110, 128);
    static Color Border => _darkTheme ? Color.FromArgb(66, 66, 66) : Color.FromArgb(224, 230, 240);
    static Color Selected => _darkTheme ? Color.FromArgb(57, 76, 101) : Color.FromArgb(216, 231, 252);
    static Color Accent => Color.FromArgb(0, 103, 220);
    internal static Color ToggleOff => _darkTheme ? Color.FromArgb(100, 100, 100) : Color.FromArgb(190, 197, 207);
    static Color IconSurface => _darkTheme ? Color.FromArgb(51, 65, 82) : Color.FromArgb(234, 243, 253);

    readonly Settings _settings;
    readonly Action _settingsChanged;
    readonly Action _exit;
    readonly Panel _sidebar;
    readonly Panel _main;
    readonly Panel _content;
    readonly FlowLayoutPanel _settingsCards;
    readonly Button _settingsNav;
    readonly Button _aboutNav;
    readonly Label _brand;
    readonly Label _tagline;
    readonly Label _status;

    public SettingsForm(Settings settings, Action settingsChanged, Action exit)
    {
        _settings = settings;
        _settingsChanged = settingsChanged;
        _exit = exit;

        Text = "Hush Little PC";
        Icon = TrayApp.MakeIcon(true);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 680);
        Size = new Size(1180, 820);
        Font = new Font("Segoe UI", 10F);
        BackColor = Page;
        AutoScaleMode = AutoScaleMode.Dpi;

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Page
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 224));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        _sidebar = new Panel { Dock = DockStyle.Fill, BackColor = Sidebar, Padding = new Padding(18, 24, 18, 18) };
        shell.Controls.Add(_sidebar, 0, 0);

        var sidebarLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Sidebar,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 156));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        _sidebar.Controls.Add(sidebarLayout);

        var identity = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Sidebar, Margin = Padding.Empty };
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        _brand = new Label { Text = "◔  Hush Little PC", Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 14F), ForeColor = PrimaryText, Margin = Padding.Empty };
        _tagline = new Label { Text = "Keep the peace. Automatically.", Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9F), ForeColor = SecondaryText, Margin = Padding.Empty };
        identity.Controls.Add(_brand, 0, 0);
        identity.Controls.Add(_tagline, 0, 1);
        sidebarLayout.Controls.Add(identity, 0, 0);

        var navigation = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 3, BackColor = Sidebar, Margin = new Padding(0, 10, 0, 0), Padding = Padding.Empty };
        for (int i = 0; i < 3; i++) navigation.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        _settingsNav = CreateNavButton("⚙   Settings", true);
        _aboutNav = CreateNavButton("ⓘ   About", false);
        var exitNav = CreateNavButton("⏻   Exit", false);
        _settingsNav.Click += (_, _) => ShowSettingsPage();
        _aboutNav.Click += (_, _) => ShowAboutPage();
        exitNav.Click += (_, _) => _exit();
        navigation.Controls.Add(_settingsNav, 0, 0);
        navigation.Controls.Add(_aboutNav, 0, 1);
        navigation.Controls.Add(exitNav, 0, 2);
        sidebarLayout.Controls.Add(navigation, 0, 1);

        var spacer = new Panel { Dock = DockStyle.Fill, BackColor = Sidebar };
        sidebarLayout.Controls.Add(spacer, 0, 2);

        var statusPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Sidebar, Margin = Padding.Empty };
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20));
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var dot = new Label { Text = "●", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(34, 197, 94), TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty };
        _status = new Label { Text = "Monitoring audio devices", Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = PrimaryText, Font = new Font("Segoe UI", 9F), TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty };
        statusPanel.Controls.Add(dot, 0, 0);
        statusPanel.Controls.Add(_status, 1, 0);
        sidebarLayout.Controls.Add(statusPanel, 0, 3);

        _main = new Panel { Dock = DockStyle.Fill, BackColor = Page, Padding = new Padding(28, 24, 28, 20) };
        shell.Controls.Add(_main, 1, 0);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Page,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        _main.Controls.Add(mainLayout);

        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Page, Margin = Padding.Empty };
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        heading.Controls.Add(new Label { Text = "Settings", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 26F), ForeColor = PrimaryText, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty }, 0, 0);
        heading.Controls.Add(new Label { Text = "Configure how Hush Little PC manages your audio.", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11F), ForeColor = SecondaryText, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty }, 0, 1);
        mainLayout.Controls.Add(heading, 0, 0);

        _content = new Panel { Dock = DockStyle.Fill, BackColor = Page, AutoScroll = true, Margin = Padding.Empty };
        _settingsCards = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Page,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _content.Controls.Add(_settingsCards);
        _content.Resize += (_, _) => ResizeCards();
        mainLayout.Controls.Add(_content, 0, 1);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Page, Margin = Padding.Empty, Padding = new Padding(0, 8, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var footerRule = new Panel { Dock = DockStyle.Fill, BackColor = Border };
        var footerContent = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Page, Margin = new Padding(0, 1, 0, 0) };
        footerContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footerContent.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footerContent.Controls.Add(new Label { Text = "ⓘ   Changes are applied automatically.", Dock = DockStyle.Fill, ForeColor = PrimaryText, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty }, 0, 0);
        var learn = new LinkLabel { Text = "Learn more", AutoSize = true, LinkColor = Accent, Anchor = AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight, Margin = new Padding(12, 0, 4, 0) };
        learn.LinkClicked += (_, _) => ShowAboutPage();
        footerContent.Controls.Add(learn, 1, 0);
        footer.Controls.Add(footerRule, 0, 0);
        footer.Controls.Add(footerContent, 0, 1);
        mainLayout.Controls.Add(footer, 0, 2);

        BuildSettingsCards();
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
            _darkTheme = ReadSystemTheme();
            ApplyTheme();
        }
    }

    void ApplyTheme()
    {
        BackColor = Page;
        _sidebar.BackColor = Sidebar;
        _main.BackColor = Page;
        foreach (Control control in ControlsRecursive(this))
        {
            if (control is TableLayoutPanel or Panel or FlowLayoutPanel)
                control.BackColor = control.Parent == _sidebar || IsInsideSidebar(control) ? Sidebar : Page;
            if (control is Label label && label != _status)
                label.ForeColor = label.ForeColor == Muted ? Muted : PrimaryText;
            if (control is Button button)
                button.ForeColor = PrimaryText;
            if (control is SwitchControl toggle)
                toggle.Invalidate();
        }
        _status.ForeColor = PrimaryText;
        _settingsNav.BackColor = Selected;
        _aboutNav.BackColor = Sidebar;
        ShowSettingsPage();
    }

    bool IsInsideSidebar(Control control)
    {
        for (Control? parent = control.Parent; parent is not null; parent = parent.Parent)
            if (parent == _sidebar) return true;
        return false;
    }

    static IEnumerable<Control> ControlsRecursive(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control descendant in ControlsRecursive(child))
                yield return descendant;
        }
    }

    void BuildSettingsCards()
    {
        if (_settingsCards.Parent != _content)
            _content.Controls.Add(_settingsCards);
        _settingsCards.SuspendLayout();
        _settingsCards.Controls.Clear();
        AddSwitchCard("◖))", "Enable Hush Little PC", "Automatically mute your audio when you're idle.", _settings.Enabled, value => { _settings.Enabled = value; Changed(); });
        AddIdleCard();
        AddSwitchCard("↗", "Unmute on activity", "Unmute when you use your keyboard or mouse.", _settings.UnmuteOnActivity, value => { _settings.UnmuteOnActivity = value; Changed(); });
        AddSwitchCard("♫", "Stay unmuted while audio is playing", "Avoid muting while music, videos, or other audio are playing.", _settings.SkipWhileAudioPlaying, value => { _settings.SkipWhileAudioPlaying = value; Changed(); });
        AddSwitchCard("↻", "Mute on output change", "Mute when the default audio device changes.", _settings.MuteOnDeviceChange, value => { _settings.MuteOnDeviceChange = value; Changed(); });
        AddSwitchCard("♧", "Notifications", "Show a notification when audio is muted.", _settings.ShowNotification, value => { _settings.ShowNotification = value; Changed(); });
        AddSwitchCard("⊞", "Run at startup", "Start Hush Little PC when you sign in to Windows.", _settings.RunOnStartup, value => { _settings.RunOnStartup = value; StartupManager.Set(value); Changed(); });
        _settingsCards.ResumeLayout(true);
        ResizeCards();
    }

    void ResizeCards()
    {
        if (_content.ClientSize.Width <= 0) return;
        int width = Math.Max(480, _content.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
        _settingsCards.Width = width;
        foreach (Control control in _settingsCards.Controls)
            control.Width = width;
    }

    void AddSwitchCard(string glyph, string title, string description, bool initial, Action<bool> changed)
    {
        var card = CreateCard(92);
        AddIcon(card, glyph);
        AddText(card, title, description);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            BackColor = Surface,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(4),
            Padding = new Padding(0),
            RightToLeft = RightToLeft.No
        };
        var toggle = new SwitchControl { Checked = initial, AccessibleName = title, Margin = new Padding(0, 2, 10, 0) };
        var state = new Label { Text = initial ? "On" : "Off", AutoSize = true, ForeColor = PrimaryText, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 6, 0, 0) };
        toggle.CheckedChanged += (_, _) => { state.Text = toggle.Checked ? "On" : "Off"; changed(toggle.Checked); };
        actionPanel.Controls.Add(toggle);
        actionPanel.Controls.Add(state);
        card.Controls.Add(actionPanel, 2, 0);
        _settingsCards.Controls.Add(card);
    }

    void AddIdleCard()
    {
        var card = CreateCard(108);
        AddIcon(card, "◷");
        AddText(card, "Idle time", "Mute after a period of inactivity.");

        var controls = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = Surface,
            Margin = new Padding(4, 4, 8, 4),
            Padding = Padding.Empty
        };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        controls.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        controls.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

        var slider = new TrackBar
        {
            Minimum = Settings.MinIdleMinutes,
            Maximum = Settings.MaxIdleMinutes,
            TickStyle = TickStyle.None,
            Value = _settings.IdleMinutes,
            Dock = DockStyle.Fill,
            AccessibleName = "Idle time slider",
            Margin = new Padding(0, 0, 8, 0)
        };
        var minutes = new NumericUpDown
        {
            Minimum = Settings.MinIdleMinutes,
            Maximum = Settings.MaxIdleMinutes,
            Value = _settings.IdleMinutes,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            TextAlign = HorizontalAlignment.Center,
            AccessibleName = "Idle time in minutes",
            Margin = new Padding(0, 4, 0, 0)
        };
        var unit = new Label { Text = "min", Dock = DockStyle.Fill, ForeColor = PrimaryText, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(6, 0, 0, 0) };
        var range = new Label { Text = "1 – 1,440 minutes", Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font("Segoe UI", 8.5F), TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty };

        bool synchronizing = false;
        slider.Scroll += (_, _) =>
        {
            if (synchronizing) return;
            synchronizing = true;
            minutes.Value = slider.Value;
            synchronizing = false;
            _settings.IdleMinutes = slider.Value;
            Changed();
        };
        minutes.ValueChanged += (_, _) =>
        {
            if (synchronizing) return;
            synchronizing = true;
            slider.Value = (int)minutes.Value;
            synchronizing = false;
            _settings.IdleMinutes = (int)minutes.Value;
            Changed();
        };

        controls.Controls.Add(slider, 0, 0);
        controls.Controls.Add(minutes, 1, 0);
        controls.Controls.Add(unit, 2, 0);
        controls.Controls.Add(range, 0, 1);
        controls.SetColumnSpan(range, 3);
        card.Controls.Add(controls, 2, 0);
        _settingsCards.Controls.Add(card);
    }

    Panel CreateCard(int height)
    {
        var card = new RoundedCard
        {
            Width = 700,
            Height = height,
            BackColor = Surface,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(14, 10, 14, 10)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(layout);
        return card;
    }

    void AddIcon(Panel card, string glyph)
    {
        var icon = new IconGlyph(glyph)
        {
            Dock = DockStyle.Fill,
            ForeColor = Accent,
            Margin = new Padding(0, 3, 12, 3)
        };
        card.Controls[0].Controls.Add(icon, 0, 0);
    }

    void AddText(Panel card, string title, string description)
    {
        var text = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Surface,
            Margin = new Padding(4, 0, 8, 0),
            Padding = Padding.Empty
        };
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        text.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, AutoEllipsis = true, Font = new Font("Segoe UI Semibold", 11F), ForeColor = PrimaryText, TextAlign = ContentAlignment.BottomLeft, Margin = Padding.Empty }, 0, 0);
        text.Controls.Add(new Label { Text = description, Dock = DockStyle.Fill, AutoEllipsis = true, Font = new Font("Segoe UI", 9.5F), ForeColor = SecondaryText, TextAlign = ContentAlignment.TopLeft, Margin = Padding.Empty }, 0, 1);
        card.Controls[0].Controls.Add(text, 1, 0);
    }

    void ShowSettingsPage()
    {
        SelectNavigation(true);
        _content.Controls.Clear();
        BuildSettingsCards();
    }

    void ShowAboutPage()
    {
        SelectNavigation(false);
        _content.Controls.Clear();
        var page = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2, BackColor = Page, AutoSize = true, Margin = Padding.Empty };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        page.Controls.Add(new Label { Text = "About", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 26F), ForeColor = PrimaryText, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty }, 0, 0);

        var card = new RoundedCard { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(24), Margin = new Padding(0, 0, 0, 8) };
        var aboutLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Surface, Margin = Padding.Empty };
        aboutLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        aboutLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        aboutLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Development";
        aboutLayout.Controls.Add(new Label { Text = "Hush Little PC", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 18F), ForeColor = PrimaryText, Margin = Padding.Empty }, 0, 0);
        aboutLayout.Controls.Add(new Label { Text = $"Version {version}\nAutomatically manages audio muting based on your activity.", Dock = DockStyle.Fill, ForeColor = SecondaryText, Margin = Padding.Empty }, 0, 1);
        var repo = new LinkLabel { Text = "View project on GitHub", AutoSize = true, LinkColor = Accent, Anchor = AnchorStyles.Left };
        repo.LinkClicked += (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/wi-ry/HushLittlePC") { UseShellExecute = true });
        aboutLayout.Controls.Add(repo, 0, 2);
        card.Controls.Add(aboutLayout);
        page.Controls.Add(card, 0, 1);
        _content.Controls.Add(page);
    }

    void Changed()
    {
        SettingsStore.Save(_settings);
        _settingsChanged();
    }

    void SelectNavigation(bool settings)
    {
        _settingsNav.BackColor = settings ? Selected : Sidebar;
        _aboutNav.BackColor = settings ? Sidebar : Selected;
        _settingsNav.Font = new Font("Segoe UI", 10F, settings ? FontStyle.Bold : FontStyle.Regular);
        _aboutNav.Font = new Font("Segoe UI", 10F, settings ? FontStyle.Regular : FontStyle.Bold);
    }

    static Button CreateNavButton(string text, bool selected) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 0 },
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(12, 0, 0, 0),
        Font = new Font("Segoe UI", 10F, selected ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = PrimaryText,
        BackColor = selected ? Selected : Sidebar,
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 2, 0, 2)
    };
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
        Margin = Padding.Empty;
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
        using var thumb = new SolidBrush(Color.White);
        e.Graphics.FillEllipse(thumb, x, track.Top + 3, diameter, diameter);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(track, -2, -2));
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

sealed class RoundedCard : Panel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(90, 128, 145, 170));
        using var path = new GraphicsPath();
        int radius = 12;
        int d = radius * 2;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        e.Graphics.DrawPath(pen, path);
    }
}


sealed class IconGlyph : Control
{
    readonly string _glyph;

    public IconGlyph(string glyph)
    {
        _glyph = glyph;
        Font = new Font("Segoe UI Symbol", 21F);
        BackColor = Color.Transparent;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int diameter = Math.Min(58, Math.Min(ClientSize.Width, ClientSize.Height) - 4);
        if (diameter <= 0) return;
        var bounds = new Rectangle((ClientSize.Width - diameter) / 2, (ClientSize.Height - diameter) / 2, diameter - 1, diameter - 1);
        using var brush = new SolidBrush(IconSurface);
        e.Graphics.FillEllipse(brush, bounds);
        TextRenderer.DrawText(e.Graphics, _glyph, Font, bounds, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
