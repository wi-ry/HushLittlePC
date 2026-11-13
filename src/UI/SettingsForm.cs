namespace HushLittlePC;

using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;

// ───────────────────────── Form ─────────────────────────

sealed class SettingsForm : Form
{
    static readonly int[] IdleSteps = { 1, 2, 3, 5, 10, 15, 20, 30, 45, 60, 90, 120, 180, 240, 360, 480, 720, 1440 };

    readonly Settings _settings;
    readonly Action _settingsChanged;
    readonly Action _exit;
    readonly System.Windows.Forms.Timer _debounce = new() { Interval = 300 };

    Panel _content = null!;
    NavItem _settingsNav = null!;
    NavItem _aboutNav = null!;
    bool _onSettings = true;
    StatusDot? _statusDot;
    Label? _statusText;

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public SettingsForm(Settings settings, Action settingsChanged, Action exit)
    {
        _settings = settings;
        _settingsChanged = settingsChanged;
        _exit = exit;

        Text = "Hush Little PC";
        Icon = TrayApp.MakeIcon(true);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.None;           // we scale ourselves via Px()
        Font = Fonts.Body(9f);
        MinimumSize = new Size(Px(900), Px(560));
        var work = Screen.FromPoint(Cursor.Position).WorkingArea;
        Size = new Size(Math.Min(Px(1000), work.Width - Px(40)), Math.Min(Px(720), work.Height - Px(40)));

        _debounce.Tick += (_, _) => { _debounce.Stop(); Changed(); };

        BuildUi();
    }

    int Px(int value) => (int)Math.Round(value * DeviceDpi / 96.0);

