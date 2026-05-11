using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using ConversorXmlNFeDanfePdf.Models;
using ConversorXmlNFeDanfePdf.Services;

namespace ConversorXmlNFeDanfePdf.UI;

public sealed class MainForm : Form
{
    private static readonly Color PageBack = Color.FromArgb(244, 247, 251);
    private static readonly Color PanelBack = Color.White;
    private static readonly Color Primary = Color.FromArgb(0, 47, 108);
    private static readonly Color Muted = Color.FromArgb(82, 95, 122);
    private static readonly Color Border = Color.FromArgb(199, 207, 219);

    private readonly TextBox _outputText = new();
    private readonly Button _loadXmlButton = new();
    private readonly Button _outputButton = new();
    private readonly Button _clearButton = new();
    private readonly Button _convertButton = new();
    private readonly Button _openOutputButton = new();
    private readonly Button _exportReportButton = new();
    private readonly Button _checkUpdateButton = new();
    private readonly CheckBox _overwriteCheck = new();
    private readonly CheckBox _unifiedPdfCheck = new();
    private readonly ComboBox _existingActionCombo = new();
    private readonly ProgressBar _progressBar = new();
    private readonly DataGridView _grid = new();
    private readonly Label _summaryLabel = new();
    private readonly Label _dropHintLabel = new();
    private readonly NotifyIcon _trayIcon = new();
    private readonly ContextMenuStrip _trayMenu = new();
    private readonly BindingList<ProcessingResult> _rows = [];
    private readonly List<string> _xmlFiles = [];
    private readonly List<string> _temporaryFolders = [];
    private readonly NFeBatchProcessor _processor = new();
    private readonly ProcessingReportService _reportService = new();
    private readonly ArchiveXmlExtractorService _archiveExtractor = new();
    private readonly XmlDocumentClassifier _xmlClassifier = new();
    private readonly PdfMergeService _pdfMergeService = new();
    private readonly AutoUpdateService _autoUpdateService = new();
    private readonly System.Windows.Forms.Timer _updateTimer = new();
    private List<ProcessingResult> _lastResults = [];
    private bool _isCheckingForUpdate;
    private bool _isUpdateWindowOpen;

