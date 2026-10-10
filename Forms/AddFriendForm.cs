using System;
using System.Drawing;
using System.Windows.Forms;
using discord_app.Models;

namespace discord_app.Forms
{
    /// <summary>
    /// 【職責分離教學 - 新增好友對話框】
    /// 這裡只負責「收集使用者的輸入資料」，包裝成一個 Friend 物件。
    /// 這裡依然不寫任何 SQL 或資料庫儲存邏輯，只負責回傳給呼叫端！
    /// </summary>
    public class AddFriendForm : Form
    {
        // Discord 深色配色
        private static readonly Color BgColor = ColorTranslator.FromHtml("#313338");
        private static readonly Color FooterBg = ColorTranslator.FromHtml("#2b2d31");
        private static readonly Color InputBg = ColorTranslator.FromHtml("#1e1f22");
        private static readonly Color TextPrimary = ColorTranslator.FromHtml("#f2f3f5");
        private static readonly Color TextMuted = ColorTranslator.FromHtml("#949ba4");
        private static readonly Color TextLabel = ColorTranslator.FromHtml("#b5bac1");
        private static readonly Color Blurple = ColorTranslator.FromHtml("#5865f2");
        private static readonly Color BlurpleHover = ColorTranslator.FromHtml("#4752c4");

        private TextBox _txtUsername = null!;
        private TextBox _txtTag = null!;
        private ComboBox _cboStatus = null!;
        private TextBox _txtNote = null!;

        /// <summary>
        /// 使用者建立完成的好友物件 (供外部 MainForm 讀取並存入資料庫)
        /// </summary>
        public Friend? ResultFriend { get; private set; }

        public AddFriendForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "新增好友";
            this.Size = new Size(460, 470);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = BgColor;
            this.ForeColor = TextPrimary;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            // 1. 標題與副標題
            var lblTitle = new Label
            {
                Text = "新增好友",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = TextPrimary,
                Location = new Point(24, 20),
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "你可以透過對方的 Discord 暱稱與標籤建立好友名單連結。",
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextMuted,
                Location = new Point(25, 48),
                Size = new Size(390, 20)
            };

            // 2. 表單輸入欄位
            int currentY = 80;

            // --- 暱稱 ---
            var lblUser = CreateFieldLabel("好友暱稱 (USERNAME)", new Point(24, currentY));
            currentY += 22;
            var pnlUser = CreateInputBox(new Point(24, currentY), out _txtUsername);
            currentY += 46;

            // --- 標籤 ---
            var lblTag = CreateFieldLabel("DISCORD 標籤 (例如 #0001)", new Point(24, currentY));
            currentY += 22;
            var pnlTag = CreateInputBox(new Point(24, currentY), out _txtTag);
            _txtTag.Text = "#";
            currentY += 46;

            // --- 在線狀態 ---
            var lblStatus = CreateFieldLabel("初始在線狀態 (STATUS)", new Point(24, currentY));
            currentY += 22;
            _cboStatus = new ComboBox
            {
                Location = new Point(24, currentY),
                Size = new Size(396, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = InputBg,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 10F)
            };
            _cboStatus.Items.AddRange(new object[] { "Online (線上)", "Idle (閒置)", "Offline (離線)" });
            _cboStatus.SelectedIndex = 0;
            currentY += 46;

            // --- 備註 ---
            var lblNote = CreateFieldLabel("備註 (NOTE - 選填)", new Point(24, currentY));
            currentY += 22;
            var pnlNote = CreateInputBox(new Point(24, currentY), out _txtNote);

            // 3. 底部按鈕條 (Footer)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 68,
                BackColor = FooterBg
            };

            var btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 38),
                Location = new Point(216, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#4e5058"),
                ForeColor = TextPrimary,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            var btnSubmit = new Button
            {
                Text = "發送好友請求",
                Size = new Size(120, 38),
                Location = new Point(302, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = Blurple,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.MouseEnter += (s, e) => btnSubmit.BackColor = BlurpleHover;
            btnSubmit.MouseLeave += (s, e) => btnSubmit.BackColor = Blurple;
            btnSubmit.Click += OnSubmitClick;

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnSubmit);

            // 加到表單
            this.Controls.AddRange(new Control[] {
                lblTitle, lblSubtitle,
                lblUser, pnlUser,
                lblTag, pnlTag,
                lblStatus, _cboStatus,
                lblNote, pnlNote,
                pnlFooter
            });
        }

        private Label CreateFieldLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = TextLabel,
                Location = location,
                AutoSize = true
            };
        }

        private Panel CreateInputBox(Point location, out TextBox textBox)
        {
            var panel = new Panel
            {
                Location = location,
                Size = new Size(396, 36),
                BackColor = InputBg,
                Padding = new Padding(8, 8, 8, 8)
            };

            textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = InputBg,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 10.5F)
            };

            panel.Controls.Add(textBox);
            return panel;
        }

        private void OnSubmitClick(object? sender, EventArgs e)
        {
            var username = _txtUsername.Text.Trim();
            var tag = _txtTag.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("請輸入好友暱稱！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(tag) || tag == "#")
            {
                tag = "#0001"; // 預設 Tag
            }
            else if (!tag.StartsWith("#"))
            {
                tag = "#" + tag;
            }

            string status = _cboStatus.SelectedIndex switch
            {
                1 => "Idle",
                2 => "Offline",
                _ => "Online"
            };

            // 【職責分離】：打包成 Model 物件，不自行寫入資料庫
            ResultFriend = new Friend
            {
                Username = username,
                UserTag = tag,
                Status = status,
                Note = string.IsNullOrWhiteSpace(_txtNote.Text) ? null : _txtNote.Text.Trim()
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
