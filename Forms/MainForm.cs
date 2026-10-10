using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using discord_app.Data;
using discord_app.Models;

namespace discord_app.Forms
{
    /// <summary>
    /// 【職責分離教學 - UI 表現層 (MainForm)】
    /// 核心理念：
    /// 1. 這裡只負責「按鈕點擊事件」、「繪製畫面」、「聆聽使用者輸入」。
    /// 2. 【絕對不允許出現任何 SQL 語法】或直接操作資料庫連線。
    /// 3. 所有要查、要存、要改、要刪的資料，通通委託給 _repo (FriendRepository) 去做！
    /// </summary>
    public class MainForm : Form
    {
        // 【依賴資料存取層】：只認 Repository，不認 SQL 或 DbContext
        private readonly FriendRepository _repo = new();

        // Discord 主題精準配色
        private static readonly Color ColorServerBar = ColorTranslator.FromHtml("#1e1f22");
        private static readonly Color ColorSidebar = ColorTranslator.FromHtml("#2b2d31");
        private static readonly Color ColorMainArea = ColorTranslator.FromHtml("#313338");
        private static readonly Color ColorCardBg = ColorTranslator.FromHtml("#2b2d31");
        private static readonly Color ColorCardHover = ColorTranslator.FromHtml("#383a40");
        private static readonly Color ColorSearchBg = ColorTranslator.FromHtml("#1e1f22");
        private static readonly Color ColorBlurple = ColorTranslator.FromHtml("#5865f2");
        private static readonly Color ColorBlurpleHover = ColorTranslator.FromHtml("#4752c4");

        private static readonly Color ColorOnline = ColorTranslator.FromHtml("#23a55a");
        private static readonly Color ColorIdle = ColorTranslator.FromHtml("#f0b232");
        private static readonly Color ColorOffline = ColorTranslator.FromHtml("#80848e");

        private static readonly Color TextWhite = ColorTranslator.FromHtml("#f2f3f5");
        private static readonly Color TextNormal = ColorTranslator.FromHtml("#dbdee1");
        private static readonly Color TextMuted = ColorTranslator.FromHtml("#949ba4");

        // 控制項
        private FlowLayoutPanel _flowFriends = null!;
        private TextBox _txtSearch = null!;
        private Button _btnTabOnline = null!;
        private Button _btnTabAll = null!;
        private Label _lblListTitle = null!;

        // 狀態快照 (記錄當前過濾分頁：false = 全部, true = 僅線上)
        private bool _filterOnlineOnly = false;

        public MainForm()
        {
            InitializeComponent();
            this.Load += async (s, e) => await InitializeAppAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Discord - 好友名單 (CRUD 架構教學範例)";
            this.Size = new Size(1150, 720);
            this.MinimumSize = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorMainArea;
            this.ForeColor = TextNormal;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            // ==========================================
            // 1. 最左側：伺服器裝飾列 (Server Bar: #1e1f22)
            // ==========================================
            var pnlServerBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 72,
                BackColor = ColorServerBar
            };
            BuildServerBar(pnlServerBar);

            // ==========================================
            // 2. 次層：側邊導覽列 (Sidebar: #2b2d31)
            // ==========================================
            var pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = ColorSidebar
            };
            BuildSidebar(pnlSidebar);

