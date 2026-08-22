using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseStarter.Platform.Migrations
{
    /// <inheritdoc />
    public partial class CompanionDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanionAiClients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SecretHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionAiClients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanionDocumentTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TitlePattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionDocumentTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanionFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanionFolders_CompanionFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "CompanionFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanionTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanionActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ActorAiClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecordType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanionDailyItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionDailyItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanionDocumentProjects",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionDocumentProjects", x => new { x.DocumentId, x.ProjectId });
                });

            migrationBuilder.CreateTable(
                name: "CompanionDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsProjectContext = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanionDocuments_CompanionFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "CompanionFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CompanionDocumentTags",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionDocumentTags", x => new { x.DocumentId, x.TagId });
                    table.ForeignKey(
                        name: "FK_CompanionDocumentTags_CompanionDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "CompanionDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanionDocumentTags_CompanionTags_TagId",
                        column: x => x.TagId,
                        principalTable: "CompanionTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanionProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContextDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanionProjects_CompanionDocuments_ContextDocumentId",
                        column: x => x.ContextDocumentId,
                        principalTable: "CompanionDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanionRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ActorAiClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanionRevisions_CompanionDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "CompanionDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanionIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentIssueId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    AssigneeUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AssigneeAiClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BlockedReason = table.Column<string>(type: "text", nullable: true),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanionIssues_CompanionIssues_ParentIssueId",
                        column: x => x.ParentIssueId,
                        principalTable: "CompanionIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanionIssues_CompanionProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "CompanionProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanionSavedViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    ColumnsJson = table.Column<string>(type: "text", nullable: false),
                    FiltersJson = table.Column<string>(type: "text", nullable: false),
                    SortJson = table.Column<string>(type: "text", nullable: false),
                    GroupBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionSavedViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanionSavedViews_CompanionProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "CompanionProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanionSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ActorAiClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanionSessions_CompanionProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "CompanionProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanionInboxItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionInboxItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanionInboxItems_CompanionDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "CompanionDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompanionInboxItems_CompanionIssues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "CompanionIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CompanionIssueBlockers",
                columns: table => new
                {
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockerIssueId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionIssueBlockers", x => new { x.IssueId, x.BlockerIssueId });
                    table.ForeignKey(
                        name: "FK_CompanionIssueBlockers_CompanionIssues_BlockerIssueId",
                        column: x => x.BlockerIssueId,
                        principalTable: "CompanionIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanionIssueBlockers_CompanionIssues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "CompanionIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanionIssueTags",
                columns: table => new
                {
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionIssueTags", x => new { x.IssueId, x.TagId });
                    table.ForeignKey(
                        name: "FK_CompanionIssueTags_CompanionIssues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "CompanionIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanionIssueTags_CompanionTags_TagId",
                        column: x => x.TagId,
                        principalTable: "CompanionTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanionActivities_OwnerUserId_OccurredAt",
                table: "CompanionActivities",
                columns: new[] { "OwnerUserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanionActivities_ProjectId",
                table: "CompanionActivities",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionActivities_RecordType_RecordId",
                table: "CompanionActivities",
                columns: new[] { "RecordType", "RecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanionActivities_SessionId",
                table: "CompanionActivities",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionAiClients_OwnerUserId_Name",
                table: "CompanionAiClients",
                columns: new[] { "OwnerUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanionDailyItems_IssueId",
                table: "CompanionDailyItems",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionDailyItems_OwnerUserId_Date_Rank",
                table: "CompanionDailyItems",
                columns: new[] { "OwnerUserId", "Date", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanionDocumentProjects_ProjectId",
                table: "CompanionDocumentProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionDocuments_CurrentRevisionId",
                table: "CompanionDocuments",
                column: "CurrentRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionDocuments_FolderId",
                table: "CompanionDocuments",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionDocuments_OwnerUserId",
                table: "CompanionDocuments",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionDocuments_OwnerUserId_Slug",
                table: "CompanionDocuments",
                columns: new[] { "OwnerUserId", "Slug" },
                unique: true,
                filter: "\"Slug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionDocumentTags_TagId",
                table: "CompanionDocumentTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionDocumentTemplates_OwnerUserId_Name",
                table: "CompanionDocumentTemplates",
                columns: new[] { "OwnerUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanionFolders_OwnerUserId_ParentFolderId_Name",
                table: "CompanionFolders",
                columns: new[] { "OwnerUserId", "ParentFolderId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanionFolders_ParentFolderId",
                table: "CompanionFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionInboxItems_DocumentId",
                table: "CompanionInboxItems",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionInboxItems_IssueId",
                table: "CompanionInboxItems",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionInboxItems_OwnerUserId_Status",
                table: "CompanionInboxItems",
                columns: new[] { "OwnerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanionIssueBlockers_BlockerIssueId",
                table: "CompanionIssueBlockers",
                column: "BlockerIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionIssues_OwnerUserId",
                table: "CompanionIssues",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionIssues_ParentIssueId",
                table: "CompanionIssues",
                column: "ParentIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionIssues_ProjectId_Status_Rank",
                table: "CompanionIssues",
                columns: new[] { "ProjectId", "Status", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanionIssueTags_TagId",
                table: "CompanionIssueTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionProjects_ContextDocumentId",
                table: "CompanionProjects",
                column: "ContextDocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanionProjects_OwnerUserId",
                table: "CompanionProjects",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionProjects_OwnerUserId_Slug",
                table: "CompanionProjects",
                columns: new[] { "OwnerUserId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanionRevisions_DocumentId_CreatedAt",
                table: "CompanionRevisions",
                columns: new[] { "DocumentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanionSavedViews_OwnerUserId_Name_ProjectId",
                table: "CompanionSavedViews",
                columns: new[] { "OwnerUserId", "Name", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanionSavedViews_ProjectId",
                table: "CompanionSavedViews",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionSessions_ProjectId_ActorUserId_FinishedAt",
                table: "CompanionSessions",
                columns: new[] { "ProjectId", "ActorUserId", "FinishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanionTags_OwnerUserId_Name",
                table: "CompanionTags",
                columns: new[] { "OwnerUserId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanionActivities_CompanionProjects_ProjectId",
                table: "CompanionActivities",
                column: "ProjectId",
                principalTable: "CompanionProjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanionActivities_CompanionSessions_SessionId",
                table: "CompanionActivities",
                column: "SessionId",
                principalTable: "CompanionSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanionDailyItems_CompanionIssues_IssueId",
                table: "CompanionDailyItems",
                column: "IssueId",
                principalTable: "CompanionIssues",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanionDocumentProjects_CompanionDocuments_DocumentId",
                table: "CompanionDocumentProjects",
                column: "DocumentId",
                principalTable: "CompanionDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanionDocumentProjects_CompanionProjects_ProjectId",
                table: "CompanionDocumentProjects",
                column: "ProjectId",
                principalTable: "CompanionProjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanionDocuments_CompanionRevisions_CurrentRevisionId",
                table: "CompanionDocuments",
                column: "CurrentRevisionId",
                principalTable: "CompanionRevisions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanionRevisions_CompanionDocuments_DocumentId",
                table: "CompanionRevisions");

            migrationBuilder.DropTable(
                name: "CompanionActivities");

            migrationBuilder.DropTable(
                name: "CompanionAiClients");

            migrationBuilder.DropTable(
                name: "CompanionDailyItems");

            migrationBuilder.DropTable(
                name: "CompanionDocumentProjects");

            migrationBuilder.DropTable(
                name: "CompanionDocumentTags");

            migrationBuilder.DropTable(
                name: "CompanionDocumentTemplates");

            migrationBuilder.DropTable(
                name: "CompanionInboxItems");

            migrationBuilder.DropTable(
                name: "CompanionIssueBlockers");

            migrationBuilder.DropTable(
                name: "CompanionIssueTags");

            migrationBuilder.DropTable(
                name: "CompanionSavedViews");

            migrationBuilder.DropTable(
                name: "CompanionSessions");

            migrationBuilder.DropTable(
                name: "CompanionIssues");

            migrationBuilder.DropTable(
                name: "CompanionTags");

            migrationBuilder.DropTable(
                name: "CompanionProjects");

            migrationBuilder.DropTable(
                name: "CompanionDocuments");

            migrationBuilder.DropTable(
                name: "CompanionFolders");

            migrationBuilder.DropTable(
                name: "CompanionRevisions");
        }
    }
}
