using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface IMemberDietRepository
    {
        Task<MemberDietAssignment?> GetActiveDietAssignmentAsync(Guid memberOrUserId);
        
        Task AddDietAssignmentAsync(MemberDietAssignment assignment);
        
        Task<IEnumerable<MemberDietAssignment>> GetDietAssignmentsAsync(Guid memberOrUserId);
    }
}
