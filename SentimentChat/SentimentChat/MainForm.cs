namespace SentimentChat
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.Text = "SentimentChat — WhatsApp Simulator";
            this.Size = new Size(1100, 660);
            this.BackColor = Color.FromArgb(10, 10, 10);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

        }
    }
}
