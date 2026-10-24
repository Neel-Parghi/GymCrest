using System;

namespace GymCrest.Api.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AllowExpiredSubscriptionAttribute : Attribute
    {
    }
}
