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

