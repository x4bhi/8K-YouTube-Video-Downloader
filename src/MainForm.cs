using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO.Compression;

namespace YouTubeDownloader
{
    //  MAIN FORM (Production Ready)
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    public class DownloaderForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        // â”€â”€ Colour Palette â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        static readonly Color C_BG = Color.FromArgb(15, 15, 18);
        static readonly Color C_SURFACE = Color.FromArgb(24, 24, 28);
        static readonly Color C_INPUT = Color.FromArgb(30, 30, 35);
        static readonly Color C_RED = Color.FromArgb(229, 57, 53);
        static readonly Color C_RED_DARK = Color.FromArgb(183, 28, 28);
        static readonly Color C_BLUE = Color.FromArgb(66, 133, 244);
        static readonly Color C_TEXT = Color.FromArgb(235, 235, 235);
        static readonly Color C_TEXT_DIM = Color.FromArgb(130, 130, 138);
        static readonly Color C_SUCCESS = Color.FromArgb(67, 160, 71);
        static readonly Color C_WARN = Color.FromArgb(251, 140, 0);

        const string URL_PLACEHOLDER = "Paste YouTube video or playlist linkâ€¦";

        // â”€â”€ Controls â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        TextBox txtUrl;
        TextBox txtSavePath;
        IsolateButton btnAnalyze;
        IsolateButton btnBrowse;
        IsolateButton btnDownload;
        IsolateButton btnUpdate;
        IsolateRadioButton radVideo;
        IsolateRadioButton radAudio;
        IsolateComboBox cmbVideoQuality;
        IsolateComboBox cmbAudioQuality;
        IsolateCheckBox chkPlaylist;
        IsolateProgressBar progressBar;
        Label lblStatus;
        TextBox txtLog;

