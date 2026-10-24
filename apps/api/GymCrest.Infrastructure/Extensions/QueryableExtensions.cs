using System;
using System.Linq;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;

namespace GymCrest.Infrastructure.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> WhereBranchContext<T>(
            this IQueryable<T> query,
            IQueryable<Branch> branches,
            Guid? branchId) where T : class, IBranchScoped
        {
            if (!branchId.HasValue)
            {
                return query;
            }

            return query.Where(x => x.BranchId == branchId.Value || 
                (x.BranchId == null && branches.Any(b => b.Id == branchId.Value && b.IsMainBranch)));
        }
    }
}
