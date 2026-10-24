using System;

namespace GymCrest.Domain.Interface
{
    public interface IBranchScoped
    {
        Guid? BranchId { get; set; }
    }
}
