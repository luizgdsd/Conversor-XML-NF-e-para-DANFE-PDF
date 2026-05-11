namespace ConversorXmlNFeDanfePdf.UI;

public sealed class TutorialOverlayForm : Form
{
    private readonly Form _owner;
    private readonly IReadOnlyList<TutorialStep> _steps;
    private readonly Panel _card = new();
    private readonly Label _title = new();
    private readonly Label _body = new();
    private readonly Label _counter = new();
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
        BackColor = Color.Black;
        Opacity = 0.78;
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

        using var pen = new Pen(Color.FromArgb(255, 214, 10), 4);
        using var brush = new SolidBrush(Color.FromArgb(45, 255, 214, 10));
        var rect = Rectangle.Inflate(target, 6, 6);
        e.Graphics.FillRectangle(brush, rect);
        e.Graphics.DrawRectangle(pen, rect);
    }

    private void BuildCard()
    {
        _card.BackColor = Color.White;
        _card.BorderStyle = BorderStyle.FixedSingle;
        _card.Size = new Size(430, 190);
        Controls.Add(_card);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 4,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        _card.Controls.Add(layout);

        _title.Dock = DockStyle.Fill;
        _title.Font = new Font("Segoe UI Semibold", 12F);
        _title.ForeColor = Color.FromArgb(0, 47, 108);
        layout.Controls.Add(_title, 0, 0);

        _body.Dock = DockStyle.Fill;
        _body.Font = new Font("Segoe UI", 9F);
        _body.ForeColor = Color.FromArgb(30, 41, 59);
        layout.Controls.Add(_body, 0, 1);

        _counter.Dock = DockStyle.Fill;
        _counter.ForeColor = Color.FromArgb(82, 95, 122);
        layout.Controls.Add(_counter, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        ConfigureButton(_close, "Fechar");
        ConfigureButton(_next, "Proximo");
        ConfigureButton(_previous, "Anterior");
        _close.Click += (_, _) => Close();
        _next.Click += (_, _) => SetStep(Math.Min(_index + 1, _steps.Count - 1));
        _previous.Click += (_, _) => SetStep(Math.Max(_index - 1, 0));
        buttons.Controls.AddRange([_close, _next, _previous]);
        layout.Controls.Add(buttons, 0, 3);
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.Width = 90;
        button.Height = 30;
        button.FlatStyle = FlatStyle.Flat;
        button.Margin = new Padding(6, 4, 0, 4);
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
        _previous.Enabled = _index > 0;
        _next.Enabled = _index < _steps.Count - 1;
        PositionCard();
        Invalidate();
    }

    private void PositionCard()
    {
        if (_steps.Count == 0)
            return;

        ResizeCard();
        var target = CurrentTargetBounds();
        var x = target.Right + 18;
        var y = target.Top;

        if (x + _card.Width > ClientSize.Width - 16)
            x = target.Left - _card.Width - 18;
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
        var width = Math.Min(430, Math.Max(260, ClientSize.Width - 32));
        var height = Math.Min(210, Math.Max(176, ClientSize.Height - 32));
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
