using GymForge.Application.Modules.Auth.Interface;
using GymForge.Application.Modules.Auth.Service;
using GymForge.Application.Modules.Diet.Interfaces;
using GymForge.Application.Modules.Diet.Services;
using GymForge.Application.Modules.Gym.Interfaces;
using GymForge.Application.Modules.Gym.Services;
using GymForge.Application.Modules.Payments.Interfaces;
using GymForge.Application.Modules.Payments.Services;
using GymForge.Application.Modules.SuperAdmin.Interfaces;
using GymForge.Application.Modules.SuperAdmin.Services;
using GymForge.Application.Modules.Users.Interface;
using GymForge.Application.Modules.Users.Services;
using GymForge.Application.Modules.Common.Interfaces;
using GymForge.Application.Modules.Workout.Interface;
using GymForge.Application.Modules.Workout.Services;
using GymForge.Application.Modules.Diet.Interface;
using GymForge.Application.Modules.PlanGeneration.Interface;
using GymForge.Application.Modules.PlanGeneration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymForge.Application
{
    public static class DIConfig
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IAuthService, AuthService>();

            services.AddScoped<IJwtService, JwtService>();

            services.AddScoped<IPasswordService, PasswordService>();

            services.AddScoped<IGymManagementService, GymManagementService>();
            services.AddScoped<IMemberAccessService, MemberAccessService>();
            services.AddScoped<IResourceAccessService, ResourceAccessService>();

            services.AddScoped<IUserService, UserService>();

            services.AddScoped<ISaaSPlanService, SaaSPlanService>();

            services.AddScoped<ISaaSPaymentService, SaaSPaymentService>();
            
            services.AddScoped<IMemberPaymentService, MemberPaymentService>();
            
            services.AddScoped<ISaaSConfigurationService, SaaSConfigurationService>();
            
            services.AddScoped<IDashboardService, DashboardService>();
            
            services.AddScoped<IReportService, ReportService>();

            services.AddScoped<IGymPlanService, GymPlanService>();

            services.AddScoped<IGymMemberService, GymMemberService>();
           
            services.AddScoped<IStaffService, StaffService>();
            
            services.AddScoped<IInventoryService, InventoryService>();

            services.AddScoped<IEquipmentService, EquipmentService>();
            
            services.AddScoped<IMaintenanceService, MaintenanceService>();
            
            services.AddScoped<IGymOwnerDashboardService, GymOwnerDashboardService>();
            
            services.AddScoped<IAttendanceService, AttendanceService>();

            services.AddScoped<IMemberBillingService, MemberBillingService>();
            
            services.AddScoped<IStaffPayrollService, StaffPayrollService>();

            services.AddScoped<IWorkoutService, WorkoutService>();
           
            services.AddScoped<IWorkoutPlanService, WorkoutPlanService>();
            
            services.AddScoped<IMemberWorkoutService, MemberWorkoutService>();

            services.AddScoped<IUserWorkoutProgressService, UserWorkoutProgressService>();

            services.AddScoped<IDietPlanService, DietPlanService>();

            services.AddScoped<IMemberDietService, MemberDietService>();

            services.AddScoped<IDietTrackingService, DietTrackingService>();

            services.AddScoped<IPlanGenerationService, PlanGenerationService>();
            services.AddScoped<IDemoDataService, DemoDataService>();
            services.AddScoped<IPlatformStatusService, PlatformStatusService>();
            
            services.AddScoped<IGymAnnouncementService, GymAnnouncementService>();
            
            services.AddScoped<ITemplateService, TemplateService>();
            
            services.AddScoped<IUserNotificationService, UserNotificationService>();

            services.AddScoped<GymForge.Application.BackgroundJobs.AccountDeletionJob>();

            services.AddScoped<GymForge.Application.BackgroundJobs.AutomatedNotificationJob>();
            
            services.AddScoped<GymForge.Application.BackgroundJobs.SaaSExpiryJob>();


            services.AddScoped<GymForge.Application.BackgroundJobs.MemberAutoUnfreezeJob>();

            return services;
        }
    }
}
