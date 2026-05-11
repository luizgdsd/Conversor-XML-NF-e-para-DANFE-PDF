namespace ConversorXmlNFeDanfePdf.UI;

public sealed class TutorialOverlayForm : Form
{
    private static readonly Color TransparentBack = Color.Fuchsia;
    private static readonly Color Primary = Color.FromArgb(0, 47, 108);
    private static readonly Color Accent = Color.FromArgb(245, 158, 11);
    private static readonly Color TextColor = Color.FromArgb(30, 41, 59);
    private static readonly Color Muted = Color.FromArgb(82, 95, 122);

    private readonly Form _owner;
    private readonly IReadOnlyList<TutorialStep> _steps;
    private readonly Panel _card = new();
    private readonly Label _eyebrow = new();
    private readonly Label _title = new();
    private readonly Label _body = new();
    private readonly Label _counter = new();
    private readonly ProgressBar _progress = new();
    private readonly Button _previous = new();
    private readonly Button _next = new();
    private readonly Button _close = new();
    private int _index;

    public TutorialOverlayForm(Form owner, IReadOnlyList<TutorialStep> steps)
    {
        _owner = owner;
        _steps = steps;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = TransparentBack;
        TransparencyKey = TransparentBack;
        TopMost = true;
        BuildCard();
        RefreshPosition();
        SetStep(0);
    }

    public void RefreshPosition()
    {
        if (_owner.WindowState == FormWindowState.Minimized)
            return;

        Bounds = _owner.Bounds;
        ResizeCard();
        PositionCard();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_steps.Count == 0)
            return;

        var target = CurrentTargetBounds();
        if (target.Width <= 0 || target.Height <= 0)
            return;

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var rect = Rectangle.Inflate(target, 5, 5);
        using var pen = new Pen(Accent, 4);
        using var softPen = new Pen(Color.FromArgb(255, 251, 191, 36), 1);
        e.Graphics.DrawRectangle(pen, rect);
        e.Graphics.DrawRectangle(softPen, Rectangle.Inflate(rect, 5, 5));
    }

    private void BuildCard()
    {
        _card.BackColor = Color.White;
        _card.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(_card);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 6,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        _card.Controls.Add(layout);

        _eyebrow.Dock = DockStyle.Fill;
        _eyebrow.Font = new Font("Segoe UI Semibold", 8F);
        _eyebrow.ForeColor = Muted;
        _eyebrow.Text = "TUTORIAL DO FLUXO";
        layout.Controls.Add(_eyebrow, 0, 0);

        _title.Dock = DockStyle.Fill;
        _title.Font = new Font("Segoe UI Semibold", 13F);
        _title.ForeColor = Primary;
        layout.Controls.Add(_title, 0, 1);

        _body.Dock = DockStyle.Fill;
        _body.Font = new Font("Segoe UI", 9.5F);
        _body.ForeColor = TextColor;
        layout.Controls.Add(_body, 0, 2);

        _counter.Dock = DockStyle.Fill;
        _counter.Font = new Font("Segoe UI", 8.5F);
        _counter.ForeColor = Muted;
        layout.Controls.Add(_counter, 0, 3);

        _progress.Dock = DockStyle.Fill;
        _progress.Margin = new Padding(0, 4, 0, 6);
        layout.Controls.Add(_progress, 0, 4);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        ConfigureButton(_close, "Fechar", false);
        ConfigureButton(_next, "Proximo", true);
        ConfigureButton(_previous, "Anterior", false);
        _close.Click += (_, _) => Close();
        _next.Click += (_, _) =>
        {
            if (_index >= _steps.Count - 1)
                Close();
            else
                SetStep(_index + 1);
        };
        _previous.Click += (_, _) => SetStep(Math.Max(_index - 1, 0));
        buttons.Controls.AddRange([_close, _next, _previous]);
        layout.Controls.Add(buttons, 0, 5);
    }

    private static void ConfigureButton(Button button, string text, bool primary)
    {
        button.Text = text;
        button.Width = 96;
        button.Height = 32;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = primary ? Primary : Color.FromArgb(199, 207, 219);
        button.BackColor = primary ? Primary : Color.White;
        button.ForeColor = primary ? Color.White : TextColor;
        button.Margin = new Padding(8, 5, 0, 5);
        button.Cursor = Cursors.Hand;
    }

    private void SetStep(int index)
    {
        if (_steps.Count == 0)
            return;

        _index = index;
        var step = _steps[_index];
        _title.Text = step.Title;
        _body.Text = step.Body;
        _counter.Text = $"Passo {_index + 1} de {_steps.Count}";
        _progress.Minimum = 0;
        _progress.Maximum = _steps.Count;
        _progress.Value = _index + 1;
        _previous.Enabled = _index > 0;
        _next.Text = _index == _steps.Count - 1 ? "Concluir" : "Proximo";

        PositionCard();
        Invalidate();
    }

    private void PositionCard()
    {
        if (_steps.Count == 0)
            return;

        ResizeCard();
        var target = CurrentTargetBounds();
        int x;
        int y;

        if (target.Width > ClientSize.Width * 0.55)
        {
            x = Math.Max(16, Math.Min(ClientSize.Width - _card.Width - 16, target.Left + (target.Width - _card.Width) / 2));
            y = target.Bottom + 14;
            if (y + _card.Height > ClientSize.Height - 16)
                y = target.Top - _card.Height - 14;
        }
        else
        {
            x = target.Right + 18;
            y = target.Top + Math.Max(0, (target.Height - _card.Height) / 2);
            if (x + _card.Width > ClientSize.Width - 16)
                x = target.Left - _card.Width - 18;
        }

        if (x < 16)
            x = Math.Max(16, (ClientSize.Width - _card.Width) / 2);
        if (y + _card.Height > ClientSize.Height - 16)
            y = ClientSize.Height - _card.Height - 16;
        if (y < 16)
            y = 16;

        _card.Location = new Point(x, y);
    }

    private void ResizeCard()
    {
        var width = Math.Min(440, Math.Max(300, ClientSize.Width - 32));
        var height = Math.Min(250, Math.Max(218, ClientSize.Height - 32));
        _card.Size = new Size(width, height);
    }

    private Rectangle CurrentTargetBounds()
    {
        var control = _steps[_index].Target;
        if (control.IsDisposed || !control.Visible)
            return new Rectangle(Width / 2 - 60, Height / 2 - 20, 120, 40);

        var screen = control.RectangleToScreen(control.ClientRectangle);
        return RectangleToClient(screen);
    }
}

public sealed record TutorialStep(Control Target, string Title, string Body);
