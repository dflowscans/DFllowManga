#nullable disable
using System;
using MangaReader.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace MangaReader.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20251229140324_AddUserTitles")]
public class AddUserTitles : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<int>("EquippedTitleId", "Users", "int", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
		migrationBuilder.CreateTable("UserTitles", delegate(ColumnsBuilder table)
		{
			OperationBuilder<AddColumnOperation> id = table.Column<int>("int", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null).Annotation("MySql:ValueGenerationStrategy", (object)(MySqlValueGenerationStrategy)1);
			int? num = 100;
			OperationBuilder<AddColumnOperation> name = table.Column<string>("varchar(100)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null).Annotation("MySql:CharSet", (object)"utf8mb4");
			num = 20;
			return new
			{
				Id = id,
				Name = name,
				Color = table.Column<string>("varchar(20)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null).Annotation("MySql:CharSet", (object)"utf8mb4"),
				LevelRequirement = table.Column<int>("int", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				CreatedAt = table.Column<DateTime>("datetime(6)", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null)
			};
		}, (string)null, table =>
		{
			table.PrimaryKey("PK_UserTitles", x => (object)x.Id);
		}, (string)null).Annotation("MySql:CharSet", (object)"utf8mb4");
		migrationBuilder.InsertData("UserTitles", new string[5] { "Id", "Color", "CreatedAt", "LevelRequirement", "Name" }, new object[3, 5]
		{
			{
				1,
				"#94a3b8",
				new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
				1,
				"Novice Reader"
			},
			{
				2,
				"#3b82f6",
				new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
				5,
				"Manga Enthusiast"
			},
			{
				3,
				"#f59e0b",
				new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
				10,
				"Legendary Scholar"
			}
		}, (string)null);
		migrationBuilder.UpdateData("Users", "Id", (object)1, "EquippedTitleId", (object)null, (string)null);
		migrationBuilder.CreateIndex("IX_Users_EquippedTitleId", "Users", "EquippedTitleId", (string)null, false, (string)null, (bool[])null);
		migrationBuilder.AddForeignKey("FK_Users_UserTitles_EquippedTitleId", "Users", "EquippedTitleId", "UserTitles", (string)null, (string)null, "Id", (ReferentialAction)0, (ReferentialAction)0);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropForeignKey("FK_Users_UserTitles_EquippedTitleId", "Users", (string)null);
		migrationBuilder.DropTable("UserTitles", (string)null);
		migrationBuilder.DropIndex("IX_Users_EquippedTitleId", "Users", (string)null);
		migrationBuilder.DropColumn("EquippedTitleId", "Users", (string)null);
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
		modelBuilder.Entity("MangaReader.Models.PfpDecoration", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("ImageUrl").IsRequired(true).HasMaxLength(500), "varchar(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsAnimated"), "tinyint(1)");
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
					LevelRequirement = 1,
					Name = "Glow Ring"
				},
				new
				{
					Id = 2,
					CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
					ImageUrl = "https://media.giphy.com/media/v1.Y2lkPTc5MGI3NjExNHJqZ3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4JmVwPXYxX2ludGVybmFsX2dpZl9ieV9pZCZjdD1z/26hpKz786Cq0Y/giphy.gif",
					IsAnimated = true,
					LevelRequirement = 5,
					Name = "Golden Sparkle"
				}
			});
		});
		modelBuilder.Entity("MangaReader.Models.User", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Id").ValueGeneratedOnAdd(), "int");
			MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn<int>(b.Property<int>("Id"));
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("AvatarUrl").HasMaxLength(500), "varchar(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "datetime(6)");
			RelationalPropertyBuilderExtensions.HasColumnType<int?>(b.Property<int?>("EquippedDecorationId"), "int");
			RelationalPropertyBuilderExtensions.HasColumnType<int?>(b.Property<int?>("EquippedTitleId"), "int");
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
					LevelRequirement = 1,
					Name = "Novice Reader"
				},
				new
				{
					Id = 2,
					Color = "#3b82f6",
					CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
					LevelRequirement = 5,
					Name = "Manga Enthusiast"
				},
				new
				{
					Id = 3,
					Color = "#f59e0b",
					CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc),
					LevelRequirement = 10,
					Name = "Legendary Scholar"
				}
			});
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
		modelBuilder.Entity("MangaReader.Models.Chapter", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.Navigation("Comments");
			b.Navigation("Pages");
		});
		modelBuilder.Entity("MangaReader.Models.ChapterComment", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.Navigation("Replies");
		});
		modelBuilder.Entity("MangaReader.Models.Manga", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.Navigation("Chapters");
		});
		modelBuilder.Entity("MangaReader.Models.User", (Action<EntityTypeBuilder>)delegate(EntityTypeBuilder b)
		{
			b.Navigation("Bookmarks");
			b.Navigation("Comments");
		});
	}
}