    // ── window plumbing ──

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTitleBarTheme();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        BuildUi();
    }

    protected override void WndProc(ref Message m)
    {
        const int WmSettingChange = 0x001A;
        base.WndProc(ref m);
        if (m.Msg != WmSettingChange) return;

        bool dark = Palette.Read();
        var accent = Palette.ReadAccent();
        if (dark == Palette.Dark && accent == Palette.AccentBase) return;
        Palette.Dark = dark;
        Palette.AccentBase = accent;
        BeginInvoke(() => { BuildUi(); ApplyTitleBarTheme(); });
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        UpdateStatus();      // picks up changes made elsewhere (e.g. tray menu)
    }

    void UpdateStatus()
    {
        if (_statusDot is null || _statusText is null) return;
        bool on = _settings.Enabled;
        _statusDot.Fill = on ? Palette.Success : Palette.Critical;
        _statusText.Text = on ? "Monitoring audio devices" : "Monitoring disabled";
    }

    void ApplyTitleBarTheme()
    {
        if (!IsHandleCreated) return;
        int dark = Palette.Dark ? 1 : 0;
        DwmSetWindowAttribute(Handle, 20 /* DWMWA_USE_IMMERSIVE_DARK_MODE */, ref dark, sizeof(int));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _debounce.Dispose();
        base.Dispose(disposing);
    }

    // ── shell ──

    void BuildUi()
    {
        SuspendLayout();
        ClearControls(this);
        BackColor = Palette.Page;
        ForeColor = Palette.Fg;

        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Palette.Page };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(232)));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        shell.Controls.Add(BuildSidebar(), 0, 0);

        _content = new Panel
        {
            Dock = DockStyle.Fill, BackColor = Palette.Page, AutoScroll = true,
            Padding = new Padding(Px(32), Px(24), Px(32), Px(16))
        };
        shell.Controls.Add(_content, 1, 0);
        Controls.Add(shell);

        ShowPage(_onSettings);
        ActiveControl = _settingsNav;
        ResumeLayout(true);
    }

    Control BuildSidebar()
    {
        var side = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = Palette.Page,
            Padding = new Padding(Px(12), Px(16), Px(12), Px(14))
        };
        side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 5; i++)
            side.RowStyles.Add(new RowStyle(i == 2 ? SizeType.Percent : SizeType.AutoSize, i == 2 ? 100 : 0));

        var brand = new TableLayoutPanel
        {
            AutoSize = true, ColumnCount = 2, RowCount = 1, BackColor = Palette.Page,
            Margin = new Padding(Px(8), 0, 0, Px(20))
        };
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        brand.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        brand.Controls.Add(new LogoControl(DeviceDpi, 28) { Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, Px(10), 0) }, 0, 0);
        brand.Controls.Add(new Label
        {
            Text = "Hush Little PC", Font = Fonts.Semibold(10.5f), ForeColor = Palette.Fg,
            AutoSize = true, Anchor = AnchorStyles.Left, Margin = Padding.Empty
        }, 1, 0);
        side.Controls.Add(brand, 0, 0);

        var nav = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 3,
            BackColor = Palette.Page, Margin = Padding.Empty
        };
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++) nav.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _settingsNav = new NavItem(DeviceDpi, "\uE713", "Settings");
        _aboutNav = new NavItem(DeviceDpi, "\uE946", "About");
        var exitNav = new NavItem(DeviceDpi, "\uE7E8", "Exit");
        _settingsNav.Click += (_, _) => ShowPage(true);
        _aboutNav.Click += (_, _) => ShowPage(false);
        exitNav.Click += (_, _) => _exit();
        nav.Controls.Add(_settingsNav, 0, 0);
        nav.Controls.Add(_aboutNav, 0, 1);
        nav.Controls.Add(exitNav, 0, 2);
        side.Controls.Add(nav, 0, 1);

        side.Controls.Add(new Panel
        {
            Dock = DockStyle.Fill, Height = 1, MinimumSize = new Size(0, 1), BackColor = Palette.CardBorder,
            Margin = new Padding(Px(4), 0, Px(4), Px(12))
        }, 0, 3);

        var status = new FlowLayoutPanel
        {
            AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight,
            BackColor = Palette.Page, Margin = new Padding(Px(10), 0, 0, 0)
        };
        _statusDot = new StatusDot(DeviceDpi) { Margin = new Padding(0, Px(5), Px(8), 0) };
        _statusText = new Label { ForeColor = Palette.Fg, Font = Fonts.Body(9f), AutoSize = true, Margin = Padding.Empty };
        status.Controls.Add(_statusDot);
        status.Controls.Add(_statusText);
        side.Controls.Add(status, 0, 4);
        UpdateStatus();
        return side;
    }

    // ── pages ──

    void ShowPage(bool settings)
    {
        _onSettings = settings;
        _settingsNav.Selected = settings;
        _aboutNav.Selected = !settings;

        _content.SuspendLayout();
        ClearControls(_content);

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 0, BackColor = Palette.Page
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _content.Controls.Add(page);

        if (settings) FillSettings(page); else FillAbout(page);
        _content.ResumeLayout(true);
    }

    void FillSettings(TableLayoutPanel page)
    {
        AddRow(page, Heading("Settings", ""));

        AddRow(page, SwitchCard("\uE767", "Enable Hush Little PC", "Automatically mute your audio when you're idle.",
            _settings.Enabled, v => { _settings.Enabled = v; Changed(); UpdateStatus(); }));

        AddRow(page, Section("Behavior"));
        AddRow(page, IdleCard());
        AddRow(page, SwitchCard("\uE765", "Unmute on activity", "Unmute when you use your keyboard or mouse.",
            _settings.UnmuteOnActivity, v => { _settings.UnmuteOnActivity = v; Changed(); }));
        AddRow(page, SwitchCard("\uE8D6", "Stay unmuted while audio is playing", "Avoid muting while music, videos, or other audio are playing.",
            _settings.SkipWhileAudioPlaying, v => { _settings.SkipWhileAudioPlaying = v; Changed(); }));
        AddRow(page, SwitchCard("\uE895", "Mute on output change", "Mute when the default audio device changes.",
            _settings.MuteOnDeviceChange, v => { _settings.MuteOnDeviceChange = v; Changed(); }));

        AddRow(page, Section("General"));
        AddRow(page, SwitchCard("\uEA8F", "Notifications", "Show a notification when audio is muted.",
            _settings.ShowNotification, v => { _settings.ShowNotification = v; Changed(); }));
        AddRow(page, SwitchCard("\uE768", "Run at startup", "Start Hush Little PC when you sign in to Windows.",
            _settings.RunOnStartup, v => { _settings.RunOnStartup = v; StartupManager.Set(v); Changed(); }));
    }

    void FillAbout(TableLayoutPanel page)
    {
        AddRow(page, Heading("About", "A quieter PC, without the extra effort."));

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Development";
        var app = new SettingCard(DeviceDpi) { Glyph = "\uE767", Title = "Hush Little PC", Description = $"Version {version}" };
        var link = new LinkLabel
        {
            Text = "View project on GitHub", AutoSize = true, BackColor = Palette.Card,
            LinkColor = Palette.Accent, ActiveLinkColor = Palette.Accent, Font = Fonts.Body(10.5f), LinkBehavior = LinkBehavior.HoverUnderline
        };
        link.LinkClicked += (_, _) => System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo("https://github.com/wi-ry/HushLittlePC") { UseShellExecute = true });
        app.AddTrailing(link);
        AddRow(page, app);

        AddRow(page, new SettingCard(DeviceDpi)
        {
            Glyph = "\uE946", Title = "What it does",
            Description = "Automatically manages audio muting based on your activity."
        });
    }

    static void AddRow(TableLayoutPanel page, Control control)
    {
        int row = page.RowCount++;
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.Controls.Add(control, 0, row);
    }

    Control Heading(string title, string subtitle) =>
        new HeadingControl(DeviceDpi, title, subtitle) { Margin = new Padding(0, 0, 0, Px(12)) };

    Control Section(string text) =>
        new SectionHeader(DeviceDpi, text) { Margin = new Padding(Px(1), Px(14), 0, Px(6)) };

    // ── cards ──

    SettingCard SwitchCard(string glyph, string title, string description, bool initial, Action<bool> apply)
    {
        var card = new SettingCard(DeviceDpi) { Glyph = glyph, Title = title, Description = description };
        var state = new Label
        {
            Text = initial ? "On" : "Off", AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Palette.Fg, BackColor = Palette.Card, Font = Fonts.Body(10.5f)
        };
        state.Size = new Size(TextRenderer.MeasureText("Off", state.Font).Width + Px(4), Px(20));
        var toggle = new SwitchControl(DeviceDpi) { Checked = initial, AccessibleName = title, BackColor = Palette.Card };
        toggle.CheckedChanged += (_, _) =>
        {
            state.Text = toggle.Checked ? "On" : "Off";
            apply(toggle.Checked);
        };
        card.AddTrailing(toggle);    // trailing controls are laid out right → left
        card.AddTrailing(state);
        return card;
    }

    SettingCard IdleCard()
    {
        int min = Settings.MinIdleMinutes, max = Settings.MaxIdleMinutes;
        int[] steps = IdleSteps.Concat(new[] { min, max }).Where(v => v >= min && v <= max).Distinct().OrderBy(v => v).ToArray();

        var card = new SettingCard(DeviceDpi) { Glyph = "\uE121", Title = "Idle time", Description = "Mute after a period of inactivity." };

        var unit = new Label
        {
            Text = "min", AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Palette.Fg, BackColor = Palette.Card, Font = Fonts.Body(10.5f)
        };
        unit.Size = new Size(TextRenderer.MeasureText(unit.Text, unit.Font).Width + Px(6), Px(20));
        var minutes = new NumberBox(DeviceDpi, min, max, _settings.IdleMinutes, "Idle time in minutes");
        var slider = new FluentSlider(DeviceDpi)
        {
            Minimum = 0, Maximum = steps.Length - 1, Value = NearestStep(steps, _settings.IdleMinutes),
            BackColor = Palette.Card, AccessibleName = "Idle time slider", Size = new Size(Px(180), Px(32))
        };

        bool syncing = false;
        slider.ValueChanged += (_, _) =>
        {
            if (syncing) return;
            syncing = true;
            minutes.Value = steps[slider.Value];
            syncing = false;
            _settings.IdleMinutes = steps[slider.Value];
            ChangedSoon();
        };
        minutes.ValueChanged += (_, _) =>
        {
            if (syncing) return;
            syncing = true;
            slider.Value = NearestStep(steps, minutes.Value);
            syncing = false;
            _settings.IdleMinutes = minutes.Value;
            ChangedSoon();
        };

        card.AddTrailing(unit);
        card.AddTrailing(minutes);
        card.AddTrailing(slider);
        return card;
    }

    static int NearestStep(int[] steps, int value)
    {
        int best = 0;
        for (int i = 1; i < steps.Length; i++)
            if (Math.Abs(steps[i] - value) < Math.Abs(steps[best] - value)) best = i;
        return best;
    }

    // ── persistence ──

    void Changed()
    {
        SettingsStore.Save(_settings);
        _settingsChanged();
    }

    void ChangedSoon()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    static void ClearControls(Control parent)
    {
        foreach (Control c in parent.Controls.Cast<Control>().ToList())
        {
            parent.Controls.Remove(c);
            c.Dispose();
        }
    }
}

