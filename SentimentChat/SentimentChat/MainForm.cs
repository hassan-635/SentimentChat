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


        // ════════ BOT PHONE ═══════════════════════════════════
        private void BuildBotPhone(TableLayoutPanel parent)
        {
            var wrap = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            parent.Controls.Add(wrap, 4, 0);
            var lbl = new Label
            {
                Text = "// AI_BOT_DEVICE",
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
            header.Controls.Add(MakeAvatar("👤", Color.FromArgb(42, 57, 66)));

            header.Controls.Add(new Label
            {
                Text = "User",
                ForeColor = TEXT_CLR,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(52, 9)
            });
            header.Controls.Add(new Label
            {
                Text = "● ONLINE",
                ForeColor = GREEN,
                Font = new Font("Courier New", 7),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(52, 28)
            });

            // Chat area
            pnlBotChat = new Panel
            {
                BackColor = Color.FromArgb(12, 20, 25),
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(5)
            };
            phone.Controls.Add(pnlBotChat);

            // Disabled input (bot side — read only)
            var inputBar = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom,
                BackColor = HEADER_BG,
                Enabled = false,
                Padding = new Padding(7)
            };
            phone.Controls.Add(inputBar);
            inputBar.Controls.Add(new TextBox
            {
                BackColor = Color.FromArgb(42, 57, 66),
                ForeColor = Color.FromArgb(80, 80, 80),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                Width = 168,
                Location = new Point(7, 12),
                Text = "AI is typing...",
                ReadOnly = true
            });
        }

        // ════════ ARROWS ══════════════════════════════════════
        private void BuildArrow(TableLayoutPanel parent, int col)
        {
            var pnl = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            parent.Controls.Add(pnl, col, 0);

            var lbl = new Label
            {
                Text = "→\n\n←",
                ForeColor = Color.FromArgb(0, 80, 20),
                Font = new Font("Courier New", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill
            };
            pnl.Controls.Add(lbl);

            bool vis = true;
            var t = new System.Windows.Forms.Timer { Interval = 900 };
            t.Tick += (s, e) =>
            {
                lbl.ForeColor = vis ? Color.FromArgb(0, 80, 20) : Color.FromArgb(0, 30, 8);
                vis = !vis;
            };
            t.Start();
        }

        // ════════ HELPERS ══════════════════════════════════════
        private Panel MakePhone()
        {
            var p = new Panel { Size = new Size(252, 562), BackColor = PHONE_BG };
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(40, 40, 40), 2);
                e.Graphics.DrawRectangle(pen, new Rectangle(1, 1, p.Width - 2, p.Height - 2));
                using var nb = new SolidBrush(Color.Black);
                e.Graphics.FillRectangle(nb, p.Width / 2 - 32, 0, 64, 16);
            };
            return p;
        }

        private Panel MakeAvatar(string emoji, Color bg)
        {
            var p = new Panel { Size = new Size(36, 36), Location = new Point(10, 11), BackColor = bg };
            var gp = new System.Drawing.Drawing2D.GraphicsPath();
            gp.AddEllipse(0, 0, 36, 36);
            p.Region = new Region(gp);
            p.Controls.Add(new Label
            {
                Text = emoji,
                Font = new Font("Segoe UI Emoji", 14),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            });
            return p;
        }
        // ════════ CHAT BUBBLE ══════════════════════════════════
        private void AddBubble(Panel chat, string text, string sentiment, bool isSent)
        {
            if (chat.InvokeRequired) { chat.Invoke(() => AddBubble(chat, text, sentiment, isSent)); return; }
            Color sentColor = sentiment switch
            {
                "Positive" => GREEN,
                "Negative" => Color.FromArgb(255, 68, 68),
                _ => Color.FromArgb(170, 170, 170)
            };

            string emoji = sentiment switch
            {
                "Positive" => "😊",
                "Negative" => "😡",
                _ => "😌"
            };

            var wrapper = new Panel
            {
                Width = chat.ClientSize.Width - 12,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 2)
            };

            var bubble = new Panel
            {
                BackColor = isSent ? BUBBLE_USER : BUBBLE_BOT,
                Padding = new Padding(8, 6, 8, 20),
                MaximumSize = new Size(200, 0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            bubble.Controls.Add(new Label
            {
                Text = $"{sentiment.ToUpper()}",
                ForeColor = sentColor,
                Font = new Font("Courier New", 7, FontStyle.Bold),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(4, 4)
            });

            bubble.Controls.Add(new Label
            {
                Text = $"{emoji} {text}",
                ForeColor = TEXT_CLR,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.Transparent,
                AutoSize = true,
                MaximumSize = new Size(180, 0),
                Location = new Point(4, 18)
            });

            bubble.Controls.Add(new Label
            {
                Text = DateTime.Now.ToString("HH:mm"),
                ForeColor = Color.FromArgb(134, 150, 160),
                Font = new Font("Courier New", 7),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(4, bubble.Height - 16)
            });

            wrapper.Controls.Add(bubble);

            wrapper.SizeChanged += (s, e) =>
            {
                bubble.Left = isSent ? wrapper.Width - bubble.Width - 4 : 4;
                // fix time label position
                foreach (Control c in bubble.Controls)
                    if (c is Label l && l.Font.Size == 7 && l.Text.Contains(":"))
                        l.Location = new Point(bubble.Width - l.Width - 8, bubble.Height - l.Height - 2);
            };

            chat.Controls.Add(wrapper);
            chat.AutoScrollPosition = new Point(0, chat.DisplayRectangle.Height);
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

