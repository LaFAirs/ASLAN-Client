using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Vyra
{
    public sealed class MainForm : Form
    {
        private VyraConfig cfg;
        private HardwareInfo hw;
        private List<JavaInstall> javas = new List<JavaInstall>();

        private TabControl tabs;
        private Label dashProfile, dashVersion, dashMods, dashRam, dashSession, dashNews, dashAccount, dashInstall;
        private PictureBox skinBox;
        private Button playBtn;
        private ProgressBar playProgress;
        private Label playStatus;

        private ListBox profileList, installedMods, searchResults, shaderList, logBox;
        private ComboBox mcVersionBox, loaderVersionBox, javaBox, backendBox;
        private TrackBar ramBar;
        private Label ramLabel;
        private TextBox javaArgsBox, searchBox, appIdBox, logView, repoBox;
        private CheckBox safeModeBox, motionBox, discordBox;
        private RadioButton profMax, profBal, profQual, profCus;
        private Label conflictLabel, benchLabel, recLabel, loginStatus, updateLabel;

        private System.Windows.Forms.Timer animTimer;
        private float glowPhase = 0;
        private Panel glowPanel;
        private CancellationTokenSource installCts;

        public MainForm(VyraConfig config)
        {
            cfg = config;
            hw = HardwareInfo.Detect();
            Text = "VYRA Client 0.2.0 — Your Minecraft. Your Performance. Your Rules.";
            Width = 1080;
            Height = 720;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(10, 10, 16);
            ForeColor = Color.FromArgb(235, 235, 245);
            Font = new Font("Segoe UI", 9.5f);
            MinimumSize = new Size(940, 620);
            BuildUi();
            RefreshAll();
            AuthService.LoadClientId();
            UpdateChecker.LoadRepoSlug();
            appIdBox.Text = AuthService.HasPlaceholderId() ? "" : AuthService.ClientId;
            repoBox.Text = UpdateChecker.RepoSlug;
            ThreadPool.QueueUserWorkItem(delegate { LoadJavas(); LoadMcVersions(); CheckUpdates(); });
        }

        private void BuildUi()
        {
            glowPanel = new Panel();
            glowPanel.Dock = DockStyle.Top;
            glowPanel.Height = 4;
            Controls.Add(glowPanel);

            Label title = new Label();
            title.Text = "VYRA CLIENT";
            title.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(167, 139, 250);
            title.Dock = DockStyle.Top;
            title.Height = 46;
            title.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(title);

            tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Appearance = TabAppearance.FlatButtons;
            tabs.ItemSize = new Size(110, 30);
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.BackColor = Color.FromArgb(16, 16, 24);
            Controls.Add(tabs);

            tabs.TabPages.Add(MakeDashboard());
            tabs.TabPages.Add(MakeProfiles());
            tabs.TabPages.Add(MakeMods());
            tabs.TabPages.Add(MakeShaders());
            tabs.TabPages.Add(MakePerformance());
            tabs.TabPages.Add(MakeAccounts());
            tabs.TabPages.Add(MakeLogs());

            playProgress = new ProgressBar();
            playProgress.Dock = DockStyle.Bottom;
            playProgress.Height = 10;
            playProgress.Style = ProgressBarStyle.Continuous;
            Controls.Add(playProgress);

            playStatus = new Label();
            playStatus.Dock = DockStyle.Bottom;
            playStatus.Height = 22;
            playStatus.TextAlign = ContentAlignment.MiddleCenter;
            playStatus.ForeColor = Color.FromArgb(148, 163, 184);
            Controls.Add(playStatus);

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 50;
            animTimer.Tick += delegate
            {
                if (cfg.ReducedMotion) return;
                glowPhase += 0.08f;
                int r = (int)(120 + 47 * Math.Sin(glowPhase));
                int b = (int)(210 + 45 * Math.Sin(glowPhase + 1.3));
                glowPanel.BackColor = Color.FromArgb(Math.Max(0, Math.Min(255, r)), 100, Math.Max(0, Math.Min(255, b)));
            };
            animTimer.Start();
        }

        private TabPage Page(string name)
        {
            TabPage p = new TabPage(name);
            p.BackColor = Color.FromArgb(14, 14, 22);
            p.ForeColor = Color.FromArgb(235, 235, 245);
            p.Padding = new Padding(14);
            return p;
        }

        private Button VButton(string text)
        {
            Button b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(124, 93, 250);
            b.FlatAppearance.BorderSize = 1;
            b.BackColor = Color.FromArgb(40, 32, 80);
            b.ForeColor = Color.White;
            b.Height = 34;
            b.Width = 170;
            b.Cursor = Cursors.Hand;
            return b;
        }

        private Label VLabel(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.ForeColor = Color.FromArgb(203, 213, 225);
            l.AutoSize = true;
            return l;
        }

        private TabPage MakeDashboard()
        {
            TabPage p = Page("Dashboard");
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            flow.AutoScroll = true;
            p.Controls.Add(flow);

            skinBox = new PictureBox();
            skinBox.Size = new Size(96, 96);
            skinBox.SizeMode = PictureBoxSizeMode.Zoom;
            skinBox.BackColor = Color.FromArgb(24, 24, 36);
            flow.Controls.Add(skinBox);

            dashProfile = VLabel("Profil: —");
            dashProfile.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            flow.Controls.Add(dashProfile);
            dashAccount = VLabel("Account: —");
            dashVersion = VLabel("Version: —");
            dashMods = VLabel("Mods: —");
            dashRam = VLabel("RAM: —");
            dashSession = VLabel("Letzte Sitzung: —");
            dashInstall = VLabel("Installation: —");
            dashNews = VLabel("Neuigkeiten: siehe Website / CHANGELOG.md");
            updateLabel = VLabel("Updates: Prüfung läuft …");
            updateLabel.MaximumSize = new Size(700, 60);
            flow.Controls.Add(dashAccount);
            flow.Controls.Add(dashVersion);
            flow.Controls.Add(dashMods);
            flow.Controls.Add(dashRam);
            flow.Controls.Add(dashSession);
            flow.Controls.Add(dashInstall);
            flow.Controls.Add(updateLabel);
            flow.Controls.Add(dashNews);

            playBtn = VButton("▶  PLAY");
            playBtn.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            playBtn.Width = 280;
            playBtn.Height = 56;
            playBtn.BackColor = Color.FromArgb(109, 72, 242);
            playBtn.Click += delegate { StartPlay(); };
            flow.Controls.Add(playBtn);

            FlowLayoutPanel row = new FlowLayoutPanel();
            row.AutoSize = true;
            Button verify = VButton("Prüfen");
            verify.Width = 120;
            verify.Click += delegate { VerifyInstall(); };
            Button repair = VButton("Reparieren");
            repair.Width = 120;
            repair.Click += delegate { StartPlayForce(); };
            Button cancel = VButton("Abbrechen");
            cancel.Width = 120;
            cancel.Click += delegate { if (installCts != null) installCts.Cancel(); };
            row.Controls.Add(verify);
            row.Controls.Add(repair);
            row.Controls.Add(cancel);
            flow.Controls.Add(row);

            Label hint = VLabel("PLAY installiert fehlende Dateien von offiziellen Mojang-/Fabric-Quellen (Hash-geprüft) und startet Minecraft mit VYRA-Mod.");
            hint.MaximumSize = new Size(700, 60);
            flow.Controls.Add(hint);
            return p;
        }

        private TabPage MakeProfiles()
        {
            TabPage p = Page("Profile");
            SplitContainer sc = new SplitContainer();
            sc.Dock = DockStyle.Fill;
            sc.SplitterDistance = 240;
            p.Controls.Add(sc);

            profileList = new ListBox();
            profileList.Dock = DockStyle.Fill;
            profileList.BackColor = Color.FromArgb(22, 22, 32);
            profileList.ForeColor = Color.White;
            profileList.SelectedIndexChanged += delegate { ShowProfile(); };
            sc.Panel1.Controls.Add(profileList);

            FlowLayoutPanel right = new FlowLayoutPanel();
            right.Dock = DockStyle.Fill;
            right.FlowDirection = FlowDirection.TopDown;
            right.WrapContents = false;
            right.AutoScroll = true;
            sc.Panel2.Controls.Add(right);

            FlowLayoutPanel row = new FlowLayoutPanel();
            row.AutoSize = true;
            Button add = VButton("+ Neu");
            add.Width = 100;
            add.Click += delegate
            {
                GameProfile g = new GameProfile();
                g.Name = "Profil " + (cfg.Profiles.Count + 1);
                cfg.Profiles.Add(g);
                cfg.ActiveProfile = g.Name;
                cfg.Save();
                RefreshAll();
            };
            Button del = VButton("Entfernen");
            del.Width = 110;
            del.Click += delegate
            {
                if (cfg.Profiles.Count <= 1) return;
                GameProfile g = cfg.GetActive();
                cfg.Profiles.Remove(g);
                cfg.ActiveProfile = cfg.Profiles[0].Name;
                cfg.Save();
                RefreshAll();
            };
            row.Controls.Add(add);
            row.Controls.Add(del);
            right.Controls.Add(row);

            right.Controls.Add(VLabel("Minecraft-Version:"));
            mcVersionBox = new ComboBox();
            mcVersionBox.Width = 260;
            mcVersionBox.DropDownStyle = ComboBoxStyle.DropDown;
            mcVersionBox.Items.Add("26.2");
            mcVersionBox.Text = "26.2";
            right.Controls.Add(mcVersionBox);

            right.Controls.Add(VLabel("Fabric-Loader:"));
            loaderVersionBox = new ComboBox();
            loaderVersionBox.Width = 260;
            loaderVersionBox.Items.Add("0.19.5");
            loaderVersionBox.Text = "0.19.5";
            right.Controls.Add(loaderVersionBox);

            right.Controls.Add(VLabel("Java:"));
            javaBox = new ComboBox();
            javaBox.Width = 420;
            javaBox.DropDownStyle = ComboBoxStyle.DropDownList;
            right.Controls.Add(javaBox);

            ramLabel = VLabel("RAM: 3072 MB");
            right.Controls.Add(ramLabel);
            ramBar = new TrackBar();
            ramBar.Minimum = 1024;
            ramBar.Maximum = 16384;
            ramBar.TickFrequency = 1024;
            ramBar.Width = 420;
            ramBar.Value = 3072;
            ramBar.Scroll += delegate { ramLabel.Text = "RAM: " + ramBar.Value + " MB"; };
            right.Controls.Add(ramBar);

            right.Controls.Add(VLabel("Java-Argumente (optional):"));
            javaArgsBox = new TextBox();
            javaArgsBox.Width = 420;
            javaArgsBox.BackColor = Color.FromArgb(22, 22, 32);
            javaArgsBox.ForeColor = Color.White;
            right.Controls.Add(javaArgsBox);

            safeModeBox = new CheckBox();
            safeModeBox.Text = "Safe Mode (optionale Mods beim Start deaktivieren)";
            safeModeBox.ForeColor = Color.FromArgb(203, 213, 225);
            safeModeBox.AutoSize = true;
            right.Controls.Add(safeModeBox);

            Button save = VButton("Speichern");
            save.Click += delegate { SaveProfile(); };
            right.Controls.Add(save);
            return p;
        }

        private TabPage MakeMods()
        {
            TabPage p = Page("Mods");
            SplitContainer sc = new SplitContainer();
            sc.Dock = DockStyle.Fill;
            sc.SplitterDistance = 430;
            p.Controls.Add(sc);

            FlowLayoutPanel left = new FlowLayoutPanel();
            left.Dock = DockStyle.Fill;
            left.FlowDirection = FlowDirection.TopDown;
            left.WrapContents = false;
            sc.Panel1.Controls.Add(left);
            left.Controls.Add(VLabel("Modrinth-Suche (offizielle API):"));
            FlowLayoutPanel srow = new FlowLayoutPanel();
            srow.AutoSize = true;
            searchBox = new TextBox();
            searchBox.Width = 240;
            searchBox.BackColor = Color.FromArgb(22, 22, 32);
            searchBox.ForeColor = Color.White;
            searchBox.Text = "sodium";
            Button go = VButton("Suchen");
            go.Width = 110;
            go.Click += delegate { DoSearch(); };
            srow.Controls.Add(searchBox);
            srow.Controls.Add(go);
            left.Controls.Add(srow);
            searchResults = new ListBox();
            searchResults.Width = 400;
            searchResults.Height = 260;
            searchResults.BackColor = Color.FromArgb(22, 22, 32);
            searchResults.ForeColor = Color.White;
            left.Controls.Add(searchResults);
            Button inst = VButton("Installieren");
            inst.Click += delegate { DoInstall(); };
            left.Controls.Add(inst);

            FlowLayoutPanel right = new FlowLayoutPanel();
            right.Dock = DockStyle.Fill;
            right.FlowDirection = FlowDirection.TopDown;
            right.WrapContents = false;
            sc.Panel2.Controls.Add(right);
            right.Controls.Add(VLabel("Installiert:"));
            installedMods = new ListBox();
            installedMods.Width = 400;
            installedMods.Height = 180;
            installedMods.BackColor = Color.FromArgb(22, 22, 32);
            installedMods.ForeColor = Color.White;
            right.Controls.Add(installedMods);
            FlowLayoutPanel brow = new FlowLayoutPanel();
            brow.AutoSize = true;
            Button en = VButton("Aktivieren");
            en.Width = 120;
            en.Click += delegate { ToggleMod(true); };
            Button dis = VButton("Deaktivieren");
            dis.Width = 120;
            dis.Click += delegate { ToggleMod(false); };
            Button rem = VButton("Entfernen");
            rem.Width = 120;
            rem.Click += delegate { RemoveMod(); };
            brow.Controls.Add(en);
            brow.Controls.Add(dis);
            brow.Controls.Add(rem);
            right.Controls.Add(brow);
            conflictLabel = VLabel("Kompatibilität: —");
            conflictLabel.MaximumSize = new Size(420, 80);
            right.Controls.Add(conflictLabel);
            right.Controls.Add(VLabel("Presets (nur verifizierte 26.2-Slugs, mit Bestätigung):"));
            FlowLayoutPanel prow = new FlowLayoutPanel();
            prow.AutoSize = true;
            Button perf = VButton("Performance-Set");
            perf.Width = 150;
            perf.Click += delegate { InstallPerfSet(); };
            Button exp = VButton("Profil exportieren");
            exp.Width = 160;
            exp.Click += delegate
            {
                try { SetStatus("Exportiert: " + ModrinthService.ExportProfile(cfg.GetActive())); }
                catch (Exception ex) { SetStatus(ex.Message); }
            };
            prow.Controls.Add(perf);
            prow.Controls.Add(exp);
            right.Controls.Add(prow);
            return p;
        }

        private TabPage MakeShaders()
        {
            TabPage p = Page("Shader");
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            p.Controls.Add(flow);
            flow.Controls.Add(VLabel("Shaderpacks (Ordner shaderpacks der aktiven Instanz):"));
            shaderList = new ListBox();
            shaderList.Width = 500;
            shaderList.Height = 220;
            shaderList.BackColor = Color.FromArgb(22, 22, 32);
            shaderList.ForeColor = Color.White;
            flow.Controls.Add(shaderList);
            FlowLayoutPanel row = new FlowLayoutPanel();
            row.AutoSize = true;
            Button en = VButton("Aktivieren");
            en.Click += delegate { ToggleShader(true); };
            Button dis = VButton("Deaktivieren");
            dis.Width = 130;
            dis.Click += delegate { ToggleShader(false); };
            Button open = VButton("Ordner öffnen");
            open.Width = 140;
            open.Click += delegate { OpenShaderDir(); };
            row.Controls.Add(en);
            row.Controls.Add(dis);
            row.Controls.Add(open);
            flow.Controls.Add(row);
            TextBox note = new TextBox();
            note.Width = 640;
            note.Height = 120;
            note.Multiline = true;
            note.ReadOnly = true;
            note.BackColor = Color.FromArgb(22, 22, 32);
            note.ForeColor = Color.FromArgb(203, 213, 225);
            note.Text = "Hinweis: Shader benötigen Iris + Sodium (OpenGL). " +
                "Vulkan ist experimentell — bei Fehlern automatisch zurück auf OpenGL wechseln. " +
                "Iris bequem über Mods-Tab installieren (Suche „iris“, MC 26.2 + Fabric wählen).";
            flow.Controls.Add(note);
            return p;
        }

        private TabPage MakePerformance()
        {
            TabPage p = Page("Performance");
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            flow.AutoScroll = true;
            p.Controls.Add(flow);

            flow.Controls.Add(VLabel("Hardware: " + hw.CpuName + " | " + hw.CpuCores + " Kerne | RAM " + hw.RamTotalMB + " MB | GPU " + hw.GpuName));
            flow.Controls.Add(VLabel("Empfehlung: Stufe " + hw.Tier() + ", RAM " + hw.RecommendedRamMB() + " MB"));

            profMax = new RadioButton(); profMax.Text = "MAX FPS"; profMax.ForeColor = Color.White; profMax.AutoSize = true;
            profBal = new RadioButton(); profBal.Text = "BALANCED"; profBal.ForeColor = Color.White; profBal.AutoSize = true; profBal.Checked = true;
            profQual = new RadioButton(); profQual.Text = "QUALITY"; profQual.ForeColor = Color.White; profQual.AutoSize = true;
            profCus = new RadioButton(); profCus.Text = "CUSTOM"; profCus.ForeColor = Color.White; profCus.AutoSize = true;
            flow.Controls.Add(profMax); flow.Controls.Add(profBal); flow.Controls.Add(profQual); flow.Controls.Add(profCus);

            flow.Controls.Add(VLabel("Grafik-Backend:"));
            backendBox = new ComboBox();
            backendBox.Width = 260;
            backendBox.DropDownStyle = ComboBoxStyle.DropDownList;
            backendBox.Items.Add("opengl");
            backendBox.Items.Add("vulkan (experimentell)");
            backendBox.SelectedIndex = 0;
            flow.Controls.Add(backendBox);

            motionBox = new CheckBox();
            motionBox.Text = "Reduced Motion (Animationen minimieren)";
            motionBox.ForeColor = Color.White;
            motionBox.AutoSize = true;
            motionBox.Checked = cfg.ReducedMotion;
            flow.Controls.Add(motionBox);

            discordBox = new CheckBox();
            discordBox.Text = "Discord Rich Presence (Opt-in)";
            discordBox.ForeColor = Color.White;
            discordBox.AutoSize = true;
            discordBox.Checked = cfg.DiscordPresence;
            flow.Controls.Add(discordBox);

            FlowLayoutPanel row = new FlowLayoutPanel();
            row.AutoSize = true;
            Button apply = VButton("Übernehmen");
            apply.Click += delegate { ApplyPerf(); };
            Button bench = VButton("Benchmark starten");
            bench.Width = 170;
            bench.Click += delegate { RunBenchmark(); };
            row.Controls.Add(apply);
            row.Controls.Add(bench);
            flow.Controls.Add(row);

            recLabel = VLabel("Optimizer: —");
            recLabel.MaximumSize = new Size(700, 140);
            flow.Controls.Add(recLabel);
            benchLabel = VLabel("Benchmark: noch nicht ausgeführt.");
            benchLabel.MaximumSize = new Size(700, 140);
            flow.Controls.Add(benchLabel);
            return p;
        }

        private TabPage MakeAccounts()
        {
            TabPage p = Page("Accounts");
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            p.Controls.Add(flow);
            loginStatus = VLabel("Status: —");
            flow.Controls.Add(loginStatus);
            flow.Controls.Add(VLabel("Microsoft App-ID (eigene Azure-App, siehe Docs/ANMELDUNG.md):"));
            appIdBox = new TextBox();
            appIdBox.Width = 420;
            appIdBox.BackColor = Color.FromArgb(22, 22, 32);
            appIdBox.ForeColor = Color.White;
            flow.Controls.Add(appIdBox);
            FlowLayoutPanel row = new FlowLayoutPanel();
            row.AutoSize = true;
            Button saveId = VButton("App-ID speichern");
            saveId.Width = 160;
            saveId.Click += delegate
            {
                string v = appIdBox.Text.Trim();
                if (v.Length == 36) { AuthService.SaveClientId(v); SetStatus("App-ID gespeichert."); }
                else SetStatus("App-ID muss eine GUID (36 Zeichen) sein.");
                RefreshLogin();
            };
            Button login = VButton("Anmelden");
            login.Width = 130;
            login.Click += delegate { DoLogin(); };
            Button logout = VButton("Abmelden");
            logout.Width = 130;
            logout.Click += delegate { AuthService.Logout(); RefreshLogin(); RefreshAll(); };
            row.Controls.Add(saveId);
            row.Controls.Add(login);
            row.Controls.Add(logout);
            flow.Controls.Add(row);
            flow.Controls.Add(VLabel("GitHub-Repo für Updates (z. B. Name/VYRA-Client, leer = Updates aus):"));
            repoBox = new TextBox();
            repoBox.Width = 420;
            repoBox.BackColor = Color.FromArgb(22, 22, 32);
            repoBox.ForeColor = Color.White;
            flow.Controls.Add(repoBox);
            Button saveRepo = VButton("Repo speichern");
            saveRepo.Width = 160;
            saveRepo.Click += delegate
            {
                UpdateChecker.SaveRepoSlug(repoBox.Text.Trim());
                SetStatus("Update-Repo gespeichert.");
                ThreadPool.QueueUserWorkItem(delegate { CheckUpdates(); });
            };
            flow.Controls.Add(saveRepo);
            flow.Controls.Add(VLabel("Es wird nie ein Passwort im Launcher abgefragt oder gespeichert. Tokens liegen nur per DPAPI (CurrentUser) unter %APPDATA%\\VYRA."));
            return p;
        }

        private TabPage MakeLogs()
        {
            TabPage p = Page("Logs");
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            p.Controls.Add(flow);
            FlowLayoutPanel row = new FlowLayoutPanel();
            row.AutoSize = true;
            Button refb = VButton("Aktualisieren");
            refb.Click += delegate { ShowLog(); ShowCrashHints(); };
            Button open = VButton("Ordner öffnen");
            open.Click += delegate { try { Process.Start(VyraPaths.LogsDir); } catch { } };
            logBox = new ListBox();
            logBox.Width = 700;
            logBox.Height = 90;
            logBox.BackColor = Color.FromArgb(22, 22, 32);
            logBox.ForeColor = Color.FromArgb(252, 165, 165);
            row.Controls.Add(refb);
            row.Controls.Add(open);
            flow.Controls.Add(row);
            flow.Controls.Add(VLabel("Crash-Analyse (mit Reparaturvorschlag):"));
            flow.Controls.Add(logBox);
            logView = new TextBox();
            logView.Width = 700;
            logView.Height = 300;
            logView.Multiline = true;
            logView.ScrollBars = ScrollBars.Both;
            logView.ReadOnly = true;
            logView.BackColor = Color.FromArgb(10, 10, 16);
            logView.ForeColor = Color.FromArgb(203, 213, 225);
            flow.Controls.Add(logView);
            return p;
        }

        private void SetStatus(string s)
        {
            if (InvokeRequired) { Invoke(new Action(delegate { SetStatus(s); })); return; }
            playStatus.Text = s;
            Logger.Info(s);
        }

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

        private void RefreshAll()
        {
            GameProfile g = cfg.GetActive();
            dashProfile.Text = "Profil: " + g.Name;
            dashVersion.Text = "Version: MC " + g.MinecraftVersion + " + " + g.Loader + " " + g.LoaderVersion;
            List<string> mods = ModrinthService.ListInstalled(g);
            dashMods.Text = "Mods: " + mods.Count + " installiert";
            dashRam.Text = "RAM: " + g.RamMB + " MB zugewiesen";
            dashSession.Text = "Letzte Sitzung: " + (g.LastPlayed == DateTime.MinValue ? "—" : g.LastPlayed.ToString());
            string acc = AuthService.StoredProfileName();
            if (acc == null) acc = cfg.PlayerName.Length > 0 ? cfg.PlayerName + " (offline)" : "nicht angemeldet";
            dashAccount.Text = "Account: " + acc;
            try
            {
                string user = acc.Split(' ')[0];
                if (user.Length >= 3 && user != "nicht")
                    skinBox.LoadAsync("https://crafatar.com/avatars/" + Uri.EscapeDataString(user) + "?size=100&overlay");
            }
            catch { }
            profileList.Items.Clear();
            for (int i = 0; i < cfg.Profiles.Count; i++) profileList.Items.Add(cfg.Profiles[i].Name);
            profileList.SelectedItem = cfg.ActiveProfile;
            ShowProfile();
            RefreshMods();
            RefreshShaders();
            RefreshLogin();
            ShowLog();
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<string> problems = InstanceInstaller.Verify(g);
                Invoke(new Action(delegate
                {
                    dashInstall.Text = "Installation: " + (problems.Count == 0 ? "vollständig ✓" : (problems.Count + " Problem(e) — Prüfen/Reparieren nutzen"));
                }));
            });
        }

        private void ShowProfile()
        {
            GameProfile g = cfg.GetActive();
            if (profileList.SelectedItem != null) { cfg.ActiveProfile = profileList.SelectedItem.ToString(); g = cfg.GetActive(); }
            mcVersionBox.Text = g.MinecraftVersion;
            loaderVersionBox.Text = g.LoaderVersion;
            ramBar.Value = Math.Max(ramBar.Minimum, Math.Min(ramBar.Maximum, g.RamMB));
            ramLabel.Text = "RAM: " + g.RamMB + " MB";
            javaArgsBox.Text = g.JavaArgs;
            safeModeBox.Checked = g.SafeMode;
        }

        private void SaveProfile()
        {
            GameProfile g = cfg.GetActive();
            g.MinecraftVersion = mcVersionBox.Text.Trim().Length > 0 ? mcVersionBox.Text.Trim() : "26.2";
            g.LoaderVersion = loaderVersionBox.Text.Trim().Length > 0 ? loaderVersionBox.Text.Trim() : "0.19.5";
            g.RamMB = ramBar.Value;
            g.JavaArgs = javaArgsBox.Text;
            g.SafeMode = safeModeBox.Checked;
            cfg.Save();
            SetStatus("Profil gespeichert.");
            RefreshAll();
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

        private void LoadMcVersions()
        {
            try
            {
                List<McVersionEntry> all = MinecraftService.GetVersions();
                Invoke(new Action(delegate
                {
                    string cur = mcVersionBox.Text;
                    mcVersionBox.Items.Clear();
                    int n = 0;
                    for (int i = 0; i < all.Count && n < 40; i++)
                    {
                        if (all[i].Type != "release") continue;
                        mcVersionBox.Items.Add(all[i].Id);
                        n++;
                    }
                    if (mcVersionBox.Items.IndexOf(cur) >= 0) mcVersionBox.Text = cur;
                }));
            }
            catch (Exception ex) { Logger.Warn("Versionsliste: " + ex.Message); }
        }

        private string SelectedJava()
        {
            if (javaBox.SelectedIndex >= 0 && javaBox.SelectedIndex < javas.Count) return javas[javaBox.SelectedIndex].Path;
            return null;
        }

        private void CheckUpdates()
        {
            try
            {
                UpdateChecker.UpdateInfo info = UpdateChecker.Check(new CancellationToken());
                Invoke(new Action(delegate
                {
                    if (info.Available)
                        updateLabel.Text = "Update verfügbar: " + info.Version + " — " + info.Notes.Substring(0, Math.Min(120, info.Notes.Length));
                    else if (UpdateChecker.RepoSlug.Length == 0)
                        updateLabel.Text = "Updates: kein Repo konfiguriert (Accounts-Tab).";
                    else
                        updateLabel.Text = "Updates: aktuell (" + UpdateChecker.CurrentVersion + ").";
                }));
            }
            catch { }
        }

        private void VerifyInstall()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<string> problems = InstanceInstaller.Verify(cfg.GetActive());
                if (problems.Count == 0) SetStatus("Installation vollständig.");
                else SetStatus("Probleme: " + string.Join(" | ", problems.ToArray()));
                Invoke(new Action(delegate { RefreshAll(); }));
            });
        }

        private void StartPlayForce()
        {
            StartPlayInternal(true);
        }

        private void StartPlay()
        {
            StartPlayInternal(false);
        }

        private void StartPlayInternal(bool forceReinstall)
        {
            GameProfile g = cfg.GetActive();
            string java = SelectedJava();
            if (java == null) { SetStatus("Kein Java gefunden — bitte Java 21+ installieren."); return; }
            int major = JavaFinder.MajorOf(JavaFinder.ProbeVersion(java));
            int needMajor = 21;
            try
            {
                string vjp = MinecraftService.EnsureVersionJson(g.MinecraftVersion);
                needMajor = InstanceInstaller.RequiredJavaMajor(File.ReadAllText(vjp, Encoding.UTF8));
            }
            catch { }
            if (major != 0 && major < needMajor) { SetStatus("Java " + major + " ist zu alt (min. " + needMajor + " für MC " + g.MinecraftVersion + ")."); return; }
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
                            SetStatus(pr.Text());
                            SetProgressBar(pr.Percent());
                            Thread.Sleep(400);
                        }
                    });
                    SetStatus("Installiere/prüfe Instanz …");
                    InstanceInstaller.EnsureInstalled(g, g.LoaderVersion, pr, ct);
                    if (g.SafeMode)
                    {
                        SetStatus("Safe Mode: deaktiviere optionale Mods …");
                        List<string> mods = ModrinthService.ListInstalled(g);
                        for (int i = 0; i < mods.Count; i++)
                        {
                            if (mods[i].EndsWith(".jar") && !mods[i].StartsWith("vyra-client"))
                                ModrinthService.SetEnabled(g, mods[i], false);
                        }
                    }
                    McAccount acc = AuthService.StoredAccount(cfg.PlayerName.Length > 0 ? cfg.PlayerName : "Spieler");
                    SetStatus("Baue Startbefehl …");
                    InstanceInstaller.LaunchPlan plan = InstanceInstaller.BuildPlan(g, acc.Name, acc.Uuid, acc.AccessToken, java, g.LoaderVersion);
                    SetStatus("Starte Minecraft (" + (acc.Online ? "online" : "offline") + " als " + acc.Name + ") …");
                    StartProcess(plan, g);
                    g.LastPlayed = DateTime.Now;
                    cfg.Save();
                    SetStatus("Minecraft läuft (PID protokolliert). Logs im Logs-Tab.");
                }
                catch (OperationCanceledException) { SetStatus("Abgebrochen."); }
                catch (Exception ex) { SetStatus("Fehler: " + ex.Message); }
                Invoke(new Action(delegate
                {
                    playBtn.Enabled = true;
                    SetProgressBar(0);
                    RefreshAll();
                }));
            });
        }

        private void StartProcess(InstanceInstaller.LaunchPlan plan, GameProfile g)
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
            string logPath = Path.Combine(VyraPaths.LogsDir, "mc-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
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
                    Logger.Info("Minecraft beendet (Code " + code + ").");
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
                        if (fs.Count > 0) SetStatus("Absturz erkannt: " + fs[0].ToString());
                        else SetStatus("Minecraft mit Code " + code + " beendet — Log prüfen.");
                    }
                    else SetStatus("Minecraft normal beendet.");
                }
                catch { }
                Invoke(new Action(delegate { RefreshAll(); }));
            };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            Logger.Info("Minecraft gestartet (PID " + proc.Id + ", Main " + plan.MainClass + ").");
        }

        private List<ModrinthHit> lastHits = new List<ModrinthHit>();

        private void DoSearch()
        {
            GameProfile g = cfg.GetActive();
            SetStatus("Suche Mods …");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<ModrinthHit> hits = ModrinthService.Search(searchBox.Text.Trim(), "fabric", g.MinecraftVersion, 25);
                    lastHits = hits;
                    Invoke(new Action(delegate
                    {
                        searchResults.Items.Clear();
                        for (int i = 0; i < hits.Count; i++)
                            searchResults.Items.Add(hits[i].Title + " — " + hits[i].Description);
                        SetStatus(hits.Count + " Treffer.");
                    }));
                }
                catch (Exception ex) { SetStatus("Suche fehlgeschlagen: " + ex.Message); }
            });
        }

        private void DoInstall()
        {
            if (searchResults.SelectedIndex < 0 || searchResults.SelectedIndex >= lastHits.Count) { SetStatus("Bitte zuerst ein Suchergebnis wählen."); return; }
            ModrinthHit h = lastHits[searchResults.SelectedIndex];
            GameProfile g = cfg.GetActive();
            string lic = ModrinthService.GetLicense(h.Slug);
            DialogResult dr = MessageBox.Show("„" + h.Title + "“ installieren?\nLizenz: " + lic + "\nAbhängigkeiten werden automatisch mitinstalliert (nur passende MC-/Fabric-Version).",
                "Mod installieren", MessageBoxButtons.OKCancel);
            if (dr != DialogResult.OK) return;
            SetStatus("Installiere " + h.Title + " …");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<ModrinthVersion> vs = ModrinthService.GetVersions(h.Slug, "fabric", g.MinecraftVersion);
                    if (vs.Count == 0) { SetStatus("Keine passende Version für MC " + g.MinecraftVersion + " + Fabric gefunden."); return; }
                    CancellationToken ct = new CancellationToken();
                    DownloadProgress dp = new DownloadProgress();
                    ModrinthService.Install(vs[0], g, dp, ct);
                    SetStatus("Installiert (Hash-geprüft): " + vs[0].FileName);
                    Invoke(new Action(delegate { RefreshMods(); }));
                }
                catch (Exception ex) { SetStatus("Installation fehlgeschlagen: " + ex.Message); }
            });
        }

        private void InstallPerfSet()
        {
            GameProfile g = cfg.GetActive();
            DialogResult dr = MessageBox.Show("Performance-Set für MC " + g.MinecraftVersion + " installieren?\n" +
                string.Join(", ", ModConflictRules.PerfSet262) +
                "\nNur Versionen passend zu MC + Fabric, Hash-geprüft. ModernFix ist bewusst NICHT dabei (kein 26.2).",
                "Performance-Set", MessageBoxButtons.OKCancel);
            if (dr != DialogResult.OK) return;
            SetStatus("Installiere Performance-Set …");
            ThreadPool.QueueUserWorkItem(delegate
            {
                int ok = 0;
                StringBuilder errs = new StringBuilder();
                for (int i = 0; i < ModConflictRules.PerfSet262.Length; i++)
                {
                    try
                    {
                        List<ModrinthVersion> vs = ModrinthService.GetVersions(ModConflictRules.PerfSet262[i], "fabric", g.MinecraftVersion);
                        if (vs.Count == 0) { errs.Append(ModConflictRules.PerfSet262[i]).Append(" (keine 26.2-Version); "); continue; }
                        ModrinthService.Install(vs[0], g, new DownloadProgress(), new CancellationToken());
                        ok++;
                        SetStatus("Set: " + ok + "/" + ModConflictRules.PerfSet262.Length + " …");
                    }
                    catch (Exception ex) { errs.Append(ModConflictRules.PerfSet262[i]).Append(": ").Append(ex.Message).Append("; "); }
                }
                SetStatus("Performance-Set: " + ok + " installiert. " + errs.ToString());
                Invoke(new Action(delegate { RefreshMods(); }));
            });
        }

        private void RefreshMods()
        {
            GameProfile g = cfg.GetActive();
            List<string> mods = ModrinthService.ListInstalled(g);
            installedMods.Items.Clear();
            for (int i = 0; i < mods.Count; i++) installedMods.Items.Add(mods[i]);
            List<string> ids = new List<string>(mods);
            string problem = ModConflictRules.CheckSet(ids);
            conflictLabel.Text = "Kompatibilität: " + (problem == null ? "keine bekannten Konflikte." : problem);
            List<string> hints = CrashAnalyzer.MissingHints(ids);
            if (hints.Count > 0) conflictLabel.Text += " Hinweis: " + string.Join(" ", hints.ToArray());
        }

        private void ToggleMod(bool enabled)
        {
            if (installedMods.SelectedItem == null) return;
            string f = installedMods.SelectedItem.ToString().Replace(" (deaktiviert)", "");
            try
            {
                ModrinthService.SetEnabled(cfg.GetActive(), f.Replace(".disabled", ""), enabled);
                RefreshMods();
            }
            catch (Exception ex) { SetStatus(ex.Message); }
        }

        private void RemoveMod()
        {
            if (installedMods.SelectedItem == null) return;
            string f = installedMods.SelectedItem.ToString().Replace(" (deaktiviert)", "").Replace(".disabled", "");
            try
            {
                ModrinthService.Remove(cfg.GetActive(), f);
                SetStatus("Entfernt (Backup unter backups/).");
                RefreshMods();
            }
            catch (Exception ex) { SetStatus(ex.Message); }
        }

        private string ShaderDir()
        {
            string d = Path.Combine(VyraPaths.ProfilesDir, MinecraftService.SafeName(cfg.GetActive().Name), "shaderpacks");
            Directory.CreateDirectory(d);
            return d;
        }

        private void RefreshShaders()
        {
            if (shaderList == null) return;
            shaderList.Items.Clear();
            try
            {
                string[] files = Directory.GetFiles(ShaderDir());
                for (int i = 0; i < files.Length; i++) shaderList.Items.Add(Path.GetFileName(files[i]));
            }
            catch { }
        }

        private void ToggleShader(bool enabled)
        {
            if (shaderList.SelectedItem == null) return;
            string f = shaderList.SelectedItem.ToString();
            try
            {
                string dir = ShaderDir();
                if (enabled && f.EndsWith(".disabled")) File.Move(Path.Combine(dir, f), Path.Combine(dir, f.Substring(0, f.Length - 9)));
                else if (!enabled && !f.EndsWith(".disabled")) File.Move(Path.Combine(dir, f), Path.Combine(dir, f + ".disabled"));
                RefreshShaders();
            }
            catch (Exception ex) { SetStatus(ex.Message); }
        }

        private void OpenShaderDir()
        {
            try { Process.Start(ShaderDir()); } catch { }
        }

        private void ApplyPerf()
        {
            PerfProfile pp = PerfProfile.Balanced;
            if (profMax.Checked) pp = PerfProfile.MaxFps;
            else if (profQual.Checked) pp = PerfProfile.Quality;
            else if (profCus.Checked) pp = PerfProfile.Custom;
            PerfSettings s = PerfSettings.ForProfile(pp, hw);
            GameProfile g = cfg.GetActive();
            g.PerfProfileName = pp.ToString();
            g.RamMB = s.AllocatedRamMB;
            cfg.ReducedMotion = motionBox.Checked;
            cfg.DiscordPresence = discordBox.Checked;
            cfg.Save();
            ramBar.Value = Math.Max(ramBar.Minimum, Math.Min(ramBar.Maximum, g.RamMB));
            StringBuilder rec = new StringBuilder();
            rec.Append("Optimizer (").Append(hw.Tier()).Append("): Render ").Append(s.RenderDistance);
            rec.Append(", Sim ").Append(s.SimulationDistance).Append(", RAM ").Append(s.AllocatedRamMB).Append(" MB");
            if (!s.ShadersAllowed) rec.Append(" — Shader aus (Empfehlung)");
            if (hw.Tier() == "LOW") rec.Append(" — Partikel aus, Max-FPS-Profil empfohlen");
            recLabel.Text = rec.ToString();
            SetStatus("Performance-Profil übernommen: " + pp.ToString());
            RefreshAll();
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
                        benchLabel.Text = string.Format("UI-Benchmark (ehrlich gelabelt, keine MC-FPS): Avg {0:0} FPS, 1%-Low {1:0}, Frametime {2:0.1} ms, Spikes {3}, RAM {4} MB.",
                            r.AvgFps, r.Low1Fps, r.AvgFrametimeMs, r.Spikes, r.RamUsedMB);
                        SetStatus("Benchmark abgeschlossen.");
                    }));
                }
                catch (Exception ex) { SetStatus("Benchmark fehlgeschlagen: " + ex.Message); }
            });
        }

        private void RefreshLogin()
        {
            if (loginStatus == null) return;
            string name = AuthService.StoredProfileName();
            if (name != null) loginStatus.Text = "Status: angemeldet als " + name + " (Berechtigung geprüft).";
            else if (AuthService.HasPlaceholderId()) loginStatus.Text = "Status: nicht angemeldet (App-ID erforderlich — siehe Docs).";
            else loginStatus.Text = "Status: nicht angemeldet.";
        }

        private void DoLogin()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    SetStatus("Fordere Device-Code an …");
                    DeviceCodeInfo d = AuthService.StartDeviceCode();
                    SetStatus("Bitte im Browser bestätigen: " + d.VerificationUri + " — Code: " + d.UserCode);
                    try { Process.Start(d.VerificationUri); } catch { }
                    CancellationToken ct = new CancellationToken();
                    string token = AuthService.PollForToken(d, ct, delegate(string s) { SetStatus(s); });
                    SetStatus("Prüfe Minecraft-Berechtigung …");
                    McAccount acc = AuthService.CompleteMinecraftLogin(token);
                    cfg.PlayerName = acc.Name;
                    cfg.Save();
                    SetStatus("Angemeldet als " + acc.Name + ".");
                    Invoke(new Action(delegate { RefreshLogin(); RefreshAll(); }));
                }
                catch (Exception ex) { SetStatus("Login: " + ex.Message); }
            });
        }

        private void ShowLog()
        {
            if (logView == null) return;
            try
            {
                string f = Logger.LogFile;
                if (File.Exists(f))
                {
                    string[] lines = File.ReadAllLines(f, Encoding.UTF8);
                    int from = Math.Max(0, lines.Length - 200);
                    StringBuilder sb = new StringBuilder();
                    for (int i = from; i < lines.Length; i++) sb.AppendLine(lines[i]);
                    logView.Text = sb.ToString();
                }
            }
            catch { }
        }

        private void ShowCrashHints()
        {
            if (logBox == null) return;
            logBox.Items.Clear();
            try
            {
                string inst = Path.Combine(VyraPaths.ProfilesDir, MinecraftService.SafeName(cfg.GetActive().Name));
                List<string> hints = MinecraftService.ReadCrashHints(inst);
                if (hints.Count == 0) logBox.Items.Add("Keine Crash-Reports in dieser Instanz.");
                for (int i = 0; i < hints.Count; i++) logBox.Items.Add(hints[i]);
            }
            catch { }
        }
    }
}