// ───────────────────────── Controls ─────────────────────────

/// <summary>One settings row: icon, title, description, and right-aligned controls. Fully custom-painted.</summary>
sealed class SettingCard : Panel
{
    readonly double _k;
    readonly List<Control> _trailing = new();
    readonly Font _titleFont = Fonts.Body(10.5f);
    readonly Font _descFont = Fonts.Body(9f);
    readonly Font _iconFont = Fonts.Icon(12f);
    int _textRight;

    public string Glyph { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";

    public SettingCard(int dpi)
    {
        _k = dpi / 96.0;
        DoubleBuffered = true;
        ResizeRedraw = true;
        Dock = DockStyle.Fill;
        BackColor = Palette.Page;      // corners show the page, the card shape is painted
        Height = P(60);
        Margin = new Padding(0, 0, 0, P(4));
        _textRight = Width - P(16);
    }

    int P(int v) => (int)Math.Round(v * _k);

    public void AddTrailing(Control control)
    {
        _trailing.Add(control);
        Controls.Add(control);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int right = Width - P(16);
        foreach (var c in _trailing)
        {
            right -= c.Width;
            c.Location = new Point(right, (Height - c.Height) / 2 + (c.Tag is int dy ? dy : 0));
            right -= P(10);
        }
        _textRight = right;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var path = Shapes.Round(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), P(4)))
        using (var fill = new SolidBrush(Palette.Card))
        using (var border = new Pen(Palette.CardBorder))
        {
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }

