using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using discord_app.Models;

namespace discord_app.Data
{
    /// <summary>
    /// 【職責分離教學 - Repository 資料存取層】
    /// 核心精神：
    /// 1. 這裡只專門負責「跟資料庫做生意」（CRUD：增、查、改、刪）。
    /// 2. 這裡絕對不碰任何 UI 控制項（完全沒有 Label, Button, Form 等畫面元件）。
    /// 3. 畫面上按鈕點擊後，只會呼叫這裡的方法，把髒活（SQL / EF Core）留在這裡處理。
    /// </summary>
    public class FriendRepository
    {
        /// <summary>
        /// [自動檢查與播種假資料]
        /// 如果資料表是空的，自動塞入 3 筆 Discord 經典假好友資料
        /// </summary>
        public async Task EnsureSeedDataAsync()
        {
            using var context = new DiscordDbContext();
            // 確保資料庫與資料表存在
            await context.Database.EnsureCreatedAsync();

            if (!await context.Friends.AnyAsync())
            {
                var seedFriends = new List<Friend>
                {
                    new Friend { Username = "CodeNinja", UserTag = "#0001", Status = "Online", Note = "培訓班超強同學" },
                    new Friend { Username = "GamerBoy",  UserTag = "#7777", Status = "Idle",   Note = "打遊戲認識的" },
                    new Friend { Username = "GhostUser", UserTag = "#9999", Status = "Offline", Note = null }
                };

                await context.Friends.AddRangeAsync(seedFriends);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// [R - Read 查詢全部好友]
        /// 按照 ID 降序或建立時間排序
        /// </summary>
        public async Task<List<Friend>> GetAllFriendsAsync()
        {
            using var context = new DiscordDbContext();
            return await context.Friends
                .AsNoTracking()
                .OrderByDescending(f => f.Id)
                .ToListAsync();
        }

        /// <summary>
        /// [R - Read 查詢與篩選好友]
        /// 支援以關鍵字（暱稱、標籤或備註）與在線狀態同時篩選
        /// </summary>
        /// <param name="keyword">搜尋文字 (空字串表示不限)</param>
        /// <param name="onlineOnly">是否只看在線好友 (Online 或 Idle)</param>
        public async Task<List<Friend>> SearchFriendsAsync(string? keyword, bool onlineOnly = false)
        {
            using var context = new DiscordDbContext();
            IQueryable<Friend> query = context.Friends.AsNoTracking();

            // 狀態過濾：線上分頁只顯示 Online 與 Idle
            if (onlineOnly)
            {
                query = query.Where(f => f.Status == "Online" || f.Status == "Idle");
            }

            // 關鍵字搜尋：比對暱稱、標籤或備註
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmed = keyword.Trim();
                query = query.Where(f => 
                    f.Username.Contains(trimmed) || 
                    f.UserTag.Contains(trimmed) || 
                    (f.Note != null && f.Note.Contains(trimmed)));
            }

            return await query.OrderByDescending(f => f.Id).ToListAsync();
        }

        /// <summary>
        /// [C - Create 新增好友]
        /// </summary>
        public async Task<Friend> AddFriendAsync(Friend friend)
        {
            if (friend == null) throw new ArgumentNullException(nameof(friend));

            using var context = new DiscordDbContext();
            friend.CreatedAt = DateTime.Now;
            context.Friends.Add(friend);
            await context.SaveChangesAsync();
            return friend;
        }

        /// <summary>
        /// [U - Update 更新好友狀態]
        /// 例如由 Online 切換為 Idle 或 Offline
        /// </summary>
        public async Task<bool> UpdateStatusAsync(int id, string newStatus)
        {
            using var context = new DiscordDbContext();
            var friend = await context.Friends.FindAsync(id);
            if (friend == null) return false;

            friend.Status = newStatus;
            await context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// [D - Delete 刪除好友]
        /// </summary>
        public async Task<bool> DeleteFriendAsync(int id)
        {
            using var context = new DiscordDbContext();
            var friend = await context.Friends.FindAsync(id);
            if (friend == null) return false;

            context.Friends.Remove(friend);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
