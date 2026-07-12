using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using TextCollectionBundleEditor.Models;
using TextCollectionBundleEditor.Services;

namespace TextCollectionBundleEditor;

public sealed class MainForm : Form
{
    private enum InterfaceMode
    {
        Table,
        Editor
    }

    private readonly BundleEditorService _service = new();
    private readonly Dictionary<string, List<TranslationEntry>> _collections =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly BindingSource _source = new();

    private readonly Button _openButton = new();
    private readonly Button _saveButton = new();
    private readonly Button _exportCsvButton = new();
    private readonly Button _importCsvButton = new();
    private readonly Button _nextEmptyButton = new();
    private readonly Button _copyOriginalButton = new();

    private readonly ComboBox _collectionBox = new();
    private readonly ComboBox _filterBox = new();
    private readonly TextBox _searchBox = new();

    private readonly Panel _contentHost = new();
    private readonly DataGridView _grid = new();

    private readonly TableLayoutPanel _editorView = new();
    private readonly ListBox _entryList = new();
    private readonly TextBox _originalBox = new();
    private readonly TextBox _translationBox = new();
    private readonly TextBox _idBox = new();
    private readonly Button _previousButton = new();
    private readonly Button _nextButton = new();
    private readonly Button _copyOriginalEditorButton = new();

    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly ToolStripProgressBar _progress = new();

    private ToolStripMenuItem? _tableModeMenuItem;
    private ToolStripMenuItem? _editorModeMenuItem;

    private bool _dirty;
    private bool _switchingCollection;
    private bool _loadingEditorSelection;
    private InterfaceMode _interfaceMode = InterfaceMode.Editor;

    private List<TranslationEntry> CurrentEntries =>
        _collectionBox.SelectedItem is string name &&
        _collections.TryGetValue(name, out List<TranslationEntry>? list)
            ? list
            : [];

    public MainForm()
    {
        Text = "TextCollection Bundle Editor 2.2.1";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1450, 820);
        MinimumSize = new Size(1050, 620);
        KeyPreview = true;

        BuildInterface();
        SwitchInterface(InterfaceMode.Editor);