        const TextFormatFlags icon = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        const TextFormatFlags text = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

        TextRenderer.DrawText(g, Glyph, _iconFont, new Rectangle(P(16), 0, P(24), Height), Palette.Fg, icon);

        int x = P(16) + P(24) + P(16);
        int width = Math.Max(0, _textRight - x - P(8));
        int th = _titleFont.Height, dh = _descFont.Height;
        int y = (Height - (th + dh + P(1))) / 2;
        TextRenderer.DrawText(g, Title, _titleFont, new Rectangle(x, y, width, th), Palette.Fg, text);
        TextRenderer.DrawText(g, Description, _descFont, new Rectangle(x, y + th + P(1), width, dh), Palette.Muted, text);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _titleFont.Dispose(); _descFont.Dispose(); _iconFont.Dispose(); }
        base.Dispose(disposing);
    }
}

/// <summary>Windows 11 style toggle with a short, reactive slide animation.</summary>
sealed class SwitchControl : Control
{
    readonly double _k;
    readonly System.Windows.Forms.Timer _anim = new() { Interval = 15 };
    bool _checked;
    float _t;                       // 0 = off … 1 = on

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            if (IsHandleCreated && SystemInformation.UIEffectsEnabled) _anim.Start();
            else { _t = value ? 1f : 0f; Invalidate(); }
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public SwitchControl(int dpi)
    {
        _k = dpi / 96.0;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
        Size = new Size(P(44), P(24));
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.CheckButton;

        _anim.Tick += (_, _) =>
        {
            float target = _checked ? 1f : 0f;
            _t = _t < target ? Math.Min(target, _t + 0.12f) : Math.Max(target, _t - 0.12f);
            Invalidate();
            if (_t == target) _anim.Stop();
        };
    }

