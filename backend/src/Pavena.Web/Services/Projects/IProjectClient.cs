using Pavena.Application.Research.DTOs;

namespace Pavena.Web.Services.Projects;

public interface IProjectClient
{
    Task<IReadOnlyList<ProjectSummaryDto>> GetUserProjectsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectSummaryDto>> GetDiscoverProjectsAsync(string? q = null, string? field = null, int limit = 20, int offset = 0, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto?> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto?> GetProjectBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto?> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto?> UpdateProjectAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto?> UpdateStatusAsync(Guid id, UpdateProjectStatusRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto?> AdvanceStageAsync(Guid id, AdvanceProjectStageRequest request, CancellationToken cancellationToken = default);
    Task<ProjectInvitationDto?> InviteMemberAsync(Guid projectId, InviteMemberRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectInvitationDto>> GetMyInvitationsAsync(CancellationToken cancellationToken = default);
    Task<bool> RespondInvitationAsync(Guid invitationId, bool accept, CancellationToken cancellationToken = default);
    Task<bool> RemoveMemberAsync(Guid projectId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMemberRoleAsync(Guid projectId, Guid targetUserId, UpdateMemberRoleRequest request, CancellationToken cancellationToken = default);
    Task<bool> TransferLeadershipAsync(Guid projectId, TransferLeadershipRequest request, CancellationToken cancellationToken = default);
    Task<ProjectMilestoneDto?> CreateMilestoneAsync(Guid projectId, CreateMilestoneRequest request, CancellationToken cancellationToken = default);
    Task<ProjectMilestoneDto?> UpdateMilestoneAsync(Guid projectId, Guid milestoneId, UpdateMilestoneRequest request, CancellationToken cancellationToken = default);
    Task<ProjectTaskDto?> CreateTaskAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<ProjectTaskDto?> UpdateTaskAsync(Guid projectId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectActivityLogDto>> GetActivityTimelineAsync(Guid projectId, CancellationToken cancellationToken = default);
}
