using System;

namespace discord_app.Models
{
    /// <summary>
    /// 【職責分離教學 - Model 實體模型層】
    /// 這裡只負責定義「好友在記憶體中的資料長相」，就像一張空白的名冊表格。
    /// 絕對不要在這裡寫任何「如何連線資料庫」或「如何畫到視窗上」的邏輯！
    /// </summary>
    public class Friend
    {
        /// <summary>
        /// 好友唯一識別編號 (對應資料庫的自動累加主鍵 ID)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 好友名稱 (例如: "CodeNinja", "Alex")
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Discord 標籤 (例如: "#0001", "#9999")
        /// </summary>
        public string UserTag { get; set; } = string.Empty;

        /// <summary>
        /// 在線狀態 ('Online' 線上, 'Idle' 閒置, 'Offline' 離線)
        /// </summary>
        public string Status { get; set; } = "Online";

        /// <summary>
        /// 自訂備註 (例如: "培訓班同學", "打遊戲認識的"，可為空)
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// 建立好友的時間戳記
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