    int P(int v) => (int)Math.Round(v * _k);

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space) { Checked = !Checked; e.Handled = true; }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        float ease = _t * _t * (3 - 2 * _t);
        int pad = P(2);
        var track = new RectangleF(pad + 0.5f, pad + 0.5f, Width - 1 - pad * 2, Height - 1 - pad * 2);
        using var trackPath = Shapes.Round(track, track.Height / 2);

        if (ease > 0f)
        {
            using var fill = new SolidBrush(Color.FromArgb((int)(255 * ease), Palette.Accent));
            g.FillPath(fill, trackPath);
        }
        if (ease < 1f)
        {
            using var border = new Pen(Color.FromArgb((int)(255 * (1 - ease)), Palette.SwitchBorder), 1f);
            g.DrawPath(border, trackPath);
        }

        float d = track.Height * (0.55f + 0.05f * ease);
        float inset = (track.Height - d) / 2f;
        float xOff = track.Left + inset;
        float xOn = track.Right - inset - d;
        float x = xOff + (xOn - xOff) * ease;
        using (var thumb = new SolidBrush(Palette.Lerp(Palette.SwitchBorder, Palette.OnAccent, ease)))
            g.FillEllipse(thumb, x, track.Top + inset, d, d);

        if (Focused && ShowFocusCues)
        {
            using var ring = new Pen(Palette.Fg, 1.5f);
            using var ringPath = Shapes.Round(new RectangleF(1, 1, Width - 2, Height - 2), (Height - 2) / 2f);
            g.DrawPath(ring, ringPath);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _anim.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>Sidebar navigation row with hover, selected state and accent pill.</summary>
sealed class NavItem : Button
{
    readonly double _k;
    readonly string _glyph;
    readonly Font _textFont = Fonts.Body(10.5f);
    readonly Font _iconFont = Fonts.Icon(11f);
    bool _hover;
    bool _selected;

    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
    }

    public NavItem(int dpi, string glyph, string caption)
    {
        _k = dpi / 96.0;
        _glyph = glyph;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Text = caption;
        Cursor = Cursors.Hand;
        BackColor = Palette.Page;
        AutoSize = false;
        Dock = DockStyle.Fill;
        MinimumSize = new Size(0, P(40));
        Height = P(40);
        Margin = new Padding(0, 0, 0, P(2));
    }

    int P(int v) => (int)Math.Round(v * _k);

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Palette.Page);

        var bounds = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        if (_selected || _hover)
        {
            using var path = Shapes.Round(bounds, P(4));
            using var brush = new SolidBrush(_selected ? Palette.NavSelected : Palette.NavHover);
            g.FillPath(brush, path);
        }
        if (_selected)
        {
            using var pill = Shapes.Round(new RectangleF(0, (Height - P(16)) / 2f, P(3), P(16)), P(1) + 0.5f);
            using var brush = new SolidBrush(Palette.Accent);
            g.FillPath(brush, pill);
        }
        if (Focused && ShowFocusCues)
        {
            using var ring = new Pen(Palette.Fg, 1.5f);
            using var path = Shapes.Round(new RectangleF(1, 1, Width - 2, Height - 2), P(4));
            g.DrawPath(ring, path);
        }

        const TextFormatFlags icon = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        const TextFormatFlags text = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

        TextRenderer.DrawText(g, _glyph, _iconFont, new Rectangle(P(12), 0, P(24), Height), Palette.Fg, icon);
        TextRenderer.DrawText(g, Text, _textFont, new Rectangle(P(46), 0, Math.Max(0, Width - P(46) - P(8)), Height), Palette.Fg, text);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _textFont.Dispose(); _iconFont.Dispose(); }
        base.Dispose(disposing);
    }
}

// ───────────────────────── Helpers ─────────────────────────

static class Palette
{
    public static bool Dark = Read();

    static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);

    public static Color Page => Dark ? C(32, 32, 32) : C(243, 243, 243);
    public static Color Card => Dark ? C(43, 43, 43) : C(251, 251, 251);
    public static Color CardBorder => Dark ? C(58, 58, 58) : C(229, 229, 229);
    public static Color Fg => Dark ? C(255, 255, 255) : C(26, 26, 26);
    public static Color Muted => Dark ? C(190, 190, 190) : C(96, 96, 96);
    public static Color AccentBase = ReadAccent();
    public static Color Accent => Dark ? Lerp(AccentBase, Color.White, 0.35f) : AccentBase;
    public static Color OnAccent => (0.299 * Accent.R + 0.587 * Accent.G + 0.114 * Accent.B) > 165 ? C(26, 26, 26) : Color.White;
    public static Color SwitchBorder => Dark ? C(165, 165, 165) : C(100, 100, 100);
    public static Color NavHover => Dark ? C(45, 45, 45) : C(236, 236, 236);
    public static Color NavSelected => Dark ? C(54, 54, 54) : C(231, 231, 231);
    public static Color IconSurface => Dark ? C(48, 58, 70) : C(234, 243, 253);

    public static Color SliderTrack => Dark ? C(152, 152, 152) : C(138, 138, 138);

    /// <summary>The user's Windows accent colour (Settings → Personalization → Colors).</summary>
    public static Color ReadAccent()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            if (key?.GetValue("AccentColorMenu") is int abgr)
                return Color.FromArgb(abgr & 0xFF, (abgr >> 8) & 0xFF, (abgr >> 16) & 0xFF);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
        }
        return Color.FromArgb(0, 103, 192);
    }
    public static Color ThumbOuter => Dark ? C(69, 69, 69) : Color.White;
    public static Color ThumbBorder => Dark ? C(88, 88, 88) : C(218, 218, 218);
    public static Color FieldFill => Dark ? C(55, 55, 55) : C(250, 250, 250);
    public static Color FieldFocus => Dark ? C(31, 31, 31) : Color.White;
    public static Color FieldBorder => Dark ? C(72, 72, 72) : C(229, 229, 229);
    public static Color FieldBottom => Dark ? C(140, 140, 140) : C(150, 150, 150);
    public static Color Subtle => Dark ? C(70, 70, 70) : C(238, 238, 238);
    public static Color Success => Dark ? C(108, 203, 95) : C(15, 123, 15);      // SystemFillColorSuccess
    public static Color Critical => Dark ? C(255, 153, 164) : C(196, 43, 28);    // SystemFillColorCritical

    public static Color Lerp(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t),
        (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));

    public static bool Read()
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
}

