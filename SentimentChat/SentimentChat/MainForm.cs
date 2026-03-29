using SentimentChat;
using SentimentChat.Services;
using System.Drawing.Drawing2D;
namespace SentimentChat
{
    public partial class MainForm : Form
    {
        // ── Services ─────────────────────────────────────────
        private SentimentService _sentimentService = null!;
        private BotService _botService = null!;
        // ── Stats ─────────────────────────────────────────────
        private int _total = 0, _pos = 0, _neg = 0, _neu = 0;

        // ── Colors ────────────────────────────────────────────
        private readonly Color GREEN = Color.FromArgb(0, 255, 65);
        private readonly Color DARK_GREEN = Color.FromArgb(0, 204, 51);
        private readonly Color DIM_GREEN = Color.FromArgb(0, 51, 0);
        private readonly Color BG = Color.FromArgb(10, 10, 10);
        private readonly Color PHONE_BG = Color.FromArgb(17, 17, 17);
        private readonly Color HEADER_BG = Color.FromArgb(31, 44, 52);
        private readonly Color BUBBLE_USER = Color.FromArgb(0, 92, 75);
        private readonly Color BUBBLE_BOT = Color.FromArgb(32, 44, 51);
        private readonly Color TEXT_CLR = Color.FromArgb(233, 237, 239);
        private readonly Color SERVER_BG = Color.FromArgb(5, 15, 5);

        // ── Matrix Rain ───────────────────────────────────────
        private System.Windows.Forms.Timer _matrixTimer = null!;
        private Bitmap _matrixBitmap = null!;
        private Graphics _matrixGfx = null!;
        private float[] _drops = null!;
        private readonly Random _rnd = new Random();
        private const string MATRIX_CHARS = "アイウエオカキクケコ01アイウエオ";

        // ── UI Refs ───────────────────────────────────────────
        private Panel pnlUserChat = null!;
        private Panel pnlBotChat = null!;
        private TextBox txtInput = null!;
        private Button btnSend = null!;
        private Label lblServerStatus = null!;
        private Label lblTotal = null!, lblPos = null!, lblNeg = null!, lblNeu = null!;
        private FlowLayoutPanel flpLog = null!;
        private PictureBox pbxMatrix = null!;
        private Label lblUserStatus = null!;

        public MainForm()
        {
            InitializeComponent();

            this.Text = "SentimentChat — WhatsApp Sentiment Simulator";
            this.Size = new Size(1120, 680);
            this.MinimumSize = new Size(1120, 680);
            this.BackColor = BG;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            InitServices();
            BuildUI();
            StartMatrixRain();
            BootLog();
        }