        // â”€â”€ Engine & State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        YtDlpEngine engine;
        CancellationTokenSource cts;
        List<MediaFormat> availableVideoFormats = new List<MediaFormat>();
        List<MediaFormat> availableAudioFormats = new List<MediaFormat>();
        bool isExtracting = true;

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DownloaderForm());
        }

        public DownloaderForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int dark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
            _ = StartExtractionAsync();
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  UI CONSTRUCTION
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private void InitializeComponent()
        {
            this.Text = "ABHISHT PANDEY - 8K YouTube Video Downloader";
            this.ClientSize = new Size(700, 640);
            this.MinimumSize = new Size(716, 670);
            this.BackColor = C_BG;
            this.ForeColor = C_TEXT;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9f);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            const int PAD = 24;
            const int ROW = 652;
            const int BTN_W = 110;
            const int GAP = 8;
            const int INP_W = ROW - BTN_W - GAP;
            int y = 0;

            // Header
            Panel pnlHeader = new Panel();
            pnlHeader.BackColor = Color.FromArgb(20, 20, 24);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 68;

            Label lblBrand1 = new Label();
            lblBrand1.AutoSize = true;
            lblBrand1.Text = "NexTube";
            lblBrand1.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            lblBrand1.ForeColor = C_RED;
            lblBrand1.Location = new Point(205, 14);
            pnlHeader.Controls.Add(lblBrand1);

            Label lblBrand2 = new Label();
            lblBrand2.AutoSize = true;
            lblBrand2.Text = "Downloader";
            lblBrand2.Font = new Font("Segoe UI", 20f, FontStyle.Regular);
            lblBrand2.ForeColor = Color.White;
            lblBrand2.Location = new Point(335, 14);
            pnlHeader.Controls.Add(lblBrand2);

            Panel stripe = new Panel();
            stripe.BackColor = Color.FromArgb(35, 35, 40);
            stripe.Dock = DockStyle.Bottom;
            stripe.Height = 1;
            pnlHeader.Controls.Add(stripe);

            this.Controls.Add(pnlHeader);

            // Content
            Panel pnlContent = new Panel();
            pnlContent.Location = new Point(0, 70);
            pnlContent.Size = new Size(700, 570);
            pnlContent.BackColor = C_BG;
            this.Controls.Add(pnlContent);

            y = 18;

            // URL Section
            pnlContent.Controls.Add(MkSectionLabel("VIDEO / PLAYLIST URL", PAD, y));
            y += 18;

            txtUrl = new TextBox();
            txtUrl.Location = new Point(PAD, y);
            txtUrl.Size = new Size(INP_W, 32);
            txtUrl.BackColor = C_INPUT;
            txtUrl.ForeColor = Color.FromArgb(160, 160, 168);
            txtUrl.BorderStyle = BorderStyle.FixedSingle;
            txtUrl.Font = new Font("Segoe UI", 9f);
            txtUrl.Text = URL_PLACEHOLDER;
            txtUrl.Enter += delegate { if (txtUrl.Text == URL_PLACEHOLDER) { txtUrl.Text = ""; txtUrl.ForeColor = C_TEXT; } };
            txtUrl.Leave += delegate { if (string.IsNullOrWhiteSpace(txtUrl.Text)) { txtUrl.Text = URL_PLACEHOLDER; txtUrl.ForeColor = Color.FromArgb(160, 160, 168); } };
            pnlContent.Controls.Add(txtUrl);

            btnAnalyze = new IsolateButton(C_BLUE, Color.FromArgb(50, 110, 220), Color.FromArgb(25, 75, 170));
            btnAnalyze.Text = "ANALYZE";
            btnAnalyze.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnAnalyze.Location = new Point(PAD + INP_W + GAP, y);
            btnAnalyze.Size = new Size(BTN_W, 32);
            btnAnalyze.Click += async (s, e) => await BtnAnalyze_ClickAsync();
            pnlContent.Controls.Add(btnAnalyze);
            y += 44;

            // Save Folder
            pnlContent.Controls.Add(MkSectionLabel("SAVE FOLDER", PAD, y));
            y += 18;

            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (!Directory.Exists(defaultPath))
                defaultPath = AppDomain.CurrentDomain.BaseDirectory;

            txtSavePath = new TextBox();
            txtSavePath.Location = new Point(PAD, y);
            txtSavePath.Size = new Size(INP_W, 32);
            txtSavePath.BackColor = C_INPUT;
            txtSavePath.ForeColor = C_TEXT;
            txtSavePath.BorderStyle = BorderStyle.FixedSingle;
            txtSavePath.Font = new Font("Segoe UI", 9f);
            txtSavePath.Text = defaultPath;
            pnlContent.Controls.Add(txtSavePath);

            btnBrowse = new IsolateButton(Color.FromArgb(42, 42, 50), Color.FromArgb(58, 58, 68), Color.FromArgb(28, 28, 35));
            btnBrowse.Text = "BROWSE";
            btnBrowse.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnBrowse.Location = new Point(PAD + INP_W + GAP, y);
            btnBrowse.Size = new Size(BTN_W, 32);
            btnBrowse.Click += BtnBrowse_Click;
            pnlContent.Controls.Add(btnBrowse);
            y += 46;

            pnlContent.Controls.Add(MkDivider(PAD, y, ROW, 1));
            y += 16;

            // Download Mode
            pnlContent.Controls.Add(MkSectionLabel("DOWNLOAD MODE", PAD, y));
            y += 20;

            radVideo = new IsolateRadioButton();
            radVideo.Text = "Video";
            radVideo.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            radVideo.ForeColor = C_TEXT;
            radVideo.Location = new Point(PAD, y);
            radVideo.Size = new Size(100, 24);
            radVideo.Checked = true;
            radVideo.CheckedChanged += FormatMode_Changed;
            pnlContent.Controls.Add(radVideo);

            radAudio = new IsolateRadioButton();
            radAudio.Text = "Audio Only  (MP3 / FLAC / WAV)";
            radAudio.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            radAudio.ForeColor = C_TEXT;
            radAudio.Location = new Point(PAD + 110, y);
            radAudio.Size = new Size(260, 24);
            radAudio.CheckedChanged += FormatMode_Changed;
            pnlContent.Controls.Add(radAudio);

            chkPlaylist = new IsolateCheckBox();
            chkPlaylist.Text = "Download Playlist";
            chkPlaylist.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            chkPlaylist.ForeColor = C_TEXT_DIM;
            chkPlaylist.Size = new Size(152, 24);
            chkPlaylist.Location = new Point(PAD + ROW - 152, y);
            pnlContent.Controls.Add(chkPlaylist);
            y += 38;

            // Quality Settings
            pnlContent.Controls.Add(MkSectionLabel("QUALITY SETTINGS", PAD, y));
            y += 20;

            const int CARD_GAP = 8;
            int cardW = (ROW - CARD_GAP) / 2;

            var pnlVideoSection = MkCard(PAD, y, cardW, 72);
            pnlContent.Controls.Add(pnlVideoSection);

            Label lblVideoQualityHdr = new Label();
            lblVideoQualityHdr.Text = "Video Quality";
            lblVideoQualityHdr.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            lblVideoQualityHdr.ForeColor = C_TEXT_DIM;
            lblVideoQualityHdr.BackColor = Color.Transparent;
            lblVideoQualityHdr.Location = new Point(12, 10);
            lblVideoQualityHdr.Size = new Size(cardW - 24, 16);
            pnlVideoSection.Controls.Add(lblVideoQualityHdr);

            cmbVideoQuality = new IsolateComboBox();
            cmbVideoQuality.Location = new Point(12, 30);
            cmbVideoQuality.Size = new Size(cardW - 24, 26);
            cmbVideoQuality.Font = new Font("Segoe UI", 8.5f);
            cmbVideoQuality.MaxDropDownItems = 15;
            ResetVideoQualities();
            pnlVideoSection.Controls.Add(cmbVideoQuality);

            var pnlAudioSection = MkCard(PAD + cardW + CARD_GAP, y, cardW, 72);
            pnlContent.Controls.Add(pnlAudioSection);

            Label lblAudioQualityHdr = new Label();
            lblAudioQualityHdr.Text = "Audio Quality";
            lblAudioQualityHdr.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            lblAudioQualityHdr.ForeColor = C_TEXT_DIM;
            lblAudioQualityHdr.BackColor = Color.Transparent;
            lblAudioQualityHdr.Location = new Point(12, 10);
            lblAudioQualityHdr.Size = new Size(cardW - 24, 16);
            pnlAudioSection.Controls.Add(lblAudioQualityHdr);

            cmbAudioQuality = new IsolateComboBox();
            cmbAudioQuality.Location = new Point(12, 30);
            cmbAudioQuality.Size = new Size(cardW - 24, 26);
            cmbAudioQuality.Font = new Font("Segoe UI", 8.5f);
            cmbAudioQuality.Enabled = false;
            cmbAudioQuality.MaxDropDownItems = 15;
            ResetAudioQualities();
            pnlAudioSection.Controls.Add(cmbAudioQuality);

            y += 88;

            pnlContent.Controls.Add(MkDivider(PAD, y, ROW, 1));
            y += 16;

            // Action Buttons
            const int UPD_BTN_W = 104;
            const int DL_BTN_W = ROW - UPD_BTN_W - GAP;

            btnDownload = new IsolateButton(Color.FromArgb(60, 60, 66), Color.FromArgb(60, 60, 66), Color.FromArgb(60, 60, 66));
            btnDownload.Text = "EXTRACTING COMPONENTSâ€¦";
            btnDownload.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnDownload.Location = new Point(PAD, y);
            btnDownload.Size = new Size(DL_BTN_W, 44);
            btnDownload.CornerRadius = 8;
            btnDownload.Enabled = false;
            btnDownload.Click += async (s, e) => await BtnDownload_ClickAsync();
            pnlContent.Controls.Add(btnDownload);

            btnUpdate = new IsolateButton(Color.FromArgb(42, 42, 50), Color.FromArgb(58, 58, 68), Color.FromArgb(28, 28, 35));
            btnUpdate.Text = "UPDATE";
            btnUpdate.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnUpdate.Location = new Point(PAD + DL_BTN_W + GAP, y);
            btnUpdate.Size = new Size(UPD_BTN_W, 44);
            btnUpdate.CornerRadius = 8;
            btnUpdate.Enabled = false;
            btnUpdate.Click += async (s, e) => await BtnUpdate_ClickAsync();
            pnlContent.Controls.Add(btnUpdate);
            y += 56;

            // Progress
            progressBar = new IsolateProgressBar();
            progressBar.Location = new Point(PAD, y);
            progressBar.Size = new Size(ROW, 5);
            pnlContent.Controls.Add(progressBar);
            y += 12;

            lblStatus = new Label();
            lblStatus.Text = "Initializing â€” extracting standalone componentsâ€¦";
            lblStatus.Location = new Point(PAD, y);
            lblStatus.Size = new Size(ROW, 18);
            lblStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblStatus.ForeColor = C_TEXT_DIM;
            lblStatus.BackColor = Color.Transparent;
            pnlContent.Controls.Add(lblStatus);
            y += 26;

            // Activity Log
            pnlContent.Controls.Add(MkSectionLabel("ACTIVITY LOG", PAD, y));
            y += 18;

            int logH = pnlContent.Height - y - 16;

            Panel pnlLog = new Panel();
            pnlLog.Location = new Point(PAD, y);
            pnlLog.Size = new Size(ROW, logH);
            pnlLog.BackColor = Color.FromArgb(18, 18, 22);
            pnlLog.BorderStyle = BorderStyle.FixedSingle;
            pnlContent.Controls.Add(pnlLog);

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.None;
            txtLog.ReadOnly = true;
            txtLog.Dock = DockStyle.Fill;
            txtLog.BackColor = Color.FromArgb(18, 18, 22);
            txtLog.ForeColor = Color.FromArgb(90, 200, 110);
            txtLog.Font = new Font("Consolas", 8f);
            txtLog.BorderStyle = BorderStyle.None;
            pnlLog.Controls.Add(txtLog);
        }

        private Label MkSectionLabel(string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lbl.ForeColor = C_TEXT_DIM;
            lbl.BackColor = Color.Transparent;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            return lbl;
        }

        private Panel MkCard(int x, int y, int w, int h)
        {
            Panel p = new Panel();
            p.Location = new Point(x, y);
            p.Size = new Size(w, h);
            p.BackColor = Color.FromArgb(24, 24, 28);
            p.BorderStyle = BorderStyle.FixedSingle;
            return p;
        }

        private Panel MkDivider(int x, int y, int w, int h)
        {
            Panel p = new Panel();
            p.Location = new Point(x, y);
            p.Size = new Size(w, h);
            p.BackColor = Color.FromArgb(40, 40, 45);
            return p;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  QUALITY MANAGEMENT
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private void ResetVideoQualities()
        {
            cmbVideoQuality.Items.Clear();
            cmbVideoQuality.Items.Add(new MediaFormat { DisplayLabel = "Best Video + Best Audio (Auto)" });
            cmbVideoQuality.Items.Add("Analyze link to view all available qualities...");
            cmbVideoQuality.SelectedIndex = 0;
            availableVideoFormats.Clear();
        }

        private void ResetAudioQualities()
        {
            cmbAudioQuality.Items.Clear();
            cmbAudioQuality.Items.Add(new MediaFormat { DisplayLabel = "Best Audio (Auto)" });
            cmbAudioQuality.Items.Add("Analyze link to view all available qualities...");
            cmbAudioQuality.SelectedIndex = 0;
            availableAudioFormats.Clear();
        }

        private void FormatMode_Changed(object sender, EventArgs e)
        {
            bool isAudio = radAudio.Checked;
            cmbVideoQuality.Enabled = !isAudio;
            cmbAudioQuality.Enabled = isAudio;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  EXTRACTION WITH VALIDATION
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private async Task StartExtractionAsync()
        {
            try
            {
                btnDownload.Text = "INITIALIZING CORE...";
                AppendLog("[INIT] Beginning core engine initialization...");
                
                var (ytdlpPath, ffmpegDir) = await ResourceExtractor.ExtractAsync(msg => 
                {
                    SetStatus(msg, C_TEXT_DIM);
                    AppendLog($"[INIT] {msg}");
                    btnDownload.Text = "EXTRACTING FILES...";
                });

                engine = new YtDlpEngine(ytdlpPath, ffmpegDir);
                engine.ProgressChanged += Engine_ProgressChanged;
                engine.LogReceived += Engine_LogReceived;

                btnDownload.Text = "VALIDATING...";
                SetStatus("Validating tools...", C_TEXT_DIM);
                AppendLog("[INIT] Validating engine integrity...");

                // FIX #6: Validate Engine before enabling download
                var validationResult = await engine.ValidateToolsAsync();
                if (!validationResult.Success)
                {
                    SetStatus("Setup failed. Please check Activity Log.", C_WARN);
                    AppendLog("[ERROR] Setup validation failed: " + validationResult.ErrorMessage);
                    
                    // FIX: Reset UI so it doesn't get stuck
                    isExtracting = false;
                    btnDownload.Text = "DOWNLOAD NOW";
                    return;
                }

                isExtracting = false;
                SetDownloadReady();
                SetStatus("Ready to download.", C_SUCCESS);
                AppendLog("[INIT] Engine successfully validated and ready.");
            }
            catch (Exception ex)
            {
                SetStatus("Initialization failed. Check Activity Log.", C_WARN);
                AppendLog("[ERROR] Error during initialization: " + ex.Message);
                
                isExtracting = false;
                btnDownload.Text = "DOWNLOAD NOW";
            }
        }

        private void SetDownloadReady()
        {
            btnDownload.Text = "DOWNLOAD NOW";
            btnDownload.UpdateColors(C_RED, Color.FromArgb(200, 40, 40), C_RED_DARK);
            btnDownload.Enabled = true;
            btnUpdate.Enabled = true;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  BROWSE
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select save folder:";
                fbd.SelectedPath = txtSavePath.Text;
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtSavePath.Text = fbd.SelectedPath;
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  ANALYZE (FIX #1 applied)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private async Task BtnAnalyze_ClickAsync()
        {
            if (isExtracting) return;

            string url = txtUrl.Text.Trim();

            // FIX #1: Comprehensive placeholder validation
            if (string.IsNullOrWhiteSpace(url) || url == URL_PLACEHOLDER || txtUrl.ForeColor != C_TEXT)
            {
                MessageBox.Show("Please enter a valid YouTube URL first.", "8K YouTube Video Downloader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUrl.Focus();
                return;
            }

            SetUIBusy(true, "Analyzing URL formatsâ€¦");
            txtLog.Clear();

            try
            {
                cts = new CancellationTokenSource();
                var result = await engine.AnalyzeFormatsAsync(url, cts.Token);

                if (!result.Success)
                {
                    SetStatus("Analysis failed: " + result.ErrorMessage, C_WARN);
                    AppendLog("[ERROR] " + result.ErrorMessage);
                    MessageBox.Show("Analysis failed:\n\n" + result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                availableVideoFormats = result.Data.video;
                availableAudioFormats = result.Data.audio;

                PopulateQualityDropdowns();

                SetStatus($"Analysis complete â€” {availableVideoFormats.Count} video, {availableAudioFormats.Count} audio formats found.", C_SUCCESS);
                AppendLog($"[ANALYZE] Successfully parsed {availableVideoFormats.Count + availableAudioFormats.Count} formats.");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Analysis cancelled.", C_TEXT_DIM);
            }
            catch (Exception ex)
            {
                SetStatus("Analysis error.", C_WARN);
                AppendLog("[ERROR] " + ex.Message);
                MessageBox.Show("Unexpected error:\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUIBusy(false);
                cts?.Dispose();
                cts = null;
            }
        }

        private void PopulateQualityDropdowns()
        {
            // FIX #5: Smart auto-selection of highest quality
            cmbVideoQuality.Items.Clear();
            cmbVideoQuality.Items.Add(new MediaFormat { DisplayLabel = "Best Video + Best Audio (Auto)" });
            foreach (var fmt in availableVideoFormats)
                cmbVideoQuality.Items.Add(fmt);
            
            // Select highest quality (already sorted by QualityScore descending)
            cmbVideoQuality.SelectedIndex = availableVideoFormats.Count > 0 ? 1 : 0;

            cmbAudioQuality.Items.Clear();
            cmbAudioQuality.Items.Add(new MediaFormat { DisplayLabel = "Best Audio (Auto)" });
            foreach (var fmt in availableAudioFormats)
                cmbAudioQuality.Items.Add(fmt);
            cmbAudioQuality.Items.Add(new MediaFormat { DisplayLabel = "Lossless FLAC" });
            cmbAudioQuality.Items.Add(new MediaFormat { DisplayLabel = "Lossless WAV" });
            
            // Select highest bitrate (already sorted descending)
            cmbAudioQuality.SelectedIndex = availableAudioFormats.Count > 0 ? 1 : 0;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  DOWNLOAD (All fixes applied)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private async Task BtnDownload_ClickAsync()
        {
            if (isExtracting) return;

            if (btnDownload.Text == "CANCEL DOWNLOAD")
            {
                CancelDownload();
                return;
            }

            string url = txtUrl.Text.Trim();
            string savePath = txtSavePath.Text.Trim();

            // FIX #1: Placeholder validation
            if (string.IsNullOrWhiteSpace(url) || url == URL_PLACEHOLDER || txtUrl.ForeColor != C_TEXT)
            {
                MessageBox.Show("Please enter a valid YouTube URL.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUrl.Focus();
                return;
            }

            if (!Directory.Exists(savePath))
            {
                try { Directory.CreateDirectory(savePath); }
                catch
                {
                    MessageBox.Show("Invalid or inaccessible save folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            progressBar.Value = 0;
            txtLog.Clear();

            btnDownload.UpdateColors(Color.FromArgb(130, 0, 0), Color.FromArgb(110, 0, 0), Color.FromArgb(90, 0, 0));
            btnDownload.Text = "CANCEL DOWNLOAD";
            btnDownload.Enabled = true;
            btnUpdate.Enabled = false;
            btnAnalyze.Enabled = false;
            txtUrl.Enabled = false;
            txtSavePath.Enabled = false;
            btnBrowse.Enabled = false;
            radVideo.Enabled = false;
            radAudio.Enabled = false;
            cmbVideoQuality.Enabled = false;
            cmbAudioQuality.Enabled = false;
            chkPlaylist.Enabled = false;
            SetStatus("Preparing downloadâ€¦", C_TEXT_DIM);

            bool isAudio = radAudio.Checked;
            var selectedVideo = cmbVideoQuality.SelectedItem as MediaFormat;
            var selectedAudio = cmbAudioQuality.SelectedItem as MediaFormat;
            bool isPlaylist = chkPlaylist.Checked;

            cts = new CancellationTokenSource();

            try
            {
                var result = await engine.DownloadAsync(url, savePath, isAudio, selectedVideo, selectedAudio, isPlaylist, cts.Token);
                OnDownloadFinished(result.Success, result.ErrorMessage);
            }
            catch (OperationCanceledException)
            {
                engine?.CleanupTempFiles();
                OnDownloadFinished(false, "Download cancelled by user.");
            }
            catch (Exception ex)
            {
                AppendLog("[ERROR] " + ex.Message);
                OnDownloadFinished(false, "Unexpected error: " + ex.Message);
            }
            finally
            {
                cts?.Dispose();
                cts = null;
            }
        }

        private void CancelDownload()
        {
            cts?.Cancel();
            SetStatus("Cancelling downloadâ€¦", C_WARN);
        }

        private void OnDownloadFinished(bool success, string errorMessage = null)
        {
            SetDownloadReady();
            btnUpdate.Enabled = true;
            btnAnalyze.Enabled = true;
            txtUrl.Enabled = true;
            txtSavePath.Enabled = true;
            btnBrowse.Enabled = true;
            radVideo.Enabled = true;
            radAudio.Enabled = true;
            cmbVideoQuality.Enabled = radVideo.Checked;
            cmbAudioQuality.Enabled = radAudio.Checked;
            chkPlaylist.Enabled = true;

            if (success)
            {
                progressBar.Value = 100;
                SetStatus("Download completed successfully!", C_SUCCESS);
                MessageBox.Show("Download completed successfully!\n\nFiles saved to:\n" + txtSavePath.Text,
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                progressBar.Value = 0;
                string msg = errorMessage ?? "Download failed. Check the activity log for details.";
                SetStatus(msg, C_WARN);
                MessageBox.Show(msg, "Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  UPDATE
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private async Task BtnUpdate_ClickAsync()
        {
            btnUpdate.Enabled = false;
            btnDownload.Enabled = false;
            SetStatus("Checking for yt-dlp updatesâ€¦", C_TEXT_DIM);

            try
            {
                cts = new CancellationTokenSource();
                var result = await engine.UpdateAsync(cts.Token);

                if (result.Success)
                {
                    SetStatus("Engine update completed!", C_SUCCESS);
                    MessageBox.Show("Download engine has been updated successfully!", "Update Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SetStatus(result.ErrorMessage ?? "Update check completed.", C_TEXT_DIM);
                }
            }
            catch (Exception ex)
            {
                SetStatus("Update error: " + ex.Message, C_WARN);
            }
            finally
            {
                btnUpdate.Enabled = true;
                btnDownload.Enabled = true;
                cts?.Dispose();
                cts = null;
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  ENGINE EVENT HANDLERS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private void Engine_ProgressChanged(object sender, DownloadProgress progress)
        {
            SafeInvoke(() =>
            {
                // Ignore buffered events if we are no longer downloading or if cancellation was requested
                if (cts == null || cts.IsCancellationRequested) return;

                progressBar.Value = progress.Percentage;
                if (!string.IsNullOrEmpty(progress.StatusMessage))
                    SetStatus(progress.StatusMessage, C_TEXT_DIM);
            });
        }

        private void Engine_LogReceived(object sender, string logLine)
        {
            SafeInvoke(() => AppendLog(logLine));
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  UI HELPERS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private void SetUIBusy(bool busy, string statusText = null)
        {
            btnAnalyze.Text = busy ? "ANALYZINGâ€¦" : "ANALYZE";
            btnAnalyze.Enabled = !busy;
            btnDownload.Enabled = !busy;
            txtUrl.Enabled = !busy;
            if (statusText != null)
                SetStatus(statusText, C_TEXT_DIM);
        }

        private void SetStatus(string text, Color color)
        {
            SafeInvoke(() =>
            {
                if (text != null)
                {
                    text = text.Replace("yt-dlp.exe", "Engine").Replace("yt-dlp", "Engine").Replace("yt_dlp", "Engine");
                    
                    var secretPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "MediaServices");
                    text = text.Replace(secretPath, "<System_Component_Directory>");
                }
                    
                lblStatus.Text = text;
                lblStatus.ForeColor = color;
            });
        }

        private void AppendLog(string text)
        {
            SafeInvoke(() =>
            {
                if (text != null)
                {
                    // Hide the engine name
                    text = text.Replace("yt-dlp.exe", "Engine").Replace("yt-dlp", "Engine").Replace("yt_dlp", "Engine");
                    
                    // Hide the secret AppData extraction path completely from the logs
                    var secretPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "MediaServices");
                    text = text.Replace(secretPath, "<System_Component_Directory>");
                }
                    
                if (txtLog.Text.Length > 50000) // Prevent memory bloat
                    txtLog.Text = txtLog.Text.Substring(txtLog.Text.Length - 40000);

                txtLog.AppendText(text + Environment.NewLine);
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            });
        }

        private void SafeInvoke(Action action)
        {
            if (IsHandleCreated && !IsDisposed)
            {
                try { this.Invoke((MethodInvoker)(() => action())); }
                catch { /* form closing */ }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                var res = MessageBox.Show(
                    "A download is in progress. Cancel and exit?",
                    "Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                cts.Cancel();
            }

            engine?.Dispose();
            cts?.Dispose();
            base.OnFormClosing(e);
        }
    }

}