static class Fonts
{
    static readonly HashSet<string> Installed = new InstalledFontCollection().Families.Select(f => f.Name).ToHashSet();

    static readonly string TextFamily = Pick("Segoe UI Variable Text", "Segoe UI");
    static readonly string IconFamily = Pick("Segoe Fluent Icons", "Segoe MDL2 Assets", "Segoe UI Symbol");

    static string Pick(params string[] names) => names.FirstOrDefault(Installed.Contains) ?? "Segoe UI";

    public static Font Body(float pt) => new(TextFamily, pt, FontStyle.Regular);
    public static Font Semibold(float pt) => new("Segoe UI Semibold", pt, FontStyle.Regular);
    public static Font Icon(float pt) => new(IconFamily, pt, FontStyle.Regular);
}

static class Shapes
{
    public static GraphicsPath Round(RectangleF r, float radius)
    {
        float d = Math.Max(1f, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>Placeholder crescent-moon mark drawn in code (with "zZ" when large). Swap for a real asset if you have one.</summary>
sealed class LogoControl : Control
{
    readonly bool _small;

    public LogoControl(int dpi, int logicalSize = 72)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        int size = (int)Math.Round(logicalSize * dpi / 96.0);
        Size = new Size(size, size);
        BackColor = Palette.Page;
        TabStop = false;
        _small = logicalSize < 48;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(BackColor);
        float s = Width;

        using (var moon = new SolidBrush(Palette.Accent))
            g.FillEllipse(moon, _small ? 0.08f * s : 0.06f * s, _small ? 0.12f * s : 0.24f * s, _small ? 0.80f * s : 0.68f * s, _small ? 0.80f * s : 0.68f * s);
        using (var cut = new SolidBrush(BackColor))
        {
            if (_small) g.FillEllipse(cut, 0.40f * s, -0.02f * s, 0.68f * s, 0.68f * s);
            else g.FillEllipse(cut, 0.30f * s, 0.10f * s, 0.62f * s, 0.62f * s);
        }
        if (_small) return;

        using var big = new Font("Segoe UI", 0.30f * s, FontStyle.Bold, GraphicsUnit.Pixel);
        using var small = new Font("Segoe UI", 0.20f * s, FontStyle.Bold, GraphicsUnit.Pixel);
        using var ink = new SolidBrush(Palette.Accent);
        g.DrawString("Z", big, ink, 0.66f * s, 0.00f * s);
        g.DrawString("z", small, ink, 0.52f * s, 0.20f * s);
    }
}

/// <summary>Small status dot (green / red). Drawn, so it stays crisp at any DPI.</summary>
sealed class StatusDot : Control
{
    Color _fill = Color.Gray;

    public Color Fill
    {
        get => _fill;
        set { _fill = value; Invalidate(); }
    }

    public StatusDot(int dpi)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        int d = (int)Math.Round(8 * dpi / 96.0);
        Size = new Size(d, d);
        BackColor = Palette.Page;
        TabStop = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        using var brush = new SolidBrush(_fill);
        g.FillEllipse(brush, 0, 0, Width - 1, Height - 1);
    }
}

/// <summary>Fluent slider: 4px rail, accent fill, 20px thumb with an inner dot that grows on hover and shrinks on press.</summary>
sealed class FluentSlider : Control
{
    readonly double _k;
    int _min, _max = 10, _value;
    bool _hover, _dragging;

    public event EventHandler? ValueChanged;

    public int Minimum { get => _min; set { _min = value; Invalidate(); } }
    public int Maximum { get => _max; set { _max = value; Invalidate(); } }

