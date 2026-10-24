using System;
using GymCrest.Contracts.Common;
using GymCrest.Shared.Enums;

namespace GymCrest.Contracts.Members
{
    public class MemberFilterParams : PaginationParams
    {
        public MemberStatus? Status { get; set; }
        public Guid? PlanId { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
    }
}
