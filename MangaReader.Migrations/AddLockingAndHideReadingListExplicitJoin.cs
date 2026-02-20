#nullable disable
using System;
using MangaReader.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace MangaReader.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20251229163701_AddLockingAndHideReadingListExplicitJoin")]
public class AddLockingAndHideReadingListExplicitJoin : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<bool>("IsLocked", "UserTitles", "tinyint(1)", (bool?)null, (int?)null, false, (string)null, false, (object)false, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
		migrationBuilder.AddColumn<bool>("HideReadingList", "Users", "tinyint(1)", (bool?)null, (int?)null, false, (string)null, false, (object)false, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
		migrationBuilder.AddColumn<bool>("IsLocked", "PfpDecorations", "tinyint(1)", (bool?)null, (int?)null, false, (string)null, false, (object)false, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
		migrationBuilder.CreateTable("UserUnlockedDecoration", (ColumnsBuilder table) => new
		{
			Id = table.Column<int>("int", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null).Annotation("MySql:ValueGenerationStrategy", (object)(MySqlValueGenerationStrategy)1),
			UserId = table.Column<int>("int", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
			DecorationId = table.Column<int>("int", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
			UnlockedAt = table.Column<DateTime>("datetime(6)", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null)
		}, (string)null, table =>
		{
			table.PrimaryKey("PK_UserUnlockedDecoration", x => (object)x.Id);
			table.ForeignKey("FK_UserUnlockedDecoration_PfpDecorations_DecorationId", x => (object)x.DecorationId, "PfpDecorations", "Id", (string)null, (ReferentialAction)0, (ReferentialAction)2);
			table.ForeignKey("FK_UserUnlockedDecoration_Users_UserId", x => (object)x.UserId, "Users", "Id", (string)null, (ReferentialAction)0, (ReferentialAction)2);
		}, (string)null).Annotation("MySql:CharSet", (object)"utf8mb4");
		migrationBuilder.CreateTable("UserUnlockedTitle", (ColumnsBuilder table) => new
		{
			Id = table.Column<int>("int", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null).Annotation("MySql:ValueGenerationStrategy", (object)(MySqlValueGenerationStrategy)1),
			UserId = table.Column<int>("int", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
			TitleId = table.Column<int>("int", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
			UnlockedAt = table.Column<DateTime>("datetime(6)", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null)
		}, (string)null, table =>
		{
			table.PrimaryKey("PK_UserUnlockedTitle", x => (object)x.Id);
			table.ForeignKey("FK_UserUnlockedTitle_UserTitles_TitleId", x => (object)x.TitleId, "UserTitles", "Id", (string)null, (ReferentialAction)0, (ReferentialAction)2);
			table.ForeignKey("FK_UserUnlockedTitle_Users_UserId", x => (object)x.UserId, "Users", "Id", (string)null, (ReferentialAction)0, (ReferentialAction)2);
		}, (string)null).Annotation("MySql:CharSet", (object)"utf8mb4");
		migrationBuilder.UpdateData("PfpDecorations", "Id", (object)1, "IsLocked", (object)false, (string)null);
		migrationBuilder.UpdateData("PfpDecorations", "Id", (object)2, "IsLocked", (object)false, (string)null);
		migrationBuilder.UpdateData("UserTitles", "Id", (object)1, "IsLocked", (object)false, (string)null);
		migrationBuilder.UpdateData("UserTitles", "Id", (object)2, "IsLocked", (object)false, (string)null);
		migrationBuilder.UpdateData("UserTitles", "Id", (object)3, "IsLocked", (object)false, (string)null);
		migrationBuilder.UpdateData("Users", "Id", (object)1, "HideReadingList", (object)false, (string)null);
		migrationBuilder.CreateIndex("IX_UserUnlockedDecoration_DecorationId", "UserUnlockedDecoration", "DecorationId", (string)null, false, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_UserUnlockedDecoration_UserId", "UserUnlockedDecoration", "UserId", (string)null, false, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_UserUnlockedTitle_TitleId", "UserUnlockedTitle", "TitleId", (string)null, false, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_UserUnlockedTitle_UserId", "UserUnlockedTitle", "UserId", (string)null, false, (string)null, (bool[])null);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable("UserUnlockedDecoration", (string)null);
		migrationBuilder.DropTable("UserUnlockedTitle", (string)null);
		migrationBuilder.DropColumn("IsLocked", "UserTitles", (string)null);
		migrationBuilder.DropColumn("HideReadingList", "Users", (string)null);
		migrationBuilder.DropColumn("IsLocked", "PfpDecorations", (string)null);
	}

	protected override void BuildTargetModel(ModelBuilder modelBuilder)
	{
		modelBuilder.HasAnnotation("ProductVersion", (object)"9.0.0").HasAnnotation("Relational:MaxIdentifierLength", (object)64);
		MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);
		modelBuilder.Entity("MangaReader.Models.Chapter", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<decimal>(b.Property<decimal>("ChapterNumber"), "decimal(18,2)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("CoverImageUrl").HasMaxLength(500), "varchar(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Description").HasMaxLength(1000), "varchar(1000)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("MangaId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("ReleasedDate"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Title").IsRequired(true).HasMaxLength(300), "varchar(300)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("UpdatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("ViewCount"), "int");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "MangaId" });
			b.HasIndex(new string[1] { "ReleasedDate" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "Chapters");
		});
		modelBuilder.Entity("MangaReader.Models.ChapterComment", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("ChapterId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Content").IsRequired(true).HasMaxLength(1000), "varchar(1000)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int?>(b.Property<int?>("ParentCommentId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("UserId"), "int");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "ChapterId" });
			b.HasIndex(new string[1] { "ParentCommentId" });
			b.HasIndex(new string[1] { "UserId" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "ChapterComments");
		});
		modelBuilder.Entity("MangaReader.Models.ChapterPage", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("ChapterId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Height"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("ImageUrl").IsRequired(true).HasMaxLength(500), "varchar(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("PageNumber"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Width"), "int");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "ChapterId" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "ChapterPages");
		});
		modelBuilder.Entity("MangaReader.Models.ChapterView", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("ChapterId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("UserId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("ViewedAt"), "datetime(6)");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "ChapterId" });
			b.HasIndex(new string[2] { "UserId", "ChapterId" }).IsUnique(true);
			RelationalEntityTypeBuilderExtensions.ToTable(b, "ChapterViews");
		});
		modelBuilder.Entity("MangaReader.Models.CommentReaction", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("CommentId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsLike"), "tinyint(1)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("UserId"), "int");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "UserId" });
			b.HasIndex(new string[2] { "CommentId", "UserId" }).IsUnique(true);
			RelationalEntityTypeBuilderExtensions.ToTable(b, "CommentReactions");
		});
		modelBuilder.Entity("MangaReader.Models.Manga", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Artist").IsRequired(true).HasMaxLength(200), "varchar(200)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Author").IsRequired(true).HasMaxLength(200), "varchar(200)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("BannerPositionX"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("BannerPositionY"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("BannerUrl").IsRequired(true).HasMaxLength(500), "varchar(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Description").IsRequired(true).HasMaxLength(1000), "varchar(1000)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Genre").IsRequired(true).HasMaxLength(500), "varchar(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("HasTitleShadow"), "tinyint(1)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("ImageUrl").IsRequired(true).HasMaxLength(500), "varchar(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsFeatured"), "tinyint(1)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime?>(b.Property<DateTime?>("LastChapterDate"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int?>(b.Property<int?>("Rating"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Status").IsRequired(true).HasMaxLength(500), "varchar(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Title").IsRequired(true).HasMaxLength(200), "varchar(200)");
			RelationalPropertyBuilderExtensions.HasColumnType<double>(b.Property<double>("TitleShadowOpacity"), "double");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("TitleShadowSize"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Type").HasMaxLength(100), "varchar(100)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("UpdatedAt"), "datetime(6)");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "IsFeatured" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "Mangas");
		});
		modelBuilder.Entity("MangaReader.Models.Notification", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsRead"), "tinyint(1)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Message").IsRequired(true).HasMaxLength(500), "varchar(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<int?>(b.Property<int?>("RelatedChapterId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int?>(b.Property<int?>("RelatedCommentId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int?>(b.Property<int?>("RelatedMangaId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int?>(b.Property<int?>("TriggerUserId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Type"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("UserId"), "int");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "RelatedChapterId" });
			b.HasIndex(new string[1] { "RelatedCommentId" });
			b.HasIndex(new string[1] { "RelatedMangaId" });
			b.HasIndex(new string[1] { "TriggerUserId" });
			b.HasIndex(new string[1] { "UserId" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "Notifications");
		});
		modelBuilder.Entity("MangaReader.Models.PfpDecoration", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("ImageUrl").IsRequired(true).HasMaxLength(500), "varchar(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsAnimated"), "tinyint(1)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsLocked"), "tinyint(1)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("LevelRequirement"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Name").IsRequired(true).HasMaxLength(100), "varchar(100)");
			b.HasKey(new string[1] { "Id" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "PfpDecorations");
			b.HasData(new object[2]
			{
				new
				{
					Id = 1,
					CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
					ImageUrl = "https://media.giphy.com/media/v1.Y2lkPTc5MGI3NjExNHJqZ3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4JmVwPXYxX2ludGVybmFsX2dpZl9ieV9pZCZjdD1z/3o7TKMGV9mGfVf8kEw/giphy.gif",
					IsAnimated = true,
					IsLocked = false,
					LevelRequirement = 1,
					Name = "Glow Ring"
				},
				new
				{
					Id = 2,
					CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
					ImageUrl = "https://media.giphy.com/media/v1.Y2lkPTc5MGI3NjExNHJqZ3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4JmVwPXYxX2ludGVybmFsX2dpZl9ieV9pZCZjdD1z/26hpKz786Cq0Y/giphy.gif",
					IsAnimated = true,
					IsLocked = false,
					LevelRequirement = 5,
					Name = "Golden Sparkle"
				}
			});
		});
		modelBuilder.Entity("MangaReader.Models.SiteSetting", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Key"), "varchar(255)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Value").IsRequired(true), "longtext");
			b.HasKey(new string[1] { "Key" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "SiteSettings");
		});
		modelBuilder.Entity("MangaReader.Models.User", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("AvatarUrl").HasMaxLength(500), "varchar(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int?>(b.Property<int?>("EquippedDecorationId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int?>(b.Property<int?>("EquippedTitleId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("HideReadingList"), "tinyint(1)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsActive"), "tinyint(1)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsAdmin"), "tinyint(1)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsSubAdmin"), "tinyint(1)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Level"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("PasswordHash").IsRequired(true).HasMaxLength(255), "varchar(255)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("UpdatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Username").IsRequired(true).HasMaxLength(100), "varchar(100)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("XP"), "int");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "EquippedDecorationId" });
			b.HasIndex(new string[1] { "EquippedTitleId" });
			b.HasIndex(new string[1] { "Username" }).IsUnique(true);
			RelationalEntityTypeBuilderExtensions.ToTable(b, "Users");
			b.HasData(new object[1]
			{
				new
				{
					Id = 1,
					CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
					HideReadingList = false,
					IsActive = true,
					IsAdmin = true,
					IsSubAdmin = false,
					Level = 1,
					PasswordHash = "$2a$11$yNru262Z6gimpMKoGk0MpOYyn4jijDcLpHruW1.VtclJAsIAwg2mq",
					UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
					Username = "Admin",
					XP = 0
				}
			});
		});
		modelBuilder.Entity("MangaReader.Models.UserBookmark", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("AddedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("MangaId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Status"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("UpdatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("UserId"), "int");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "MangaId" });
			b.HasIndex(new string[1] { "UserId" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "UserBookmarks");
		});
		modelBuilder.Entity("MangaReader.Models.UserRating", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("MangaId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Rating"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("UpdatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("UserId"), "int");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "MangaId" });
			b.HasIndex(new string[2] { "UserId", "MangaId" }).IsUnique(true);
			RelationalEntityTypeBuilderExtensions.ToTable(b, "UserRatings");
		});
		modelBuilder.Entity("MangaReader.Models.UserTitle", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Color").IsRequired(true).HasMaxLength(20), "varchar(20)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsLocked"), "tinyint(1)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("LevelRequirement"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Name").IsRequired(true).HasMaxLength(100), "varchar(100)");
			b.HasKey(new string[1] { "Id" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "UserTitles");
			b.HasData(new object[3]
			{
				new
				{
					Id = 1,
					Color = "#94a3b8",
					CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
					IsLocked = false,
					LevelRequirement = 1,
					Name = "Novice Reader"
				},
				new
				{
					Id = 2,
					Color = "#3b82f6",
					CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
					IsLocked = false,
					LevelRequirement = 5,
					Name = "Manga Enthusiast"
				},
				new
				{
					Id = 3,
					Color = "#f59e0b",
					CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
					IsLocked = false,
					LevelRequirement = 10,
					Name = "Legendary Scholar"
				}
			});
		});
		modelBuilder.Entity("MangaReader.Models.UserUnlockedDecoration", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("DecorationId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("UnlockedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("UserId"), "int");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "DecorationId" });
			b.HasIndex(new string[1] { "UserId" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "UserUnlockedDecoration");
		});
		modelBuilder.Entity("MangaReader.Models.UserUnlockedTitle", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("TitleId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("UnlockedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("UserId"), "int");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "TitleId" });
			b.HasIndex(new string[1] { "UserId" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "UserUnlockedTitle");
		});
		modelBuilder.Entity("MangaReader.Models.Chapter", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.HasOne("MangaReader.Models.Manga", "Manga").WithMany("Chapters").HasForeignKey(new string[1] { "MangaId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Manga");
		});
		modelBuilder.Entity("MangaReader.Models.ChapterComment", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.HasOne("MangaReader.Models.Chapter", "Chapter").WithMany("Comments").HasForeignKey(new string[1] { "ChapterId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.HasOne("MangaReader.Models.ChapterComment", "ParentComment").WithMany("Replies").HasForeignKey(new string[1] { "ParentCommentId" });
			b.HasOne("MangaReader.Models.User", "User").WithMany("Comments").HasForeignKey(new string[1] { "UserId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Chapter");
			b.Navigation("ParentComment");
			b.Navigation("User");
		});
		modelBuilder.Entity("MangaReader.Models.ChapterPage", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.HasOne("MangaReader.Models.Chapter", "Chapter").WithMany("Pages").HasForeignKey(new string[1] { "ChapterId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Chapter");
		});
		modelBuilder.Entity("MangaReader.Models.ChapterView", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.HasOne("MangaReader.Models.Chapter", "Chapter").WithMany((string)null).HasForeignKey(new string[1] { "ChapterId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.HasOne("MangaReader.Models.User", "User").WithMany((string)null).HasForeignKey(new string[1] { "UserId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Chapter");
			b.Navigation("User");
		});
		modelBuilder.Entity("MangaReader.Models.CommentReaction", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.HasOne("MangaReader.Models.ChapterComment", "Comment").WithMany("Reactions").HasForeignKey(new string[1] { "CommentId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.HasOne("MangaReader.Models.User", "User").WithMany("CommentReactions").HasForeignKey(new string[1] { "UserId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Comment");
			b.Navigation("User");
		});
		modelBuilder.Entity("MangaReader.Models.Notification", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.HasOne("MangaReader.Models.Chapter", "RelatedChapter").WithMany((string)null).HasForeignKey(new string[1] { "RelatedChapterId" });
			b.HasOne("MangaReader.Models.ChapterComment", "RelatedComment").WithMany((string)null).HasForeignKey(new string[1] { "RelatedCommentId" });
			b.HasOne("MangaReader.Models.Manga", "RelatedManga").WithMany((string)null).HasForeignKey(new string[1] { "RelatedMangaId" });
			b.HasOne("MangaReader.Models.User", "TriggerUser").WithMany((string)null).HasForeignKey(new string[1] { "TriggerUserId" })
				.OnDelete((DeleteBehavior)2);
			b.HasOne("MangaReader.Models.User", "User").WithMany("Notifications").HasForeignKey(new string[1] { "UserId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("RelatedChapter");
			b.Navigation("RelatedComment");
			b.Navigation("RelatedManga");
			b.Navigation("TriggerUser");
			b.Navigation("User");
		});
		modelBuilder.Entity("MangaReader.Models.User", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.HasOne("MangaReader.Models.PfpDecoration", "EquippedDecoration").WithMany((string)null).HasForeignKey(new string[1] { "EquippedDecorationId" });
			b.HasOne("MangaReader.Models.UserTitle", "EquippedTitle").WithMany((string)null).HasForeignKey(new string[1] { "EquippedTitleId" });
			b.Navigation("EquippedDecoration");
			b.Navigation("EquippedTitle");
		});
		modelBuilder.Entity("MangaReader.Models.UserBookmark", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.HasOne("MangaReader.Models.Manga", "Manga").WithMany((string)null).HasForeignKey(new string[1] { "MangaId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.HasOne("MangaReader.Models.User", "User").WithMany("Bookmarks").HasForeignKey(new string[1] { "UserId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Manga");
			b.Navigation("User");
		});
		modelBuilder.Entity("MangaReader.Models.UserRating", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.HasOne("MangaReader.Models.Manga", "Manga").WithMany((string)null).HasForeignKey(new string[1] { "MangaId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.HasOne("MangaReader.Models.User", "User").WithMany((string)null).HasForeignKey(new string[1] { "UserId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Manga");
			b.Navigation("User");
		});
		modelBuilder.Entity("MangaReader.Models.UserUnlockedDecoration", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.HasOne("MangaReader.Models.PfpDecoration", "Decoration").WithMany((string)null).HasForeignKey(new string[1] { "DecorationId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.HasOne("MangaReader.Models.User", "User").WithMany("UnlockedDecorations").HasForeignKey(new string[1] { "UserId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Decoration");
			b.Navigation("User");
		});
		modelBuilder.Entity("MangaReader.Models.UserUnlockedTitle", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.HasOne("MangaReader.Models.UserTitle", "Title").WithMany((string)null).HasForeignKey(new string[1] { "TitleId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.HasOne("MangaReader.Models.User", "User").WithMany("UnlockedTitles").HasForeignKey(new string[1] { "UserId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Title");
			b.Navigation("User");
		});
		modelBuilder.Entity("MangaReader.Models.Chapter", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.Navigation("Comments");
			b.Navigation("Pages");
		});
		modelBuilder.Entity("MangaReader.Models.ChapterComment", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.Navigation("Reactions");
			b.Navigation("Replies");
		});
		modelBuilder.Entity("MangaReader.Models.Manga", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.Navigation("Chapters");
		});
		modelBuilder.Entity("MangaReader.Models.User", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.Navigation("Bookmarks");
			b.Navigation("CommentReactions");
			b.Navigation("Comments");
			b.Navigation("Notifications");
			b.Navigation("UnlockedDecorations");
			b.Navigation("UnlockedTitles");
		});
	}
}