    public int Value
    {
        get => _value;
        set
        {
            value = Math.Clamp(value, _min, _max);
            if (_value == value) return;
            _value = value;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public FluentSlider(int dpi)
    {
        _k = dpi / 96.0;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Size = new Size(P(200), P(32));
        TabStop = true;
        AccessibleRole = AccessibleRole.Slider;
    }

    int P(int v) => (int)Math.Round(v * _k);
    float TrackL => P(10);
    float TrackR => Width - P(10);
    float ThumbX => _max == _min ? TrackL : TrackL + (float)(_value - _min) / (_max - _min) * (TrackR - TrackL);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        float cy = Height / 2f, rail = P(4), x = ThumbX;

        using (var path = Shapes.Round(new RectangleF(TrackL, cy - rail / 2, TrackR - TrackL, rail), rail / 2))
        using (var brush = new SolidBrush(Palette.SliderTrack))
            g.FillPath(brush, path);

        if (x - TrackL > 0.5f)
        {
            using var path = Shapes.Round(new RectangleF(TrackL, cy - rail / 2, Math.Max(rail, x - TrackL), rail), rail / 2);
            using var brush = new SolidBrush(Palette.Accent);
            g.FillPath(brush, path);
        }

        float outer = P(20);
        var outerRect = new RectangleF(x - outer / 2, cy - outer / 2, outer, outer);
        using (var brush = new SolidBrush(Palette.ThumbOuter)) g.FillEllipse(brush, outerRect);
        using (var pen = new Pen(Palette.ThumbBorder)) g.DrawEllipse(pen, outerRect);

        float inner = _dragging ? P(10) : _hover ? P(14) : P(12);
        using (var brush = new SolidBrush(Palette.Accent))
            g.FillEllipse(brush, x - inner / 2, cy - inner / 2, inner, inner);

        if (Focused && ShowFocusCues)
        {
            using var ring = new Pen(Palette.Fg, 1.5f);
            g.DrawEllipse(ring, RectangleF.Inflate(outerRect, P(3), P(3)));
        }
    }

    void SetFromX(int x)
    {
        float ratio = Math.Clamp((x - TrackL) / Math.Max(1f, TrackR - TrackL), 0f, 1f);
        Value = _min + (int)Math.Round(ratio * (_max - _min));
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        _dragging = true;
        SetFromX(e.X);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging) SetFromX(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (Focused) Value += e.Delta > 0 ? 1 : -1;     // only when focused, so page scrolling isn't hijacked
    }

    protected override bool IsInputKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End
        || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Left or Keys.Down: Value--; e.Handled = true; break;
            case Keys.Right or Keys.Up: Value++; e.Handled = true; break;
            case Keys.Home: Value = _min; e.Handled = true; break;
            case Keys.End: Value = _max; e.Handled = true; break;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
}

/// <summary>Fluent NumberBox: 4px rounded field, accent underline on focus, compact spin chevrons on hover/focus.</summary>
sealed class NumberBox : Control
{
    readonly double _k;
    readonly TextBox _text;
    readonly Font _chevronFont = Fonts.Icon(7f);
    readonly int _min, _max;
    int _value;
    int _hot;                // 1 = up chevron, -1 = down chevron
    bool _hover;

    public event EventHandler? ValueChanged;

    public int Value
    {
        get => _value;
        set => SetValue(value);
    }

    public NumberBox(int dpi, int min, int max, int value, string accessibleName)
    {
        _k = dpi / 96.0;
        _min = min;
        _max = max;
        _value = Math.Clamp(value, min, max);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        BackColor = Palette.Card;

        _text = new TextBox
        {
            BorderStyle = BorderStyle.None, Font = Fonts.Body(10.5f), Text = _value.ToString(),
            MaxLength = max.ToString().Length, ForeColor = Palette.Fg, BackColor = Palette.FieldFill,
            AccessibleName = accessibleName
        };
        Controls.Add(_text);
        Size = new Size(P(84), P(32));

        _text.GotFocus += (_, _) =>
        {
            _text.BackColor = Palette.FieldFocus;
            Invalidate();
            if (IsHandleCreated) BeginInvoke(() => _text.SelectAll());
        };
        _text.LostFocus += (_, _) =>
        {
            Commit();
            _text.BackColor = Palette.FieldFill;
            Invalidate();
        };
        _text.KeyPress += (_, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
        };
        _text.KeyDown += (_, e) =>
        {
            switch (e.KeyCode)
            {
                case Keys.Enter: Commit(); _text.SelectAll(); e.Handled = e.SuppressKeyPress = true; break;
                case Keys.Up: Step(1); e.Handled = true; break;
                case Keys.Down: Step(-1); e.Handled = true; break;
            }
        };
        _text.MouseWheel += (_, e) =>
        {
            if (!_text.Focused) return;
            Step(e.Delta > 0 ? 1 : -1);
            if (e is HandledMouseEventArgs handled) handled.Handled = true;
        };
        _text.MouseEnter += (_, _) => UpdateHover();
        _text.MouseLeave += (_, _) => UpdateHover();
    }

    int P(int v) => (int)Math.Round(v * _k);

    void LayoutText()
    {
        _text.Width = Math.Max(10, Width - P(10) - P(30));
        _text.Location = new Point(P(10), Math.Max(0, (Height - _text.Height) / 2));
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutText(); }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); LayoutText(); }

