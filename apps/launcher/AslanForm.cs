using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Aslan
{
    // ASLAN-Designsystem (Schwarz-Gold, Löwe)
    public static class AslanTheme
    {
        public static readonly Color Bg = Color.FromArgb(0x0B, 0x0A, 0x08);
        public static readonly Color Surface = Color.FromArgb(0x17, 0x13, 0x0E);
        public static readonly Color Surface2 = Color.FromArgb(0x22, 0x1C, 0x14);
        public static readonly Color Gold = Color.FromArgb(0xFF, 0xBF, 0x47);
        public static readonly Color Orange = Color.FromArgb(0xFF, 0x95, 0x1C);
        public static readonly Color Bronze = Color.FromArgb(0xA8, 0x6B, 0x20);
        public static readonly Color Cream = Color.FromArgb(0xF5, 0xE9, 0xCE);
        public static readonly Color Muted = Color.FromArgb(0xA9, 0xA3, 0x9A);
        public static readonly Color Danger = Color.FromArgb(0xE0, 0x5A, 0x4E);
        public static readonly Color Ok = Color.FromArgb(0x7BC47F);
    }

    public sealed class AslanForm : Form
    {
        private AslanConfig cfg;
        private HardwareInfo hw;
        private List<JavaInstall> javas = new List<JavaInstall>();
        private bool animations;
        private float glowK = 1.0f;

        private Panel sidebar;
        private Panel marker;
        private Panel main;
        private Dictionary<string, Panel> pages = new Dictionary<string, Panel>();
        private Dictionary<string, Button> navBtns = new Dictionary<string, Button>();
        private string currentPage = "Dashboard";
        private System.Windows.Forms.Timer animTimer;
        private int markerTargetY = 0;
        private Panel slidePanel;
        private int slideX = 0;

        private Label statusLabel;
        private Label versionLabel;
        private Panel notifPanel;
        private Label notifLabel;
        private System.Windows.Forms.Timer notifTimer;
        private int notifTicks = 0;

        // Dashboard
        private Label dashProfile, dashAccount, dashVersion, dashMods, dashRam, dashSession, dashInstall, dashUpdate, dashNews;
        private PictureBox skinBox;
        private Button playBtn;
        private ProgressBar playProgress;
        private Label playStatus;
        private ListBox newsBox;
        private CancellationTokenSource installCts;

        // Profile
        private ListBox profileList;
        private Label profileDetail;

        // Mods
        private TextBox modSearchBox;
        private ListBox modResults;
        private Label modInfo;
        private Button modTabMr, modTabCf, modTabInst, modTabUpd;
        private string modTab = "modrinth";
        private List<ModrinthHit> lastMrHits = new List<ModrinthHit>();
        private List<CfHit> lastCfHits = new List<CfHit>();
        private ListBox installedMods;
        private Label modConflict;
        private TextBox cfKeyBox;

        // Modpacks
        private TextBox packSearchBox;
        private ListBox packResults;
        private Label packInfo;
        private List<ModrinthHit> lastPackHits = new List<ModrinthHit>();

        // Shader / Resourcepacks
        private TextBox shaderSearchBox;
        private ListBox shaderResults, shaderInstalled;
        private List<ModrinthHit> lastShaderHits = new List<ModrinthHit>();
        private TextBox rpSearchBox;
        private ListBox rpResults, rpInstalled;
        private List<ModrinthHit> lastRpHits = new List<ModrinthHit>();

        // Performance
        private RadioButton profMax, profBal, profQual, profCus;
        private TrackBar ramBar;
        private Label ramLabel, recLabel, benchLabel;
        private ComboBox javaBox, backendBox;
        private CheckBox motionBox, discordBox, autoSimpBox;

        // Accounts
        private ListBox accList;
        private Label accStatus;
        private PictureBox accSkin;

        // Einstellungen
        private TrackBar animSpeedBar, glowBar;
        private CheckBox notifBox;
        private Label dlLabel;
        private ListBox dlList;

        public AslanForm(AslanConfig config)
        {
            cfg = config;
            hw = HardwareInfo.Detect();
            animations = !cfg.ReducedMotion && !(cfg.AutoSimplifyOnWeakHardware && hw.Tier() == "LOW");
            glowK = cfg.GlowIntensity / 100.0f;
            Text = "ASLAN CLIENT 0.3.0 — PLAY BEYOND LIMITS";
            Width = 1240;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = AslanTheme.Bg;
            ForeColor = AslanTheme.Cream;
            Font = new Font("Segoe UI", 9.5f);
            MinimumSize = new Size(1060, 680);
            BuildUi();
            AuthService.Init();
            UpdateChecker.LoadRepoSlug();
            RefreshAll();
            ThreadPool.QueueUserWorkItem(delegate { LoadJavas(); CheckUpdates(); });
            DownloadCenter.Changed += delegate
            {
                try { Invoke(new Action(delegate { RefreshDownloads(); })); } catch { }
            };
        }

        // ================= Basis-Bausteine =================
        private Button NavButton(string text, string glyph)
        {
            Button b = new Button();
            b.Text = "  " + glyph + "    " + text;
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.Transparent;
            b.ForeColor = AslanTheme.Muted;
            b.Font = new Font("Segoe UI", 10.5f);
            b.Height = 46;
            b.Dock = DockStyle.Top;
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.MouseOverBackColor = AslanTheme.Surface2;
            return b;
        }

        private Button GoldButton(string text)
        {
            Button b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = AslanTheme.Bronze;
            b.FlatAppearance.BorderSize = 1;
            b.BackColor = Color.FromArgb(0x3A, 0x2A, 0x12);
            b.ForeColor = AslanTheme.Gold;
            b.Height = 34;
            b.Width = 150;
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(0x55, 0x3D, 0x18);
            return b;
        }

        private Button DarkButton(string text)
        {
            Button b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(0x4A, 0x3E, 0x2E);
            b.FlatAppearance.BorderSize = 1;
            b.BackColor = AslanTheme.Surface;
            b.ForeColor = AslanTheme.Cream;
            b.Height = 34;
            b.Width = 150;
            b.Cursor = Cursors.Hand;
            return b;
        }

        private Label CreamLabel(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.ForeColor = AslanTheme.Cream;
            l.AutoSize = true;
            return l;
        }

        private Label GoldLabel(string text, float size, bool bold)
        {
            Label l = new Label();
            l.Text = text;
            l.ForeColor = AslanTheme.Gold;
            l.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
            l.AutoSize = true;
            return l;
        }

        private Panel Card()
        {
            Panel p = new Panel();
            p.BackColor = AslanTheme.Surface;
            p.Padding = new Padding(14);
            return p;
        }

        private ListBox DarkList(int w, int h)
        {
            ListBox l = new ListBox();
            l.Width = w;
            l.Height = h;
            l.BackColor = Color.FromArgb(0x0F, 0x0D, 0x0A);
            l.ForeColor = AslanTheme.Cream;
            l.BorderStyle = BorderStyle.FixedSingle;
            return l;
        }

        private TextBox DarkBox(int w)
        {
            TextBox t = new TextBox();
            t.Width = w;
            t.BackColor = Color.FromArgb(0x0F, 0x0D, 0x0A);
            t.ForeColor = AslanTheme.Cream;
            t.BorderStyle = BorderStyle.FixedSingle;
            return t;
        }

        // ================= Aufbau =================
        private void BuildUi()
        {
            sidebar = new Panel();
            sidebar.Width = 210;
            sidebar.Dock = DockStyle.Left;
            sidebar.BackColor = AslanTheme.Surface;
            Controls.Add(sidebar);

            Label logo = GoldLabel("ASLAN CLIENT", 15, true);
            logo.Location = new Point(18, 14);
            sidebar.Controls.Add(logo);
            Label sub = new Label();
            sub.Text = "PLAY BEYOND LIMITS";
            sub.ForeColor = AslanTheme.Muted;
            sub.Font = new Font("Segoe UI", 8);
            sub.Location = new Point(20, 40);
            sub.AutoSize = true;
            sidebar.Controls.Add(sub);

            marker = new Panel();
            marker.Width = 4;
            marker.Height = 46;
            marker.BackColor = AslanTheme.Gold;
            marker.Location = new Point(0, 80);
            sidebar.Controls.Add(marker);

            Panel navHost = new Panel();
            navHost.Dock = DockStyle.Top;
            navHost.Height = 430;
            navHost.Location = new Point(0, 80);
            navHost.Padding = new Padding(0, 0, 0, 0);
            sidebar.Controls.Add(navHost);
            // Reihenfolge: Buttons werden Top-gedockt, daher rückwärts hinzufügen
            string[] order = new string[] { "Einstellungen", "Accounts", "Performance", "Resourcepacks", "Shader", "Modpacks", "Mods", "Profile", "Dashboard" };
            string[] glyphs = new string[] { "⚙", "◉", "◔", "▦", "◈", "▣", "✚", "⬣", "⌂" };
            for (int i = 0; i < order.Length; i++)
            {
                Button b = NavButton(order[i], glyphs[i]);
                string name = order[i];
                b.Click += delegate { ShowPage(name); };
                navHost.Controls.Add(b);
                navBtns[name] = b;
            }
            navHost.Controls.Add(marker);

            Label motto = new Label();
            motto.Text = "STRENGTH\r\nCREATES\r\nFREEDOM";
            motto.ForeColor = AslanTheme.Bronze;
            motto.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            motto.Location = new Point(20, 520);
            motto.AutoSize = true;
            sidebar.Controls.Add(motto);

            versionLabel = new Label();
            versionLabel.Text = "ASLAN CLIENT 0.3.0";
            versionLabel.ForeColor = AslanTheme.Cream;
            versionLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            versionLabel.Location = new Point(20, 640);
            versionLabel.AutoSize = true;
            sidebar.Controls.Add(versionLabel);
            statusLabel = new Label();
            statusLabel.Text = "Bereit zum Spielen.";
            statusLabel.ForeColor = AslanTheme.Muted;
            statusLabel.Font = new Font("Segoe UI", 8.5f);
            statusLabel.Location = new Point(20, 658);
            statusLabel.AutoSize = true;
            sidebar.Controls.Add(statusLabel);

            main = new Panel();
            main.Dock = DockStyle.Fill;
            main.BackColor = AslanTheme.Bg;
            main.Padding = new Padding(18);
            Controls.Add(main);

            pages["Dashboard"] = BuildDashboard();
            pages["Profile"] = BuildProfiles();
            pages["Mods"] = BuildMods();
            pages["Modpacks"] = BuildModpacks();
            pages["Shader"] = BuildShader();
            pages["Resourcepacks"] = BuildResourcepacks();
            pages["Performance"] = BuildPerformance();
            pages["Accounts"] = BuildAccounts();
            pages["Einstellungen"] = BuildSettings();
            foreach (KeyValuePair<string, Panel> kv in pages)
            {
                kv.Value.Dock = DockStyle.Fill;
                kv.Value.Visible = false;
                main.Controls.Add(kv.Value);
            }
            ShowPage("Dashboard");

            notifPanel = new Panel();
            notifPanel.Size = new Size(360, 54);
            notifPanel.BackColor = AslanTheme.Surface2;
            notifPanel.Visible = false;
            notifLabel = new Label();
            notifLabel.Dock = DockStyle.Fill;
            notifLabel.ForeColor = AslanTheme.Gold;
            notifLabel.TextAlign = ContentAlignment.MiddleCenter;
            notifPanel.Controls.Add(notifLabel);
            Controls.Add(notifPanel);

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 16;
            animTimer.Tick += delegate
            {
                // Sidebar-Marker gleitet
                if (marker.Location.Y != markerTargetY)
                {
                    int d = markerTargetY - marker.Location.Y;
                    int step = Math.Max(2, Math.Abs(d) / 4);
                    if (!animations) marker.Location = new Point(0, markerTargetY);
                    else marker.Location = new Point(0, marker.Location.Y + Math.Sign(d) * Math.Min(step, Math.Abs(d)));
                }
                // Seiten-Slide
                if (slidePanel != null)
                {
                    if (!animations || slideX >= 0) { slidePanel.Location = new Point(18, 18); slidePanel = null; }
                    else
                    {
                        slideX += 6;
                        if (slideX >= 0) { slidePanel.Location = new Point(18, 18); slidePanel = null; }
                        else slidePanel.Location = new Point(18 - slideX, 18);
                    }
                }
            };
            animTimer.Start();

            notifTimer = new System.Windows.Forms.Timer();
            notifTimer.Interval = 400;
            notifTimer.Tick += delegate
            {
                notifTicks++;
                if (notifTicks > 8) { notifPanel.Visible = false; notifTimer.Stop(); }
            };
        }

        private void Notify(string s)
        {
            if (InvokeRequired) { Invoke(new Action(delegate { Notify(s); })); return; }
            notifLabel.Text = s;
            notifPanel.Location = new Point(Width - 400, Height - 120);
            notifPanel.Visible = true;
            notifTicks = 0;
            notifTimer.Start();
            Logger.Info(s);
        }

        private void SetStatus(string s)
        {
            if (InvokeRequired) { Invoke(new Action(delegate { SetStatus(s); })); return; }
            statusLabel.Text = s.Length > 34 ? s.Substring(0, 34) + "…" : s;
            Logger.Info(s);
        }

        private void ShowPage(string name)
        {
            if (InvokeRequired) { Invoke(new Action(delegate { ShowPage(name); })); return; }
            foreach (KeyValuePair<string, Panel> kv in pages) kv.Value.Visible = false;
            pages[name].Visible = true;
            currentPage = name;
            if (animations)
            {
                slidePanel = pages[name];
                slideX = -30;
                slidePanel.Location = new Point(48, 18);
            }
            foreach (KeyValuePair<string, Button> kv in navBtns)
                kv.Value.ForeColor = kv.Key == name ? AslanTheme.Gold : AslanTheme.Muted;
            if (navBtns.ContainsKey(name)) markerTargetY = 80 + (8 - PageIndex(name)) * 0 + navBtns[name].Location.Y;
            if (name == "Dashboard") RefreshDashboard();
            if (name == "Profile") RefreshProfiles();
            if (name == "Accounts") RefreshAccounts();
            if (name == "Einstellungen") RefreshDownloads();
        }

        private int PageIndex(string name)
        {
            string[] order = new string[] { "Dashboard", "Profile", "Mods", "Modpacks", "Shader", "Resourcepacks", "Performance", "Accounts", "Einstellungen" };
            for (int i = 0; i < order.Length; i++) if (order[i] == name) return i;
            return 0;
        }

        // ================= Dashboard =================
        private Panel BuildDashboard()
        {
            Panel p = new Panel();
            p.BackColor = AslanTheme.Bg;
            p.AutoScroll = true;

            Label hi = new Label();
            hi.Text = "W I L L K O M M E N   Z U R Ü C K";
            hi.ForeColor = AslanTheme.Muted;
            hi.Font = new Font("Segoe UI", 10);
            hi.Location = new Point(4, 4);
            hi.AutoSize = true;
            p.Controls.Add(hi);
            Label hero = GoldLabel("ASLAN CLIENT", 30, true);
            hero.Location = new Point(0, 28);
            p.Controls.Add(hero);
            Label tag = new Label();
            tag.Text = "DEIN MINECRAFT. DEINE LIMITS. UNSERE LEIDENSCHAFT.";
            tag.ForeColor = AslanTheme.Muted;
            tag.Location = new Point(4, 72);
            tag.AutoSize = true;
            p.Controls.Add(tag);

            FlowLayoutPanel badges = new FlowLayoutPanel();
            badges.Location = new Point(0, 100);
            badges.Size = new Size(900, 44);
            badges.AutoSize = false;
            p.Controls.Add(badges);
            string[] b = new string[] { "◆ Optimiert für Performance", "⚡ Modding ohne Grenzen", "⬣ Sicher & Zuverlässig" };
            for (int i = 0; i < b.Length; i++)
            {
                Label l = new Label();
                l.Text = b[i];
                l.ForeColor = AslanTheme.Gold;
                l.BackColor = AslanTheme.Surface;
                l.Margin = new Padding(0, 0, 10, 0);
                l.Padding = new Padding(10, 8, 10, 8);
                l.AutoSize = true;
                badges.Controls.Add(l);
            }

            Panel card = Card();
            card.Location = new Point(0, 154);
            card.Size = new Size(940, 300);
            p.Controls.Add(card);

            skinBox = new PictureBox();
            skinBox.Size = new Size(110, 110);
            skinBox.Location = new Point(14, 14);
            skinBox.SizeMode = PictureBoxSizeMode.Zoom;
            skinBox.BackColor = Color.FromArgb(0x0F, 0x0D, 0x0A);
            card.Controls.Add(skinBox);

            dashProfile = GoldLabel("Profil: —", 13, true);
            dashProfile.Location = new Point(140, 10);
            card.Controls.Add(dashProfile);
            dashAccount = CreamLabel("Account: —"); dashAccount.Location = new Point(140, 44);
            dashVersion = CreamLabel("Version: —"); dashVersion.Location = new Point(140, 66);
            dashMods = CreamLabel("Mods: —"); dashMods.Location = new Point(140, 88);
            dashRam = CreamLabel("RAM: —"); dashRam.Location = new Point(140, 110);
            dashSession = CreamLabel("Letzte Sitzung: —"); dashSession.Location = new Point(140, 132);
            dashInstall = CreamLabel("Installation: —"); dashInstall.Location = new Point(140, 154);
            dashInstall.MaximumSize = new Size(330, 60);
            dashUpdate = CreamLabel("Updates: —"); dashUpdate.Location = new Point(140, 196);
            dashNews = CreamLabel("Neuigkeiten: siehe Website / CHANGELOG.md"); dashNews.Location = new Point(140, 218);
            card.Controls.Add(dashAccount); card.Controls.Add(dashVersion); card.Controls.Add(dashMods);
            card.Controls.Add(dashRam); card.Controls.Add(dashSession); card.Controls.Add(dashInstall);
            card.Controls.Add(dashUpdate); card.Controls.Add(dashNews);

            playBtn = new Button();
            playBtn.Text = "▶  PLAY";
            playBtn.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            playBtn.ForeColor = Color.FromArgb(0x2A, 0x1C, 0x08);
            playBtn.BackColor = AslanTheme.Gold;
            playBtn.FlatStyle = FlatStyle.Flat;
            playBtn.FlatAppearance.BorderSize = 0;
            playBtn.Size = new Size(400, 74);
            playBtn.Location = new Point(510, 20);
            playBtn.Cursor = Cursors.Hand;
            playBtn.Click += delegate { StartPlay(false); };
            playBtn.MouseEnter += delegate { if (animations) playBtn.BackColor = AslanTheme.Orange; };
            playBtn.MouseLeave += delegate { playBtn.BackColor = AslanTheme.Gold; };
            card.Controls.Add(playBtn);

            playStatus = new Label();
            playStatus.ForeColor = AslanTheme.Muted;
            playStatus.Location = new Point(510, 102);
            playStatus.Size = new Size(400, 44);
            card.Controls.Add(playStatus);

            playProgress = new ProgressBar();
            playProgress.Location = new Point(510, 150);
            playProgress.Size = new Size(400, 12);
            playProgress.Style = ProgressBarStyle.Continuous;
            card.Controls.Add(playProgress);

            FlowLayoutPanel row = new FlowLayoutPanel();
            row.Location = new Point(510, 172);
            row.AutoSize = true;
            Button verify = DarkButton("⌕ Prüfen");
            verify.Click += delegate { VerifyInstall(); };
            Button repair = DarkButton("⚒ Reparieren");
            repair.Click += delegate { StartPlay(true); };
            Button cancel = DarkButton("✕ Abbrechen");
            cancel.Click += delegate { if (installCts != null) installCts.Cancel(); };
            row.Controls.Add(verify); row.Controls.Add(repair); row.Controls.Add(cancel);
            card.Controls.Add(row);

            FlowLayoutPanel bottom = new FlowLayoutPanel();
            bottom.Location = new Point(0, 464);
            bottom.Size = new Size(950, 220);
            p.Controls.Add(bottom);

            Panel news = Card();
            news.Size = new Size(460, 200);
            Label nt = GoldLabel("NEUIGKEITEN", 11, true);
            nt.Location = new Point(14, 10);
            news.Controls.Add(nt);
            newsBox = DarkList(430, 140);
            newsBox.Location = new Point(14, 40);
            news.Controls.Add(newsBox);
            bottom.Controls.Add(news);

            Panel feat = Card();
            feat.Size = new Size(460, 200);
            Label ft = GoldLabel("FEATURES", 11, true);
            ft.Location = new Point(14, 10);
            feat.Controls.Add(ft);
            string[] feats = new string[] {
                "⚡ Maximale Performance — optimierte Mods, weniger Lags.",
                "✚ Einfaches Modding — Modrinth + CurseForge direkt im Client.",
                "⬣ Stabil & Sicher — geprüfte Quellen, Backups, Safe Mode." };
            for (int i = 0; i < feats.Length; i++)
            {
                Label l = CreamLabel(feats[i]);
                l.Location = new Point(14, 44 + i * 44);
                l.MaximumSize = new Size(420, 60);
                feat.Controls.Add(l);
            }
            bottom.Controls.Add(feat);
            return p;
        }

        // ================= Profile =================
        private Panel BuildProfiles()
        {
            Panel p = new Panel();
            p.BackColor = AslanTheme.Bg;
            p.AutoScroll = true;
            Label t = GoldLabel("PROFILE", 20, true);
            t.Location = new Point(4, 4);
            p.Controls.Add(t);

            profileList = DarkList(280, 420);
            profileList.Location = new Point(4, 50);
            profileList.SelectedIndexChanged += delegate { ShowProfileDetail(); };
            p.Controls.Add(profileList);

            profileDetail = CreamLabel("—");
            profileDetail.Location = new Point(300, 50);
            profileDetail.MaximumSize = new Size(600, 220);
            p.Controls.Add(profileDetail);

            FlowLayoutPanel row = new FlowLayoutPanel();
            row.Location = new Point(300, 280);
            row.AutoSize = true;
            Button neu = GoldButton("+ Neues Profil");
            neu.Click += delegate { OpenProfileDialog(null); };
            Button edit = DarkButton("Bearbeiten");
            edit.Click += delegate { if (profileList.SelectedItem != null) OpenProfileDialog(profileList.SelectedItem.ToString()); };
            Button dup = DarkButton("Duplizieren");
            dup.Click += delegate { DuplicateProfile(); };
            Button del = DarkButton("Löschen");
            del.Click += delegate { DeleteProfile(); };
            row.Controls.Add(neu); row.Controls.Add(edit); row.Controls.Add(dup); row.Controls.Add(del);
            p.Controls.Add(row);

            FlowLayoutPanel row2 = new FlowLayoutPanel();
            row2.Location = new Point(300, 324);
            row2.AutoSize = true;
            Button exp = DarkButton("Exportieren");
            exp.Click += delegate { ExportProfile(); };
            Button imp = DarkButton("Importieren");
            imp.Click += delegate { ImportProfile(); };
            Button bak = DarkButton("Sichern");
            bak.Click += delegate { BackupProfile(); };
            Button open = DarkButton("Ordner öffnen");
            open.Click += delegate { OpenProfileFolder(); };
            Button loader = GoldButton("Loader installieren");
            loader.Width = 170;
            loader.Click += delegate { InstallLoaderFlow(); };
            row2.Controls.Add(exp); row2.Controls.Add(imp); row2.Controls.Add(bak); row2.Controls.Add(open); row2.Controls.Add(loader);
            p.Controls.Add(row2);
            return p;
        }

        // ================= Mods =================
        private Panel BuildMods()
        {
            Panel p = new Panel();
            p.BackColor = AslanTheme.Bg;
            p.AutoScroll = true;
            Label t = GoldLabel("MODS", 20, true);
            t.Location = new Point(4, 4);
            p.Controls.Add(t);

            FlowLayoutPanel tabs = new FlowLayoutPanel();
            tabs.Location = new Point(4, 44);
            tabs.AutoSize = true;
            modTabMr = GoldButton("Modrinth"); modTabMr.Width = 120;
            modTabCf = DarkButton("CurseForge"); modTabCf.Width = 130;
            modTabInst = DarkButton("Installiert"); modTabInst.Width = 120;
            modTabUpd = DarkButton("Updates"); modTabUpd.Width = 120;
            modTabMr.Click += delegate { SetModTab("modrinth"); };
            modTabCf.Click += delegate { SetModTab("curseforge"); };
            modTabInst.Click += delegate { SetModTab("installiert"); };
            modTabUpd.Click += delegate { SetModTab("updates"); };
            tabs.Controls.Add(modTabMr); tabs.Controls.Add(modTabCf); tabs.Controls.Add(modTabInst); tabs.Controls.Add(modTabUpd);
            p.Controls.Add(tabs);

            modSearchBox = DarkBox(300);
            modSearchBox.Location = new Point(4, 88);
            modSearchBox.Text = "sodium";
            p.Controls.Add(modSearchBox);
            Button go = GoldButton("Suchen");
            go.Width = 110;
            go.Location = new Point(312, 86);
            go.Click += delegate { DoModSearch(); };
            p.Controls.Add(go);

            Label cfk = CreamLabel("CurseForge API-Key (nur für CurseForge-Tab nötig):");
            cfk.Location = new Point(440, 88);
            cfk.AutoSize = true;
            p.Controls.Add(cfk);
            cfKeyBox = DarkBox(220);
            cfKeyBox.Location = new Point(440, 110);
            cfKeyBox.UseSystemPasswordChar = true;
            if (CurseForgeService.HasKey()) cfKeyBox.Text = "••••••••••";
            p.Controls.Add(cfKeyBox);
            Button ksave = DarkButton("Speichern");
            ksave.Width = 110;
            ksave.Location = new Point(668, 108);
            ksave.Click += delegate
            {
                try
                {
                    if (cfKeyBox.Text.Contains("•")) { Notify("Key bereits gespeichert."); return; }
                    CurseForgeService.SaveKey(cfKeyBox.Text.Trim());
                    Notify("CurseForge-Key gespeichert (DPAPI).");
                }
                catch (Exception ex) { Notify(ex.Message); }
            };
            p.Controls.Add(ksave);

            modResults = DarkList(430, 320);
            modResults.Location = new Point(4, 130);
            modResults.SelectedIndexChanged += delegate { ShowModInfo(); };
            p.Controls.Add(modResults);

            modInfo = CreamLabel("Mod wählen oder suchen …");
            modInfo.Location = new Point(450, 140);
            modInfo.MaximumSize = new Size(460, 200);
            p.Controls.Add(modInfo);

            FlowLayoutPanel row = new FlowLayoutPanel();
            row.Location = new Point(450, 350);
            row.AutoSize = true;
            Button inst = GoldButton("Installieren");
            inst.Click += delegate { DoModInstall(); };
            Button open = DarkButton("Seite öffnen");
            open.Click += delegate { OpenModPage(); };
            row.Controls.Add(inst); row.Controls.Add(open);
            p.Controls.Add(row);

            installedMods = DarkList(430, 150);
            installedMods.Location = new Point(4, 460);
            p.Controls.Add(installedMods);
            modConflict = CreamLabel("Kompatibilität: —");
            modConflict.Location = new Point(450, 460);
            modConflict.MaximumSize = new Size(460, 150);
            p.Controls.Add(modConflict);
            return p;
        }

        // ================= Modpacks =================
        private Panel BuildModpacks()
        {
            Panel p = new Panel();
            p.BackColor = AslanTheme.Bg;
            p.AutoScroll = true;
            Label t = GoldLabel("MODPACKS", 20, true);
            t.Location = new Point(4, 4);
            p.Controls.Add(t);

            packSearchBox = DarkBox(300);
            packSearchBox.Location = new Point(4, 50);
            packSearchBox.Text = "adventure";
            p.Controls.Add(packSearchBox);
            Button go = GoldButton("Suchen");
            go.Width = 110;
            go.Location = new Point(312, 48);
            go.Click += delegate { DoPackSearch(); };
            p.Controls.Add(go);

            packResults = DarkList(430, 280);
            packResults.Location = new Point(4, 92);
            packResults.SelectedIndexChanged += delegate { ShowPackInfo(); };
            p.Controls.Add(packResults);

            packInfo = CreamLabel("Modpack wählen, suchen oder Datei importieren …");
            packInfo.Location = new Point(450, 92);
            packInfo.MaximumSize = new Size(460, 180);
            p.Controls.Add(packInfo);

            FlowLayoutPanel row = new FlowLayoutPanel();
            row.Location = new Point(450, 280);
            row.AutoSize = true;
            Button inst = GoldButton("Installieren");
            inst.Click += delegate { DoPackInstall(); };
            Button mr = DarkButton(".mrpack importieren");
            mr.Width = 170;
            mr.Click += delegate { ImportMrpackFile(); };
            Button cf = DarkButton("CF-Archiv importieren");
            cf.Width = 180;
            cf.Click += delegate { ImportCfFile(); };
            row.Controls.Add(inst); row.Controls.Add(mr); row.Controls.Add(cf);
            p.Controls.Add(row);
            return p;
        }

        // ================= Shader =================
        private Panel BuildShader()
        {
            Panel p = new Panel();
            p.BackColor = AslanTheme.Bg;
            p.AutoScroll = true;
            Label t = GoldLabel("SHADER", 20, true);
            t.Location = new Point(4, 4);
            p.Controls.Add(t);
            p.Controls.Add(CreamLabelAt("Shader brauchen Iris + Sodium (OpenGL). Suche über Modrinth, Installation Hash-geprüft.", 4, 44));

            shaderSearchBox = DarkBox(300);
            shaderSearchBox.Location = new Point(4, 76);
            shaderSearchBox.Text = "complementary";
            p.Controls.Add(shaderSearchBox);
            Button go = GoldButton("Suchen");
            go.Width = 110;
            go.Location = new Point(312, 74);
            go.Click += delegate { DoShaderSearch(); };
            p.Controls.Add(go);

            shaderResults = DarkList(430, 220);
            shaderResults.Location = new Point(4, 118);
            p.Controls.Add(shaderResults);
            Button inst = GoldButton("Installieren");
            inst.Location = new Point(4, 344);
            inst.Click += delegate { DoShaderInstall(); };
            p.Controls.Add(inst);

            p.Controls.Add(CreamLabelAt("Installiert (shaderpacks):", 450, 76));
            shaderInstalled = DarkList(430, 220);
            shaderInstalled.Location = new Point(450, 100);
            p.Controls.Add(shaderInstalled);
            FlowLayoutPanel row = new FlowLayoutPanel();
            row.Location = new Point(450, 326);
            row.AutoSize = true;
            Button en = DarkButton("Aktivieren"); en.Click += delegate { ToggleShader(true); };
            Button dis = DarkButton("Deaktivieren"); dis.Click += delegate { ToggleShader(false); };
            Button open = DarkButton("Ordner öffnen"); open.Click += delegate { OpenDir(ShaderDir()); };
            row.Controls.Add(en); row.Controls.Add(dis); row.Controls.Add(open);
            p.Controls.Add(row);
            return p;
        }

        private Label CreamLabelAt(string text, int x, int y)
        {
            Label l = CreamLabel(text);
            l.Location = new Point(x, y);
            return l;
        }

        // ================= Resourcepacks =================
        private Panel BuildResourcepacks()
        {
            Panel p = new Panel();
            p.BackColor = AslanTheme.Bg;
            p.AutoScroll = true;
            Label t = GoldLabel("RESOURCEPACKS", 20, true);
            t.Location = new Point(4, 4);
            p.Controls.Add(t);

            rpSearchBox = DarkBox(300);
            rpSearchBox.Location = new Point(4, 50);
            rpSearchBox.Text = "faithful";
            p.Controls.Add(rpSearchBox);
            Button go = GoldButton("Suchen");
            go.Width = 110;
            go.Location = new Point(312, 48);
            go.Click += delegate { DoRpSearch(); };
            p.Controls.Add(go);

            rpResults = DarkList(430, 240);
            rpResults.Location = new Point(4, 92);
            p.Controls.Add(rpResults);
            Button inst = GoldButton("Installieren");
            inst.Location = new Point(4, 338);
            inst.Click += delegate { DoRpInstall(); };
            p.Controls.Add(inst);

            p.Controls.Add(CreamLabelAt("Installiert (resourcepacks):", 450, 50));
            rpInstalled = DarkList(430, 240);
            rpInstalled.Location = new Point(450, 74);
            p.Controls.Add(rpInstalled);
            Button open = DarkButton("Ordner öffnen");
            open.Location = new Point(450, 320);
            open.Click += delegate { OpenDir(RpDir()); };
            p.Controls.Add(open);
            return p;
        }

        // ================= Performance =================
        private Panel BuildPerformance()
        {
            Panel p = new Panel();
            p.BackColor = AslanTheme.Bg;
            p.AutoScroll = true;
            Label t = GoldLabel("PERFORMANCE", 20, true);
            t.Location = new Point(4, 4);
            p.Controls.Add(t);
            p.Controls.Add(CreamLabelAt("Hardware: " + hw.CpuName + " | " + hw.CpuCores + " Kerne | RAM " + hw.RamTotalMB + " MB | GPU " + hw.GpuName + " (Stufe " + hw.Tier() + ")", 4, 44));

            FlowLayoutPanel cards = new FlowLayoutPanel();
            cards.Location = new Point(4, 72);
            cards.AutoSize = true;
            profMax = PerfRadio("MAX FPS");
            profBal = PerfRadio("BALANCED"); profBal.Checked = true;
            profQual = PerfRadio("QUALITY");
            profCus = PerfRadio("CUSTOM");
            cards.Controls.Add(profMax); cards.Controls.Add(profBal); cards.Controls.Add(profQual); cards.Controls.Add(profCus);
            p.Controls.Add(cards);

            ramLabel = CreamLabel("RAM: 3072 MB");
            ramLabel.Location = new Point(4, 130);
            p.Controls.Add(ramLabel);
            ramBar = new TrackBar();
            ramBar.Minimum = 1024; ramBar.Maximum = 16384; ramBar.TickFrequency = 1024;
            ramBar.Width = 420; ramBar.Value = 3072;
            ramBar.Location = new Point(0, 152);
            ramBar.Scroll += delegate { ramLabel.Text = "RAM: " + ramBar.Value + " MB"; };
            p.Controls.Add(ramBar);

            p.Controls.Add(CreamLabelAt("Java:", 4, 210));
            javaBox = new ComboBox();
            javaBox.Width = 420;
            javaBox.DropDownStyle = ComboBoxStyle.DropDownList;
            javaBox.Location = new Point(4, 232);
            p.Controls.Add(javaBox);
            p.Controls.Add(CreamLabelAt("Grafik-Backend:", 4, 264));
            backendBox = new ComboBox();
            backendBox.Width = 260;
            backendBox.DropDownStyle = ComboBoxStyle.DropDownList;
            backendBox.Items.Add("opengl");
            backendBox.Items.Add("vulkan (experimentell)");
            backendBox.SelectedIndex = 0;
            backendBox.Location = new Point(4, 286);
            p.Controls.Add(backendBox);

            motionBox = new CheckBox(); motionBox.Text = "Reduzierte Animationen"; motionBox.ForeColor = AslanTheme.Cream; motionBox.AutoSize = true; motionBox.Location = new Point(4, 320);
            discordBox = new CheckBox(); discordBox.Text = "Discord Rich Presence (Opt-in)"; discordBox.ForeColor = AslanTheme.Cream; discordBox.AutoSize = true; discordBox.Location = new Point(4, 344);
            autoSimpBox = new CheckBox(); autoSimpBox.Text = "Animationen auf schwacher Hardware automatisch vereinfachen"; autoSimpBox.ForeColor = AslanTheme.Cream; autoSimpBox.AutoSize = true; autoSimpBox.Location = new Point(4, 368); autoSimpBox.Checked = true;
            motionBox.Checked = cfg.ReducedMotion;
            discordBox.Checked = cfg.DiscordPresence;
            p.Controls.Add(motionBox); p.Controls.Add(discordBox); p.Controls.Add(autoSimpBox);

            FlowLayoutPanel row = new FlowLayoutPanel();
            row.Location = new Point(4, 400);
            row.AutoSize = true;
            Button apply = GoldButton("Übernehmen");
            apply.Click += delegate { ApplyPerf(); };
            Button bench = DarkButton("Benchmark starten");
            bench.Width = 170;
            bench.Click += delegate { RunBenchmark(); };
            Button perfset = GoldButton("Performance-Mods");
            perfset.Width = 170;
            perfset.Click += delegate { InstallPerfSet(); };
            row.Controls.Add(apply); row.Controls.Add(bench); row.Controls.Add(perfset);
            p.Controls.Add(row);

            recLabel = CreamLabel("Optimizer: —");
            recLabel.Location = new Point(4, 444);
            recLabel.MaximumSize = new Size(880, 80);
            p.Controls.Add(recLabel);
            benchLabel = CreamLabel("Benchmark: noch nicht ausgeführt.");
            benchLabel.Location = new Point(4, 500);
            benchLabel.MaximumSize = new Size(880, 80);
            p.Controls.Add(benchLabel);
            return p;
        }

        private RadioButton PerfRadio(string text)
        {
            RadioButton r = new RadioButton();
            r.Text = text;
            r.ForeColor = AslanTheme.Gold;
            r.BackColor = AslanTheme.Surface;
            r.Padding = new Padding(12, 10, 12, 10);
            r.Margin = new Padding(0, 0, 10, 0);
            r.AutoSize = true;
            r.Appearance = Appearance.Button;
            r.FlatStyle = FlatStyle.Flat;
            r.Cursor = Cursors.Hand;
            return r;
        }

        // ================= Accounts =================
        private Panel BuildAccounts()
        {
            Panel p = new Panel();
            p.BackColor = AslanTheme.Bg;
            p.AutoScroll = true;
            Label t = GoldLabel("ACCOUNTS", 20, true);
            t.Location = new Point(4, 4);
            p.Controls.Add(t);

            accList = DarkList(300, 220);
            accList.Location = new Point(4, 50);
            accList.SelectedIndexChanged += delegate { ShowAccSkin(); };
            p.Controls.Add(accList);

            accSkin = new PictureBox();
            accSkin.Size = new Size(96, 96);
            accSkin.Location = new Point(320, 50);
            accSkin.SizeMode = PictureBoxSizeMode.Zoom;
            p.Controls.Add(accSkin);
            accStatus = CreamLabel("Status: —");
            accStatus.Location = new Point(320, 154);
            accStatus.MaximumSize = new Size(560, 120);
            p.Controls.Add(accStatus);

            FlowLayoutPanel row = new FlowLayoutPanel();
            row.Location = new Point(4, 280);
            row.AutoSize = true;
            Button login = GoldButton("Anmelden");
            login.Click += delegate { DoLogin(); };
            Button logout = DarkButton("Entfernen");
            logout.Click += delegate
            {
                if (accList.SelectedItem != null) { AuthService.Remove(accList.SelectedItem.ToString()); RefreshAccounts(); }
            };
            Button sw = DarkButton("Wechseln");
            sw.Click += delegate
            {
                if (accList.SelectedItem != null) { AuthService.SwitchTo(accList.SelectedItem.ToString()); RefreshAccounts(); RefreshDashboard(); }
            };
            row.Controls.Add(login); row.Controls.Add(sw); row.Controls.Add(logout);
            p.Controls.Add(row);
            p.Controls.Add(CreamLabelAt("Niemals wird ein Passwort im Launcher abgefragt oder gespeichert (Device-Code-Flow, DPAPI).", 4, 330));
            return p;
        }

        // ================= Einstellungen =================
        private Panel BuildSettings()
        {
            Panel p = new Panel();
            p.BackColor = AslanTheme.Bg;
            p.AutoScroll = true;
            Label t = GoldLabel("EINSTELLUNGEN", 20, true);
            t.Location = new Point(4, 4);
            p.Controls.Add(t);

            p.Controls.Add(GoldLabelAt("Design", 4, 44));
            motionBox = null;
            CheckBox rm = new CheckBox(); rm.Text = "Reduzierter Bewegungsmodus"; rm.ForeColor = AslanTheme.Cream; rm.AutoSize = true; rm.Location = new Point(4, 72); rm.Checked = cfg.ReducedMotion;
            rm.CheckedChanged += delegate { cfg.ReducedMotion = rm.Checked; cfg.Save(); animations = !rm.Checked; Notify("Bewegungsmodus gespeichert."); };
            p.Controls.Add(rm);
            p.Controls.Add(CreamLabelAt("Animationsgeschwindigkeit:", 4, 100));
            animSpeedBar = new TrackBar(); animSpeedBar.Minimum = 25; animSpeedBar.Maximum = 200; animSpeedBar.Value = cfg.AnimSpeed; animSpeedBar.Width = 300; animSpeedBar.Location = new Point(0, 122);
            animSpeedBar.Scroll += delegate { cfg.AnimSpeed = animSpeedBar.Value; cfg.Save(); };
            p.Controls.Add(animSpeedBar);
            p.Controls.Add(CreamLabelAt("Glow-Intensität:", 4, 170));
            glowBar = new TrackBar(); glowBar.Minimum = 0; glowBar.Maximum = 150; glowBar.Value = cfg.GlowIntensity; glowBar.Width = 300; glowBar.Location = new Point(0, 192);
            glowBar.Scroll += delegate { cfg.GlowIntensity = glowBar.Value; glowK = cfg.GlowIntensity / 100.0f; cfg.Save(); };
            p.Controls.Add(glowBar);

            p.Controls.Add(GoldLabelAt("Allgemein", 4, 240));
            notifBox = new CheckBox(); notifBox.Text = "Benachrichtigungen anzeigen"; notifBox.ForeColor = AslanTheme.Cream; notifBox.AutoSize = true; notifBox.Location = new Point(4, 268); notifBox.Checked = true;
            p.Controls.Add(notifBox);

            p.Controls.Add(GoldLabelAt("Microsoft App-ID (Accounts):", 4, 300));
            TextBox appId = DarkBox(360);
            appId.Location = new Point(4, 326);
            if (!AuthService.HasPlaceholderId()) appId.Text = AuthService.ClientId;
            p.Controls.Add(appId);
            Button asv = GoldButton("Speichern");
            asv.Width = 120;
            asv.Location = new Point(372, 324);
            asv.Click += delegate
            {
                if (appId.Text.Trim().Length == 36) { AuthService.SaveClientId(appId.Text.Trim()); Notify("App-ID gespeichert."); }
                else Notify("App-ID muss 36 Zeichen haben.");
            };
            p.Controls.Add(asv);

            p.Controls.Add(GoldLabelAt("Update-Repo (GitHub, z. B. Name/ASLAN-Client):", 4, 366));
            TextBox repo = DarkBox(360);
            repo.Location = new Point(4, 392);
            repo.Text = UpdateChecker.RepoSlug;
            p.Controls.Add(repo);
            Button rsv = GoldButton("Speichern");
            rsv.Width = 120;
            rsv.Location = new Point(372, 390);
            rsv.Click += delegate { UpdateChecker.SaveRepoSlug(repo.Text.Trim()); Notify("Update-Repo gespeichert."); ThreadPool.QueueUserWorkItem(delegate { CheckUpdates(); }); };
            p.Controls.Add(rsv);

            p.Controls.Add(GoldLabelAt("Downloads", 4, 432));
            dlLabel = CreamLabel("Warteschlange: —");
            dlLabel.Location = new Point(4, 458);
            p.Controls.Add(dlLabel);
            dlList = DarkList(600, 120);
            dlList.Location = new Point(4, 482);
            p.Controls.Add(dlList);
            FlowLayoutPanel drow = new FlowLayoutPanel();
            drow.Location = new Point(4, 608);
            drow.AutoSize = true;
            Button clr = DarkButton("Erledigte löschen");
            clr.Click += delegate { DownloadCenter.ClearDone(); RefreshDownloads(); };
            Button cnl = DarkButton("Alle abbrechen");
            cnl.Click += delegate { DownloadCenter.CancelAll(); RefreshDownloads(); };
            Button cache = DarkButton("Cache leeren");
            cache.Click += delegate
            {
                try
                {
                    string[] fs = Directory.GetFiles(AslanPaths.CacheDir);
                    for (int i = 0; i < fs.Length; i++) File.Delete(fs[i]);
                    Notify("Cache geleert.");
                }
                catch (Exception ex) { Notify(ex.Message); }
            };
            drow.Controls.Add(clr); drow.Controls.Add(cnl); drow.Controls.Add(cache);
            p.Controls.Add(drow);

            p.Controls.Add(GoldLabelAt("Datenschutz", 4, 650));
            Button wipe = DarkButton("Alle lokalen Daten löschen");
            wipe.Width = 220;
            wipe.Location = new Point(4, 676);
            wipe.Click += delegate
            {
                DialogResult dr = MessageBox.Show("Wirklich alle ASLAN-Daten (Profile, Tokens, Logs) löschen? Spielstände in Profilen gehen verloren.", "Löschen", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (dr != DialogResult.OK) return;
                try
                {
                    string[] dirs = new string[] { AslanPaths.ProfilesDir, AslanPaths.LogsDir, AslanPaths.CacheDir, AslanPaths.BackupsDir };
                    for (int i = 0; i < dirs.Length; i++) if (Directory.Exists(dirs[i])) Directory.Delete(dirs[i], true);
                    string[] files = Directory.GetFiles(AslanPaths.BaseDir);
                    for (int i = 0; i < files.Length; i++) File.Delete(files[i]);
                    Notify("Lokale Daten gelöscht. Neustart empfohlen.");
                    RefreshAll();
                }
                catch (Exception ex) { Notify(ex.Message); }
            };
            p.Controls.Add(wipe);
            return p;
        }

        private Label GoldLabelAt(string text, int x, int y)
        {
            Label l = GoldLabel(text, 11, true);
            l.Location = new Point(x, y);
            return l;
        }

        // ================= Daten-Refresh =================
        private void RefreshAll()
        {
            RefreshDashboard();
            RefreshProfiles();
            RefreshMods();
            RefreshShaderLists();
            RefreshRpLists();
            RefreshAccounts();
            ShowLog();
        }

        private void RefreshDashboard()
        {
            AslanProfile g = cfg.GetActive();
            dashProfile.Text = "Profil:  " + g.Name;
            McAccount acc = AuthService.GetActive(cfg.PlayerName.Length > 0 ? cfg.PlayerName : "Spieler");
            dashAccount.Text = "Account:  " + acc.Name + (acc.Online ? "" : "  (offline)");
            dashVersion.Text = "Version:  MC " + g.MinecraftVersion + " + " + g.Loader + " " + g.LoaderVersion;
            dashMods.Text = "Mods:  " + ModrinthService.ListInstalled(g).Count + " installiert";
            dashRam.Text = "RAM:  " + g.RamMB + " MB zugewiesen";
            dashSession.Text = "Letzte Sitzung:  " + (g.LastPlayed == DateTime.MinValue ? "—" : g.LastPlayed.ToString());
            try
            {
                if (acc.Name.Length >= 3)
                    skinBox.LoadAsync("https://crafatar.com/avatars/" + Uri.EscapeDataString(acc.Name) + "?size=110&overlay");
            }
            catch { }
            newsBox.Items.Clear();
            newsBox.Items.Add("ASLAN CLIENT 0.3.0 — Willkommen im Rudel.");
            newsBox.Items.Add("Neu: Quilt-Support, Modpacks, CurseForge, Resourcepacks.");
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<string> problems = InstanceInstaller.Verify(g);
                Invoke(new Action(delegate
                {
                    dashInstall.Text = "Installation:  " + (problems.Count == 0 ? "vollständig ✓" : (problems.Count + " Problem(e) — Prüfen/Reparieren"));
                }));
            });
        }

        private void RefreshProfiles()
        {
            profileList.Items.Clear();
            for (int i = 0; i < cfg.Profiles.Count; i++) profileList.Items.Add(cfg.Profiles[i].Name);
            profileList.SelectedItem = cfg.ActiveProfile;
            ShowProfileDetail();
        }

        private void ShowProfileDetail()
        {
            AslanProfile g = cfg.GetActive();
            if (profileList.SelectedItem != null) { cfg.ActiveProfile = profileList.SelectedItem.ToString(); g = cfg.GetActive(); }
            int mods = ModrinthService.ListInstalled(g).Count;
            profileDetail.Text = g.Name + "  —  MC " + g.MinecraftVersion + " + " + g.Loader + " " + g.LoaderVersion +
                "\nRAM " + g.RamMB + " MB, " + mods + " Mods" + (g.SafeMode ? ", Safe Mode" : "") +
                (g.ModpackName.Length > 0 ? "\nModpack: " + g.ModpackName : "") +
                "\nOrdner: " + InstanceInstaller.InstanceDir(g);
        }

        private void RefreshMods()
        {
            AslanProfile g = cfg.GetActive();
            installedMods.Items.Clear();
            List<string> mods = ModrinthService.ListInstalled(g);
            for (int i = 0; i < mods.Count; i++) installedMods.Items.Add(mods[i]);
            string problem = ModConflictRules.CheckSet(new List<string>(mods));
            modConflict.Text = "Kompatibilität: " + (problem == null ? "keine bekannten Konflikte." : problem);
            List<string> hints = CrashAnalyzer.MissingHints(new List<string>(mods));
            if (hints.Count > 0) modConflict.Text += " Hinweis: " + string.Join(" ", hints.ToArray());
        }

        private void RefreshShaderLists()
        {
            if (shaderInstalled == null) return;
            shaderInstalled.Items.Clear();
            List<string> fs = ModrinthService.ListFiles(cfg.GetActive(), "shaderpacks", "*.zip");
            fs.AddRange(ModrinthService.ListFiles(cfg.GetActive(), "shaderpacks", "*.zip.disabled"));
            for (int i = 0; i < fs.Count; i++) shaderInstalled.Items.Add(fs[i]);
        }

        private void RefreshRpLists()
        {
            if (rpInstalled == null) return;
            rpInstalled.Items.Clear();
            List<string> fs = ModrinthService.ListFiles(cfg.GetActive(), "resourcepacks", "*.zip");
            for (int i = 0; i < fs.Count; i++) rpInstalled.Items.Add(fs[i]);
        }

        private void RefreshAccounts()
        {
            accList.Items.Clear();
            for (int i = 0; i < AuthService.Accounts.Count; i++) accList.Items.Add(AuthService.Accounts[i].Name);
            if (AuthService.ActiveName.Length > 0) accList.SelectedItem = AuthService.ActiveName;
            McAccount a = AuthService.GetActive("Spieler");
            accStatus.Text = AuthService.Accounts.Count == 0 ? "Status: nicht angemeldet (offline als Spieler)." :
                ("Status: " + a.Name + (a.Online ? " (online, Lizenz geprüft)" : " (Token abgelaufen — erneut anmelden)"));
            ShowAccSkin();
        }

        private void ShowAccSkin()
        {
            try
            {
                string n = accList.SelectedItem != null ? accList.SelectedItem.ToString() : AuthService.ActiveName;
                if (n.Length >= 3) accSkin.LoadAsync("https://crafatar.com/avatars/" + Uri.EscapeDataString(n) + "?size=96&overlay");
            }
            catch { }
        }

        private void ShowLog() { }

        private void RefreshDownloads()
        {
            if (dlList == null) return;
            List<DownloadItem> items = DownloadCenter.Snapshot();
            dlList.Items.Clear();
            int active = 0;
            for (int i = 0; i < items.Count; i++)
            {
                DownloadItem it = items[i];
                string pct = it.Total > 0 ? (" " + (it.Done * 100L / Math.Max(1, it.Total)) + "%") : "";
                double sp = it.SpeedBps;
                string spt = sp > 1048576 ? (" " + (sp / 1048576.0).ToString("0.0") + " MB/s") : (sp > 1024 ? (" " + (sp / 1024.0).ToString("0") + " KB/s") : "");
                dlList.Items.Add(it.Status + pct + spt + " — " + it.Label);
                if (it.Status == "aktiv" || it.Status == "wartend") active++;
            }
            dlLabel.Text = "Warteschlange: " + active + " aktiv, " + items.Count + " gesamt";
            if (currentPage == "Dashboard" && active > 0) playStatus.Text = "Downloads aktiv: " + active;
        }

        // ================= Aktionen: Profile =================
        private void OpenProfileDialog(string editName)
        {
            AslanProfileDialog dlg = new AslanProfileDialog(cfg, editName);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                cfg.Save();
                RefreshProfiles();
                RefreshDashboard();
                Notify("Profil gespeichert.");
            }
        }

        private void DuplicateProfile()
        {
            if (profileList.SelectedItem == null) return;
            AslanProfile src = cfg.GetActive();
            AslanProfile c = new AslanProfile();
            c.Name = src.Name + " (Kopie)";
            c.MinecraftVersion = src.MinecraftVersion;
            c.Loader = src.Loader;
            c.LoaderVersion = src.LoaderVersion;
            c.RamMB = src.RamMB;
            c.PerfProfileName = src.PerfProfileName;
            c.JavaArgs = src.JavaArgs;
            c.SafeMode = src.SafeMode;
            cfg.Profiles.Add(c);
            cfg.ActiveProfile = c.Name;
            cfg.Save();
            RefreshProfiles();
            Notify("Profil dupliziert.");
        }

        private void DeleteProfile()
        {
            if (profileList.SelectedItem == null || cfg.Profiles.Count <= 1) return;
            string name = profileList.SelectedItem.ToString();
            DialogResult dr = MessageBox.Show("Profil „" + name + "“ löschen? Dies entfernt auch Mods, Shader und Spielstände dieses Profils.",
                "Profil löschen", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (dr != DialogResult.OK) return;
            try
            {
                string dir = InstanceInstaller.InstanceDir(cfg.GetActive());
                for (int i = 0; i < cfg.Profiles.Count; i++)
                    if (cfg.Profiles[i].Name == name) { cfg.Profiles.RemoveAt(i); break; }
                cfg.ActiveProfile = cfg.Profiles[0].Name;
                cfg.Save();
                try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
                RefreshProfiles();
                RefreshDashboard();
                Notify("Profil gelöscht.");
            }
            catch (Exception ex) { Notify(ex.Message); }
        }

        private void ExportProfile()
        {
            try
            {
                SaveFileDialog s = new SaveFileDialog();
                s.Filter = "ASLAN-Profil (*.aslanprofile.json)|*.aslanprofile.json";
                s.FileName = MinecraftService.SafeName(cfg.GetActive().Name) + ".aslanprofile.json";
                if (s.ShowDialog(this) != DialogResult.OK) return;
                File.WriteAllText(s.FileName, cfg.GetActive().Name + "\n" + new AslanConfig().Serialize(), Encoding.UTF8);
                // Vollständig: Profil-JSON + Hinweis auf Mods (Export der Modliste)
                string mods = ModrinthService.ExportProfile(cfg.GetActive());
                Notify("Exportiert: " + s.FileName + " (+ " + mods + ")");
            }
            catch (Exception ex) { Notify(ex.Message); }
        }

        private void ImportProfile()
        {
            try
            {
                OpenFileDialog o = new OpenFileDialog();
                o.Filter = "ASLAN-Profil (*.aslanprofile.json)|*.aslanprofile.json";
                if (o.ShowDialog(this) != DialogResult.OK) return;
                string[] lines = File.ReadAllLines(o.FileName, Encoding.UTF8);
                if (lines.Length == 0) return;
                AslanProfile p = new AslanProfile();
                p.Name = lines[0].Trim() + " (Import)";
                cfg.Profiles.Add(p);
                cfg.ActiveProfile = p.Name;
                cfg.Save();
                RefreshProfiles();
                Notify("Profil importiert (Version/Loader ggf. anpassen).");
            }
            catch (Exception ex) { Notify(ex.Message); }
        }

        private void BackupProfile()
        {
            try
            {
                AslanProfile g = cfg.GetActive();
                string src = InstanceInstaller.InstanceDir(g);
                string dst = Path.Combine(AslanPaths.BackupsDir, MinecraftService.SafeName(g.Name) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                CopyTree(src, dst);
                Notify("Backup: " + dst);
            }
            catch (Exception ex) { Notify(ex.Message); }
        }

        private void CopyTree(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            string[] files = Directory.GetFiles(src);
            for (int i = 0; i < files.Length; i++)
                try { File.Copy(files[i], Path.Combine(dst, Path.GetFileName(files[i])), true); } catch { }
            string[] dirs = Directory.GetDirectories(src);
            for (int i = 0; i < dirs.Length; i++)
            {
                string n = Path.GetFileName(dirs[i]);
                if (n == "natives" || n == "cache") continue;
                CopyTree(dirs[i], Path.Combine(dst, n));
            }
        }

        private void OpenProfileFolder()
        {
            try { Process.Start(InstanceInstaller.InstanceDir(cfg.GetActive())); } catch (Exception ex) { Notify(ex.Message); }
        }

        private void OpenDir(string d)
        {
            try { Process.Start(d); } catch (Exception ex) { Notify(ex.Message); }
        }

        private void InstallLoaderFlow()
        {
            AslanProfile g = cfg.GetActive();
            if (g.Loader == "vanilla") { Notify("Vanilla braucht keinen Loader."); return; }
            if (g.Loader == "fabric" || g.Loader == "quilt") { StartPlay(true); return; }
            // Forge/NeoForge: assistiert
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    SetStatus("Lade " + g.Loader + "-Installer …");
                    InstallProgress pr = new InstallProgress();
                    CancellationToken ct = new CancellationToken();
                    InstanceInstaller.EnsureInstalled(g, g.LoaderVersion, pr, ct);
                    Invoke(new Action(delegate
                    {
                        // EnsureForged gibt ASSISTED:pfad|ordner zurück, wenn Installer nötig war
                        Notify(g.Loader + " vorbereitet — ggf. Installer im Ordner ausführen.");
                        RefreshDashboard();
                    }));
                }
                catch (Exception ex) { SetStatus("Loader: " + ex.Message); }
            });
        }

        // ================= Aktionen: PLAY =================
        private void SetProgressBar(int pct)
        {
            if (InvokeRequired) { Invoke(new Action(delegate { SetProgressBar(pct); })); return; }
            try
            {
                playProgress.Style = ProgressBarStyle.Continuous;
                playProgress.Value = Math.Max(0, Math.Min(100, pct));
            }
            catch { }
        }

        private void SetPlayStatus(string s)
        {
            if (InvokeRequired) { Invoke(new Action(delegate { SetPlayStatus(s); })); return; }
            playStatus.Text = s;
            SetStatus(s);
        }

        private void VerifyInstall()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<string> problems = InstanceInstaller.Verify(cfg.GetActive());
                if (problems.Count == 0) SetPlayStatus("Installation vollständig.");
                else SetPlayStatus("Probleme: " + string.Join(" | ", problems.ToArray()));
                Invoke(new Action(delegate { RefreshDashboard(); }));
            });
        }

        private string SelectedJava()
        {
            if (javaBox.SelectedIndex >= 0 && javaBox.SelectedIndex < javas.Count) return javas[javaBox.SelectedIndex].Path;
            // Fallback: erstes Java 21+
            for (int i = 0; i < javas.Count; i++)
                if (JavaFinder.MajorOf(javas[i].Version) >= 21) return javas[i].Path;
            return null;
        }

        private void StartPlay(bool force)
        {
            AslanProfile g = cfg.GetActive();
            string java = SelectedJava();
            if (java == null) { SetPlayStatus("Kein Java gefunden — bitte Java 21+ installieren."); return; }
            playBtn.Enabled = false;
            installCts = new CancellationTokenSource();
            CancellationToken ct = installCts.Token;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    InstallProgress pr = new InstallProgress();
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        while (!ct.IsCancellationRequested && pr.Phase != "Fertig")
                        {
                            SetPlayStatus(pr.Text());
                            SetProgressBar(pr.Percent());
                            Thread.Sleep(400);
                        }
                    });
                    SetPlayStatus("Installiere/prüfe Instanz …");
                    InstanceInstaller.EnsureInstalled(g, g.LoaderVersion, pr, ct);
                    if (g.SafeMode)
                    {
                        SetPlayStatus("Safe Mode …");
                        List<string> mods = ModrinthService.ListInstalled(g);
                        for (int i = 0; i < mods.Count; i++)
                            if (mods[i].EndsWith(".jar") && !mods[i].StartsWith("aslan-client") && !mods[i].StartsWith("vyra-client"))
                                ModrinthService.SetEnabled(g, mods[i], false);
                    }
                    McAccount acc = AuthService.GetActive(cfg.PlayerName.Length > 0 ? cfg.PlayerName : "Spieler");
                    SetPlayStatus("Baue Startbefehl …");
                    InstanceInstaller.LaunchPlan plan = InstanceInstaller.BuildPlan(g, acc.Name, acc.Uuid, acc.AccessToken, java, g.LoaderVersion);
                    SetPlayStatus("Starte Minecraft (" + (acc.Online ? "online" : "offline") + " als " + acc.Name + ") …");
                    StartProcess(plan, g);
                    g.LastPlayed = DateTime.Now;
                    cfg.Save();
                    SetPlayStatus("Minecraft läuft. Logs unter %APPDATA%\\ASLANCLIENT\\logs.");
                }
                catch (OperationCanceledException) { SetPlayStatus("Abgebrochen."); }
                catch (Exception ex) { SetPlayStatus("Fehler: " + ex.Message); }
                Invoke(new Action(delegate
                {
                    playBtn.Enabled = true;
                    SetProgressBar(0);
                    RefreshDashboard();
                }));
            });
        }

        private void StartProcess(InstanceInstaller.LaunchPlan plan, AslanProfile g)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = plan.JavaExe;
            psi.Arguments = plan.Arguments;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.WorkingDirectory = plan.WorkDir;
            Process proc = new Process();
            proc.StartInfo = psi;
            proc.EnableRaisingEvents = true;
            string logPath = Path.Combine(AslanPaths.LogsDir, "mc-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
            object lockObj = new object();
            DataReceivedEventHandler handler = delegate(object s, DataReceivedEventArgs e)
            {
                if (e.Data == null) return;
                lock (lockObj)
                {
                    try { File.AppendAllText(logPath, e.Data + Environment.NewLine, Encoding.UTF8); } catch { }
                }
            };
            proc.OutputDataReceived += handler;
            proc.ErrorDataReceived += handler;
            proc.Exited += delegate
            {
                try
                {
                    int code = proc.ExitCode;
                    if (code != 0)
                    {
                        string tail = "";
                        try
                        {
                            string[] lines = File.ReadAllLines(logPath, Encoding.UTF8);
                            int from = Math.Max(0, lines.Length - 60);
                            StringBuilder sb = new StringBuilder();
                            for (int i = from; i < lines.Length; i++) sb.AppendLine(lines[i]);
                            tail = sb.ToString();
                        }
                        catch { }
                        List<CrashFinding> fs = CrashAnalyzer.Analyze(tail);
                        if (fs.Count > 0) Notify("Absturz: " + fs[0].ToString());
                        else Notify("Minecraft Code " + code + " — Logs prüfen.");
                    }
                    else Notify("Minecraft normal beendet.");
                }
                catch { }
                Invoke(new Action(delegate { RefreshDashboard(); }));
            };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            Logger.Info("Minecraft gestartet (PID " + proc.Id + ", Main " + plan.MainClass + ").");
        }

        private void LoadJavas()
        {
            try
            {
                javas = JavaFinder.FindAll();
                Invoke(new Action(delegate
                {
                    javaBox.Items.Clear();
                    for (int i = 0; i < javas.Count; i++) javaBox.Items.Add(javas[i].ToString());
                    if (javaBox.Items.Count > 0)
                    {
                        int sel = 0;
                        for (int i = 0; i < javas.Count; i++)
                            if (JavaFinder.MajorOf(javas[i].Version) >= 21) { sel = i; break; }
                        javaBox.SelectedIndex = sel;
                    }
                    else javaBox.Text = "kein Java gefunden";
                }));
            }
            catch { }
        }

        private void CheckUpdates()
        {
            try
            {
                UpdateChecker.UpdateInfo info = UpdateChecker.Check(new CancellationToken());
                Invoke(new Action(delegate
                {
                    if (info.Available)
                        dashUpdate.Text = "Updates: " + info.Version + " verfügbar!";
                    else if (UpdateChecker.RepoSlug.Length == 0)
                        dashUpdate.Text = "Updates: kein Repo (Einstellungen)";
                    else
                        dashUpdate.Text = "Updates: aktuell (" + UpdateChecker.CurrentVersion + ")";
                }));
            }
            catch { }
        }

        // ================= Aktionen: Mods =================
        private void SetModTab(string tab)
        {
            modTab = tab;
            modTabMr.BackColor = tab == "modrinth" ? Color.FromArgb(0x55, 0x3D, 0x18) : AslanTheme.Surface;
            modTabCf.BackColor = tab == "curseforge" ? Color.FromArgb(0x55, 0x3D, 0x18) : AslanTheme.Surface;
            modTabInst.BackColor = tab == "installiert" ? Color.FromArgb(0x55, 0x3D, 0x18) : AslanTheme.Surface;
            modTabUpd.BackColor = tab == "updates" ? Color.FromArgb(0x55, 0x3D, 0x18) : AslanTheme.Surface;
            if (tab == "installiert" || tab == "updates") RefreshMods();
        }

        private void DoModSearch()
        {
            AslanProfile g = cfg.GetActive();
            modResults.Items.Clear();
            modResults.Items.Add("Suche läuft …");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    if (modTab == "curseforge")
                    {
                        List<CfHit> hits = CurseForgeService.Search(modSearchBox.Text.Trim(), g.MinecraftVersion, g.Loader, 25);
                        lastCfHits = hits;
                        Invoke(new Action(delegate
                        {
                            modResults.Items.Clear();
                            for (int i = 0; i < hits.Count; i++)
                                modResults.Items.Add(hits[i].Name + " — " + hits[i].Summary);
                            SetStatus(hits.Count + " Treffer (CurseForge).");
                        }));
                    }
                    else
                    {
                        List<ModrinthHit> hits = ModrinthService.Search(modSearchBox.Text.Trim(), g.Loader, g.MinecraftVersion, "mod", 25);
                        lastMrHits = hits;
                        Invoke(new Action(delegate
                        {
                            modResults.Items.Clear();
                            for (int i = 0; i < hits.Count; i++)
                                modResults.Items.Add(hits[i].Title + " — " + hits[i].Description);
                            SetStatus(hits.Count + " Treffer (Modrinth).");
                        }));
                    }
                }
                catch (CfException cfex) { SetStatus("CurseForge: " + cfex.Message); Invoke(new Action(delegate { modResults.Items.Clear(); })); }
                catch (Exception ex) { SetStatus("Suche fehlgeschlagen: " + ex.Message); Invoke(new Action(delegate { modResults.Items.Clear(); })); }
            });
        }

        private void ShowModInfo()
        {
            if (modResults.SelectedIndex < 0) return;
            try
            {
                if (modTab == "curseforge" && modResults.SelectedIndex < lastCfHits.Count)
                {
                    CfHit h = lastCfHits[modResults.SelectedIndex];
                    modInfo.Text = h.Name + "\n" + h.Summary + "\nDownloads: " + h.DownloadCount + "\n" + h.WebsiteUrl;
                }
                else if (modResults.SelectedIndex < lastMrHits.Count)
                {
                    ModrinthHit h = lastMrHits[modResults.SelectedIndex];
                    modInfo.Text = h.Title + "\n" + h.Description + "\nLizenz: " + h.License + " · Downloads: " + h.Downloads;
                }
            }
            catch { }
        }

        private void DoModInstall()
        {
            if (modResults.SelectedIndex < 0) { Notify("Bitte zuerst ein Ergebnis wählen."); return; }
            AslanProfile g = cfg.GetActive();
            if (g.Loader == "vanilla") { Notify("Vanilla-Profile unterstützen keine Mods — Loader im Profil wählen."); return; }
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    if (modTab == "curseforge")
                    {
                        CfHit h = lastCfHits[modResults.SelectedIndex];
                        List<CfFile> fs = CurseForgeService.GetFiles(h.Id, g.MinecraftVersion, g.Loader);
                        if (fs.Count == 0) { SetStatus("Keine passende Datei für dieses Profil."); return; }
                        CfFile f = fs[0];
                        Invoke(new Action(delegate
                        {
                            if (MessageBox.Show("„" + h.Name + "“ installieren?\nDatei: " + f.FileName, "Mod installieren", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
                            ThreadPool.QueueUserWorkItem(delegate
                            {
                                try
                                {
                                    CurseForgeService.Install(f, g, new DownloadProgress(), new CancellationToken());
                                    SetStatus("Installiert: " + f.FileName);
                                    Invoke(new Action(delegate { RefreshMods(); }));
                                }
                                catch (Exception ex) { SetStatus(ex.Message); }
                            });
                        }));
                    }
                    else
                    {
                        ModrinthHit h = lastMrHits[modResults.SelectedIndex];
                        List<ModrinthVersion> vs = ModrinthService.GetVersions(h.Slug, g.Loader, g.MinecraftVersion);
                        if (vs.Count == 0) { SetStatus("Keine passende Version für MC " + g.MinecraftVersion + " + " + g.Loader + "."); return; }
                        ModrinthVersion v = vs[0];
                        string lic = ModrinthService.GetLicense(h.Slug);
                        DialogResult dr = DialogResult.OK;
                        Invoke(new Action(delegate
                        {
                            dr = MessageBox.Show("„" + h.Title + "“ installieren?\nLizenz: " + lic + "\nAbhängigkeiten automatisch, Hash-geprüft.", "Mod installieren", MessageBoxButtons.OKCancel);
                        }));
                        if (dr != DialogResult.OK) return;
                        ModrinthService.Install(v, g, "mod", new DownloadProgress(), new CancellationToken());
                        SetStatus("Installiert: " + v.FileName);
                        Invoke(new Action(delegate { RefreshMods(); }));
                    }
                }
                catch (CfException cfex) { SetStatus("CurseForge: " + cfex.Message); }
                catch (Exception ex) { SetStatus("Installation: " + ex.Message); }
            });
        }

        private void OpenModPage()
        {
            try
            {
                if (modTab == "curseforge" && modResults.SelectedIndex >= 0 && modResults.SelectedIndex < lastCfHits.Count)
                    Process.Start(lastCfHits[modResults.SelectedIndex].WebsiteUrl);
                else if (modTab == "curseforge")
                    Process.Start("https://www.curseforge.com/minecraft/search?page=1&pageSize=20&searchText=" + Uri.EscapeDataString(modSearchBox.Text));
                else if (modResults.SelectedIndex >= 0 && modResults.SelectedIndex < lastMrHits.Count)
                    Process.Start("https://modrinth.com/mod/" + lastMrHits[modResults.SelectedIndex].Slug);
            }
            catch (Exception ex) { Notify(ex.Message); }
        }

        // ================= Aktionen: Modpacks =================
        private void DoPackSearch()
        {
            AslanProfile g = cfg.GetActive();
            packResults.Items.Clear();
            packResults.Items.Add("Suche läuft …");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<ModrinthHit> hits = ModrinthService.Search(packSearchBox.Text.Trim(), "", "", "modpack", 25);
                    lastPackHits = hits;
                    Invoke(new Action(delegate
                    {
                        packResults.Items.Clear();
                        for (int i = 0; i < hits.Count; i++) packResults.Items.Add(hits[i].Title + " — " + hits[i].Description);
                        SetStatus(hits.Count + " Modpacks gefunden.");
                    }));
                }
                catch (Exception ex) { SetStatus(ex.Message); Invoke(new Action(delegate { packResults.Items.Clear(); })); }
            });
        }

        private void ShowPackInfo()
        {
            if (packResults.SelectedIndex < 0 || packResults.SelectedIndex >= lastPackHits.Count) return;
            ModrinthHit h = lastPackHits[packResults.SelectedIndex];
            packInfo.Text = h.Title + "\n" + h.Description + "\nLizenz: " + h.License + "\nBei Installation wird ein NEUES Profil angelegt.";
        }

        private void DoPackInstall()
        {
            if (packResults.SelectedIndex < 0 || packResults.SelectedIndex >= lastPackHits.Count) { Notify("Bitte Modpack wählen."); return; }
            ModrinthHit h = lastPackHits[packResults.SelectedIndex];
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<ModrinthVersion> vs = ModrinthService.GetVersions(h.Slug, "", "");
                    // Modpack-Versionen sind loader-unabhängig gelistet; erste nehmen
                    if (vs.Count == 0) { SetStatus("Keine Pack-Version gefunden."); return; }
                    ModrinthVersion v = vs[0];
                    Invoke(new Action(delegate
                    {
                        if (MessageBox.Show("Modpack „" + h.Title + "“ (" + v.VersionNumber + ") installieren? Es wird ein neues Profil angelegt.", "Modpack", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            try
                            {
                                AslanProfile np = new AslanProfile();
                                np.Name = h.Title;
                                np.MinecraftVersion = v.GameVersion != null ? v.GameVersion : cfg.GetActive().MinecraftVersion;
                                np.Loader = v.Loader != null && v.Loader.Length > 0 ? v.Loader : "fabric";
                                string tmp = Path.Combine(AslanPaths.CacheDir, v.FileName);
                                DownloadCenter.Download(v.FileUrl, tmp, new DownloadProgress(), new CancellationToken());
                                ModrinthService.MrpackResult r = ModrinthService.ImportMrpack(tmp, np, new DownloadProgress(), new CancellationToken());
                                if (r.Loader != null && r.Loader.Length > 0) np.Loader = r.Loader;
                                if (r.McVersion != null && r.McVersion.Length > 0) np.MinecraftVersion = r.McVersion;
                                Invoke(new Action(delegate
                                {
                                    cfg.Profiles.Add(np);
                                    cfg.ActiveProfile = np.Name;
                                    cfg.Save();
                                    RefreshProfiles();
                                    RefreshDashboard();
                                    Notify("Modpack installiert: " + r.FilesOk + " Dateien, Fehler: " + r.Errors.Count);
                                }));
                            }
                            catch (Exception ex) { SetStatus("Modpack: " + ex.Message); }
                        });
                    }));
                }
                catch (Exception ex) { SetStatus(ex.Message); }
            });
        }

        private void ImportMrpackFile()
        {
            try
            {
                OpenFileDialog o = new OpenFileDialog();
                o.Filter = "Modrinth-Pack (*.mrpack)|*.mrpack";
                if (o.ShowDialog(this) != DialogResult.OK) return;
                AslanProfile np = new AslanProfile();
                np.Name = Path.GetFileNameWithoutExtension(o.FileName);
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        ModrinthService.MrpackResult r = ModrinthService.ImportMrpack(o.FileName, np, new DownloadProgress(), new CancellationToken());
                        if (r.Loader != null && r.Loader.Length > 0) np.Loader = r.Loader;
                        if (r.McVersion != null && r.McVersion.Length > 0) np.MinecraftVersion = r.McVersion;
                        np.ModpackName = r.Name;
                        Invoke(new Action(delegate
                        {
                            cfg.Profiles.Add(np);
                            cfg.ActiveProfile = np.Name;
                            cfg.Save();
                            RefreshProfiles();
                            Notify("Importiert: " + r.FilesOk + " Dateien, Fehler: " + r.Errors.Count);
                        }));
                    }
                    catch (Exception ex) { SetStatus("Import: " + ex.Message); }
                });
            }
            catch (Exception ex) { Notify(ex.Message); }
        }

        private void ImportCfFile()
        {
            try
            {
                OpenFileDialog o = new OpenFileDialog();
                o.Filter = "CurseForge-Export (*.zip)|*.zip";
                if (o.ShowDialog(this) != DialogResult.OK) return;
                if (!CurseForgeService.HasKey()) { Notify("Für CF-Import ist ein API-Key nötig (Mods-Tab)."); return; }
                AslanProfile np = new AslanProfile();
                np.Name = Path.GetFileNameWithoutExtension(o.FileName);
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        CurseForgeService.CfManifestResult r = CurseForgeService.ImportManifest(o.FileName, np, new DownloadProgress(), new CancellationToken());
                        np.ModpackName = r.Name;
                        if (r.McVersion != null) np.MinecraftVersion = r.McVersion;
                        Invoke(new Action(delegate
                        {
                            cfg.Profiles.Add(np);
                            cfg.ActiveProfile = np.Name;
                            cfg.Save();
                            RefreshProfiles();
                            Notify("Importiert: " + r.FilesOk + " Dateien, Fehler: " + r.Errors.Count);
                        }));
                    }
                    catch (CfException cfex) { SetStatus("CurseForge: " + cfex.Message); }
                    catch (Exception ex) { SetStatus("Import: " + ex.Message); }
                });
            }
            catch (Exception ex) { Notify(ex.Message); }
        }

        // ================= Aktionen: Shader / RP =================
        private void DoShaderSearch()
        {
            shaderResults.Items.Clear();
            shaderResults.Items.Add("Suche läuft …");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<ModrinthHit> hits = ModrinthService.Search(shaderSearchBox.Text.Trim(), "", "", "shader", 25);
                    lastShaderHits = hits;
                    Invoke(new Action(delegate
                    {
                        shaderResults.Items.Clear();
                        for (int i = 0; i < hits.Count; i++) shaderResults.Items.Add(hits[i].Title + " — " + hits[i].Description);
                    }));
                }
                catch (Exception ex) { SetStatus(ex.Message); }
            });
        }

        private void DoShaderInstall()
        {
            if (shaderResults.SelectedIndex < 0 || shaderResults.SelectedIndex >= lastShaderHits.Count) return;
            ModrinthHit h = lastShaderHits[shaderResults.SelectedIndex];
            AslanProfile g = cfg.GetActive();
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<ModrinthVersion> vs = ModrinthService.GetVersions(h.Slug, "", "");
                    if (vs.Count == 0) { SetStatus("Keine Shader-Version gefunden."); return; }
                    ModrinthService.Install(vs[0], g, "shader", new DownloadProgress(), new CancellationToken());
                    SetStatus("Shader installiert: " + vs[0].FileName + " (Iris+Sodium nötig)");
                    Invoke(new Action(delegate { RefreshShaderLists(); }));
                }
                catch (Exception ex) { SetStatus(ex.Message); }
            });
        }

        private string ShaderDir()
        {
            string d = Path.Combine(AslanPaths.ProfilesDir, MinecraftService.SafeName(cfg.GetActive().Name), "shaderpacks");
            Directory.CreateDirectory(d);
            return d;
        }

        private string RpDir()
        {
            string d = Path.Combine(AslanPaths.ProfilesDir, MinecraftService.SafeName(cfg.GetActive().Name), "resourcepacks");
            Directory.CreateDirectory(d);
            return d;
        }

        private void ToggleShader(bool enabled)
        {
            if (shaderInstalled.SelectedItem == null) return;
            try
            {
                string dir = ShaderDir();
                string f = shaderInstalled.SelectedItem.ToString();
                if (enabled && f.EndsWith(".disabled")) File.Move(Path.Combine(dir, f), Path.Combine(dir, f.Substring(0, f.Length - 9)));
                else if (!enabled && !f.EndsWith(".disabled")) File.Move(Path.Combine(dir, f), Path.Combine(dir, f + ".disabled"));
                RefreshShaderLists();
            }
            catch (Exception ex) { Notify(ex.Message); }
        }

        private void DoRpSearch()
        {
            rpResults.Items.Clear();
            rpResults.Items.Add("Suche läuft …");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<ModrinthHit> hits = ModrinthService.Search(rpSearchBox.Text.Trim(), "", "", "resourcepack", 25);
                    lastRpHits = hits;
                    Invoke(new Action(delegate
                    {
                        rpResults.Items.Clear();
                        for (int i = 0; i < hits.Count; i++) rpResults.Items.Add(hits[i].Title + " — " + hits[i].Description);
                    }));
                }
                catch (Exception ex) { SetStatus(ex.Message); }
            });
        }

        private void DoRpInstall()
        {
            if (rpResults.SelectedIndex < 0 || rpResults.SelectedIndex >= lastRpHits.Count) return;
            ModrinthHit h = lastRpHits[rpResults.SelectedIndex];
            AslanProfile g = cfg.GetActive();
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<ModrinthVersion> vs = ModrinthService.GetVersions(h.Slug, "", "");
                    if (vs.Count == 0) { SetStatus("Keine Version gefunden."); return; }
                    ModrinthService.Install(vs[0], g, "resourcepack", new DownloadProgress(), new CancellationToken());
                    SetStatus("Installiert: " + vs[0].FileName + " (ingame unter Resourcepacks aktivieren)");
                    Invoke(new Action(delegate { RefreshRpLists(); }));
                }
                catch (Exception ex) { SetStatus(ex.Message); }
            });
        }

        // ================= Aktionen: Performance =================
        private void ApplyPerf()
        {
            PerfProfile pp = PerfProfile.Balanced;
            if (profMax.Checked) pp = PerfProfile.MaxFps;
            else if (profQual.Checked) pp = PerfProfile.Quality;
            else if (profCus.Checked) pp = PerfProfile.Custom;
            PerfSettings s = PerfSettings.ForProfile(pp, hw);
            AslanProfile g = cfg.GetActive();
            g.PerfProfileName = pp.ToString();
            g.RamMB = s.AllocatedRamMB;
            cfg.ReducedMotion = motionBox.Checked;
            cfg.DiscordPresence = discordBox.Checked;
            cfg.AutoSimplifyOnWeakHardware = autoSimpBox.Checked;
            animations = !cfg.ReducedMotion;
            cfg.Save();
            ramBar.Value = Math.Max(ramBar.Minimum, Math.Min(ramBar.Maximum, g.RamMB));
            StringBuilder rec = new StringBuilder();
            rec.Append("Optimizer (").Append(hw.Tier()).Append("): Render ").Append(s.RenderDistance);
            rec.Append(", Sim ").Append(s.SimulationDistance).Append(", RAM ").Append(s.AllocatedRamMB).Append(" MB");
            if (!s.ShadersAllowed) rec.Append(" — Shader aus");
            recLabel.Text = rec.ToString();
            Notify("Performance-Profil: " + pp.ToString());
            RefreshDashboard();
        }

        private void RunBenchmark()
        {
            SetStatus("Benchmark läuft …");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<double> samples = new List<double>();
                    DateTime t0 = DateTime.Now;
                    long last = DateTime.Now.Ticks;
                    for (int i = 0; i < 300; i++)
                    {
                        Thread.Sleep(16);
                        long now = DateTime.Now.Ticks;
                        double dtMs = (now - last) / 10000.0;
                        last = now;
                        if (dtMs > 0.1 && dtMs < 250) samples.Add(1000.0 / dtMs);
                    }
                    TimeSpan load = DateTime.Now - t0;
                    Process cur = Process.GetCurrentProcess();
                    long ram = cur.WorkingSet64 / 1048576;
                    BenchmarkResult r = BenchmarkResult.FromFpsSamples(samples, ram, load);
                    Invoke(new Action(delegate
                    {
                        benchLabel.Text = string.Format("UI-Benchmark (ehrlich, keine MC-FPS): Avg {0:0} FPS, 1%-Low {1:0}, Frametime {2:0.1} ms, Spikes {3}, RAM {4} MB.",
                            r.AvgFps, r.Low1Fps, r.AvgFrametimeMs, r.Spikes, r.RamUsedMB);
                        SetStatus("Benchmark fertig.");
                    }));
                }
                catch (Exception ex) { SetStatus(ex.Message); }
            });
        }

        private void InstallPerfSet()
        {
            AslanProfile g = cfg.GetActive();
            if (g.Loader != "fabric" && g.Loader != "quilt") { Notify("Performance-Set braucht Fabric/Quilt-Profil."); return; }
            DialogResult dr = MessageBox.Show("Performance-Set installieren? Nur passende Versionen, Hash-geprüft.",
                "Performance-Set", MessageBoxButtons.OKCancel);
            if (dr != DialogResult.OK) return;
            SetStatus("Installiere Performance-Set …");
            ThreadPool.QueueUserWorkItem(delegate
            {
                int ok = 0;
                StringBuilder errs = new StringBuilder();
                for (int i = 0; i < ModConflictRules.PerfSet.Length; i++)
                {
                    try
                    {
                        List<ModrinthVersion> vs = ModrinthService.GetVersions(ModConflictRules.PerfSet[i], g.Loader, g.MinecraftVersion);
                        if (vs.Count == 0) { errs.Append(ModConflictRules.PerfSet[i]).Append(" (keine Version); "); continue; }
                        ModrinthService.Install(vs[0], g, "mod", new DownloadProgress(), new CancellationToken());
                        ok++;
                    }
                    catch (Exception ex) { errs.Append(ModConflictRules.PerfSet[i]).Append(": ").Append(ex.Message).Append("; "); }
                }
                SetStatus("Performance-Set: " + ok + " installiert. " + errs.ToString());
                Invoke(new Action(delegate { RefreshMods(); }));
            });
        }

        // ================= Aktionen: Accounts =================
        private void DoLogin()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    SetStatus("Fordere Device-Code an …");
                    DeviceCodeInfo d = AuthService.StartDeviceCode();
                    SetStatus("Browser: " + d.VerificationUri + " — Code: " + d.UserCode);
                    try { Process.Start(d.VerificationUri); } catch { }
                    CancellationToken ct = new CancellationToken();
                    string token = AuthService.PollForToken(d, ct, delegate(string s) { SetStatus(s); });
                    SetStatus("Prüfe Minecraft-Lizenz …");
                    McAccount acc = AuthService.CompleteMinecraftLogin(token);
                    cfg.PlayerName = acc.Name;
                    cfg.Save();
                    Notify("Angemeldet: " + acc.Name);
                    Invoke(new Action(delegate { RefreshAccounts(); RefreshDashboard(); }));
                }
                catch (Exception ex) { SetStatus("Login: " + ex.Message); }
            });
        }
    }

    // ================= Profil-Dialog =================
    public sealed class AslanProfileDialog : Form
    {
        private AslanConfig cfg;
        private AslanProfile edit;
        private TextBox nameBox;
        private ComboBox mcBox;
        private CheckBox snapBox;
        private FlowLayoutPanel loaderPanel;
        private string loader = "fabric";
        private ComboBox loaderVerBox;
        private TrackBar ramBar;
        private Label ramLabel;
        private CheckBox safeBox;
        private Label statusLabel;
        private Dictionary<string, Button> loaderBtns = new Dictionary<string, Button>();

        public AslanProfileDialog(AslanConfig config, string editName)
        {
            cfg = config;
            if (editName != null)
                for (int i = 0; i < cfg.Profiles.Count; i++)
                    if (cfg.Profiles[i].Name == editName) edit = cfg.Profiles[i];
            Text = edit == null ? "Neues Profil" : "Profil bearbeiten";
            Width = 640;
            Height = 620;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AslanTheme.Bg;
            ForeColor = AslanTheme.Cream;
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Build();
        }

        private void Build()
        {
            int y = 14;
            AddLabel("Profilname:", 14, y); y += 24;
            nameBox = new TextBox();
            nameBox.Width = 580; nameBox.Location = new Point(14, y);
            nameBox.BackColor = Color.FromArgb(0x0F, 0x0D, 0x0A); nameBox.ForeColor = AslanTheme.Cream;
            nameBox.Text = edit != null ? edit.Name : "Mein Profil";
            Controls.Add(nameBox); y += 36;

            AddLabel("Minecraft-Version:", 14, y); y += 24;
            mcBox = new ComboBox();
            mcBox.Width = 360; mcBox.Location = new Point(14, y);
            mcBox.DropDownStyle = ComboBoxStyle.DropDownList;
            mcBox.Items.Add(edit != null ? edit.MinecraftVersion : "1.21.11");
            mcBox.SelectedIndex = 0;
            Controls.Add(mcBox);
            snapBox = new CheckBox();
            snapBox.Text = "Snapshots";
            snapBox.ForeColor = AslanTheme.Muted; snapBox.AutoSize = true;
            snapBox.Location = new Point(384, y + 3);
            snapBox.CheckedChanged += delegate { LoadVersions(); };
            Controls.Add(snapBox); y += 36;

            AddLabel("Mod-Loader:", 14, y); y += 24;
            loaderPanel = new FlowLayoutPanel();
            loaderPanel.Location = new Point(14, y);
            loaderPanel.Size = new Size(590, 150);
            Controls.Add(loaderPanel); y += 156;

            AddLabel("Loader-Version:", 14, y); y += 24;
            loaderVerBox = new ComboBox();
            loaderVerBox.Width = 360; loaderVerBox.Location = new Point(14, y);
            Controls.Add(loaderVerBox); y += 36;

            ramLabel = new Label();
            ramLabel.Text = "RAM: 3072 MB";
            ramLabel.ForeColor = AslanTheme.Cream; ramLabel.AutoSize = true;
            ramLabel.Location = new Point(14, y); y += 22;
            Controls.Add(ramLabel);
            ramBar = new TrackBar();
            ramBar.Minimum = 1024; ramBar.Maximum = 16384; ramBar.TickFrequency = 1024;
            ramBar.Width = 580; ramBar.Location = new Point(10, y);
            ramBar.Value = edit != null ? Math.Max(1024, Math.Min(16384, edit.RamMB)) : 3072;
            ramBar.Scroll += delegate { ramLabel.Text = "RAM: " + ramBar.Value + " MB"; };
            Controls.Add(ramBar); y += 48;

            safeBox = new CheckBox();
            safeBox.Text = "Safe Mode";
            safeBox.ForeColor = AslanTheme.Cream; safeBox.AutoSize = true;
            safeBox.Location = new Point(14, y);
            safeBox.Checked = edit != null && edit.SafeMode;
            Controls.Add(safeBox); y += 30;

            statusLabel = new Label();
            statusLabel.ForeColor = AslanTheme.Muted;
            statusLabel.Location = new Point(14, y);
            statusLabel.Size = new Size(590, 40);
            Controls.Add(statusLabel); y += 44;

            Button ok = new Button();
            ok.Text = edit == null ? "PROFIL ERSTELLEN" : "SPEICHERN";
            ok.BackColor = AslanTheme.Gold;
            ok.ForeColor = Color.FromArgb(0x2A, 0x1C, 0x08);
            ok.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            ok.FlatStyle = FlatStyle.Flat;
            ok.Size = new Size(280, 44);
            ok.Location = new Point(14, y);
            ok.Cursor = Cursors.Hand;
            ok.Click += delegate { Save(); };
            Controls.Add(ok);
            Button cancel = new Button();
            cancel.Text = "Abbrechen";
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.BackColor = AslanTheme.Surface;
            cancel.ForeColor = AslanTheme.Cream;
            cancel.Size = new Size(140, 44);
            cancel.Location = new Point(304, y);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            if (edit != null) loader = edit.Loader;
            BuildLoaderCards();
            LoadVersions();
        }

        private void AddLabel(string t, int x, int y)
        {
            Label l = new Label();
            l.Text = t;
            l.ForeColor = AslanTheme.Gold;
            l.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            l.Location = new Point(x, y);
            l.AutoSize = true;
            Controls.Add(l);
        }

        private void BuildLoaderCards()
        {
            loaderPanel.Controls.Clear();
            loaderBtns.Clear();
            List<string> avail = LoaderMatrix.SuggestFor(mcBox.Text);
            for (int i = 0; i < LoaderMatrix.All.Length; i++)
            {
                LoaderMatrix.LoaderInfo li = LoaderMatrix.All[i];
                Button b = new Button();
                b.Text = li.Title + "\n" + (avail.Contains(li.Id) ? (li.AutoInstall ? "Auto" : "Assistiert") : "n/a");
                b.Width = 108; b.Height = 64;
                b.FlatStyle = FlatStyle.Flat;
                b.Cursor = Cursors.Hand;
                b.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                string id = li.Id;
                b.Click += delegate { loader = id; PaintLoaderCards(); LoadLoaderVersions(); };
                loaderBtns[id] = b;
                loaderPanel.Controls.Add(b);
                ToolTip tt = new ToolTip();
                tt.SetToolTip(b, li.Desc);
            }
            PaintLoaderCards();
        }

        private void PaintLoaderCards()
        {
            foreach (KeyValuePair<string, Button> kv in loaderBtns)
            {
                bool sel = kv.Key == loader;
                kv.Value.BackColor = sel ? Color.FromArgb(0x55, 0x3D, 0x18) : AslanTheme.Surface;
                kv.Value.ForeColor = sel ? AslanTheme.Gold : AslanTheme.Muted;
                kv.Value.FlatAppearance.BorderColor = sel ? AslanTheme.Gold : AslanTheme.Bronze;
            }
        }

        private void LoadVersions()
        {
            statusLabel.Text = "Lade Versionen …";
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<McVersionEntry> all = MinecraftService.GetVersions(snapBox.Checked);
                    Invoke(new Action(delegate
                    {
                        string cur = mcBox.Text;
                        mcBox.Items.Clear();
                        for (int i = 0; i < all.Count && i < 200; i++) mcBox.Items.Add(all[i].Id);
                        if (mcBox.Items.IndexOf(cur) >= 0) mcBox.Text = cur;
                        else if (mcBox.Items.Count > 0) mcBox.SelectedIndex = 0;
                        BuildLoaderCards();
                        LoadLoaderVersions();
                        statusLabel.Text = all.Count + " Versionen.";
                    }));
                }
                catch (Exception ex)
                {
                    Invoke(new Action(delegate { statusLabel.Text = "Offline? " + ex.Message; }));
                }
            });
        }

        private void LoadLoaderVersions()
        {
            loaderVerBox.Items.Clear();
            loaderVerBox.Text = "";
            string mc = mcBox.Text;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<string> vs = new List<string>();
                    if (loader == "fabric")
                    {
                        string meta = Http.Get(MinecraftService.FabricMeta + Uri.EscapeDataString(mc), 15000);
                        vs = MiniJson.ExtractAllStrings(meta, "version");
                    }
                    else if (loader == "quilt")
                    {
                        string meta = Http.Get(MinecraftService.QuiltMeta + Uri.EscapeDataString(mc), 15000);
                        vs = MiniJson.ExtractAllStrings(meta, "version");
                    }
                    else if (loader == "forge") vs = InstanceInstaller.FetchForgeVersions(mc);
                    else if (loader == "neoforge") vs = InstanceInstaller.FetchNeoForgeVersions(mc);
                    else if (loader == "vanilla") vs.Add("-");
                    Invoke(new Action(delegate
                    {
                        loaderVerBox.Items.Clear();
                        int n = 0;
                        for (int i = 0; i < vs.Count && n < 30; i++)
                        {
                            if (vs[i].Length > 0 && char.IsDigit(vs[i][0])) { loaderVerBox.Items.Add(vs[i]); n++; }
                        }
                        if (loaderVerBox.Items.Count > 0) loaderVerBox.SelectedIndex = 0;
                        else loaderVerBox.Text = loader == "vanilla" ? "-" : "keine Version gefunden";
                    }));
                }
                catch (Exception ex)
                {
                    Invoke(new Action(delegate { loaderVerBox.Text = "Fehler: " + ex.Message; }));
                }
            });
        }

        private void Save()
        {
            if (nameBox.Text.Trim().Length == 0) { statusLabel.Text = "Bitte Namen eingeben."; return; }
            AslanProfile p = edit ?? new AslanProfile();
            bool isNew = edit == null;
            p.Name = nameBox.Text.Trim();
            p.MinecraftVersion = mcBox.Text;
            p.Loader = loader;
            p.LoaderVersion = loaderVerBox.Text.Length > 0 ? loaderVerBox.Text : (loader == "vanilla" ? "-" : "latest");
            p.RamMB = ramBar.Value;
            p.SafeMode = safeBox.Checked;
            if (isNew) cfg.Profiles.Add(p);
            cfg.ActiveProfile = p.Name;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
