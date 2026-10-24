using AutoMapper;
using GymCrest.Contracts.Attendance;
using GymCrest.Domain.Entities;

namespace GymCrest.Application.Mappings
{
    public class AttendanceMappingProfile : Profile
    {
        public AttendanceMappingProfile()
        {
            // AttendanceLog -> AttendanceLogResponse
            CreateMap<AttendanceLog, AttendanceLogResponse>()
                .ForMember(dest => dest.CheckInTime,
                           opt => opt.MapFrom(src => src.CheckInTime.ToUniversalTime()))
                .ForMember(dest => dest.CheckOutTime,
                           opt => opt.MapFrom(src => src.CheckOutTime.HasValue 
                               ? src.CheckOutTime.Value.ToUniversalTime() 
                               : (DateTime?)null))
                .ForMember(dest => dest.MemberName,
                           opt => opt.MapFrom(src => $"{src.Member.FirstName} {src.Member.LastName}"))
                .ForMember(dest => dest.MembershipNumber,
                           opt => opt.MapFrom(src => src.Member.MembershipNumber))
                .ForMember(dest => dest.Initials,
                           opt => opt.MapFrom(src =>
                               $"{src.Member.FirstName.FirstOrDefault()}{src.Member.LastName.FirstOrDefault()}".ToUpper()))
                .ForMember(dest => dest.PlanName,
                           opt => opt.MapFrom(src =>
                               src.Member.Subscriptions
                                  .OrderByDescending(s => s.CreatedOn)
                                  .FirstOrDefault(s => s.IsActive) != null
                                   ? src.Member.Subscriptions
                                         .OrderByDescending(s => s.CreatedOn)
                                         .First(s => s.IsActive).PlanNameSnapshot
                                   : "General"))
                .ForMember(dest => dest.Duration,
                           opt => opt.MapFrom(src =>
                               src.CheckOutTime.HasValue
                                   ? CalculateDuration(src.CheckInTime, src.CheckOutTime.Value)
                                   : "--"))
                .ForMember(dest => dest.Status,
                           opt => opt.MapFrom(src =>
                               src.CheckOutTime.HasValue ? "Completed" : "Active"))
                .ForMember(dest => dest.BranchName,
                           opt => opt.MapFrom(src =>
                               src.Branch != null ? src.Branch.Name : "Main Branch"));

            // AttendanceLog -> CheckedInMemberResponse (for live occupancy list)
            CreateMap<AttendanceLog, CheckedInMemberResponse>()
                .ForMember(dest => dest.CheckInTime,
                           opt => opt.MapFrom(src => src.CheckInTime.ToUniversalTime()))
                .ForMember(dest => dest.LogId,
                           opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name,
                           opt => opt.MapFrom(src => $"{src.Member.FirstName} {src.Member.LastName}"))
                .ForMember(dest => dest.MembershipNumber,
                           opt => opt.MapFrom(src => src.Member.MembershipNumber))
                .ForMember(dest => dest.Initials,
                           opt => opt.MapFrom(src =>
                               $"{src.Member.FirstName.FirstOrDefault()}{src.Member.LastName.FirstOrDefault()}".ToUpper()))
                .ForMember(dest => dest.PlanName,
                           opt => opt.MapFrom(src =>
                               src.Member.Subscriptions
                                  .OrderByDescending(s => s.CreatedOn)
                                  .FirstOrDefault(s => s.IsActive) != null
                                   ? src.Member.Subscriptions
                                         .OrderByDescending(s => s.CreatedOn)
                                         .First(s => s.IsActive).PlanNameSnapshot
                                   : "General"));
        }

        private static string CalculateDuration(DateTime checkIn, DateTime checkOut)
        {
            TimeSpan diff = checkOut - checkIn;
            return diff.TotalHours >= 1
                ? $"{(int)diff.TotalHours}h {diff.Minutes}m"
                : $"{diff.Minutes}m";
        }
    }
}
