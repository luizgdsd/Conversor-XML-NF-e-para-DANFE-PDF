using ConversorXmlNFeDanfePdf.Models;
using ConversorXmlNFeDanfePdf.Services;

namespace ConversorXmlNFeDanfePdf.UI;

public sealed class UpdateForm : Form
{
    private readonly AutoUpdateService _updateService;
    private readonly UpdateInfo _update;
    private readonly ProgressBar _progressBar = new();
    private readonly Label _statusLabel = new();
    private readonly Button _updateButton = new();
    private readonly Button _laterButton = new();

    public UpdateForm(AutoUpdateService updateService, UpdateInfo update)
    {
        _updateService = updateService;
        _update = update;

        Text = "Atualizacao disponivel";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 330);
        Font = new Font("Segoe UI", 9F);
        Icon = SystemIcons.Information;

        BuildLayout();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 5,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"Nova versao {_update.TagName} disponivel",
            Font = new Font("Segoe UI Semibold", 13F),
            ForeColor = Color.FromArgb(0, 47, 108)
        }, 0, 0);

        var notes = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = string.IsNullOrWhiteSpace(_update.ReleaseNotes)
                ? "Sem notas de versao informadas."
                : _update.ReleaseNotes
        };
        root.Controls.Add(notes, 0, 1);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Text = $"Versao atual: {_update.CurrentVersion} | Nova versao: {_update.LatestVersion}";
        root.Controls.Add(_statusLabel, 0, 2);

        _progressBar.Dock = DockStyle.Fill;
        root.Controls.Add(_progressBar, 0, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        _updateButton.Text = "Atualizar agora";
        _updateButton.Width = 130;
        _updateButton.Height = 32;
        _updateButton.Click += async (_, _) => await UpdateNowAsync();
        _laterButton.Text = "Depois";
        _laterButton.Width = 90;
        _laterButton.Height = 32;
        _laterButton.Click += (_, _) => Close();
        buttons.Controls.AddRange([_updateButton, _laterButton]);
        root.Controls.Add(buttons, 0, 4);
    }

    private async Task UpdateNowAsync()
    {
        _updateButton.Enabled = false;
        _laterButton.Enabled = false;
        _statusLabel.Text = "Baixando atualizacao...";

        try
        {
            var progress = new Progress<int>(value => _progressBar.Value = Math.Clamp(value, 0, 100));
            var installer = await _updateService.DownloadInstallerAsync(_update, progress);
            _statusLabel.Text = "Instalador baixado. Reiniciando para aplicar atualizacao...";
            _updateService.StartInstaller(installer);
            Application.Exit();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Falha ao atualizar.";
            MessageBox.Show(ex.Message, "Atualizacao", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _updateButton.Enabled = true;
            _laterButton.Enabled = true;
        }
    }
}
