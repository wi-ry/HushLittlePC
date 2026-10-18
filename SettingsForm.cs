namespace HushLittlePC;

/// <summary>
/// Layout follows the Windows UX guidelines: sibling controls grouped in group boxes,
/// standard-sized OK/Cancel right-aligned in that order, positively worded check boxes,
/// a numeric box linked to a slider, tooltips for extra detail, and one consistent
/// spacing rhythm (all values below are logical px at 100% DPI and scale automatically).
/// </summary>
class SettingsForm : Form
{
    const int LogicalWidth = 420;
    const int PageMargin = 20;      // outer margin (Win32 minimum is ~11; a bit more room on purpose)
    const int GroupGap = 16;        // between group boxes
    const int BarHeight = 52;       // command bar
    static readonly Size ButtonSize = new(104, 26);

    readonly ToolTip _tips = new() { InitialDelay = 400, ShowAlways = true };
    readonly TableLayoutPanel _root;
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
        BackColor = SystemColors.Control;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(LogicalWidth, 600); // real height is computed in OnLoad

        // ---- Group 1: idle time -------------------------------------------------
        var minutes = new NumericUpDown
        {
            Minimum = Settings.MinIdleMinutes, Maximum = Settings.MaxIdleMinutes, Width = 60,
            Value = Math.Clamp(s.IdleMinutes, Settings.MinIdleMinutes, Settings.MaxIdleMinutes),
            Margin = new Padding(0, 0, 8, 0),
        };
        var slider = new TrackBar
        {
            Minimum = 1, Maximum = 120, TickStyle = TickStyle.None,
            Value = Math.Clamp(s.IdleMinutes, 1, 120),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 4, 0, 4),
        };
        slider.Scroll += (_, _) => minutes.Value = slider.Value;
        minutes.ValueChanged += (_, _) => slider.Value = Math.Min(slider.Maximum, (int)minutes.Value);
        _tips.SetToolTip(minutes, "Inactivity means no keyboard or mouse input. Type up to 1440 (24 hours).");
        _tips.SetToolTip(slider, "Drag to set up to 2 hours.");

        var timeRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 4, 0, 4) };
        // Labels draw their text a few px inset; the negative left margin lines the text up with check box glyphs.
        timeRow.Controls.Add(new Label { Text = "Mute after", AutoSize = true, Margin = new Padding(-3, 4, 8, 0) });
        timeRow.Controls.Add(minutes);
        timeRow.Controls.Add(new Label { Text = "minutes of inactivity", AutoSize = true, Margin = new Padding(0, 4, 0, 0) });

        // ---- Group 2 & 3: options ----------------------------------------------
        var cbEnabled = Check("Enable HushLittlePC", s.Enabled,
            "Turns all muting on or off. You can also click the tray icon.");
        var cbUnmute = Check("Unmute when I come back (only if HushLittlePC muted it)", s.UnmuteOnActivity,
            "A mute you set yourself is never undone.");
        var cbSkip = Check("Stay unmuted while audio is playing", s.SkipWhileAudioPlaying,
            "Handy for videos or music when you aren't touching the keyboard or mouse.");
        var cbDevice = Check("Mute when the audio output changes", s.MuteOnDeviceChange,
            "For example when Bluetooth headphones connect or Windows switches to your speakers. The new device is muted so nothing plays at a surprising volume.");
        var cbNotify = Check("Show a notification when audio is muted", s.ShowNotification,
            "A Windows notification with an Unmute button each time HushLittlePC mutes.");
        var cbStartup = Check("Start HushLittlePC with Windows", s.RunOnStartup,
            "Adds HushLittlePC to your startup apps (current user only).");

        _root = new TableLayoutPanel
        {
            AutoSize = true, ColumnCount = 1, Dock = DockStyle.Top, BackColor = Color.White,
        };
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.Controls.Add(Group("Idle time", timeRow, slider));
        _root.Controls.Add(Group("Behaviour", cbEnabled, cbUnmute, cbSkip, cbDevice));
        _root.Controls.Add(Group("General", cbNotify, cbStartup));

        _body = new Panel
        {
            Dock = DockStyle.Fill, BackColor = Color.White,
            Padding = new Padding(PageMargin, PageMargin, PageMargin, PageMargin - GroupGap),
        };
        _body.Controls.Add(_root);

        // ---- Command bar: Save changes then Cancel, right-aligned ---------------
        var save = new Button
        {
            Text = "Save changes", DialogResult = DialogResult.OK,
            Size = ButtonSize, Margin = new Padding(0), Enabled = false
        };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = ButtonSize, Margin = new Padding(8, 0, 0, 0) };
        AcceptButton = save;
        CancelButton = cancel;
        save.Click += (_, _) =>
        {
            s.IdleMinutes = (int)minutes.Value;
            s.Enabled = cbEnabled.Checked;
            s.UnmuteOnActivity = cbUnmute.Checked;
            s.SkipWhileAudioPlaying = cbSkip.Checked;
            s.MuteOnDeviceChange = cbDevice.Checked;
            s.ShowNotification = cbNotify.Checked;
            s.RunOnStartup = cbStartup.Checked;
        };

        void UpdateSaveButton() =>
            save.Enabled =
                (int)minutes.Value != s.IdleMinutes ||
                cbEnabled.Checked != s.Enabled ||
                cbUnmute.Checked != s.UnmuteOnActivity ||
                cbSkip.Checked != s.SkipWhileAudioPlaying ||
                cbDevice.Checked != s.MuteOnDeviceChange ||
                cbNotify.Checked != s.ShowNotification ||
                cbStartup.Checked != s.RunOnStartup;

        minutes.ValueChanged += (_, _) => UpdateSaveButton();
        slider.ValueChanged += (_, _) => UpdateSaveButton();
        cbEnabled.CheckedChanged += (_, _) => UpdateSaveButton();
        cbUnmute.CheckedChanged += (_, _) => UpdateSaveButton();
        cbSkip.CheckedChanged += (_, _) => UpdateSaveButton();
        cbDevice.CheckedChanged += (_, _) => UpdateSaveButton();
        cbNotify.CheckedChanged += (_, _) => UpdateSaveButton();
        cbStartup.CheckedChanged += (_, _) => UpdateSaveButton();

        FormClosing += (_, e) =>
        {
            if (DialogResult == DialogResult.OK || !save.Enabled) return;

            var result = MessageBox.Show(
                this,
                "You have unsaved changes. If you close now, your changes will be lost.\n\nClose without saving?",
                "Unsaved changes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            e.Cancel = result != DialogResult.Yes;
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false,
            Padding = new Padding(PageMargin, (BarHeight - ButtonSize.Height) / 2, PageMargin, 0),
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);

        _bar = new Panel { Dock = DockStyle.Bottom, Height = BarHeight, BackColor = SystemColors.Control };
        _bar.Controls.Add(buttons);
        _bar.Paint += (_, e) => e.Graphics.DrawLine(SystemPens.ControlLight, 0, 0, _bar.Width, 0);

        // Fill first, edge-docked last (docked controls are laid out in reverse order).
        Controls.Add(_body);
        Controls.Add(_bar);

        ResumeLayout(true);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Everything is DPI-scaled by now, so size the window to its real content.
        int width = ClientSize.Width;
        int inner = width - _body.Padding.Horizontal;
        int contentHeight = _root.GetPreferredSize(new Size(inner, 0)).Height;
        ClientSize = new Size(width, _body.Padding.Vertical + contentHeight + _bar.Height);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tips.Dispose();
        base.Dispose(disposing);
    }

    CheckBox Check(string text, bool value, string tip)
    {
        var box = new CheckBox { Text = text, Checked = value, AutoSize = true, Margin = new Padding(0, 4, 0, 4) };
        _tips.SetToolTip(box, tip);
        return box;
    }

    static GroupBox Group(string caption, params Control[] rows)
    {
        var table = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Top };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var row in rows) table.Controls.Add(row);

        var box = new GroupBox
        {
            Text = caption, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 4, 12, 8),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 0, 0, GroupGap),
        };
        box.Controls.Add(table);
        return box;
    }
}
