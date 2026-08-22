using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.ModuleAbstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace EnterpriseStarter.Companion.Infrastructure;

public sealed class CompanionModelContributor : IEntityModelContributor
{
    public string Key => "companion";

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("CompanionProjects");
            entity.Property(project => project.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(project => project.Name).HasMaxLength(200).IsRequired();
            entity.Property(project => project.Slug).HasMaxLength(128).IsRequired();
            entity.HasIndex(project => new { project.OwnerUserId, project.Slug }).IsUnique();
            entity.HasIndex(project => project.OwnerUserId);
            entity.HasGeneratedTsVectorColumn(project => project.SearchVector, "simple", project => new { project.Name });
            entity.HasIndex(project => project.SearchVector).HasMethod("GIN");
            entity.HasOne(project => project.ContextDocument)
                .WithOne()
                .HasForeignKey<Project>(project => project.ContextDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Folder>(entity =>
        {
            entity.ToTable("CompanionFolders");
            entity.Property(folder => folder.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(folder => folder.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(folder => folder.ParentFolder)
                .WithMany(folder => folder.Children)
                .HasForeignKey(folder => folder.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(folder => new { folder.OwnerUserId, folder.ParentFolderId, folder.Name }).IsUnique();
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("CompanionDocuments");
            entity.Property(document => document.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(document => document.Title).HasMaxLength(500).IsRequired();
            entity.Property(document => document.Slug).HasMaxLength(128);
            entity.HasOne(document => document.Folder)
                .WithMany(folder => folder.Documents)
                .HasForeignKey(document => document.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(document => document.CurrentRevision)
                .WithMany()
                .HasForeignKey(document => document.CurrentRevisionId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(document => new { document.OwnerUserId, document.Slug })
                .IsUnique()
                .HasFilter("\"Slug\" IS NOT NULL");
            entity.HasIndex(document => document.OwnerUserId);
        });

        modelBuilder.Entity<Revision>(entity =>
        {
            entity.ToTable("CompanionRevisions");
            entity.Property(revision => revision.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(revision => revision.Title).HasMaxLength(500).IsRequired();
            entity.Property(revision => revision.Body).IsRequired();
            entity.Property(revision => revision.ActorUserId).HasMaxLength(450).IsRequired();
            entity.Property(revision => revision.Kind).HasMaxLength(40).IsRequired();
            entity.HasOne(revision => revision.Document)
                .WithMany(document => document.Revisions)
                .HasForeignKey(revision => revision.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(revision => new { revision.DocumentId, revision.CreatedAt });
            entity.HasGeneratedTsVectorColumn(
                revision => revision.SearchVector,
                "simple",
                revision => new { revision.Title, revision.Body });
            entity.HasIndex(revision => revision.SearchVector).HasMethod("GIN");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("CompanionTags");
            entity.Property(tag => tag.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(tag => tag.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(tag => new { tag.OwnerUserId, tag.Name }).IsUnique();
        });

        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.ToTable("CompanionDocumentTemplates");
            entity.Property(template => template.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(template => template.Name).HasMaxLength(200).IsRequired();
            entity.Property(template => template.TitlePattern).HasMaxLength(500).IsRequired();
            entity.Property(template => template.Body).IsRequired();
            entity.HasIndex(template => new { template.OwnerUserId, template.Name }).IsUnique();
        });

        modelBuilder.Entity<DocumentProject>(entity =>
        {
            entity.ToTable("CompanionDocumentProjects");
            entity.HasKey(link => new { link.DocumentId, link.ProjectId });
            entity.HasOne(link => link.Document)
                .WithMany(document => document.ProjectLinks)
                .HasForeignKey(link => link.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Project)
                .WithMany(project => project.DocumentLinks)
                .HasForeignKey(link => link.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentTag>(entity =>
        {
            entity.ToTable("CompanionDocumentTags");
            entity.HasKey(link => new { link.DocumentId, link.TagId });
            entity.HasOne(link => link.Document)
                .WithMany(document => document.TagLinks)
                .HasForeignKey(link => link.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Tag)
                .WithMany(tag => tag.DocumentLinks)
                .HasForeignKey(link => link.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.ToTable("CompanionIssues");
            entity.Property(issue => issue.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(issue => issue.Title).HasMaxLength(500).IsRequired();
            entity.Property(issue => issue.AssigneeUserId).HasMaxLength(450);
            entity.Property(issue => issue.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(issue => issue.Priority).HasConversion<string>().HasMaxLength(32);
            entity.HasOne(issue => issue.Project)
                .WithMany(project => project.Issues)
                .HasForeignKey(issue => issue.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(issue => issue.ParentIssue)
                .WithMany(issue => issue.Children)
                .HasForeignKey(issue => issue.ParentIssueId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(issue => new { issue.ProjectId, issue.Status, issue.Rank });
            entity.HasIndex(issue => issue.OwnerUserId);
            entity.HasGeneratedTsVectorColumn(
                issue => issue.SearchVector,
                "simple",
                issue => new { issue.Title, issue.Description });
            entity.HasIndex(issue => issue.SearchVector).HasMethod("GIN");
        });

        modelBuilder.Entity<IssueBlocker>(entity =>
        {
            entity.ToTable("CompanionIssueBlockers");
            entity.HasKey(link => new { link.IssueId, link.BlockerIssueId });
            entity.HasOne(link => link.Issue)
                .WithMany(issue => issue.Blockers)
                .HasForeignKey(link => link.IssueId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(link => link.BlockerIssue)
                .WithMany(issue => issue.Blocking)
                .HasForeignKey(link => link.BlockerIssueId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IssueTag>(entity =>
        {
            entity.ToTable("CompanionIssueTags");
            entity.HasKey(link => new { link.IssueId, link.TagId });
            entity.HasOne(link => link.Issue)
                .WithMany(issue => issue.TagLinks)
                .HasForeignKey(link => link.IssueId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Tag)
                .WithMany(tag => tag.IssueLinks)
                .HasForeignKey(link => link.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyItem>(entity =>
        {
            entity.ToTable("CompanionDailyItems");
            entity.Property(item => item.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(item => item.CustomText).HasMaxLength(500);
            entity.HasOne(item => item.Issue)
                .WithMany()
                .HasForeignKey(item => item.IssueId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(item => new { item.OwnerUserId, item.Date, item.Rank });
        });

        modelBuilder.Entity<InboxItem>(entity =>
        {
            entity.ToTable("CompanionInboxItems");
            entity.Property(item => item.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(item => item.Text).HasMaxLength(4000).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasOne(item => item.Document)
                .WithMany()
                .HasForeignKey(item => item.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(item => item.Issue)
                .WithMany()
                .HasForeignKey(item => item.IssueId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(item => new { item.OwnerUserId, item.Status });
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("CompanionSessions");
            entity.Property(session => session.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(session => session.ActorUserId).HasMaxLength(450).IsRequired();
            entity.Property(session => session.Summary).HasMaxLength(4000);
            entity.HasOne(session => session.Project)
                .WithMany(project => project.Sessions)
                .HasForeignKey(session => session.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(session => new { session.ProjectId, session.ActorUserId, session.FinishedAt });
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("CompanionActivities");
            entity.Property(activity => activity.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(activity => activity.ActorUserId).HasMaxLength(450).IsRequired();
            entity.Property(activity => activity.ActionType).HasMaxLength(100).IsRequired();
            entity.Property(activity => activity.RecordType).HasMaxLength(100).IsRequired();
            entity.Property(activity => activity.Summary).HasMaxLength(500).IsRequired();
            entity.HasOne(activity => activity.Project)
                .WithMany()
                .HasForeignKey(activity => activity.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(activity => activity.Session)
                .WithMany()
                .HasForeignKey(activity => activity.SessionId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(activity => new { activity.OwnerUserId, activity.OccurredAt });
            entity.HasIndex(activity => new { activity.RecordType, activity.RecordId });
            entity.HasGeneratedTsVectorColumn(activity => activity.SearchVector, "simple", activity => new { activity.Summary });
            entity.HasIndex(activity => activity.SearchVector).HasMethod("GIN");
        });

        modelBuilder.Entity<SavedView>(entity =>
        {
            entity.ToTable("CompanionSavedViews");
            entity.Property(view => view.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(view => view.Name).HasMaxLength(200).IsRequired();
            entity.Property(view => view.EntityType).HasConversion<string>().HasMaxLength(32);
            entity.Property(view => view.ColumnsJson).IsRequired();
            entity.Property(view => view.FiltersJson).IsRequired();
            entity.Property(view => view.SortJson).IsRequired();
            entity.Property(view => view.GroupBy).HasMaxLength(100);
            entity.HasOne(view => view.Project)
                .WithMany()
                .HasForeignKey(view => view.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(view => new { view.OwnerUserId, view.Name, view.ProjectId }).IsUnique();
        });

        modelBuilder.Entity<AiClient>(entity =>
        {
            entity.ToTable("CompanionAiClients");
            entity.Property(client => client.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(client => client.Name).HasMaxLength(200).IsRequired();
            entity.Property(client => client.SecretHash).HasMaxLength(200).IsRequired();
            entity.HasIndex(client => new { client.OwnerUserId, client.Name }).IsUnique();
        });
    }
}