    public MainForm()
    {
        Text = $"Conversor XML NF-e para DANFE PDF v{AppVersionText}";
        MinimumSize = new Size(940, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BackColor = PageBack;
        AllowDrop = true;
        Icon = LoadAppIcon();

        BuildLayout();
        ConfigureGrid();
        ConfigureTrayIcon();
        WireEvents();
        ConfigureAutoUpdate();
        UpdateSummary();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(22, 18, 22, 16),
            BackColor = PageBack
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        Controls.Add(root);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = Padding.Empty };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        var title = new Label
        {
            Text = "Conversor XML NF-e para DANFE PDF",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 16F),
            ForeColor = Primary,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var brand = new Label
        {
            Text = "Um sistema desenvolvido por Gugu Soluções",
            Dock = DockStyle.Fill,
            Font = new Font("Comic Sans MS", 10F, FontStyle.Bold),
            ForeColor = Color.Blue,
            TextAlign = ContentAlignment.MiddleRight
        };
        var version = new Label
        {
            Text = $"v{AppVersionText}",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Muted,
            TextAlign = ContentAlignment.MiddleCenter
        };
        header.Controls.Add(title, 0, 0);
        header.Controls.Add(version, 1, 0);
        header.Controls.Add(brand, 2, 0);
        root.Controls.Add(header, 0, 0);

        _dropHintLabel.Text = "Arraste XMLs, pastas ou compactados aqui; ou clique em Carregar arquivos";
        _dropHintLabel.Dock = DockStyle.Fill;
        _dropHintLabel.TextAlign = ContentAlignment.MiddleCenter;
        _dropHintLabel.Font = new Font("Segoe UI Semibold", 10F);
        _dropHintLabel.ForeColor = Primary;
        _dropHintLabel.BackColor = PanelBack;
        _dropHintLabel.BorderStyle = BorderStyle.FixedSingle;
        _dropHintLabel.Margin = new Padding(0, 0, 0, 12);
        _dropHintLabel.AllowDrop = true;
        root.Controls.Add(_dropHintLabel, 0, 1);

        var outputPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = Padding.Empty };
        outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        outputPanel.Controls.Add(new Label
        {
            Text = "Pasta de saida",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Muted
        }, 0, 0);
        ConfigureText(_outputText);
        _outputText.PlaceholderText = "Escolha onde os PDFs serao salvos";
        outputPanel.Controls.Add(_outputText, 1, 0);
        ConfigureButton(_outputButton, "Selecionar");
        _outputButton.Dock = DockStyle.Fill;
        outputPanel.Controls.Add(_outputButton, 2, 0);
        root.Controls.Add(outputPanel, 0, 2);

        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, Margin = Padding.Empty };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        ConfigureButton(_loadXmlButton, "Carregar arquivos");
        ConfigureButton(_clearButton, "Limpar lista");
        _overwriteCheck.Text = "Sobrescrever PDFs";
        _overwriteCheck.AutoSize = true;
        _overwriteCheck.Dock = DockStyle.Fill;
        _overwriteCheck.ForeColor = Color.FromArgb(30, 41, 59);
        _unifiedPdfCheck.Text = "Gerar PDF unico";
        _unifiedPdfCheck.AutoSize = true;
        _unifiedPdfCheck.Dock = DockStyle.Fill;
        _unifiedPdfCheck.ForeColor = Color.FromArgb(30, 41, 59);
        _existingActionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _existingActionCombo.Dock = DockStyle.Fill;
        _existingActionCombo.Margin = new Padding(6, 9, 12, 9);
        _existingActionCombo.Items.AddRange(["Gerar com sufixo incremental", "Ignorar PDF existente", "Sobrescrever PDF existente"]);
        _existingActionCombo.SelectedIndex = 0;
        ConfigureButton(_convertButton, "Converter XMLs");
        _convertButton.BackColor = Primary;
        _convertButton.ForeColor = Color.White;
        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Margin = new Padding(12, 15, 0, 15);
        actions.Controls.Add(_loadXmlButton, 0, 0);
        actions.Controls.Add(_clearButton, 1, 0);
        actions.Controls.Add(_existingActionCombo, 2, 0);
        actions.Controls.Add(_convertButton, 3, 0);
        actions.Controls.Add(_unifiedPdfCheck, 4, 0);
        actions.Controls.Add(_overwriteCheck, 5, 0);
        actions.Controls.Add(_progressBar, 6, 0);
        root.Controls.Add(actions, 0, 3);

        _grid.Dock = DockStyle.Fill;
        _grid.Margin = new Padding(0, 4, 0, 8);
        root.Controls.Add(_grid, 0, 4);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Margin = Padding.Empty };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
        _summaryLabel.Dock = DockStyle.Fill;
        _summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        _summaryLabel.ForeColor = Muted;
        ConfigureButton(_openOutputButton, "Abrir saida");
        ConfigureButton(_exportReportButton, "Exportar");
        ConfigureButton(_checkUpdateButton, "Atualizar");
        _openOutputButton.Dock = DockStyle.Fill;
        _exportReportButton.Dock = DockStyle.Fill;
        _checkUpdateButton.Dock = DockStyle.Fill;
        footer.Controls.Add(_summaryLabel, 0, 0);
        footer.Controls.Add(_checkUpdateButton, 1, 0);
        footer.Controls.Add(_openOutputButton, 2, 0);
        footer.Controls.Add(_exportReportButton, 3, 0);
        root.Controls.Add(footer, 0, 5);
    }

    private void ConfigureGrid()
    {
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.BackgroundColor = PanelBack;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.GridColor = Color.FromArgb(226, 232, 240);
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _grid.DataSource = _rows;
        _grid.AllowDrop = true;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 241, 248);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
        _grid.ColumnHeadersHeight = 32;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 253);
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        AddGridColumn("Arquivo XML", nameof(ProcessingResult.XmlFile), 28);
        AddGridColumn("Chave NF-e", nameof(ProcessingResult.Key), 18);
        AddGridColumn("Numero", nameof(ProcessingResult.Number), 7);
        AddGridColumn("Emitente", nameof(ProcessingResult.Issuer), 16);
        AddGridColumn("Destinatario", nameof(ProcessingResult.Recipient), 16);
        AddGridColumn("Status", nameof(ProcessingResult.Status), 8);
        AddGridColumn("Mensagem / erro", nameof(ProcessingResult.Message), 20);
    }

    private void WireEvents()
    {
        _loadXmlButton.Click += (_, _) => LoadXmlFilesByClick();
        _clearButton.Click += (_, _) => ClearLoadedFiles();
        _outputButton.Click += (_, _) => SelectFolder(_outputText);
        _overwriteCheck.CheckedChanged += (_, _) =>
        {
            if (_overwriteCheck.Checked)
                _existingActionCombo.SelectedIndex = 2;
        };
        _existingActionCombo.SelectedIndexChanged += (_, _) =>
        {
            _overwriteCheck.Checked = _existingActionCombo.SelectedIndex == 2;
        };
        _convertButton.Click += async (_, _) => await ConvertAsync();
        _checkUpdateButton.Click += async (_, _) => await CheckForUpdatesAsync(showWhenUpdated: true);
        _openOutputButton.Click += (_, _) => OpenOutputFolder();
        _exportReportButton.Click += (_, _) => ExportReport();

        DragEnter += HandleDragEnter;
        DragDrop += HandleDragDrop;
        _grid.DragEnter += HandleDragEnter;
        _grid.DragDrop += HandleDragDrop;
        _dropHintLabel.DragEnter += HandleDragEnter;
        _dropHintLabel.DragDrop += HandleDragDrop;
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
                HideToTray();
        };
        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing && _trayIcon.Visible)
            {
                e.Cancel = true;
                HideToTray();
            }
        };
    }

    private void ConfigureAutoUpdate()
    {
        _updateTimer.Interval = 5 * 60 * 1000;
        _updateTimer.Tick += async (_, _) => await CheckForUpdatesAsync();
        _updateTimer.Start();
        Shown += async (_, _) => await CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync(bool showWhenUpdated = false)
    {
        if (_isCheckingForUpdate || _isUpdateWindowOpen)
            return;

        _isCheckingForUpdate = true;
        try
        {
            var update = await _autoUpdateService.CheckForUpdateAsync();
            if (update?.IsUpdateAvailable != true)
            {
                if (showWhenUpdated)
                    MessageBox.Show($"Voce ja esta usando a versao mais recente: v{AppVersionText}.", "Atualizacao", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _isUpdateWindowOpen = true;
            BeginInvoke(new Action(() =>
            {
                RestoreFromTray();
                using var form = new UpdateForm(_autoUpdateService, update);
                form.ShowDialog(this);
                _isUpdateWindowOpen = false;
            }));
        }
        catch
        {
            // Falha silenciosa: a verificacao roda periodicamente e nao deve atrapalhar o uso.
        }
        finally
        {
            _isCheckingForUpdate = false;
        }
    }

    private void ConfigureTrayIcon()
    {
        _trayMenu.Items.Add("Abrir", null, (_, _) => RestoreFromTray());
        _trayMenu.Items.Add("Verificar atualizacao", null, async (_, _) => await CheckForUpdatesAsync(showWhenUpdated: true));
        _trayMenu.Items.Add("Sair", null, (_, _) => ExitApplication());

        _trayIcon.Icon = Icon ?? LoadAppIcon();
        _trayIcon.Text = $"Conversor XML NF-e para DANFE PDF v{AppVersionText}";
        _trayIcon.ContextMenuStrip = _trayMenu;
        _trayIcon.Visible = true;
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
        _trayIcon.ShowBalloonTip(
            2500,
            "Conversor XML NF-e para DANFE PDF",
            "O sistema continua rodando na bandeja.",
            ToolTipIcon.Info);
    }

    private void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }

    private void LoadXmlFilesByClick()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Selecione XMLs ou arquivos compactados",
            Filter = "XMLs e compactados|*.xml;*.zip;*.rar;*.7z;*.tar;*.gz;*.tgz;*.bz2;*.tbz2;*.xz;*.txz|Arquivos XML (*.xml)|*.xml|Compactados|*.zip;*.rar;*.7z;*.tar;*.gz;*.tgz;*.bz2;*.tbz2;*.xz;*.txz|Todos os arquivos (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
            AddInputPaths(dialog.FileNames);
    }

    private void HandleDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void HandleDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
            AddInputPaths(paths);
    }

    private IEnumerable<string> ExpandInputPaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (File.Exists(path) && path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                yield return path;
            }
            else if (File.Exists(path) && _archiveExtractor.IsSupportedArchive(path))
            {
                ExtractedXmlFiles extracted;
                try
                {
                    extracted = _archiveExtractor.ExtractXmlFiles(path);
                }
                catch (Exception ex)
                {
                    _rows.Add(new ProcessingResult
                    {
                        XmlFile = path,
                        Status = "Erro",
                        Message = $"Nao foi possivel extrair o compactado: {ex.Message}"
                    });
                    continue;
                }

                _temporaryFolders.Add(extracted.TempFolder);
                foreach (var xml in extracted.XmlFiles)
                    yield return xml;
            }
            else if (Directory.Exists(path))
            {
                foreach (var xml in Directory.EnumerateFiles(path, "*.xml", SearchOption.AllDirectories))
                    yield return xml;

                foreach (var archive in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Where(_archiveExtractor.IsSupportedArchive))
                {
                    ExtractedXmlFiles extracted;
                    try
                    {
                        extracted = _archiveExtractor.ExtractXmlFiles(archive);
                    }
                    catch (Exception ex)
                    {
                        _rows.Add(new ProcessingResult
                        {
                            XmlFile = archive,
                            Status = "Erro",
                            Message = $"Nao foi possivel extrair o compactado: {ex.Message}"
                        });
                        continue;
                    }

                    _temporaryFolders.Add(extracted.TempFolder);
                    foreach (var xml in extracted.XmlFiles)
                        yield return xml;
                }
            }
        }
    }

    private void AddInputPaths(IEnumerable<string> paths)
    {
        AddXmlFiles(ExpandInputPaths(paths));
    }

    private void AddXmlFiles(IEnumerable<string> files)
    {
        var added = 0;
        var ignored = 0;
        foreach (var file in files.Select(Path.GetFullPath).Where(File.Exists))
        {
            if (!file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                continue;
            if (_xmlFiles.Contains(file, StringComparer.OrdinalIgnoreCase))
                continue;

            var classification = _xmlClassifier.Classify(file);
            if (!classification.CanGenerateDanfe)
            {
                _rows.Add(new ProcessingResult
                {
                    XmlFile = file,
                    Status = "Ignorado",
                    Message = classification.Message
                });
                ignored++;
                continue;
            }

            _xmlFiles.Add(file);
            _rows.Add(new ProcessingResult
            {
                XmlFile = file,
                Status = "Carregado",
                Message = "Aguardando conversao."
            });
            added++;
        }

        var prefix = added == 0
            ? "Nenhuma NF-e nova foi carregada."
            : $"{added} NF-e(s) carregada(s).";
        if (ignored > 0)
            prefix += $" {ignored} XML(s) ignorado(s).";
        UpdateSummary(prefix);
    }

    private void ClearLoadedFiles()
    {
        _xmlFiles.Clear();
        _rows.Clear();
        _lastResults = [];
        CleanupTemporaryFolders();
        _progressBar.Value = 0;
        UpdateSummary("Lista limpa.");
    }

    private async Task ConvertAsync()
    {
        if (_xmlFiles.Count == 0)
        {
            MessageBox.Show("Carregue um ou mais arquivos XML antes de converter.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_outputText.Text))
        {
            MessageBox.Show("Escolha a pasta de saida dos PDFs.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var output = _outputText.Text;
        var effectiveOutput = _unifiedPdfCheck.Checked
            ? Path.Combine(Path.GetTempPath(), "ConversorXmlNFeDanfePdf", "UnifiedWork", Guid.NewGuid().ToString("N"))
            : output;
        var options = new ProcessingOptions
        {
            OutputFolder = effectiveOutput,
            GenerateUnifiedPdf = _unifiedPdfCheck.Checked,
            ExistingPdfAction = SelectedExistingAction()
        };

        _rows.Clear();
        _lastResults = [];
        _progressBar.Value = 0;
        _summaryLabel.Text = "Processando XMLs...";
        ToggleUi(false);

        try
        {
            var resultProgress = new Progress<ProcessingResult>(result => _rows.Add(result));
            var percentProgress = new Progress<int>(percent => _progressBar.Value = Math.Clamp(percent, 0, 100));
            _lastResults = (await _processor.ProcessFilesAsync(_xmlFiles.ToList(), options, resultProgress, percentProgress)).ToList();
            var unifiedPdfPath = "";
            if (options.GenerateUnifiedPdf)
            {
                var generatedPdfs = _lastResults
                    .Where(x => x.Status is "Gerado" or "Sobrescrito")
                    .Select(x => x.PdfPath)
                    .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    .ToList();

                if (generatedPdfs.Count > 0)
                {
                    unifiedPdfPath = _pdfMergeService.Merge(generatedPdfs, output);
                    foreach (var item in _lastResults.Where(x => x.Status is "Gerado" or "Sobrescrito"))
                    {
                        item.Status = "Unificado";
                        item.PdfPath = "";
                        item.Message = "Incluido no PDF unico. PDF individual nao foi salvo.";
                    }
                    var unifiedResult = new ProcessingResult
                    {
                        XmlFile = "PDF unico",
                        Status = "Unificado",
                        PdfPath = unifiedPdfPath,
                        Message = $"Arquivo unificado gerado: {unifiedPdfPath}"
                    };
                    _lastResults.Add(unifiedResult);
                    _rows.Add(unifiedResult);
                }

                TryDeleteFolder(effectiveOutput);
            }
            _reportService.SaveTxtSummary(output, _lastResults);
            var generated = options.GenerateUnifiedPdf
                ? _lastResults.Count(x => x.XmlFile == "PDF unico")
                : _lastResults.Count(x => x.Status is "Gerado" or "Sobrescrito");
            var ignored = _lastResults.Count(x => x.Status == "Ignorado");
            var errors = _lastResults.Count(x => x.Status == "Erro");
            var unifiedInfo = string.IsNullOrWhiteSpace(unifiedPdfPath) ? "" : $" | Unificado: {Path.GetFileName(unifiedPdfPath)}";
            _summaryLabel.Text = $"Concluido. XMLs: {_xmlFiles.Count} | PDFs: {generated} | Ignorados: {ignored} | Erros: {errors}{unifiedInfo} | Saida: {output}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            _summaryLabel.Text = "Processamento interrompido por erro.";
        }
        finally
        {
            ToggleUi(true);
        }
    }

    private ExistingPdfAction SelectedExistingAction() => _existingActionCombo.SelectedIndex switch
    {
        1 => ExistingPdfAction.Ignore,
        2 => ExistingPdfAction.Overwrite,
        _ => ExistingPdfAction.IncrementSuffix
    };

    private void ExportReport()
    {
        if (_lastResults.Count == 0)
        {
            MessageBox.Show("Nenhum processamento disponivel para exportar.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var csv = _reportService.SaveCsv(_outputText.Text, _lastResults);
        MessageBox.Show($"Relatorio exportado:\n{csv}", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenOutputFolder()
    {
        if (string.IsNullOrWhiteSpace(_outputText.Text))
        {
            MessageBox.Show("Escolha uma pasta de saida primeiro.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Directory.CreateDirectory(_outputText.Text);
        Process.Start(new ProcessStartInfo(_outputText.Text) { UseShellExecute = true });
    }

    private static void SelectFolder(TextBox target)
    {
        using var dialog = new FolderBrowserDialog { UseDescriptionForTitle = true, Description = "Selecione a pasta de saida" };
        if (Directory.Exists(target.Text))
            dialog.SelectedPath = target.Text;
        if (dialog.ShowDialog() == DialogResult.OK)
            target.Text = dialog.SelectedPath;
    }

    private void ToggleUi(bool enabled)
    {
        _loadXmlButton.Enabled = enabled;
        _clearButton.Enabled = enabled;
        _outputButton.Enabled = enabled;
        _convertButton.Enabled = enabled;
        _checkUpdateButton.Enabled = enabled;
        _overwriteCheck.Enabled = enabled;
        _unifiedPdfCheck.Enabled = enabled;
        _existingActionCombo.Enabled = enabled;
    }

    private void UpdateSummary(string? prefix = null)
    {
        var text = $"{_xmlFiles.Count} XML(s) carregado(s).";
        if (!string.IsNullOrWhiteSpace(prefix))
            text = $"{prefix} {text}";
        _summaryLabel.Text = text;
    }

    private void CleanupTemporaryFolders()
    {
        foreach (var folder in _temporaryFolders.ToList())
        {
            try
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);
            }
            catch
            {
            }
        }

        _temporaryFolders.Clear();
    }

    private static void ConfigureText(TextBox textBox)
    {
        textBox.Dock = DockStyle.Fill;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Margin = new Padding(0, 8, 10, 8);
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.BackColor = PanelBack;
        button.ForeColor = Color.FromArgb(23, 43, 77);
        button.Margin = new Padding(4, 8, 8, 8);
        button.Height = 32;
        button.Dock = DockStyle.Fill;
        button.Cursor = Cursors.Hand;
    }

    private void AddGridColumn(string title, string property, float fillWeight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = title,
            DataPropertyName = property,
            FillWeight = fillWeight,
            MinimumWidth = title == "Numero" ? 70 : 100,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
    }

    private static Icon LoadAppIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "barcode-scanner.ico");
        return File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;
    }

    private static string AppVersionText
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
            return $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
        }
    }

    private static void TryDeleteFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
        catch
        {
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _trayMenu.Dispose();
            _updateTimer.Dispose();
            CleanupTemporaryFolders();
        }

        base.Dispose(disposing);
    }
}