            // ==========================================
            // 3. 右側主工作區 (Main Area: #313338)
            // ==========================================
            var pnlMainArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorMainArea
            };

            // 頂部導覽列 (Header Bar)
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ColorMainArea,
                Padding = new Padding(20, 10, 20, 10)
            };
            pnlHeader.Paint += (s, e) =>
            {
                // 畫一條 Discord 風格細底線
                using var pen = new Pen(ColorTranslator.FromHtml("#202225"), 1);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };
            BuildHeader(pnlHeader);

            // 主內容區卡片標題
            _lblListTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                Text = "全部好友 — 載入中...",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = TextMuted,
                Padding = new Padding(24, 8, 0, 0)
            };

            // 好友卡片流動清單 (FlowLayoutPanel)
            _flowFriends = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(20, 5, 20, 20),
                BackColor = ColorMainArea
            };
            _flowFriends.SizeChanged += (s, e) => AdjustCardWidths();

            pnlMainArea.Controls.Add(_flowFriends);
            pnlMainArea.Controls.Add(_lblListTitle);
            pnlMainArea.Controls.Add(pnlHeader);

            // 組合到表單
            this.Controls.Add(pnlMainArea);
            this.Controls.Add(pnlSidebar);
            this.Controls.Add(pnlServerBar);
        }

        #region UI 介面佈局組件建立

        private void BuildServerBar(Panel panel)
        {
            // Discord 招牌首頁圖示按鈕
            var btnHome = new Button
            {
                Size = new Size(48, 48),
                Location = new Point(12, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorBlurple,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Symbol", 16F, FontStyle.Bold),
                Text = "🎮",
                Cursor = Cursors.Hand
            };
            btnHome.FlatAppearance.BorderSize = 0;

            // 分隔線
            var pnlDivider = new Panel
            {
                Size = new Size(32, 2),
                Location = new Point(20, 70),
                BackColor = ColorTranslator.FromHtml("#35363c")
            };

            // 假伺服器圖示 (裝飾用)
            var btnServer1 = CreateMockServerPill("C#", 82, ColorTranslator.FromHtml("#313338"));
            var btnServer2 = CreateMockServerPill("SQL", 138, ColorTranslator.FromHtml("#313338"));

            // 新增伺服器加號
            var btnAddServer = CreateMockServerPill("+", 194, ColorTranslator.FromHtml("#313338"));
            btnAddServer.ForeColor = ColorOnline;

            panel.Controls.AddRange(new Control[] { btnHome, pnlDivider, btnServer1, btnServer2, btnAddServer });
        }

        private Button CreateMockServerPill(string text, int top, Color bg)
        {
            var btn = new Button
            {
                Size = new Size(48, 48),
                Location = new Point(12, top),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Text = text,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => { btn.BackColor = ColorBlurple; btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { btn.BackColor = bg; btn.ForeColor = TextWhite; };
            return btn;
        }

        private void BuildSidebar(Panel panel)
        {
            // 搜尋或對話觸發列 (裝飾)
            var btnSearchDeco = new Button
            {
                Size = new Size(216, 32),
                Location = new Point(12, 14),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorSearchBg,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5F),
                Text = "尋找或開始新對話",
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btnSearchDeco.FlatAppearance.BorderSize = 0;

            // 好友專屬按鈕 (選中狀態)
            var btnFriends = new Button
            {
                Size = new Size(216, 42),
                Location = new Point(12, 56),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#3f4248"),
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Text = "   👥   好友",
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btnFriends.FlatAppearance.BorderSize = 0;

            // 私訊標籤
            var lblDm = new Label
            {
                Text = "DIRECT MESSAGES",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = TextMuted,
                Location = new Point(16, 114),
                AutoSize = true
            };

            // 底部使用者卡片
            var pnlUserFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = ColorTranslator.FromHtml("#232428"),
                Padding = new Padding(8, 8, 8, 8)
            };

            var lblUserName = new Label
            {
                Text = "MyAccount",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = TextWhite,
                Location = new Point(48, 10),
                AutoSize = true
            };

            var lblUserTag = new Label
            {
                Text = "#0001 (線上)",
                Font = new Font("Segoe UI", 8F),
                ForeColor = ColorOnline,
                Location = new Point(48, 28),
                AutoSize = true
            };

            // 頭像圓圈
            var pnlAvatar = new Panel
            {
                Size = new Size(34, 34),
                Location = new Point(8, 10),
                BackColor = ColorBlurple
            };
            pnlAvatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(ColorBlurple);
                e.Graphics.FillEllipse(brush, 0, 0, 33, 33);
                using var font = new Font("Segoe UI", 11F, FontStyle.Bold);
                using var textBrush = new SolidBrush(Color.White);
                e.Graphics.DrawString("ME", font, textBrush, 3, 5);
            };

            pnlUserFooter.Controls.AddRange(new Control[] { pnlAvatar, lblUserName, lblUserTag });

            panel.Controls.AddRange(new Control[] { btnSearchDeco, btnFriends, lblDm, pnlUserFooter });
        }

        private void BuildHeader(Panel panel)
        {
            // 標題「好友」
            var lblTitle = new Label
            {
                Text = "👥 好友",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = TextWhite,
                Location = new Point(16, 16),
                AutoSize = true
            };

            var lblSep = new Label
            {
                Text = "|",
                Font = new Font("Segoe UI", 12F),
                ForeColor = ColorTranslator.FromHtml("#4e5058"),
                Location = new Point(90, 15),
                AutoSize = true
            };

            // 分頁切換鈕：線上
            _btnTabOnline = new Button
            {
                Text = "線上",
                Size = new Size(60, 32),
                Location = new Point(115, 14),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorMainArea,
                ForeColor = TextNormal,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnTabOnline.FlatAppearance.BorderSize = 0;
            _btnTabOnline.Click += async (s, e) => await SwitchTabAsync(onlineOnly: true);

            // 分頁切換鈕：全部
            _btnTabAll = new Button
            {
                Text = "全部",
                Size = new Size(60, 32),
                Location = new Point(180, 14),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#383a40"),
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnTabAll.FlatAppearance.BorderSize = 0;
            _btnTabAll.Click += async (s, e) => await SwitchTabAsync(onlineOnly: false);

            // [C - Create] 新增好友按鈕 (Discord 招牌紫色 Blurple)
            var btnAddFriend = new Button
            {
                Text = "+ 新增好友",
                Size = new Size(100, 32),
                Location = new Point(255, 14),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorBlurple,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddFriend.FlatAppearance.BorderSize = 0;
            btnAddFriend.MouseEnter += (s, e) => btnAddFriend.BackColor = ColorBlurpleHover;
            btnAddFriend.MouseLeave += (s, e) => btnAddFriend.BackColor = ColorBlurple;
            btnAddFriend.Click += async (s, e) => await OnAddFriendClickAsync();

            // [R - Read] 搜尋框包裝 Panel
            var pnlSearch = new Panel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(240, 34),
                Location = new Point(panel.Width - 260, 13),
                BackColor = ColorSearchBg,
                Padding = new Padding(8, 7, 8, 7)
            };

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = ColorSearchBg,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9.5F)
            };
            _txtSearch.PlaceholderText = "🔍 搜尋好友或備註...";
            _txtSearch.TextChanged += async (s, e) => await RefreshFriendsListAsync();

            pnlSearch.Controls.Add(_txtSearch);

            panel.Controls.AddRange(new Control[] {
                lblTitle, lblSep, _btnTabOnline, _btnTabAll, btnAddFriend, pnlSearch
            });
        }

        #endregion

        #region 【職責分離教學 - 業務邏輯與畫面綁定】

        /// <summary>
        /// 程式初始化：確保假資料並載入第一批好友
        /// </summary>
        private async Task InitializeAppAsync()
        {
            try
            {
                // 【職責分離】：通知 Repository 檢查並塞假資料，UI 完全不管底層是怎麼 INSERT 的！
                await _repo.EnsureSeedDataAsync();
                await RefreshFriendsListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"連線 SQL Server 發生錯誤：\n{ex.Message}\n\n請確認 SQL Server 服務已啟動且密碼正確！", 
                    "資料庫連線失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 切換「線上」與「全部」分頁
        /// </summary>
        private async Task SwitchTabAsync(bool onlineOnly)
        {
            _filterOnlineOnly = onlineOnly;

            // 切換按鈕的高亮樣式
            if (_filterOnlineOnly)
            {
                _btnTabOnline.BackColor = ColorTranslator.FromHtml("#383a40");
                _btnTabOnline.ForeColor = TextWhite;
                _btnTabAll.BackColor = ColorMainArea;
                _btnTabAll.ForeColor = TextNormal;
            }
            else
            {
                _btnTabAll.BackColor = ColorTranslator.FromHtml("#383a40");
                _btnTabAll.ForeColor = TextWhite;
                _btnTabOnline.BackColor = ColorMainArea;
                _btnTabOnline.ForeColor = TextNormal;
            }

            await RefreshFriendsListAsync();
        }

        /// <summary>
        /// [R - Read 核心刷新方法]
        /// 呼叫 Repository 查詢資料，並轉化為深色卡片清單
        /// </summary>
        private async Task RefreshFriendsListAsync()
        {
            string keyword = _txtSearch.Text.Trim();

            // 【職責分離】：畫面層只負責拿回資料清單 List<Friend>，完全沒有 SELECT 語法
            List<Friend> friends = await _repo.SearchFriendsAsync(keyword, _filterOnlineOnly);

            // 更新標題數量
            string tabName = _filterOnlineOnly ? "線上好友" : "全部好友";
            _lblListTitle.Text = $"{tabName} — {friends.Count} 位";

            // 重新渲染卡片
            _flowFriends.SuspendLayout();
            _flowFriends.Controls.Clear();

            if (friends.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "沒有找到任何符合條件的好友 🍃",
                    Font = new Font("Segoe UI", 11F),
                    ForeColor = TextMuted,
                    Size = new Size(500, 60),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(50, 40, 0, 0)
                };
                _flowFriends.Controls.Add(lblEmpty);
            }
            else
            {
                foreach (var friend in friends)
                {
                    var card = CreateFriendCard(friend);
                    _flowFriends.Controls.Add(card);
                }
            }

            _flowFriends.ResumeLayout();
            AdjustCardWidths();
        }

        /// <summary>
        /// 動態生成單張 Discord 風格好友卡片 (包含頭像、狀態圓點、右鍵選單、刪除按鈕)
        /// </summary>
        private Panel CreateFriendCard(Friend friend)
        {
            var pnlCard = new Panel
            {
                Height = 64,
                Width = Math.Max(400, _flowFriends.ClientSize.Width - 40),
                BackColor = ColorCardBg,
                Margin = new Padding(0, 0, 0, 6),
                Cursor = Cursors.Default
            };

            // 1. 狀態指示色與字串
            Color statusColor = friend.Status switch
            {
                "Online" => ColorOnline,
                "Idle" => ColorIdle,
                _ => ColorOffline
            };

            string statusText = friend.Status switch
            {
                "Online" => "在線",
                "Idle" => "閒置",
                _ => "離線"
            };

            // 2. 圓形頭像區
            var pnlAvatar = new Panel
            {
                Size = new Size(42, 42),
                Location = new Point(14, 11),
                BackColor = ColorCardBg
            };
            pnlAvatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // 畫深色圓底
                using var bgBrush = new SolidBrush(ColorBlurple);
                e.Graphics.FillEllipse(bgBrush, 0, 0, 40, 40);

                // 畫名稱首字
                string initial = string.IsNullOrEmpty(friend.Username) ? "?" : friend.Username.Substring(0, 1).ToUpper();
                using var font = new Font("Segoe UI", 13F, FontStyle.Bold);
                using var textBrush = new SolidBrush(Color.White);
                var size = e.Graphics.MeasureString(initial, font);
                e.Graphics.DrawString(initial, font, textBrush, (40 - size.Width) / 2 + 1, (40 - size.Height) / 2 + 1);

                // 畫狀態圓點燈號 (右下角)
                using var dotBrush = new SolidBrush(statusColor);
                using var borderPen = new Pen(ColorCardBg, 2);
                e.Graphics.FillEllipse(dotBrush, 26, 26, 14, 14);
                e.Graphics.DrawEllipse(borderPen, 26, 26, 14, 14);
            };

            // 3. 好友名稱與 Tag
            var lblUsername = new Label
            {
                Text = friend.Username,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = TextWhite,
                Location = new Point(68, 12),
                AutoSize = true
            };

            var lblTag = new Label
            {
                Text = friend.UserTag,
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextMuted,
                Location = new Point(lblUsername.Right + 4, 14),
                AutoSize = true
            };

            // 4. 狀態與備註文字
            string noteDisplay = string.IsNullOrWhiteSpace(friend.Note) 
                ? statusText 
                : $"{statusText}  •  💬 {friend.Note}";

            var lblSubtitle = new Label
            {
                Text = noteDisplay,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = TextMuted,
                Location = new Point(68, 35),
                AutoSize = true
            };

            // 5. 右側快速操作：刪除按鈕 (🗑️)
            var btnDelete = new Button
            {
                Text = "🗑️",
                Size = new Size(36, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlCard.Width - 50, 14),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#232428"),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI Emoji", 11F),
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.MouseEnter += (s, e) => { btnDelete.BackColor = ColorTranslator.FromHtml("#da373c"); btnDelete.ForeColor = Color.White; };
            btnDelete.MouseLeave += (s, e) => { btnDelete.BackColor = ColorTranslator.FromHtml("#232428"); btnDelete.ForeColor = TextMuted; };
            btnDelete.Click += async (s, e) => await OnDeleteFriendAsync(friend);

            // 6. [U - Update & D - Delete] 右鍵選單 (ContextMenuStrip)
            var contextMenu = new ContextMenuStrip
            {
                BackColor = ColorTranslator.FromHtml("#111214"),
                ForeColor = TextWhite,
                ShowImageMargin = false
            };

            var mnuOnline = new ToolStripMenuItem("🟢  設為線上 (Online)", null, async (s, e) => await OnUpdateStatusAsync(friend.Id, "Online"));
            var mnuIdle = new ToolStripMenuItem("🟡  設為閒置 (Idle)", null, async (s, e) => await OnUpdateStatusAsync(friend.Id, "Idle"));
            var mnuOffline = new ToolStripMenuItem("⚫  設為離線 (Offline)", null, async (s, e) => await OnUpdateStatusAsync(friend.Id, "Offline"));
            var mnuDelete = new ToolStripMenuItem("🗑️  刪除好友 (Delete)", null, async (s, e) => await OnDeleteFriendAsync(friend));
            mnuDelete.ForeColor = ColorTranslator.FromHtml("#fa777c");

            contextMenu.Items.AddRange(new ToolStripItem[] {
                mnuOnline, mnuIdle, mnuOffline,
                new ToolStripSeparator(),
                mnuDelete
            });

            pnlCard.ContextMenuStrip = contextMenu;

            // 7. Hover 微亮回饋動畫感
            void ApplyHover(Control ctrl)
            {
                ctrl.MouseEnter += (s, e) => pnlCard.BackColor = ColorCardHover;
                ctrl.MouseLeave += (s, e) =>
                {
                    if (!pnlCard.ClientRectangle.Contains(pnlCard.PointToClient(Cursor.Position)))
                        pnlCard.BackColor = ColorCardBg;
                };
            }

            ApplyHover(pnlCard);
            ApplyHover(lblUsername);
            ApplyHover(lblTag);
            ApplyHover(lblSubtitle);
            ApplyHover(pnlAvatar);

            // 組裝進卡片 Panel
            pnlCard.Controls.AddRange(new Control[] {
                pnlAvatar, lblUsername, lblTag, lblSubtitle, btnDelete
            });

            return pnlCard;
        }

        private void AdjustCardWidths()
        {
            int targetWidth = Math.Max(400, _flowFriends.ClientSize.Width - 30);
            foreach (Control ctrl in _flowFriends.Controls)
            {
                if (ctrl is Panel pnl)
                {
                    pnl.Width = targetWidth;
                }
            }
        }

        #endregion

        #region 【CRUD 事件委派 - 嚴格呼叫 Repository】

        /// <summary>
        /// [C - Create] 新增好友事件
        /// </summary>
        private async Task OnAddFriendClickAsync()
        {
            using var dlg = new AddFriendForm();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultFriend != null)
            {
                try
                {
                    // 【職責分離】：呼叫 Repository 執行寫入，UI 保持純潔無瑕
                    await _repo.AddFriendAsync(dlg.ResultFriend);

                    // 即時重刷畫面
                    await RefreshFriendsListAsync();

                    MessageBox.Show($"成功新增好友「{dlg.ResultFriend.Username}{dlg.ResultFriend.UserTag}」！", 
                        "新增成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"新增好友失敗：\n{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// [U - Update] 更新好友在線狀態
        /// </summary>
        private async Task OnUpdateStatusAsync(int friendId, string newStatus)
        {
            try
            {
                // 【職責分離】：交給 Repository 執行 UPDATE 語法
                bool success = await _repo.UpdateStatusAsync(friendId, newStatus);
                if (success)
                {
                    await RefreshFriendsListAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新狀態失敗：\n{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// [D - Delete] 刪除好友
        /// </summary>
        private async Task OnDeleteFriendAsync(Friend friend)
        {
            var confirm = MessageBox.Show(
                $"確定要將「{friend.Username}{friend.UserTag}」自好友名單中移除嗎？\n移除後無法復原！",
                "刪除好友確認",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    // 【職責分離】：交給 Repository 執行 DELETE 語法
                    bool success = await _repo.DeleteFriendAsync(friend.Id);
                    if (success)
                    {
                        await RefreshFriendsListAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"刪除好友失敗：\n{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion
    }
}