        FormClosing += MainForm_FormClosing;
        FormClosed += (_, _) => _service.Dispose();
        KeyDown += MainForm_KeyDown;
    }

    private static readonly Color Background = Color.FromArgb(30, 30, 30);
    private static readonly Color Surface = Color.FromArgb(37, 37, 38);
    private static readonly Color SurfaceLight = Color.FromArgb(45, 45, 48);
    private static readonly Color Border = Color.FromArgb(75, 75, 75);
    private static readonly Color Foreground = Color.FromArgb(235, 235, 235);
    private static readonly Color Muted = Color.FromArgb(175, 175, 175);
    private static readonly Color Accent = Color.FromArgb(0, 122, 204);
    private static readonly Color RowUntranslated = Color.FromArgb(78, 43, 111);
    private static readonly Color RowTranslatedSaved = Color.FromArgb(30, 104, 60);
    private static readonly Color RowTranslatedUnsaved = Color.FromArgb(27, 91, 140);
    private static readonly Color RowWarningText = Color.FromArgb(255, 190, 120);

    private void BuildInterface()
    {
        BackColor = Background;
        ForeColor = Foreground;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Background,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        root.Controls.Add(BuildMenu(), 0, 0);
        root.Controls.Add(BuildToolbar(), 0, 1);
        root.Controls.Add(BuildHeader(), 0, 2);

        ConfigureGrid();
        ConfigureEditorView();

        _contentHost.Dock = DockStyle.Fill;
        _contentHost.BackColor = Background;
        _contentHost.Controls.Add(_grid);
        _contentHost.Controls.Add(_editorView);
        root.Controls.Add(_contentHost, 0, 3);

        _status.BackColor = SurfaceLight;
        _status.ForeColor = Foreground;
        _status.SizingGrip = false;
        _status.Dock = DockStyle.Fill;
        _status.Items.Add(_statusLabel);
        _status.Items.Add(new ToolStripStatusLabel { Spring = true });
        _status.Items.Add(_progress);
        root.Controls.Add(_status, 0, 4);

        Controls.Add(root);
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceLight,
            ForeColor = Foreground,
            Renderer = new DarkRenderer()
        };

        var fileMenu = new ToolStripMenuItem("Arquivo");
        fileMenu.DropDownItems.Add("Abrir bundle", null, OpenButton_Click);
        fileMenu.DropDownItems.Add("Salvar como...", null, SaveButton_Click);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Sair", null, (_, _) => Close());

        var editMenu = new ToolStripMenuItem("Editar");
        editMenu.DropDownItems.Add("Pesquisar", null, (_, _) =>
        {
            _searchBox.Focus();
            _searchBox.SelectAll();
        });
        editMenu.DropDownItems.Add("Próximo vazio", null, NextEmptyButton_Click);

        var toolsMenu = new ToolStripMenuItem("Ferramentas");
        toolsMenu.DropDownItems.Add("Exportar CSV", null, ExportCsvButton_Click);
        toolsMenu.DropDownItems.Add("Importar CSV", null, ImportCsvButton_Click);

        var helpMenu = new ToolStripMenuItem("Ajuda");
        var interfaceMenu = new ToolStripMenuItem("Interface");

        _tableModeMenuItem = new ToolStripMenuItem("Interface V1 — Tabela")
        {
            CheckOnClick = true
        };
        _tableModeMenuItem.Click += (_, _) => SwitchInterface(InterfaceMode.Table);

        _editorModeMenuItem = new ToolStripMenuItem("Interface V2 — Editor")
        {
            CheckOnClick = true
        };
        _editorModeMenuItem.Click += (_, _) => SwitchInterface(InterfaceMode.Editor);

        interfaceMenu.DropDownItems.Add(_tableModeMenuItem);
        interfaceMenu.DropDownItems.Add(_editorModeMenuItem);

        helpMenu.DropDownItems.Add(interfaceMenu);
        helpMenu.DropDownItems.Add("Legenda de cores", null, (_, _) =>
            MessageBox.Show(
                "ROXO — texto ainda não traduzido\n" +
                "VERDE — texto traduzido e salvo\n" +
                "AZUL — texto traduzido ou alterado, mas ainda não salvo\n" +
                "TEXTO LARANJA — possível variável, tag ou quebra de linha ausente",
                "Legenda de cores",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information));
        helpMenu.DropDownItems.Add(new ToolStripSeparator());
        helpMenu.DropDownItems.Add("Sobre", null, (_, _) =>
            MessageBox.Show(
                "TextCollection Bundle Editor\n\n" +
                "A interface V2 é o modo padrão.\n" +
                "A interface V1 altera somente a área central.",
                "Sobre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information));

        menu.Items.AddRange([fileMenu, editMenu, toolsMenu, helpMenu]);
        MainMenuStrip = menu;
        return menu;
    }

    private ToolStrip BuildToolbar()
    {
        var toolbar = new ToolStrip
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            ForeColor = Foreground,
            GripStyle = ToolStripGripStyle.Hidden,
            Renderer = new DarkRenderer(),
            Padding = new Padding(7, 4, 7, 4)
        };

        toolbar.Items.Add(MakeToolButton("Abrir", OpenButton_Click));
        toolbar.Items.Add(MakeToolButton("Salvar", SaveButton_Click));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(MakeToolButton("Exportar CSV", ExportCsvButton_Click));
        toolbar.Items.Add(MakeToolButton("Importar CSV", ImportCsvButton_Click));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(MakeToolButton("Próximo pendente", NextEmptyButton_Click));

        return toolbar;
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            ColumnCount = 6,
            RowCount = 1,
            Padding = new Padding(10, 10, 10, 9)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

        _collectionBox.DropDownStyle = ComboBoxStyle.DropDownList;
        StyleCombo(_collectionBox);
        _collectionBox.SelectedIndexChanged += CollectionBox_SelectedIndexChanged;

        _filterBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _filterBox.Items.Clear();
        _filterBox.Items.AddRange(
            ["Todos", "Não traduzidos", "Traduzidos", "Iguais ao original", "Com alerta"]);
        _filterBox.SelectedIndex = 0;
        StyleCombo(_filterBox);
        _filterBox.SelectedIndexChanged += (_, _) => ApplyFilter();

        _searchBox.PlaceholderText = "Pesquisar texto ou ID...";
        StyleTextBox(_searchBox);
        _searchBox.TextChanged += (_, _) => ApplyFilter();

        header.Controls.Add(MakeCaption("Coleção:"), 0, 0);
        header.Controls.Add(_collectionBox, 1, 0);
        header.Controls.Add(MakeCaption("Filtro:"), 2, 0);
        header.Controls.Add(_filterBox, 3, 0);
        header.Controls.Add(MakeCaption("Pesquisar:"), 4, 0);
        header.Controls.Add(_searchBox, 5, 0);

        return header;
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.Margin = new Padding(10);
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _grid.MultiSelect = true;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _grid.EditMode = DataGridViewEditMode.EditOnEnter;
        _grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
        _grid.BackgroundColor = Surface;
        _grid.GridColor = Border;
        _grid.BorderStyle = BorderStyle.None;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceLight;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Foreground;
        _grid.DefaultCellStyle.BackColor = Surface;
        _grid.DefaultCellStyle.ForeColor = Foreground;
        _grid.DefaultCellStyle.SelectionBackColor = Accent;
        _grid.DefaultCellStyle.SelectionForeColor = Color.White;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Nº",
            DataPropertyName = nameof(TranslationEntry.Index),
            ReadOnly = true,
            Width = 58
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "ID",
            DataPropertyName = nameof(TranslationEntry.Id),
            ReadOnly = true,
            Width = 275
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Texto Original",
            DataPropertyName = nameof(TranslationEntry.OriginalText),
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 50
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Texto Editado",
            DataPropertyName = nameof(TranslationEntry.TranslatedText),
            ReadOnly = false,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 50
        });

        _grid.CellEndEdit += Grid_CellEndEdit;
        _grid.CellFormatting += Grid_CellFormatting;
        _grid.SelectionChanged += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is TranslationEntry entry)
                SelectEntryInEditor(entry);
        };
        _grid.DataError += (_, e) => e.ThrowException = false;
        _grid.DataSource = _source;
    }

    private void Grid_CellEndEdit(
        object? sender,
        DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 ||
            e.ColumnIndex < 0 ||
            e.ColumnIndex >= _grid.Columns.Count)
        {
            return;
        }

        // Somente a coluna "Texto Editado" pode alterar a tradução.
        if (_grid.Columns[e.ColumnIndex].DataPropertyName !=
            nameof(TranslationEntry.TranslatedText))
        {
            return;
        }

        if (_grid.Rows[e.RowIndex].DataBoundItem is not TranslationEntry entry)
            return;

        string newText =
            Convert.ToString(_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value)
            ?? string.Empty;

        // Atualiza o objeto diretamente, sem recarregar o filtro ou a tabela.
        // Isso evita o antigo bug em que a linha "voltava" ao texto anterior.
        entry.TranslatedText = newText;

        _source.EndEdit();
        _dirty = true;

        // Sincroniza a interface V2 com o mesmo texto.
        SelectEntryInEditor(entry);

        // Atualiza apenas o necessário, sem desmontar a lista.
        _grid.InvalidateRow(e.RowIndex);
        _entryList.Refresh();
        UpdateStatus();
    }

    private void ConfigureEditorView()
    {
        _editorView.Dock = DockStyle.Fill;
        _editorView.BackColor = Background;
        _editorView.ColumnCount = 2;
        _editorView.RowCount = 1;
        _editorView.Padding = new Padding(10);
        _editorView.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        _editorView.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));

        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 5, 0)
        };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.Controls.Add(MakeSectionTitle("Textos"), 0, 0);

        _entryList.Dock = DockStyle.Fill;
        _entryList.DisplayMember = nameof(TranslationEntry.OriginalText);
        _entryList.IntegralHeight = false;
        _entryList.BackColor = SurfaceLight;
        _entryList.ForeColor = Foreground;
        _entryList.BorderStyle = BorderStyle.FixedSingle;
        _entryList.Font = new Font("Segoe UI", 10);
        _entryList.SelectedIndexChanged += EntryList_SelectedIndexChanged;
        left.Controls.Add(_entryList, 0, 1);

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            ColumnCount = 1,
            RowCount = 10,
            Padding = new Padding(18),
            Margin = new Padding(5, 0, 0, 0)
        };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        right.Controls.Add(MakeSectionTitle("Editor de tradução"), 0, 0);
        right.Controls.Add(MakeCaption("Texto original"), 0, 1);

        _originalBox.Dock = DockStyle.Fill;
        _originalBox.Multiline = true;
        _originalBox.ReadOnly = true;
        _originalBox.ScrollBars = ScrollBars.Vertical;
        StyleTextBox(_originalBox);
        right.Controls.Add(_originalBox, 0, 2);

        right.Controls.Add(MakeCaption("Tradução"), 0, 4);

        _translationBox.Dock = DockStyle.Fill;
        _translationBox.Multiline = true;
        _translationBox.ScrollBars = ScrollBars.Vertical;
        _translationBox.AcceptsReturn = true;
        StyleTextBox(_translationBox);
        _translationBox.TextChanged += TranslationBox_TextChanged;
        right.Controls.Add(_translationBox, 0, 5);

        right.Controls.Add(MakeCaption("ID"), 0, 7);

        _idBox.Dock = DockStyle.Fill;
        _idBox.ReadOnly = true;
        StyleTextBox(_idBox);
        right.Controls.Add(_idBox, 0, 8);

        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 7, 0, 0)
        };

        ConfigureButton(_nextButton, "Próximo ▶", (_, _) => MoveEditorSelection(1), true);
        ConfigureButton(_previousButton, "◀ Anterior", (_, _) => MoveEditorSelection(-1), true);
        ConfigureButton(_copyOriginalEditorButton, "Copiar original", (_, _) =>
        {
            if (_entryList.SelectedItem is not TranslationEntry entry)
                return;

            _translationBox.Text = entry.OriginalText;
            _translationBox.Focus();
        }, true);

        StyleButton(_nextButton);
        StyleButton(_previousButton);
        StyleButton(_copyOriginalEditorButton);

        navigation.Controls.Add(_nextButton);
        navigation.Controls.Add(_previousButton);
        navigation.Controls.Add(_copyOriginalEditorButton);
        right.Controls.Add(navigation, 0, 9);

        _editorView.Controls.Add(left, 0, 0);
        _editorView.Controls.Add(right, 1, 0);
    }

    private void SwitchInterface(InterfaceMode mode)
    {
        _interfaceMode = mode;
        _grid.Visible = mode == InterfaceMode.Table;
        _editorView.Visible = mode == InterfaceMode.Editor;

        if (_tableModeMenuItem is not null)
            _tableModeMenuItem.Checked = mode == InterfaceMode.Table;
        if (_editorModeMenuItem is not null)
            _editorModeMenuItem.Checked = mode == InterfaceMode.Editor;

        if (mode == InterfaceMode.Table)
            _grid.BringToFront();
        else
            _editorView.BringToFront();

        SyncSelectionBetweenViews();
        UpdateStatus();
    }

    private static ToolStripButton MakeToolButton(string text, EventHandler handler)
    {
        var button = new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ForeColor = Foreground,
            AutoSize = true,
            Margin = new Padding(4, 0, 4, 0)
        };
        button.Click += handler;
        return button;
    }

    private static Label MakeCaption(string text) => new()
    {
        Text = text,
        ForeColor = Muted,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Segoe UI", 9)
    };

    private static Label MakeSectionTitle(string text) => new()
    {
        Text = text,
        ForeColor = Foreground,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Segoe UI Semibold", 11)
    };

    private static void StyleTextBox(TextBox box)
    {
        box.BackColor = SurfaceLight;
        box.ForeColor = Foreground;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Font = new Font("Segoe UI", 10);
    }

    private static void StyleCombo(ComboBox box)
    {
        box.Dock = DockStyle.Fill;
        box.BackColor = SurfaceLight;
        box.ForeColor = Foreground;
        box.FlatStyle = FlatStyle.Flat;
        box.Font = new Font("Segoe UI", 9);
    }

    private static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = SurfaceLight;
        button.ForeColor = Foreground;
        button.FlatAppearance.BorderColor = Border;
    }

    private sealed class DarkRenderer : ToolStripProfessionalRenderer
    {
        public DarkRenderer() : base(new DarkColorTable())
        {
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled
                ? Foreground
                : Color.FromArgb(125, 125, 125);

            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(
            ToolStripItemRenderEventArgs e)
        {
            Color background = e.Item.Selected
                ? Color.FromArgb(62, 62, 66)
                : e.ToolStrip is ToolStripDropDown
                    ? Surface
                    : SurfaceLight;

            using var brush = new SolidBrush(background);
            e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));

            if (e.Item.Selected)
            {
                using var pen = new Pen(Accent);
                var borderRectangle = new Rectangle(
                    0,
                    0,
                    Math.Max(0, e.Item.Width - 1),
                    Math.Max(0, e.Item.Height - 1));
                e.Graphics.DrawRectangle(pen, borderRectangle);
            }
        }
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Surface;
        public override Color ImageMarginGradientBegin => Surface;
        public override Color ImageMarginGradientMiddle => Surface;
        public override Color ImageMarginGradientEnd => Surface;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Accent;
        public override Color MenuItemSelected => SurfaceLight;
        public override Color MenuItemSelectedGradientBegin => SurfaceLight;
        public override Color MenuItemSelectedGradientEnd => SurfaceLight;
        public override Color MenuItemPressedGradientBegin => SurfaceLight;
        public override Color MenuItemPressedGradientEnd => SurfaceLight;
        public override Color ToolStripGradientBegin => Surface;
        public override Color ToolStripGradientMiddle => Surface;
        public override Color ToolStripGradientEnd => Surface;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
        public override Color ButtonSelectedBorder => Accent;
        public override Color ButtonSelectedGradientBegin => SurfaceLight;
        public override Color ButtonSelectedGradientEnd => SurfaceLight;
        public override Color ButtonPressedGradientBegin => Accent;
        public override Color ButtonPressedGradientEnd => Accent;
    }

    private static void ConfigureButton(
        Button button,
        string text,
        EventHandler click,
        bool enabled)
    {
        button.Text = text;
        button.AutoSize = true;
        button.Height = 34;
        button.Enabled = enabled;
        button.Click += click;
        StyleButton(button);
    }

    private void OpenButton_Click(object? sender, EventArgs e)
    {
        if (!ConfirmDiscardUnsaved())
            return;

        using var dialog = new OpenFileDialog
        {
            Title = "Selecione o bundle de localização",
            Filter = "Unity AssetBundle (*.bundle)|*.bundle|Todos os arquivos (*.*)|*.*"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            UseWaitCursor = true;
            _statusLabel.Text = "Abrindo bundle...";
            Application.DoEvents();

            IReadOnlyList<string> collectionNames = _service.Open(dialog.FileName);
            if (collectionNames.Count == 0)
                throw new InvalidDataException(
                    "Nenhuma coleção TextCollection compatível foi encontrada.");

            _collections.Clear();
            _collectionBox.Items.Clear();
            foreach (string name in collectionNames)
                _collectionBox.Items.Add(name);

            string preferred = collectionNames.FirstOrDefault(name =>
                name.Equals(
                    "english-texts-fallback",
                    StringComparison.OrdinalIgnoreCase)) ?? collectionNames[0];

            _dirty = false;
            _collectionBox.SelectedItem = preferred;
            SetEditingButtonsEnabled(true);
            Text = $"TextCollection Bundle Editor — {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Erro ao abrir",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _statusLabel.Text = "Falha ao abrir o bundle";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void SetEditingButtonsEnabled(bool enabled)
    {
        _saveButton.Enabled = enabled;
        _exportCsvButton.Enabled = enabled;
        _importCsvButton.Enabled = enabled;
        _nextEmptyButton.Enabled = enabled;
        _copyOriginalButton.Enabled = enabled;
        _translationBox.Enabled = enabled;
    }

    private void CollectionBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_switchingCollection || _collectionBox.SelectedItem is not string name)
            return;

        try
        {
            UseWaitCursor = true;
            _switchingCollection = true;

            if (!_collections.TryGetValue(name, out List<TranslationEntry>? loaded))
            {
                loaded = _service.LoadCollection(name);
                _collections[name] = loaded;
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Erro ao carregar coleção",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _switchingCollection = false;
            UseWaitCursor = false;
        }
    }

    private void ApplyFilter()
    {
        string? selectedId = GetSelectedEntry()?.Id;

        string term = _searchBox.Text.Trim();
        IEnumerable<TranslationEntry> query = CurrentEntries;

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(entry =>
                entry.Id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                entry.OriginalText.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                entry.TranslatedText.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        query = (_filterBox.SelectedItem?.ToString()) switch
        {
            "Não traduzidos" => query.Where(IsUntranslated),
            "Traduzidos" => query.Where(IsTranslated),
            "Iguais ao original" => query.Where(entry =>
                string.Equals(
                    entry.OriginalText,
                    entry.TranslatedText,
                    StringComparison.Ordinal)),
            "Com alerta" => query.Where(HasPlaceholderWarning),
            _ => query
        };

        List<TranslationEntry> visible = query.ToList();
        _source.DataSource = visible;
        _entryList.DataSource = null;
        _entryList.DataSource = visible;
        _entryList.DisplayMember = nameof(TranslationEntry.OriginalText);

        RestoreSelection(selectedId);
        UpdateStatus();
    }

    private TranslationEntry? GetSelectedEntry()
    {
        if (_interfaceMode == InterfaceMode.Editor)
            return _entryList.SelectedItem as TranslationEntry;

        return _grid.CurrentRow?.DataBoundItem as TranslationEntry;
    }

    private void RestoreSelection(string? id)
    {
        if (_source.Count == 0)
        {
            ClearEditor();
            return;
        }

        int targetIndex = 0;
        if (!string.IsNullOrWhiteSpace(id))
        {
            for (int index = 0; index < _source.Count; index++)
            {
                if (_source[index] is TranslationEntry entry &&
                    entry.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    targetIndex = index;
                    break;
                }
            }
        }

        _source.Position = targetIndex;
        _entryList.SelectedIndex = targetIndex;

        if (_grid.Rows.Count > targetIndex)
            _grid.CurrentCell = _grid.Rows[targetIndex].Cells[3];

        if (_source[targetIndex] is TranslationEntry selected)
            SelectEntryInEditor(selected);
    }

    private void EntryList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_entryList.SelectedItem is not TranslationEntry entry)
        {
            ClearEditor();
            return;
        }

        _source.Position = _entryList.SelectedIndex;
        SelectEntryInEditor(entry);
    }

    private void SelectEntryInEditor(TranslationEntry entry)
    {
        _loadingEditorSelection = true;
        _originalBox.Text = entry.OriginalText;
        _translationBox.Text = entry.TranslatedText;
        _idBox.Text = entry.Id;
        _loadingEditorSelection = false;
    }

    private void ClearEditor()
    {
        _loadingEditorSelection = true;
        _originalBox.Clear();
        _translationBox.Clear();
        _idBox.Clear();
        _loadingEditorSelection = false;
    }

    private void TranslationBox_TextChanged(object? sender, EventArgs e)
    {
        if (_loadingEditorSelection ||
            _entryList.SelectedItem is not TranslationEntry entry)
        {
            return;
        }

        entry.TranslatedText = _translationBox.Text;
        _dirty = true;
        _grid.Refresh();
        _entryList.Refresh();
        UpdateStatus();
    }

    private void SyncSelectionBetweenViews()
    {
        TranslationEntry? entry = GetSelectedEntry();
        if (entry is null && _source.Current is TranslationEntry current)
            entry = current;

        if (entry is null)
            return;

        RestoreSelection(entry.Id);
    }

    private void MoveEditorSelection(int direction)
    {
        if (_entryList.Items.Count == 0)
            return;

        int next = Math.Clamp(
            _entryList.SelectedIndex + direction,
            0,
            _entryList.Items.Count - 1);

        _entryList.SelectedIndex = next;
        _entryList.TopIndex = Math.Max(0, next - 3);
        _translationBox.Focus();
    }

    private static bool IsTranslated(TranslationEntry entry) =>
        !string.IsNullOrWhiteSpace(entry.TranslatedText) &&
        !string.Equals(
            entry.OriginalText,
            entry.TranslatedText,
            StringComparison.Ordinal);

    private static bool IsUntranslated(TranslationEntry entry) =>
        string.IsNullOrWhiteSpace(entry.TranslatedText) ||
        string.Equals(
            entry.OriginalText,
            entry.TranslatedText,
            StringComparison.Ordinal);

    private static readonly Regex TokenRegex = new(
        @"(\{[^{}]+\}|<[^<>]+>|%\w|\\n)",
        RegexOptions.Compiled);

    private static bool HasPlaceholderWarning(TranslationEntry entry)
    {
        string[] originalTokens = TokenRegex
            .Matches(entry.OriginalText)
            .Select(match => match.Value)
            .OrderBy(value => value)
            .ToArray();

        string[] editedTokens = TokenRegex
            .Matches(entry.TranslatedText ?? string.Empty)
            .Select(match => match.Value)
            .OrderBy(value => value)
            .ToArray();

        return IsTranslated(entry) &&
               !originalTokens.SequenceEqual(
                   editedTokens,
                   StringComparer.Ordinal);
    }

    private void Grid_CellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 ||
            _grid.Rows[e.RowIndex].DataBoundItem is not TranslationEntry entry)
        {
            return;
        }

        DataGridViewRow row = _grid.Rows[e.RowIndex];

        Color background;
        if (entry.IsModified)
            background = RowTranslatedUnsaved;
        else if (IsTranslated(entry))
            background = RowTranslatedSaved;
        else
            background = RowUntranslated;

        row.DefaultCellStyle.BackColor = background;
        row.DefaultCellStyle.ForeColor = Foreground;
        row.DefaultCellStyle.SelectionBackColor = ControlPaint.Light(background, 0.15f);
        row.DefaultCellStyle.SelectionForeColor = Color.White;

        if (HasPlaceholderWarning(entry))
        {
            row.DefaultCellStyle.ForeColor = RowWarningText;
            row.DefaultCellStyle.SelectionForeColor = RowWarningText;
        }
    }

    private void UpdateStatus()
    {
        List<TranslationEntry> entries = CurrentEntries;
        int translated = entries.Count(IsTranslated);
        int alerts = entries.Count(HasPlaceholderWarning);
        int modified = entries.Count(entry => entry.IsModified);
        int total = entries.Count;
        int percent = total == 0
            ? 0
            : (int)Math.Round(translated * 100d / total);

        _progress.Value = Math.Clamp(percent, 0, 100);
        _statusLabel.Text =
            $"{total} textos • {translated} traduzidos • {percent}% • " +
            $"{_source.Count} exibidos • {alerts} alertas • {modified} não salvos";
    }

    private void NextEmptyButton_Click(object? sender, EventArgs e)
    {
        TranslationEntry? target = CurrentEntries.FirstOrDefault(IsUntranslated);
        if (target is null)
        {
            MessageBox.Show(
                "Não há textos pendentes nesta coleção.",
                "Próximo vazio",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _filterBox.SelectedItem = "Todos";
        _searchBox.Clear();
        ApplyFilter();
        RestoreSelection(target.Id);

        if (_interfaceMode == InterfaceMode.Table &&
            _grid.CurrentCell is not null)
        {
            _grid.BeginEdit(true);
        }
        else
        {
            _translationBox.Focus();
        }
    }

    private void CopyOriginalButton_Click(object? sender, EventArgs e)
    {
        List<TranslationEntry> selected = [];

        if (_interfaceMode == InterfaceMode.Table)
        {
            selected = _grid.SelectedCells
                .Cast<DataGridViewCell>()
                .Select(cell => cell.OwningRow.DataBoundItem as TranslationEntry)
                .Where(entry => entry is not null)
                .Cast<TranslationEntry>()
                .Distinct()
                .ToList();
        }
        else if (_entryList.SelectedItem is TranslationEntry editorEntry)
        {
            selected.Add(editorEntry);
        }

        if (selected.Count == 0)
        {
            MessageBox.Show(
                "Selecione uma ou mais linhas primeiro.",
                "Copiar texto",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        foreach (TranslationEntry entry in selected)
            entry.TranslatedText = entry.OriginalText;

        _dirty = true;
        ApplyFilter();
    }

    private void ExportCsvButton_Click(object? sender, EventArgs e)
    {
        CommitEdits();

        if (CurrentEntries.Count == 0)
        {
            MessageBox.Show(
                "Nenhum texto foi carregado.",
                "Exportar CSV",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Exportar tradução para CSV",
            Filter = "Arquivo CSV UTF-8 (*.csv)|*.csv",
            FileName = $"{_collectionBox.SelectedItem ?? "traducao"}.csv"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            using var writer = new StreamWriter(
                dialog.FileName,
                false,
                new UTF8Encoding(true));

            writer.WriteLine("ID;Texto Original;Texto Editado");

            foreach (TranslationEntry entry in CurrentEntries)
            {
                writer.WriteLine(string.Join(";", [
                    EscapeCsv(entry.Id),
                    EscapeCsv(entry.OriginalText),
                    EscapeCsv(entry.TranslatedText)
                ]));
            }

            MessageBox.Show(
                "CSV exportado. Traduza somente a coluna Texto Editado.",
                "Exportação concluída",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível exportar o CSV.\n\n{ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ImportCsvButton_Click(object? sender, EventArgs e)
    {
        if (CurrentEntries.Count == 0)
        {
            MessageBox.Show(
                "Abra um bundle e carregue uma coleção antes de importar.",
                "Importar CSV",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = "Importar tradução de um CSV",
            Filter = "Arquivo CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            Dictionary<string, string> translations = ReadCsv(dialog.FileName);
            int imported = 0;

            foreach (TranslationEntry entry in CurrentEntries)
            {
                if (!translations.TryGetValue(
                        entry.Id,
                        out string? translatedText))
                {
                    continue;
                }

                entry.TranslatedText = translatedText;
                imported++;
            }

            _dirty = imported > 0 || _dirty;
            ApplyFilter();

            MessageBox.Show(
                $"{imported} traduções foram importadas pelo ID.",
                "Importação concluída",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível importar o CSV.\n\n{ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static Dictionary<string, string> ReadCsv(string path)
    {
        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        char delimiter = DetectCsvDelimiter(path);

        using var parser = new TextFieldParser(path, Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };

        parser.SetDelimiters(delimiter.ToString());

        string[]? header = parser.ReadFields();

        if (header is null)
            throw new InvalidDataException("O CSV está vazio.");

        for (int index = 0; index < header.Length; index++)
        {
            header[index] = header[index]
                .Trim()
                .TrimStart('\uFEFF');
        }

        int idIndex = FindCsvColumn(
            header,
            "ID",
            "Id",
            "Identificador");

        int editedIndex = FindCsvColumn(
            header,
            "Texto Editado",
            "Texto Traduzido",
            "Tradução",
            "Traducao",
            "Translation",
            "Translated Text");

        if (idIndex < 0 || editedIndex < 0)
        {
            string detectedColumns = string.Join(", ", header);

            throw new InvalidDataException(
                "O CSV precisa ter as colunas 'ID' e 'Texto Editado'.\n\n" +
                $"Colunas encontradas: {detectedColumns}\n" +
                $"Separador detectado: '{delimiter}'");
        }

        while (!parser.EndOfData)
        {
            string[]? fields = parser.ReadFields();

            if (fields is null ||
                fields.Length <= Math.Max(idIndex, editedIndex))
            {
                continue;
            }

            string id = fields[idIndex]
                .Trim()
                .TrimStart('\uFEFF');

            if (!string.IsNullOrWhiteSpace(id))
                result[id] = fields[editedIndex];
        }

        return result;
    }

    private static char DetectCsvDelimiter(string path)
    {
        using var reader = new StreamReader(
            path,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        string firstLine = reader.ReadLine() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(firstLine))
            throw new InvalidDataException("O CSV está vazio.");

        var candidates = new[] { ';', ',', '\t' };

        char bestDelimiter = ';';
        int bestCount = -1;

        foreach (char candidate in candidates)
        {
            int count = CountDelimiterOutsideQuotes(firstLine, candidate);

            if (count > bestCount)
            {
                bestCount = count;
                bestDelimiter = candidate;
            }
        }

        if (bestCount <= 0)
        {
            throw new InvalidDataException(
                "Não foi possível detectar o separador do CSV. " +
                "Use vírgula, ponto e vírgula ou tabulação.");
        }

        return bestDelimiter;
    }

    private static int CountDelimiterOutsideQuotes(
        string line,
        char delimiter)
    {
        bool insideQuotes = false;
        int count = 0;

        for (int index = 0; index < line.Length; index++)
        {
            char current = line[index];

            if (current == '"')
            {
                if (insideQuotes &&
                    index + 1 < line.Length &&
                    line[index + 1] == '"')
                {
                    index++;
                    continue;
                }

                insideQuotes = !insideQuotes;
                continue;
            }

            if (!insideQuotes && current == delimiter)
                count++;
        }

        return count;
    }

    private static int FindCsvColumn(
        string[] header,
        params string[] acceptedNames)
    {
        for (int index = 0; index < header.Length; index++)
        {
            string normalizedHeader = NormalizeColumnName(header[index]);

            foreach (string acceptedName in acceptedNames)
            {
                if (normalizedHeader == NormalizeColumnName(acceptedName))
                    return index;
            }
        }

        return -1;
    }

    private static string NormalizeColumnName(string value)
    {
        return value
            .Trim()
            .TrimStart('\uFEFF')
            .Replace("_", " ")
            .Replace("-", " ")
            .ToUpperInvariant();
    }

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;
        bool needsQuotes =
            value.Contains(';') ||
            value.Contains('"') ||
            value.Contains('\r') ||
            value.Contains('\n');

        value = value.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{value}\"" : value;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        CommitEdits();

        using var dialog = new SaveFileDialog
        {
            Title = "Salvar bundle modificado",
            Filter =
                "Unity AssetBundle (*.bundle)|*.bundle|" +
                "Todos os arquivos (*.*)|*.*",
            FileName = _service.OpenedPath is null
                ? "localisation-modificado.bundle"
                : Path.GetFileName(_service.OpenedPath),
            InitialDirectory = _service.OpenedPath is null
                ? null
                : Path.GetDirectoryName(_service.OpenedPath)
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        if (_service.OpenedPath is not null &&
            string.Equals(
                Path.GetFullPath(dialog.FileName),
                Path.GetFullPath(_service.OpenedPath),
                StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                "Salve com outro nome. O arquivo original está aberto.",
                "Escolha outro nome",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        int alerts = _collections.Values
            .SelectMany(collection => collection)
            .Count(HasPlaceholderWarning);

        if (alerts > 0)
        {
            DialogResult continueResult = MessageBox.Show(
                $"Existem {alerts} textos com possíveis tags, variáveis " +
                "ou quebras de linha faltando.\n\nDeseja salvar mesmo assim?",
                "Alertas de validação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (continueResult != DialogResult.Yes)
                return;
        }

        try
        {
            UseWaitCursor = true;
            _statusLabel.Text = "Salvando todas as coleções carregadas...";
            Application.DoEvents();

            _service.SaveAs(dialog.FileName, _collections);

            foreach (TranslationEntry entry in _collections.Values.SelectMany(items => items))
                entry.SavedText = entry.TranslatedText;

            _dirty = false;
            _grid.Refresh();
            _entryList.Refresh();
            UpdateStatus();

            MessageBox.Show(
                "Bundle salvo com sucesso. Se já existia um arquivo " +
                "com esse nome, foi criado um backup .bak.",
                "Concluído",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Erro ao salvar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _statusLabel.Text = "Falha ao salvar";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void CommitEdits()
    {
        if (_grid.IsCurrentCellInEditMode)
            _grid.EndEdit();

        _source.EndEdit();

        if (_grid.CurrentRow?.DataBoundItem is TranslationEntry gridEntry &&
            _grid.CurrentCell is not null &&
            _grid.Columns[_grid.CurrentCell.ColumnIndex].DataPropertyName ==
            nameof(TranslationEntry.TranslatedText))
        {
            gridEntry.TranslatedText =
                Convert.ToString(_grid.CurrentCell.Value) ?? string.Empty;
        }

        if (_entryList.SelectedItem is TranslationEntry editorEntry &&
            !_loadingEditorSelection)
        {
            editorEntry.TranslatedText = _translationBox.Text;
        }
    }

    private bool ConfirmDiscardUnsaved()
    {
        if (!_dirty)
            return true;

        return MessageBox.Show(
            "Existem alterações não salvas. Deseja descartá-las?",
            "Alterações não salvas",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    private void MainForm_FormClosing(
        object? sender,
        FormClosingEventArgs e)
    {
        if (!ConfirmDiscardUnsaved())
            e.Cancel = true;
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.D1)
        {
            SwitchInterface(InterfaceMode.Table);
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.D2)
        {
            SwitchInterface(InterfaceMode.Editor);
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.F)
        {
            _searchBox.Focus();
            _searchBox.SelectAll();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.F3)
        {
            NextEmptyButton_Click(this, EventArgs.Empty);
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.S)
        {
            SaveButton_Click(this, EventArgs.Empty);
            e.SuppressKeyPress = true;
        }
    }
}
