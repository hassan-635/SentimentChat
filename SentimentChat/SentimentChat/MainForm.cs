using SentimentChat.Services;
using System.Drawing.Drawing2D;

namespace SentimentChat
{
    public partial class MainForm : Form
    {
        // ── Services ──────────────────────────────────────────────────────
        private SentimentService _sentimentService = null!;
        private BotService _botService = null!;

        // ── Stats ─────────────────────────────────────────────────────────
        private int _total = 0, _pos = 0, _neg = 0, _neu = 0;

        // ── Colors ────────────────────────────────────────────────────────
        private static readonly Color GREEN = Color.FromArgb(0, 255, 65);
        private static readonly Color DIM_GREEN = Color.FromArgb(0, 110, 30);
        private static readonly Color BG = Color.FromArgb(8, 8, 8);
        private static readonly Color PHONE_BG = Color.FromArgb(18, 18, 18);
        private static readonly Color HEADER_BG = Color.FromArgb(28, 40, 48);
        private static readonly Color CHAT_BG = Color.FromArgb(11, 18, 23);
        private static readonly Color BUBBLE_USER = Color.FromArgb(0, 92, 75);
        private static readonly Color BUBBLE_BOT = Color.FromArgb(32, 44, 51);
        private static readonly Color TEXT_CLR = Color.FromArgb(233, 237, 239);
        private static readonly Color SERVER_BG = Color.FromArgb(4, 12, 4);

        // ── Matrix rain ────────────────────────────────────────────────── 
        private System.Windows.Forms.Timer _matrixTimer = null!;
        private Bitmap _matrixBmp = null!;
        private Graphics _matrixGfx = null!;
        private float[] _drops = null!;
        private readonly Random _rnd = new();
        private const string MATRIX_CHARS = "アイウエオカキクケコ01アイウエオ10ウカ";

        // ── UI refs ───────────────────────────────────────────────────────
        private Panel pnlUserChat = null!;
        private Panel pnlBotChat = null!;
        private TextBox txtInput = null!;
        private Button btnSend = null!;
        private Label lblUserStatus = null!;
        private Label lblServerStatus = null!;
        private Label lblTotal = null!, lblPos = null!,
                                lblNeg = null!, lblNeu = null!;
        private FlowLayoutPanel flpLog = null!;
        private PictureBox pbxMatrix = null!;

        // ══════════════════════════════════════════════════════════════════
        public MainForm()
        {
            InitializeComponent();
            Text = "SentimentChat  —  WhatsApp Sentiment Simulator";
            Size = new Size(1420, 840);
            MinimumSize = new Size(1420, 840);
            BackColor = BG;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            InitServices();
            BuildUI();
            StartMatrixRain();
            BootLog();
        }