        private void InitServices()
        {
            try
            {
                _sentimentService = new SentimentService();
                _botService = new BotService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup Error:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuildUI()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(18, 14, 18, 14)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 255));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 298));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 255));
            this.Controls.Add(mainLayout);
            mainLayout.BringToFront();
            BuildUserPhone(mainLayout);
            BuildArrow(mainLayout, 1);
            BuildServer(mainLayout);
            BuildArrow(mainLayout, 3);
            BuildBotPhone(mainLayout);
        }

        // ════════ USER PHONE ══════════════════════════════════
        private void BuildUserPhone(TableLayoutPanel parent)
        {
            var wrap = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            parent.Controls.Add(wrap, 0, 0);

            var lbl = new Label
            {
                Text = "// USER_DEVICE",
                ForeColor = GREEN,
                Font = new Font("Courier New", 8, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(0, 4)
            };
            wrap.Controls.Add(lbl);

            var phone = MakePhone();
            phone.Location = new Point(0, 24);
            wrap.Controls.Add(phone);

            // Header
            var header = new Panel
            {
                Height = 58,
                Dock = DockStyle.Top,
                BackColor = HEADER_BG,
                Padding = new Padding(10, 8, 0, 0)
            };
            phone.Controls.Add(header);
            header.Controls.Add(MakeAvatar("🤖", Color.FromArgb(0, 92, 75)));

            header.Controls.Add(new Label
            {
                Text = "AI Bot",
                ForeColor = TEXT_CLR,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(52, 9)
            });

            lblUserStatus = new Label
            {
                Text = "● ONLINE",
                ForeColor = GREEN,
                Font = new Font("Courier New", 7),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(52, 28)
            };
            header.Controls.Add(lblUserStatus);

            // Chat area
            pnlUserChat = new Panel
            {
                BackColor = Color.FromArgb(12, 20, 25),
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(5)
            };
            phone.Controls.Add(pnlUserChat);

            // Input bar
            var inputBar = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom,
                BackColor = HEADER_BG,
                Padding = new Padding(7)
            };
            phone.Controls.Add(inputBar);

            txtInput = new TextBox
            {
                BackColor = Color.FromArgb(42, 57, 66),
                ForeColor = TEXT_CLR,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                Width = 168,
                Location = new Point(7, 12)
            };
            txtInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = SendMessage(); }
            };
            inputBar.Controls.Add(txtInput);

            btnSend = new Button
            {
                Text = "➤",
                BackColor = GREEN,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 36),
                Location = new Point(182, 7),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += async (s, e) => await SendMessage();
            inputBar.Controls.Add(btnSend);
        }

        private void BuildServer(TableLayoutPanel parent)
{
var wrap = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
parent.Controls.Add(wrap, 2, 0);
        var lbl = new Label
        {
            Text      = "// SENTIMENT_SERVER",
            ForeColor = GREEN,
            Font      = new Font("Courier New", 8, FontStyle.Bold),
            AutoSize  = true,
            BackColor = Color.Transparent,
            Location  = new Point(0, 4)
        };
        wrap.Controls.Add(lbl);

        var server = new Panel
        {
            Size      = new Size(293, 568),
            Location  = new Point(0, 24),
            BackColor = SERVER_BG
        };
        server.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen  = new Pen(GREEN, 1);
            using var glow = new Pen(Color.FromArgb(25, 0, 255, 65), 6);
            var r = new Rectangle(1, 1, server.Width - 2, server.Height - 2);
            e.Graphics.DrawRectangle(glow, r);
            e.Graphics.DrawRectangle(pen, r);
        };
        wrap.Controls.Add(server);

        // Header
        var hdr = new Panel
        {
            Height    = 34,
            Dock      = DockStyle.Top,
            BackColor = Color.FromArgb(0, 26, 0)
        };
        server.Controls.Add(hdr);
        hdr.Controls.Add(new Label
        {
            Text      = "▶ SENTIMENT_ANALYSIS_NODE  v1.0",
            ForeColor = GREEN,
            Font      = new Font("Courier New", 8, FontStyle.Bold),
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(10, 10)
        });

        // Stats row
        var statsRow = new Panel
        {
            Height    = 48,
            Dock      = DockStyle.Top,
            BackColor = Color.FromArgb(2, 13, 2)
        };
        server.Controls.Add(statsRow);

        int sx = 14;
        lblTotal = MakeStat("0", "TOTAL",    GREEN,                        ref sx, statsRow);
        lblPos   = MakeStat("0", "POSITIVE", GREEN,                        ref sx, statsRow);
        lblNeg   = MakeStat("0", "NEGATIVE", Color.FromArgb(255, 68, 68),  ref sx, statsRow);
        lblNeu   = MakeStat("0", "NEUTRAL",  Color.FromArgb(170,170,170),  ref sx, statsRow);

        // Log area
        var logScroll = new Panel
        {
            Dock       = DockStyle.Fill,
            BackColor  = Color.Transparent,
            AutoScroll = true,
            Padding    = new Padding(6, 4, 6, 4)
        };
        server.Controls.Add(logScroll);

        flpLog = new FlowLayoutPanel
        {
            FlowDirection  = FlowDirection.TopDown,
            WrapContents   = false,
            AutoSize       = true,
            AutoSizeMode   = AutoSizeMode.GrowAndShrink,
            BackColor      = Color.Transparent,
            Width          = 272
        };
        logScroll.Controls.Add(flpLog);

        // Footer
        var footer = new Panel
        {
            Height    = 26,
            Dock      = DockStyle.Bottom,
            BackColor = Color.FromArgb(0, 26, 0)
        };
        server.Controls.Add(footer);

        var dot = new Panel
        {
            Size      = new Size(8, 8),
            Location  = new Point(10, 9),
            BackColor = GREEN
        };
        var dp = new System.Drawing.Drawing2D.GraphicsPath();
        dp.AddEllipse(0, 0, 8, 8);
        dot.Region = new Region(dp);
        footer.Controls.Add(dot);

        var blinkTimer = new System.Windows.Forms.Timer { Interval = 700 };
        blinkTimer.Tick += (s, e) => dot.Visible = !dot.Visible;
        blinkTimer.Start();

        lblServerStatus = new Label
        {
            Text      = "IDLE — MONITORING TRAFFIC",
            ForeColor = DIM_GREEN,
            Font      = new Font("Courier New", 7),
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(24, 7)
        };
        footer.Controls.Add(lblServerStatus);
    }

    private Label MakeStat(string val, string name, Color color, ref int x, Panel parent)
    {
        var valLbl = new Label
        {
            Text      = val,
            ForeColor = color,
            Font      = new Font("Courier New", 13, FontStyle.Bold),
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(x, 4)
        };
        var namLbl = new Label
        {
            Text      = name,
            ForeColor = DIM_GREEN,
            Font      = new Font("Courier New", 6),
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(x, 24)
        };
        parent.Controls.Add(valLbl);
        parent.Controls.Add(namLbl);
        x += 64;
        return valLbl;
    }


        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _matrixTimer?.Stop();
            _matrixGfx?.Dispose();
            _matrixBitmap?.Dispose();
            _botService?.Dispose();
            base.OnFormClosed(e);
        }

        private void StartMatrixRain()
        {
            _matrixBitmap = new Bitmap(this.Width, this.Height);
            _matrixGfx = Graphics.FromImage(_matrixBitmap);
            int cols = this.Width / 14;
            _drops = new float[cols];
            for (int i = 0; i < cols; i++)
                _drops[i] = _rnd.Next(this.Height / 14);

            pbxMatrix = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pbxMatrix.Image = _matrixBitmap;
            this.Controls.Add(pbxMatrix);
            pbxMatrix.SendToBack();

            _matrixTimer = new System.Windows.Forms.Timer { Interval = 60 };
            _matrixTimer.Tick += (s, e) => DrawMatrix();
            _matrixTimer.Start();
        }

        private void DrawMatrix()
        {
            using var fadeBrush = new SolidBrush(Color.FromArgb(15, 10, 10, 10));
            _matrixGfx.FillRectangle(fadeBrush, 0, 0, _matrixBitmap.Width, _matrixBitmap.Height);

            using var font = new Font("Courier New", 11);
            using var brush = new SolidBrush(Color.FromArgb(20, 0, 255, 65));

            for (int i = 0; i < _drops.Length; i++)
            {
                char c = MATRIX_CHARS[_rnd.Next(MATRIX_CHARS.Length)];
                _matrixGfx.DrawString(c.ToString(), font, brush, i * 14, _drops[i] * 14);

                if (_drops[i] * 14 > _matrixBitmap.Height && _rnd.NextDouble() > 0.975)
                    _drops[i] = 0;
                _drops[i] += 0.5f;
            }
            pbxMatrix.Refresh();
        }
    }
}

