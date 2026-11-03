using Pavena.Application.Research.DTOs;

namespace Pavena.Application.Research.Interfaces;

public interface IProjectService
{
    Task<ProjectSummaryDto> CreateProjectAsync(Guid creatorUserId, CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto> GetProjectByIdAsync(Guid callerUserId, Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto> GetProjectBySlugAsync(Guid callerUserId, string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectSummaryDto>> GetUserProjectsAsync(Guid callerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectSummaryDto>> GetDiscoverProjectsAsync(Guid callerUserId, string? q, string? field, int limit = 20, int offset = 0, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto> UpdateProjectAsync(Guid callerUserId, Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto> UpdateProjectStatusAsync(Guid callerUserId, Guid projectId, UpdateProjectStatusRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto> AdvanceProjectStageAsync(Guid callerUserId, Guid projectId, AdvanceProjectStageRequest request, CancellationToken cancellationToken = default);
    Task TransferLeadershipAsync(Guid callerUserId, Guid projectId, TransferLeadershipRequest request, CancellationToken cancellationToken = default);
    
    Task<ProjectInvitationDto> InviteMemberAsync(Guid callerUserId, Guid projectId, InviteMemberRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectInvitationDto>> GetMyInvitationsAsync(Guid callerUserId, CancellationToken cancellationToken = default);
    Task RespondInvitationAsync(Guid callerUserId, Guid invitationId, RespondInvitationRequest request, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid callerUserId, Guid projectId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task UpdateMemberRoleAsync(Guid callerUserId, Guid projectId, Guid targetUserId, UpdateMemberRoleRequest request, CancellationToken cancellationToken = default);

    Task<ProjectTaskDto> CreateTaskAsync(Guid callerUserId, Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<ProjectTaskDto> UpdateTaskAsync(Guid callerUserId, Guid projectId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(Guid callerUserId, Guid projectId, Guid taskId, CancellationToken cancellationToken = default);

    Task<ProjectMilestoneDto> CreateMilestoneAsync(Guid callerUserId, Guid projectId, CreateMilestoneRequest request, CancellationToken cancellationToken = default);
    Task<ProjectMilestoneDto> UpdateMilestoneAsync(Guid callerUserId, Guid projectId, Guid milestoneId, UpdateMilestoneRequest request, CancellationToken cancellationToken = default);
    Task DeleteMilestoneAsync(Guid callerUserId, Guid projectId, Guid milestoneId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectActivityLogDto>> GetActivityTimelineAsync(Guid callerUserId, Guid projectId, CancellationToken cancellationToken = default);
}
