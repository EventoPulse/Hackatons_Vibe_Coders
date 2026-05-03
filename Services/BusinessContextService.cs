using EventsApp.Data;
using EventsApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Services
{
    public class OrganizerBusinessContext
    {
        public BusinessWorkspace? Workspace { get; set; }

        public OrganizerProfile? Page { get; set; }

        public IReadOnlyList<BusinessWorkspace> Workspaces { get; set; } = Array.Empty<BusinessWorkspace>();

        public IReadOnlyList<OrganizerProfile> Pages { get; set; } = Array.Empty<OrganizerProfile>();
    }

    public interface IBusinessContextService
    {
        Task<OrganizerBusinessContext> GetContextAsync(HttpContext httpContext, CancellationToken cancellationToken = default);

        Task<BusinessWorkspace> EnsureDefaultWorkspaceAsync(string ownerId, CancellationToken cancellationToken = default);

        Task SetActiveContextAsync(HttpContext httpContext, int workspaceId, int? pageId, CancellationToken cancellationToken = default);
    }

    public class BusinessContextService : IBusinessContextService
    {
        private const string WorkspaceCookie = "evento.activeWorkspaceId";
        private const string PageCookie = "evento.activePageId";

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public BusinessContextService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<OrganizerBusinessContext> GetContextAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var ownerId = _userManager.GetUserId(httpContext.User);
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return new OrganizerBusinessContext();
            }

            await EnsureDefaultWorkspaceAsync(ownerId, cancellationToken);
            await EnsureProfilesHaveWorkspacesAsync(ownerId, cancellationToken);

            var workspaces = await _db.BusinessWorkspaces
                .AsNoTracking()
                .Where(w => w.OwnerId == ownerId && w.Status != BusinessWorkspaceStatus.Archived)
                .OrderByDescending(w => w.IsDefault)
                .ThenBy(w => w.DisplayName)
                .ToListAsync(cancellationToken);

            var requestedWorkspaceId = TryReadIntCookie(httpContext, WorkspaceCookie);
            var workspace = workspaces.FirstOrDefault(w => w.Id == requestedWorkspaceId)
                ?? workspaces.FirstOrDefault(w => w.IsDefault)
                ?? workspaces.FirstOrDefault();

            var pagesQuery = _db.OrganizerProfiles
                .AsNoTracking()
                .Where(p => p.OwnerId == ownerId && p.IsActive && p.Status != BusinessWorkspaceStatus.Archived);

            if (workspace != null)
            {
                pagesQuery = pagesQuery.Where(p => p.BusinessWorkspaceId == workspace.Id);
            }

            var pages = await pagesQuery
                .OrderByDescending(p => p.IsDefaultForWorkspace)
                .ThenByDescending(p => p.IsDefault)
                .ThenBy(p => p.DisplayName)
                .ToListAsync(cancellationToken);

            var requestedPageId = TryReadIntCookie(httpContext, PageCookie);
            var page = pages.FirstOrDefault(p => p.Id == requestedPageId)
                ?? pages.FirstOrDefault(p => p.IsDefaultForWorkspace)
                ?? pages.FirstOrDefault(p => p.IsDefault)
                ?? pages.FirstOrDefault();

            return new OrganizerBusinessContext
            {
                Workspace = workspace,
                Page = page,
                Workspaces = workspaces,
                Pages = pages,
            };
        }

        public async Task<BusinessWorkspace> EnsureDefaultWorkspaceAsync(string ownerId, CancellationToken cancellationToken = default)
        {
            var existingDefault = await _db.BusinessWorkspaces
                .FirstOrDefaultAsync(w => w.OwnerId == ownerId && w.IsDefault && w.Status != BusinessWorkspaceStatus.Archived, cancellationToken);

            if (existingDefault != null)
            {
                return existingDefault;
            }

            var fallback = await _db.BusinessWorkspaces
                .Where(w => w.OwnerId == ownerId && w.Status != BusinessWorkspaceStatus.Archived)
                .OrderBy(w => w.DisplayName)
                .FirstOrDefaultAsync(cancellationToken);

            if (fallback != null)
            {
                fallback.IsDefault = true;
                await _db.SaveChangesAsync(cancellationToken);
                return fallback;
            }

            var orgData = await _db.OrganizerData.AsNoTracking().FirstOrDefaultAsync(o => o.OrganizerId == ownerId, cancellationToken);
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ownerId, cancellationToken);
            var displayName = orgData?.OrganizationName ?? user?.UserName ?? "Evento Workspace";

            var workspace = new BusinessWorkspace
            {
                OwnerId = ownerId,
                DisplayName = displayName,
                LegalName = displayName,
                CompanyNumber = orgData?.CompanyNumber,
                BillingEmail = user?.Email,
                PhoneNumber = orgData?.PhoneNumber ?? user?.PhoneNumber,
                IsDefault = true,
            };

            _db.BusinessWorkspaces.Add(workspace);
            await _db.SaveChangesAsync(cancellationToken);
            return workspace;
        }

        public async Task SetActiveContextAsync(HttpContext httpContext, int workspaceId, int? pageId, CancellationToken cancellationToken = default)
        {
            var ownerId = _userManager.GetUserId(httpContext.User);
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return;
            }

            var ownsWorkspace = await _db.BusinessWorkspaces.AnyAsync(w =>
                w.Id == workspaceId &&
                w.OwnerId == ownerId &&
                w.Status != BusinessWorkspaceStatus.Archived,
                cancellationToken);
            if (!ownsWorkspace)
            {
                return;
            }

            if (pageId.HasValue)
            {
                var ownsPage = await _db.OrganizerProfiles.AnyAsync(p =>
                    p.Id == pageId.Value &&
                    p.OwnerId == ownerId &&
                    p.BusinessWorkspaceId == workspaceId &&
                    p.IsActive &&
                    p.Status != BusinessWorkspaceStatus.Archived,
                    cancellationToken);

                if (!ownsPage)
                {
                    pageId = null;
                }
            }

            httpContext.Response.Cookies.Append(WorkspaceCookie, workspaceId.ToString(), CookieOptions());
            if (pageId.HasValue)
            {
                httpContext.Response.Cookies.Append(PageCookie, pageId.Value.ToString(), CookieOptions());
            }
            else
            {
                httpContext.Response.Cookies.Delete(PageCookie);
            }
        }

        private async Task EnsureProfilesHaveWorkspacesAsync(string ownerId, CancellationToken cancellationToken)
        {
            var workspace = await EnsureDefaultWorkspaceAsync(ownerId, cancellationToken);
            var profiles = await _db.OrganizerProfiles
                .Where(p => p.OwnerId == ownerId && p.BusinessWorkspaceId == null)
                .ToListAsync(cancellationToken);

            foreach (var profile in profiles)
            {
                profile.BusinessWorkspaceId = workspace.Id;
                profile.IsDefaultForWorkspace = profile.IsDefault;
            }

            if (profiles.Count > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        private static int? TryReadIntCookie(HttpContext httpContext, string name)
        {
            return int.TryParse(httpContext.Request.Cookies[name], out var value) ? value : null;
        }

        private static CookieOptions CookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
            };
        }
    }
}