    void Commit()
    {
        if (int.TryParse(_text.Text, out int parsed)) SetValue(parsed);
        _text.Text = _value.ToString();
    }

    void SetValue(int value)
    {
        value = Math.Clamp(value, _min, _max);
        _text.Text = value.ToString();
        if (value == _value) return;
        _value = value;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    void Step(int direction)
    {
        Commit();
        SetValue(_value + direction);
        _text.Focus();
        _text.SelectAll();
    }

    void UpdateHover()
    {
        bool hover = ClientRectangle.Contains(PointToClient(Cursor.Position));
        if (hover == _hover) return;
        _hover = hover;
        if (!hover) _hot = 0;
        Invalidate();
    }

    bool ShowSpin => _hover || _text.Focused;

    int HitSpin(Point p) => !ShowSpin || p.X < Width - P(28) ? 0 : (p.Y < Height / 2 ? 1 : -1);

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); UpdateHover(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); UpdateHover(); }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int hot = HitSpin(e.Location);
        if (hot == _hot) return;
        _hot = hot;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        int hit = HitSpin(e.Location);
        if (hit != 0) Step(hit); else _text.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        bool focused = _text.Focused;

        using var path = Shapes.Round(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), P(4));
        using (var fill = new SolidBrush(focused ? Palette.FieldFocus : Palette.FieldFill)) g.FillPath(fill, path);
        using (var border = new Pen(Palette.FieldBorder)) g.DrawPath(border, path);

        // stronger bottom edge (accent + 2px when focused), following the corner curve
        var state = g.Save();
        g.SetClip(new Rectangle(0, Height - P(4), Width, P(4)));
        if (focused)
        {
            using var inset = Shapes.Round(new RectangleF(1f, 1f, Width - 2, Height - 2), P(4) - 0.5f);
            using var pen = new Pen(Palette.Accent, 2f);
            g.DrawPath(pen, inset);
        }
        else
        {
            using var pen = new Pen(Palette.FieldBottom, 1f);
            g.DrawPath(pen, path);
        }
        g.Restore(state);

        if (!ShowSpin) return;
        const TextFormatFlags center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        int x = Width - P(28);
        var up = new Rectangle(x, P(4), P(24), Height / 2 - P(4));
        var down = new Rectangle(x, Height / 2, P(24), Height / 2 - P(4));
        foreach (var (rect, hit) in new[] { (up, 1), (down, -1) })
        {
            if (_hot == hit)
            {
                using var hotPath = Shapes.Round(rect, P(3));
                using var hotBrush = new SolidBrush(Palette.Subtle);
                g.FillPath(hotBrush, hotPath);
            }
        }
        TextRenderer.DrawText(g, "\uE70E", _chevronFont, up, Palette.Muted, center);
        TextRenderer.DrawText(g, "\uE70D", _chevronFont, down, Palette.Muted, center);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _chevronFont.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>Page heading painted without GDI text padding, so it lines up with the card edge.</summary>
sealed class HeadingControl : Control
{
    readonly Font _titleFont = Fonts.Semibold(21f);
    readonly Font _subtitleFont = Fonts.Body(10.5f);
    readonly string _title, _subtitle;
    readonly int _gap;

    public HeadingControl(int dpi, string title, string subtitle)
    {
        _title = title;
        _subtitle = subtitle;
        _gap = (int)Math.Round(2 * dpi / 96.0);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Palette.Page;
        Dock = DockStyle.Fill;
        Height = _titleFont.Height + (string.IsNullOrEmpty(subtitle) ? 0 : _gap + _subtitleFont.Height);
        TabStop = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(e.Graphics, _title, _titleFont, new Rectangle(0, 0, Width, _titleFont.Height), Palette.Fg, flags);
        if (!string.IsNullOrEmpty(_subtitle))
        TextRenderer.DrawText(e.Graphics, _subtitle, _subtitleFont, new Rectangle(0, _titleFont.Height + _gap, Width, _subtitleFont.Height), Palette.Muted, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _titleFont.Dispose(); _subtitleFont.Dispose(); }
        base.Dispose(disposing);
    }
}

/// <summary>Group title above a set of cards (14px semibold, like Windows Settings).</summary>
sealed class SectionHeader : Control
{
    readonly Font _font = Fonts.Semibold(10.5f);

    public SectionHeader(int dpi, string text)
    {
        Text = text;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Palette.Page;
        Dock = DockStyle.Fill;
        Height = _font.Height;
        TabStop = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        TextRenderer.DrawText(e.Graphics, Text, _font, ClientRectangle, Palette.Fg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _font.Dispose();
        base.Dispose(disposing);
    }
}
