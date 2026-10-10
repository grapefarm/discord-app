using Microsoft.EntityFrameworkCore;
using discord_app.Models;

namespace discord_app.Data
{
    /// <summary>
    /// 【職責分離教學 - EF Core DbContext 資料庫上下文】
    /// 這是 Entity Framework Core 溝通資料庫的「橋樑」。
    /// 負責：
    /// 1. 指定 SQL Server 的連線字串。
    /// 2. 將 C# 的 Friend 物件映射到 SQL Server 的 Friends 資料表。
    /// </summary>
    public class DiscordDbContext : DbContext
    {
        /// <summary>
        /// 連線字串配置：預設指向本地 SQL Server 的 DiscordDemo 資料庫
        /// </summary>
        public const string ConnectionString = 
            "Server=localhost;Database=DiscordDemo;User Id=sa;Password=123456;TrustServerCertificate=True;";

        /// <summary>
        /// 好友資料集 (對應資料庫中的 Friends 資料表)
        /// </summary>
        public DbSet<Friend> Friends { get; set; } = null!;

        public DiscordDbContext()
        {
        }

        public DiscordDbContext(DbContextOptions<DiscordDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 明確指定資料表名稱與欄位長度規範
            modelBuilder.Entity<Friend>(entity =>
            {
                entity.ToTable("Friends");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.Property(e => e.UserTag).HasMaxLength(10).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Note).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });
        }
    }
}
