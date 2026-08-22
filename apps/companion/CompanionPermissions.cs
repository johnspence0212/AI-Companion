using EnterpriseStarter.ModuleAbstractions;

namespace EnterpriseStarter.Companion;

public static class CompanionPermissions
{
    public const string ProjectsRead = "projects.read";
    public const string ProjectsManage = "projects.manage";
    public const string DocumentsRead = "documents.read";
    public const string DocumentsManage = "documents.manage";
    public const string IssuesRead = "issues.read";
    public const string IssuesManage = "issues.manage";
    public const string DailyRead = "daily.read";
    public const string DailyManage = "daily.manage";
    public const string InboxRead = "inbox.read";
    public const string InboxManage = "inbox.manage";
    public const string SearchRead = "search.read";
    public const string SearchManage = "search.manage";
    public const string SessionsRead = "sessions.read";
    public const string SessionsManage = "sessions.manage";
    public const string ViewsRead = "views.read";
    public const string ViewsManage = "views.manage";
    public const string TagsRead = "tags.read";
    public const string TagsManage = "tags.manage";
    public const string ActivityRead = "activity.read";
    public const string ActivityManage = "activity.manage";
    public const string AiClientsManage = "aiclients.manage";

    public static IReadOnlyList<ModulePermission> Catalog { get; } =
    [
        new(ProjectsRead, "View projects", "Projects"),
        new(ProjectsManage, "Manage projects", "Projects"),
        new(DocumentsRead, "View documents", "Documents"),
        new(DocumentsManage, "Manage documents", "Documents"),
        new(IssuesRead, "View issues", "Issues"),
        new(IssuesManage, "Manage issues", "Issues"),
        new(DailyRead, "View daily", "Daily"),
        new(DailyManage, "Manage daily", "Daily"),
        new(InboxRead, "View inbox", "Inbox"),
        new(InboxManage, "Manage inbox", "Inbox"),
        new(SearchRead, "View search", "Search"),
        new(SearchManage, "Manage search", "Search"),
        new(SessionsRead, "View sessions", "Sessions"),
        new(SessionsManage, "Manage sessions", "Sessions"),
        new(ViewsRead, "View saved views", "Views"),
        new(ViewsManage, "Manage saved views", "Views"),
        new(TagsRead, "View tags", "Tags"),
        new(TagsManage, "Manage tags", "Tags"),
        new(ActivityRead, "View activity", "Activity"),
        new(ActivityManage, "Manage activity", "Activity"),
        new(AiClientsManage, "Manage AI clients", "AI Clients"),
    ];

    public static IReadOnlyList<string> All { get; } = Catalog.Select(permission => permission.Key).ToArray();
}
