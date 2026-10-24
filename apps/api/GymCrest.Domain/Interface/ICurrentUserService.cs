using System;

namespace GymCrest.Domain.Interface
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
    }
}
