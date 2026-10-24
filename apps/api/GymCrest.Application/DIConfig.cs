using GymCrest.Application.Modules.Auth.Interface;
using GymCrest.Application.Modules.Auth.Service;
using GymCrest.Application.Modules.Diet.Interfaces;
using GymCrest.Application.Modules.Diet.Services;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Application.Modules.Gym.Services;
using GymCrest.Application.Modules.Payments.Interfaces;
using GymCrest.Application.Modules.Payments.Services;
using GymCrest.Application.Modules.SuperAdmin.Interfaces;
using GymCrest.Application.Modules.SuperAdmin.Services;
using GymCrest.Application.Modules.Users.Interface;
using GymCrest.Application.Modules.Users.Services;
using GymCrest.Application.Modules.Common.Interfaces;
using GymCrest.Application.Modules.Workout.Interface;
using GymCrest.Application.Modules.Workout.Services;
using GymCrest.Application.Modules.Diet.Interface;
using GymCrest.Application.Modules.PlanGeneration.Interface;
using GymCrest.Application.Modules.PlanGeneration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymCrest.Application
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

            services.AddScoped<GymCrest.Application.BackgroundJobs.AccountDeletionJob>();

            services.AddScoped<GymCrest.Application.BackgroundJobs.AutomatedNotificationJob>();
            
            services.AddScoped<GymCrest.Application.BackgroundJobs.SaaSExpiryJob>();


            services.AddScoped<GymCrest.Application.BackgroundJobs.MemberAutoUnfreezeJob>();

            return services;
        }
    }
}
