using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace EnterpriseStarter.Platform.Migrations
{
    /// <inheritdoc />
    public partial class CompanionSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "CompanionRevisions",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "simple")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "Body" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "CompanionProjects",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "simple")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "CompanionIssues",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "simple")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "Description" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "CompanionActivities",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "simple")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Summary" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanionRevisions_SearchVector",
                table: "CompanionRevisions",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionProjects_SearchVector",
                table: "CompanionProjects",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionIssues_SearchVector",
                table: "CompanionIssues",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionActivities_SearchVector",
                table: "CompanionActivities",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanionRevisions_SearchVector",
                table: "CompanionRevisions");

            migrationBuilder.DropIndex(
                name: "IX_CompanionProjects_SearchVector",
                table: "CompanionProjects");

            migrationBuilder.DropIndex(
                name: "IX_CompanionIssues_SearchVector",
                table: "CompanionIssues");

            migrationBuilder.DropIndex(
                name: "IX_CompanionActivities_SearchVector",
                table: "CompanionActivities");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "CompanionRevisions");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "CompanionProjects");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "CompanionIssues");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "CompanionActivities");
        }
    }
}
