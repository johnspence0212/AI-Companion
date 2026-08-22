using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EnterpriseStarter.Platform.Migrations;

[DbContext(typeof(EnterpriseDbContext))]
public sealed class EnterpriseDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.10")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);
        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity<IdentityRole>(entity =>
        {
            entity.Property<string>("Id").HasColumnType("text");
            entity.Property<string>("ConcurrencyStamp").IsConcurrencyToken().HasColumnType("text");
            entity.Property<string>("Name").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("NormalizedName").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.HasKey("Id");
            entity.HasIndex("NormalizedName").IsUnique().HasDatabaseName("RoleNameIndex");
            entity.ToTable("AspNetRoles");
        });

        modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            entity.Property<string>("ClaimType").HasColumnType("text");
            entity.Property<string>("ClaimValue").HasColumnType("text");
            entity.Property<string>("RoleId").IsRequired().HasColumnType("text");
            entity.HasKey("Id");
            entity.HasIndex("RoleId");
            entity.ToTable("AspNetRoleClaims");
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property<string>("Id").HasColumnType("text");
            entity.Property<int>("AccessFailedCount").HasColumnType("integer");
            entity.Property<string>("ConcurrencyStamp").IsConcurrencyToken().HasColumnType("text");
            entity.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            entity.Property<DateTime?>("DeactivatedAt").HasColumnType("timestamp with time zone");
            entity.Property<string>("DisplayName").HasMaxLength(200).HasColumnType("character varying(200)");
            entity.Property<string>("Email").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<bool>("EmailConfirmed").HasColumnType("boolean");
            entity.Property<bool>("IsActive").HasColumnType("boolean");
            entity.Property<DateTime?>("LastLoginAt").HasColumnType("timestamp with time zone");
            entity.Property<bool>("LockoutEnabled").HasColumnType("boolean");
            entity.Property<DateTimeOffset?>("LockoutEnd").HasColumnType("timestamp with time zone");
            entity.Property<bool>("MustChangePassword").HasColumnType("boolean");
            entity.Property<string>("NormalizedEmail").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("NormalizedUserName").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<DateTime?>("PasswordChangedAt").HasColumnType("timestamp with time zone");
            entity.Property<string>("PasswordHash").HasColumnType("text");
            entity.Property<string>("PhoneNumber").HasColumnType("text");
            entity.Property<bool>("PhoneNumberConfirmed").HasColumnType("boolean");
            entity.Property<string>("SecurityStamp").HasColumnType("text");
            entity.Property<bool>("TwoFactorEnabled").HasColumnType("boolean");
            entity.Property<DateTime>("UpdatedAt").HasColumnType("timestamp with time zone");
            entity.Property<string>("UserName").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.HasKey("Id");
            entity.HasIndex("NormalizedEmail").HasDatabaseName("EmailIndex");
            entity.HasIndex("IsActive");
            entity.HasIndex("NormalizedUserName").IsUnique().HasDatabaseName("UserNameIndex");
            entity.ToTable("AspNetUsers");
        });

        modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            entity.Property<string>("ClaimType").HasColumnType("text");
            entity.Property<string>("ClaimValue").HasColumnType("text");
            entity.Property<string>("UserId").IsRequired().HasColumnType("text");
            entity.HasKey("Id");
            entity.HasIndex("UserId");
            entity.ToTable("AspNetUserClaims");
        });

        modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.Property<string>("LoginProvider").HasColumnType("text");
            entity.Property<string>("ProviderKey").HasColumnType("text");
            entity.Property<string>("ProviderDisplayName").HasColumnType("text");
            entity.Property<string>("UserId").IsRequired().HasColumnType("text");
            entity.HasKey("LoginProvider", "ProviderKey");
            entity.HasIndex("UserId");
            entity.ToTable("AspNetUserLogins");
        });

        modelBuilder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.Property<string>("UserId").HasColumnType("text");
            entity.Property<string>("RoleId").HasColumnType("text");
            entity.HasKey("UserId", "RoleId");
            entity.HasIndex("RoleId");
            entity.ToTable("AspNetUserRoles");
        });

        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property<string>("UserId").HasColumnType("text");
            entity.Property<string>("LoginProvider").HasColumnType("text");
            entity.Property<string>("Name").HasColumnType("text");
            entity.Property<string>("Value").HasColumnType("text");
            entity.HasKey("UserId", "LoginProvider", "Name");
            entity.ToTable("AspNetUserTokens");
        });

        modelBuilder.Entity<SecurityAuditEvent>(entity =>
        {
            entity.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("bigint")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            entity.Property<string>("ActorEmail").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("ActorUserId").HasMaxLength(450).HasColumnType("character varying(450)");
            entity.Property<string>("Details").HasMaxLength(4000).HasColumnType("character varying(4000)");
            entity.Property<string>("EventType").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<string>("IpAddress").HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<DateTime>("OccurredAt").HasColumnType("timestamp with time zone");
            entity.Property<string>("Outcome").IsRequired().HasMaxLength(40).HasColumnType("character varying(40)");
            entity.Property<string>("SubjectId").HasMaxLength(450).HasColumnType("character varying(450)");
            entity.Property<string>("UserAgent").HasMaxLength(512).HasColumnType("character varying(512)");
            entity.HasKey("Id");
            entity.HasIndex("ActorUserId");
            entity.HasIndex("EventType");
            entity.HasIndex("OccurredAt");
            entity.ToTable("SecurityAuditEvents");
        });

        modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
            entity.HasOne<IdentityRole>().WithMany().HasForeignKey("RoleId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.HasOne<IdentityRole>().WithMany().HasForeignKey("RoleId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });
        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired());
    }
}
