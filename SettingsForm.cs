namespace HushLittlePC;

class SettingsForm : Form
{
    static readonly Color Muted = Color.FromArgb(110, 110, 110);
    static readonly Color Accent = Color.FromArgb(46, 160, 67);

    const int LogicalWidth = 480;     // client width at 100% DPI
    const int ButtonBarHeight = 68;   // logical px

    readonly TableLayoutPanel _content;
    readonly Panel _body;
    readonly Panel _bar;

    public SettingsForm(Settings s)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        Text = "HushLittlePC Settings";
        Icon = TrayApp.MakeIcon(true);
        BackColor = SystemColors.Control; // any uncovered edge matches the button bar, not white
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(LogicalWidth, 600); // real height is computed in OnLoad

        // ---- Inactivity ---------------------------------------------------------
        var minutes = new NumericUpDown
        {
            Minimum = 1, Maximum = 1440, Width = 64,
            Value = Math.Clamp(s.IdleMinutes, 1, 1440),
            Margin = new Padding(0, 0, 10, 0),
        };
        var slider = new TrackBar
        {
            Minimum = 1, Maximum = 120, TickStyle = TickStyle.None,
            Value = Math.Clamp(s.IdleMinutes, 1, 120),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 10, 0, 0),
        };
        slider.Scroll += (_, _) => minutes.Value = slider.Value;
        minutes.ValueChanged += (_, _) => slider.Value = Math.Min(slider.Maximum, (int)minutes.Value);

        var timeRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        timeRow.Controls.Add(Plain("Mute after", new Padding(0, 4, 10, 0)));
        timeRow.Controls.Add(minutes);
        timeRow.Controls.Add(Plain("minutes of no keyboard or mouse input", new Padding(0, 4, 0, 0)));

        var hint = new Label
        {
            Text = "Drag the slider up to 2 hours, or type any value up to 24 hours.",
            AutoSize = true, ForeColor = Muted, Margin = new Padding(0, 0, 0, 8),
        };

        // ---- Options ------------------------------------------------------------
        var enabled = Option("Enable idle muting", "Same as clicking the tray icon.", s.Enabled, out var cbEnabled);
        var unmute = Option("Unmute when I come back",
            "Only if HushLittlePC did the muting, so your own mute is never undone.",
            s.UnmuteOnActivity, out var cbUnmute);
        var skip = Option("Don't mute while audio is playing",
            "Handy for watching videos or listening to music without touching the PC.",
            s.SkipWhileAudioPlaying, out var cbSkip);
        var notify = Option("Show a notification when muting", null, s.ShowNotification, out var cbNotify);
        var startup = Option("Start with Windows", null, s.RunOnStartup, out var cbStartup);

        _content = new TableLayoutPanel
        {
            AutoSize = true, ColumnCount = 1, Dock = DockStyle.Top, BackColor = Color.White,
        };
        _content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var c in new Control[]
        {
            Heading("INACTIVITY", 0), timeRow, slider, hint,
            Heading("BEHAVIOUR", 20), enabled, unmute, skip,
            Heading("GENERAL", 8), notify, startup,
        })
            _content.Controls.Add(c);

        _body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(36, 32, 36, 24), BackColor = Color.White };
        _body.Controls.Add(_content);

        // ---- Button bar ---------------------------------------------------------
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(100, 34), Margin = new Padding(8, 0, 0, 0) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(100, 34), Margin = new Padding(0) };
        AcceptButton = ok;
        CancelButton = cancel;
        ok.Click += (_, _) =>
        {
            s.IdleMinutes = (int)minutes.Value;
            s.Enabled = cbEnabled.Checked;
            s.UnmuteOnActivity = cbUnmute.Checked;
            s.SkipWhileAudioPlaying = cbSkip.Checked;
            s.ShowNotification = cbNotify.Checked;
            s.RunOnStartup = cbStartup.Checked;
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(20, 17, 24, 17), WrapContents = false,
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        _bar = new Panel { Dock = DockStyle.Bottom, Height = ButtonBarHeight, BackColor = SystemColors.Control };
        _bar.Controls.Add(buttons);
        _bar.Paint += (_, e) => e.Graphics.DrawLine(SystemPens.ControlLight, 0, 0, _bar.Width, 0);

        // Fill first, edge-docked last (docked controls are laid out in reverse order).
        Controls.Add(_body);
        Controls.Add(_bar);

        ResumeLayout(true);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Everything is DPI-scaled by now, so size the window to its real content.
        int width = ClientSize.Width;
        int inner = width - _body.Padding.Horizontal;
        int contentHeight = _content.GetPreferredSize(new Size(inner, 0)).Height;
        ClientSize = new Size(width, _body.Padding.Vertical + contentHeight + _bar.Height);
    }

    static Label Plain(string text, Padding margin) => new() { Text = text, AutoSize = true, Margin = margin };

    static Label Heading(string text, int topGap) => new()
    {
        Text = text, AutoSize = true, ForeColor = Accent,
        Font = new Font("Segoe UI Semibold", 8.5F), Margin = new Padding(0, topGap, 0, 12),
    };

    static Control Option(string text, string? description, bool isChecked, out CheckBox box)
    {
        box = new CheckBox { Text = text, Checked = isChecked, AutoSize = true, Margin = new Padding(0) };
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false,
            Margin = new Padding(0, 0, 0, 16),
        };
        panel.Controls.Add(box);
        if (description != null)
            panel.Controls.Add(new Label
            {
                Text = description, AutoSize = true, ForeColor = Muted,
                MaximumSize = new Size(380, 0), Margin = new Padding(22, 3, 0, 0),
            });
        return panel;
    }
}