        // ─────────────────────────────────────────────────────────────────
        private void InitServices()
        {
            try
            {
                _sentimentService = new SentimentService();
                _botService = new BotService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error:\n\n{ex.Message}",
                    "Init Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════ LAYOUT ════════════════════════════════════════
        private void BuildUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(24, 18, 24, 18)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 398));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));

            Controls.Add(root);
            root.BringToFront();

            BuildUserPhone(root);
            BuildArrow(root, 1);
            BuildServer(root);
            BuildArrow(root, 3);
            BuildBotPhone(root);
        }

        // ══════════════════ USER PHONE ════════════════════════════════════
        private void BuildUserPhone(TableLayoutPanel parent)
        {
            var wrap = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            parent.Controls.Add(wrap, 0, 0);
            wrap.Controls.Add(DeviceLabel("// USER_DEVICE"));

            var phone = PhoneFrame(318, 730);
            phone.Location = new Point(0, 26);
            wrap.Controls.Add(phone);

            var hdr = new Panel { Height = 68, Dock = DockStyle.Top, BackColor = HEADER_BG };
            phone.Controls.Add(hdr);
            hdr.Controls.Add(CircleAvatar("AI", Color.FromArgb(0, 100, 82)));
            hdr.Controls.Add(new Label
            {
                Text = "AI Bot",
                ForeColor = TEXT_CLR,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(62, 11)
            });
            lblUserStatus = new Label
            {
                Text = "● ONLINE",
                ForeColor = GREEN,
                Font = new Font("Courier New", 9, FontStyle.Bold),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(62, 36)
            };
            hdr.Controls.Add(lblUserStatus);

            pnlUserChat = MakeChatArea();
            phone.Controls.Add(pnlUserChat);

            var bar = MakeInputBar(phone);
            txtInput = new TextBox
            {
                BackColor = Color.FromArgb(42, 57, 66),
                ForeColor = TEXT_CLR,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 12),
                Width = 220,
                Height = 36,
                Location = new Point(10, 12)
            };
            txtInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                { e.SuppressKeyPress = true; _ = SendMessage(); }
            };
            bar.Controls.Add(txtInput);

            btnSend = new Button
            {
                Text = "➤",
                BackColor = GREEN,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(46, 42),
                Location = new Point(240, 10),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 13, FontStyle.Bold)
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += async (s, e) => await SendMessage();
            bar.Controls.Add(btnSend);
        }

        // ══════════════════ SERVER ════════════════════════════════════════
        private void BuildServer(TableLayoutPanel parent)
        {
            var wrap = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            parent.Controls.Add(wrap, 2, 0);
            wrap.Controls.Add(DeviceLabel("// SENTIMENT_SERVER"));

            var srv = new Panel
            {
                Size = new Size(393, 730),
                Location = new Point(0, 26),
                BackColor = SERVER_BG
            };
            srv.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var glow = new Pen(Color.FromArgb(28, 0, 255, 65), 8);
                using var line = new Pen(GREEN, 1);
                var r = new Rectangle(1, 1, srv.Width - 2, srv.Height - 2);
                e.Graphics.DrawRectangle(glow, r);
                e.Graphics.DrawRectangle(line, r);
            };
            wrap.Controls.Add(srv);

            var titleBar = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(0, 20, 0)
            };
            srv.Controls.Add(titleBar);
            titleBar.Controls.Add(new Label
            {
                Text = "▶  SENTIMENT_ANALYSIS_NODE   v2.0",
                ForeColor = GREEN,
                Font = new Font("Courier New", 9, FontStyle.Bold),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(10, 11)
            });

            var statsRow = new Panel
            {
                Height = 62,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(2, 12, 2)
            };
            srv.Controls.Add(statsRow);
            int sx = 14;
            lblTotal = StatLabel("0", "TOTAL", GREEN, ref sx, statsRow);
            lblPos = StatLabel("0", "POSITIVE", Color.FromArgb(0, 220, 60), ref sx, statsRow);
            lblNeg = StatLabel("0", "NEGATIVE", Color.FromArgb(255, 70, 70), ref sx, statsRow);
            lblNeu = StatLabel("0", "NEUTRAL", Color.FromArgb(160, 160, 160), ref sx, statsRow);

            var logScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 6, 8, 6)
            };
            srv.Controls.Add(logScroll);

            flpLog = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Width = 368
            };
            logScroll.Controls.Add(flpLog);

            var footer = new Panel
            {
                Height = 30,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(0, 20, 0)
            };
            srv.Controls.Add(footer);

            var dot = new Panel { Size = new Size(10, 10), Location = new Point(10, 10), BackColor = GREEN };
            var gp = new GraphicsPath(); gp.AddEllipse(0, 0, 10, 10);
            dot.Region = new Region(gp);
            footer.Controls.Add(dot);

            var blink = new System.Windows.Forms.Timer { Interval = 700 };
            blink.Tick += (s, e) => dot.Visible = !dot.Visible;
            blink.Start();

            lblServerStatus = new Label
            {
                Text = "IDLE — MONITORING TRAFFIC",
                ForeColor = DIM_GREEN,
                Font = new Font("Courier New", 8),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(28, 8)
            };
            footer.Controls.Add(lblServerStatus);
        }

        // ══════════════════ BOT PHONE ═════════════════════════════════════
        private void BuildBotPhone(TableLayoutPanel parent)
        {
            var wrap = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            parent.Controls.Add(wrap, 4, 0);
            wrap.Controls.Add(DeviceLabel("// AI_BOT_DEVICE"));

            var phone = PhoneFrame(318, 730);
            phone.Location = new Point(0, 26);
            wrap.Controls.Add(phone);

            var hdr = new Panel { Height = 68, Dock = DockStyle.Top, BackColor = HEADER_BG };
            phone.Controls.Add(hdr);
            hdr.Controls.Add(CircleAvatar("U", Color.FromArgb(42, 57, 66)));
            hdr.Controls.Add(new Label
            {
                Text = "User",
                ForeColor = TEXT_CLR,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(62, 11)
            });
            hdr.Controls.Add(new Label
            {
                Text = "● ONLINE",
                ForeColor = GREEN,
                Font = new Font("Courier New", 9, FontStyle.Bold),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(62, 36)
            });

            pnlBotChat = MakeChatArea();
            phone.Controls.Add(pnlBotChat);

            var bar = MakeInputBar(phone);
            bar.Enabled = false;
            bar.Controls.Add(new TextBox
            {
                BackColor = Color.FromArgb(32, 44, 50),
                ForeColor = Color.FromArgb(80, 90, 100),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 12),
                Width = 220,
                Location = new Point(10, 12),
                Text = "AI is typing...",
                ReadOnly = true
            });
        }

        // ══════════════════ ARROWS ════════════════════════════════════════
        private void BuildArrow(TableLayoutPanel parent, int col)
        {
            var pnl = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            parent.Controls.Add(pnl, col, 0);
            var lbl = new Label
            {
                Text = "→\n\n←",
                ForeColor = Color.FromArgb(0, 90, 25),
                Font = new Font("Courier New", 18, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill
            };
            pnl.Controls.Add(lbl);
            bool v = true;
            var t = new System.Windows.Forms.Timer { Interval = 900 };
            t.Tick += (s, e) =>
            {
                lbl.ForeColor = v ? Color.FromArgb(0, 90, 25) : Color.FromArgb(0, 28, 8);
                v = !v;
            };
            t.Start();
        }

        // ══════════════════ SHARED BUILDERS ══════════════════════════════
        private static Label DeviceLabel(string text) => new()
        {
            Text = text,
            ForeColor = GREEN,
            Font = new Font("Courier New", 9, FontStyle.Bold),
            AutoSize = true,
            BackColor = Color.Transparent,
            Location = new Point(0, 4)
        };

        private static Panel PhoneFrame(int w, int h)
        {
            var p = new Panel { Size = new Size(w, h), BackColor = PHONE_BG };
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(50, 50, 50), 2);
                e.Graphics.DrawRectangle(pen, new Rectangle(1, 1, p.Width - 2, p.Height - 2));
                using var nb = new SolidBrush(Color.Black);
                e.Graphics.FillRectangle(nb, p.Width / 2 - 36, 0, 72, 12);
            };
            return p;
        }

        private static Panel CircleAvatar(string initials, Color bg)
        {
            var p = new Panel { Size = new Size(42, 42), Location = new Point(12, 13), BackColor = bg };
            var gp = new GraphicsPath(); gp.AddEllipse(0, 0, 42, 42);
            p.Region = new Region(gp);
            p.Controls.Add(new Label
            {
                Text = initials,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            });
            return p;
        }

        private static Panel MakeChatArea() => new()
        {
            BackColor = CHAT_BG,
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(6, 8, 6, 8)
        };

        private static Panel MakeInputBar(Panel phone)
        {
            var bar = new Panel
            {
                Height = 62,
                Dock = DockStyle.Bottom,
                BackColor = HEADER_BG,
                Padding = new Padding(10)
            };
            phone.Controls.Add(bar);
            return bar;
        }

        // ══════════════════ CHAT BUBBLE ═══════════════════════════════════
        // Renders a proper WhatsApp-style bubble.
        // msgText  = the actual message to display
        // sentiment = "Positive" / "Negative" / "Neutral"
        // isSent   = true → right-aligned green (user sent)
        //            false → left-aligned dark (received)
        private void AddBubble(Panel chat, string msgText,
                               string sentiment, bool isSent)
        {
            if (chat.InvokeRequired)
            {
                chat.Invoke(() => AddBubble(chat, msgText, sentiment, isSent));
                return;
            }

            Color sentColor = sentiment switch
            {
                "Positive" => Color.FromArgb(0, 210, 60),
                "Negative" => Color.FromArgb(255, 70, 70),
                _ => Color.FromArgb(160, 160, 160)
            };
            string sentLabel = sentiment switch
            {
                "Positive" => "● POSITIVE",
                "Negative" => "● NEGATIVE",
                _ => "● NEUTRAL"
            };

            Color bgColor = isSent ? BUBBLE_USER : BUBBLE_BOT;
            int maxInner = 248;
            int padX = 12;
            int padY = 8;

            // Measure everything so the bubble fits the text exactly
            using var g = chat.CreateGraphics();
            var msgFont = new Font("Segoe UI", 12);
            var tagFont = new Font("Courier New", 8, FontStyle.Bold);
            var timeFont = new Font("Courier New", 8);

            var msgSz = g.MeasureString(msgText, msgFont, maxInner);
            var tagSz = g.MeasureString(sentLabel, tagFont);
            var timeSz = g.MeasureString("00:00 ✓✓", timeFont);

            int bw = (int)Math.Min(
                Math.Max(msgSz.Width, Math.Max(tagSz.Width, timeSz.Width)) + padX * 2 + 6,
                maxInner + padX * 2);

            int bh = padY
                   + (int)tagSz.Height + 4
                   + (int)msgSz.Height + 6
                   + (int)timeSz.Height + padY;

            // ── Row (full width of chat panel) ────────────────────────────
            var row = new Panel
            {
                Width = chat.ClientSize.Width - 14,
                Height = bh + 10,
                BackColor = Color.Transparent
            };

            // ── Bubble ────────────────────────────────────────────────────
            var bubble = new Panel
            {
                Size = new Size(bw, bh),
                Top = 5,
                BackColor = Color.Transparent  // painted below
            };

            bubble.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(bgColor);
                using var rr = RoundedRect(new Rectangle(0, 0, bw - 1, bh - 1), 10);
                e.Graphics.FillPath(br, rr);
            };

            // Sentiment tag
            bubble.Controls.Add(new Label
            {
                Text = sentLabel,
                ForeColor = sentColor,
                Font = tagFont,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(padX, padY)
            });

            // Message text — this is what the user typed / bot replied
            bubble.Controls.Add(new Label
            {
                Text = msgText,
                ForeColor = TEXT_CLR,
                Font = msgFont,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(bw - padX * 2, (int)msgSz.Height + 4),
                Location = new Point(padX, padY + (int)tagSz.Height + 4)
            });

            // Timestamp + ticks
            string timeStr = isSent
                ? $"{DateTime.Now:HH:mm}  ✓✓"
                : $"{DateTime.Now:HH:mm}";

            var timeLbl = new Label
            {
                Text = timeStr,
                ForeColor = Color.FromArgb(110, 130, 140),
                Font = timeFont,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            // Position after bubble sizes itself
            bubble.Controls.Add(timeLbl);
            bubble.Layout += (s, e) =>
            {
                timeLbl.Location = new Point(
                    bw - (int)timeSz.Width - padX,
                    bh - (int)timeSz.Height - padY + 2);
            };

            // ── Align bubble: right = sent, left = received ───────────────
            bubble.Left = isSent ? row.Width - bw - 4 : 4;
            row.Resize += (s, e) =>
            {
                bubble.Left = isSent ? row.Width - bw - 4 : 4;
            };

            row.Controls.Add(bubble);
            chat.Controls.Add(row);
            ScrollDown(chat);
        }

        // ══════════════════ TYPING INDICATOR ══════════════════════════════
        private Panel AddTyping(Panel chat)
        {
            if (chat.InvokeRequired)
            { Panel r = null!; chat.Invoke(() => r = AddTyping(chat)); return r; }

            var row = new Panel
            {
                Width = chat.ClientSize.Width - 14,
                Height = 44,
                BackColor = Color.Transparent
            };
            var bubble = new Panel
            {
                Size = new Size(72, 34),
                Location = new Point(4, 5),
                BackColor = Color.Transparent
            };
            bubble.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(BUBBLE_BOT);
                using var rr = RoundedRect(new Rectangle(0, 0, 71, 33), 10);
                e.Graphics.FillPath(br, rr);
            };

            var dots = new Label
            {
                Text = "● ● ●",
                ForeColor = Color.FromArgb(110, 130, 140),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(8, 5)
            };
            bubble.Controls.Add(dots);
            row.Controls.Add(bubble);
            chat.Controls.Add(row);
            ScrollDown(chat);

            string[] frames = { "●", "● ●", "● ● ●" };
            int f = 0;
            var t = new System.Windows.Forms.Timer { Interval = 450 };
            t.Tick += (s, e) => { if (!dots.IsDisposed) dots.Text = frames[f++ % 3]; };
            t.Start();
            row.Tag = t;
            return row;
        }

        private void RemoveTyping(Panel chat, Panel? row)
        {
            if (row == null) return;
            if (chat.InvokeRequired) { chat.Invoke(() => RemoveTyping(chat, row)); return; }
            if (row.Tag is System.Windows.Forms.Timer t) t.Stop();
            chat.Controls.Remove(row);
        }

        private static void ScrollDown(Panel p) =>
            p.AutoScrollPosition = new Point(0, p.DisplayRectangle.Height);

        // ══════════════════ SEND MESSAGE ══════════════════════════════════
        // Flow:
        //   1. User types message → show in USER phone (right bubble) + BOT phone (left bubble)
        //   2. ML.NET analyzes sentiment of user message
        //   3. Claude API generates bot reply
        //   4. Show bot reply in USER phone (left bubble, received) + BOT phone (right bubble, sent)
        private async Task SendMessage()
        {
            string userText = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(userText) || !btnSend.Enabled) return;
            if (_sentimentService == null || _botService == null)
            {
                MessageBox.Show("Services not initialized. Check startup errors.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            txtInput.Clear();
            btnSend.Enabled = false;
            lblUserStatus.Text = "● PROCESSING...";
            lblUserStatus.ForeColor = Color.Orange;
            SetStatus("PROCESSING...");

            Log("IN", $"USER → \"{Trunc(userText, 42)}\"", "in");
            Log("ML", "Running SdcaMaximumEntropy...", "ml");

            // Show typing on bot side while analyzing
            var botTyping = AddTyping(pnlBotChat);

            try
            {
                // ── Step 1: Analyze user message ──────────────────────────
                var (uSent, uEmoji, uConf) = _sentimentService.Analyze(userText);
                RemoveTyping(pnlBotChat, botTyping);

                // USER phone: right bubble (user sent this)
                // BOT phone:  left bubble  (bot received this)
                AddBubble(pnlUserChat, userText, uSent, isSent: true);
                AddBubble(pnlBotChat, userText, uSent, isSent: false);
                UpdateStats(uSent);

                Log("ML", $"USER ■ {uSent} {uEmoji}  ({uConf * 100:F1}%)",
                    uSent == "Positive" ? "pos" : uSent == "Negative" ? "neg" : "neu");
                Log("BOT", "Calling Claude API...", "bot");

                // ── Step 2: Get Claude reply ───────────────────────────────
                await Task.Delay(400);
                var userTyping = AddTyping(pnlUserChat);  // typing shown on user side

                string botReply;
                try
                {
                    botReply = await _botService.GetBotReplyAsync(userText);
                }
                catch (Exception apiEx)
                {
                    RemoveTyping(pnlUserChat, userTyping);
                    // Show the clean error message — not raw JSON
                    botReply = $"⚠ {apiEx.Message}";
                    Log("ERR", Trunc(apiEx.Message, 55), "neg");
                }

                await Task.Delay(350);
                RemoveTyping(pnlUserChat, userTyping);

                // ── Step 3: Analyze bot reply ─────────────────────────────
                var (bSent, bEmoji, bConf) = _sentimentService.Analyze(botReply);

                // USER phone: left bubble  (bot's reply received by user)
                // BOT phone:  right bubble (bot sent this)
                AddBubble(pnlUserChat, botReply, bSent, isSent: false);
                AddBubble(pnlBotChat, botReply, bSent, isSent: true);
                UpdateStats(bSent);

                Log("OUT", $"BOT → \"{Trunc(botReply, 42)}\"", "out");
                Log("ML", $"BOT  ■ {bSent} {bEmoji}  ({bConf * 100:F1}%)",
                    bSent == "Positive" ? "pos" : bSent == "Negative" ? "neg" : "neu");
                Log("──", new string('─', 46), "text");

                SetStatus($"IDLE — T:{_total}  POS:{_pos}  NEG:{_neg}  NEU:{_neu}");
                lblUserStatus.Text = "● ONLINE";
                lblUserStatus.ForeColor = GREEN;
            }
            catch (Exception ex)
            {
                RemoveTyping(pnlBotChat, botTyping);
                Log("ERR", ex.Message, "neg");
                SetStatus("ERROR — SEE LOG");
                lblUserStatus.Text = "● ERROR";
                lblUserStatus.ForeColor = Color.Red;
            }
            finally
            {
                btnSend.Enabled = true;
                txtInput.Focus();
            }
        }

        // ══════════════════ LOG ═══════════════════════════════════════════
        private void Log(string tag, string msg, string type)
        {
            if (flpLog.InvokeRequired) { flpLog.Invoke(() => Log(tag, msg, type)); return; }

            Color c = type switch
            {
                "in" => Color.FromArgb(0, 185, 255),
                "out" => Color.FromArgb(255, 185, 0),
                "ml" => GREEN,
                "bot" => Color.FromArgb(200, 80, 255),
                "pos" => Color.FromArgb(0, 220, 60),
                "neg" => Color.FromArgb(255, 80, 80),
                "neu" => Color.FromArgb(150, 150, 150),
                _ => Color.FromArgb(0, 170, 40)
            };

            flpLog.Controls.Add(new Label
            {
                Text = $"[{DateTime.Now:HH:mm:ss}] [{tag,-5}] {msg}",
                ForeColor = c,
                Font = new Font("Courier New", 8),
                BackColor = Color.Transparent,
                AutoSize = true,
                MaximumSize = new Size(355, 0),
                Margin = new Padding(0, 1, 0, 1)
            });

            while (flpLog.Controls.Count > 120)
                flpLog.Controls.RemoveAt(0);

            var scroll = flpLog.Parent as Panel;
            if (scroll != null && !scroll.IsDisposed)
                scroll.AutoScrollPosition = new Point(0, scroll.DisplayRectangle.Height);
        }

        private void SetStatus(string text)
        {
            if (lblServerStatus.InvokeRequired)
            { lblServerStatus.Invoke(() => SetStatus(text)); return; }
            lblServerStatus.Text = text;
        }

        private void UpdateStats(string sentiment)
        {
            _total++;
            if (sentiment == "Positive") _pos++;
            else if (sentiment == "Negative") _neg++;
            else _neu++;

            void Apply()
            {
                lblTotal.Text = _total.ToString();
                lblPos.Text = _pos.ToString();
                lblNeg.Text = _neg.ToString();
                lblNeu.Text = _neu.ToString();
            }
            if (lblTotal.InvokeRequired) lblTotal.Invoke(Apply); else Apply();
        }

        private static Label StatLabel(string val, string name,
            Color color, ref int x, Panel parent)
        {
            var vl = new Label
            {
                Text = val,
                ForeColor = color,
                Font = new Font("Courier New", 16, FontStyle.Bold),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(x, 6)
            };
            var nl = new Label
            {
                Text = name,
                ForeColor = Color.FromArgb(0, 90, 20),
                Font = new Font("Courier New", 7),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(x, 32)
            };
            parent.Controls.Add(vl);
            parent.Controls.Add(nl);
            x += 90;
            return vl;
        }

        private void BootLog()
        {
            Task.Run(async () =>
            {
                await Task.Delay(400); Log("BOOT", "ML.NET SDCA trainer loading...", "text");
                await Task.Delay(700); Log("BOOT", "Training sentiment model...", "text");
                await Task.Delay(900); Log("BOOT", "Reading .env configuration...", "bot");
                await Task.Delay(500); Log("BOOT", "Claude API client ready.", "bot");
                await Task.Delay(200); Log("READY", "■ SERVER ONLINE — AWAITING INPUT", "pos");
            });
        }

        private static string Trunc(string s, int max) =>
            s.Length > max ? s[..max] + "…" : s;

        // ══════════════════ ROUNDED RECT ══════════════════════════════════
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var gp = new GraphicsPath();
            gp.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            gp.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            gp.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            gp.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            gp.CloseFigure();
            return gp;
        }

        // ══════════════════ MATRIX RAIN ═══════════════════════════════════
        private void StartMatrixRain()
        {
            _matrixBmp = new Bitmap(Width, Height);
            _matrixGfx = Graphics.FromImage(_matrixBmp);
            int cols = Width / 14;
            _drops = new float[cols];
            for (int i = 0; i < cols; i++)
                _drops[i] = _rnd.Next(Height / 14);

            pbxMatrix = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pbxMatrix.Image = _matrixBmp;
            Controls.Add(pbxMatrix);
            pbxMatrix.SendToBack();

            _matrixTimer = new System.Windows.Forms.Timer { Interval = 55 };
            _matrixTimer.Tick += (s, e) => DrawMatrix();
            _matrixTimer.Start();
        }

        private void DrawMatrix()
        {
            using var fade = new SolidBrush(Color.FromArgb(18, 8, 8, 8));
            _matrixGfx.FillRectangle(fade, 0, 0, _matrixBmp.Width, _matrixBmp.Height);
            using var font = new Font("Courier New", 11);
            using var brush = new SolidBrush(Color.FromArgb(16, 0, 255, 65));
            for (int i = 0; i < _drops.Length; i++)
            {
                _matrixGfx.DrawString(
                    MATRIX_CHARS[_rnd.Next(MATRIX_CHARS.Length)].ToString(),
                    font, brush, i * 14, _drops[i] * 14);
                if (_drops[i] * 14 > _matrixBmp.Height && _rnd.NextDouble() > 0.975)
                    _drops[i] = 0;
                _drops[i] += 0.5f;
            }
            pbxMatrix.Refresh();
        }

        // ══════════════════ CLEANUP ═══════════════════════════════════════
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _matrixTimer?.Stop();
            _matrixGfx?.Dispose();
            _matrixBmp?.Dispose();
            _botService?.Dispose();
            base.OnFormClosed(e);
        }
    }
}