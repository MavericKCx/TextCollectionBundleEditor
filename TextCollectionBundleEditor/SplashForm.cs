namespace TextCollectionBundleEditor;

public sealed class SplashForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new();

    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 280);
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;
        ShowInTaskbar = false;
        TopMost = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(32)
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var logo = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = Icon?.ToBitmap()
        };

        var title = new Label
        {
            Text = "TextCollection Bundle Editor",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 20),
            ForeColor = Color.White
        };

        var subtitle = new Label
        {
            Text = "Professional Unity Localization Tool",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(185, 185, 185)
        };

        var progress = new ProgressBar
        {
            Dock = DockStyle.Bottom,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 28,
            Height = 8
        };

        var version = new Label
        {
            Text = "Versão 2.2.0",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(160, 160, 160)
        };

        root.Controls.Add(logo, 0, 0);
        root.Controls.Add(title, 0, 1);
        root.Controls.Add(subtitle, 0, 2);
        root.Controls.Add(progress, 0, 3);
        root.Controls.Add(version, 0, 4);

        Controls.Add(root);

        Shown += (_, _) =>
        {
            _timer.Interval = 1400;
            _timer.Tick += (_, _) =>
            {
                _timer.Stop();
                Close();
            };
            _timer.Start();
        };
    }
}
