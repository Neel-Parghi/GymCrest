using GymForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;


namespace GymForge.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }


        public DbSet<User> Users { get; set; }
        
        public DbSet<UserProfile> UserProfiles { get; set; }
        
        public DbSet<UserPreference> UserPreferences { get; set; }
        
        public DbSet<DailyRoutine> DailyRoutines { get; set; }
        
        public DbSet<DailyRoutineCompletion> DailyRoutineCompletions { get; set; }

        public DbSet<UserSecurity> UserSecurities { get; set; }

        public DbSet<Gym> Gyms { get; set; }

        public DbSet<GymConfiguration> GymConfigurations { get; set; }

        public DbSet<Address> Addresses { get; set; }

        public DbSet<Plan> Plans { get; set; }
        
        public DbSet<Branch> Branches { get; set; }
        
        public DbSet<SubscriptionRecord> SubscriptionRecords { get; set; }
        
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<SaaSPaymentTransaction> SaaSPaymentTransactions { get; set; }
        
        public DbSet<SaaSConfiguration> SaaSConfigurations { get; set; }
        
        public DbSet<GymPlan> GymPlans { get; set; }
        
        public DbSet<GymMember> GymMembers { get; set; }
        
        public DbSet<MemberSubscription> MemberSubscriptions { get; set; }

        public DbSet<Staff> Staff { get; set; }
        
        public DbSet<PTAssignment> PTAssignments { get; set; }
        
        public DbSet<MemberMeasurement> MemberMeasurements { get; set; }
        
        public DbSet<InventoryItem> InventoryItems { get; set; }
        
        public DbSet<Equipment> Equipment { get; set; }

        public DbSet<DietLog> DietLogs { get; set; }
       
        public DbSet<MealLogEntry> MealLogEntries { get; set; }
        
        public DbSet<MaintenanceLog> MaintenanceLogs { get; set; }
        
        public DbSet<SaleTransaction> SaleTransactions { get; set; }

        public DbSet<AttendanceLog> AttendanceLogs { get; set; }

        public DbSet<StaffPayrollRule> StaffPayrollRules { get; set; }

        public DbSet<StaffPayoutLog> StaffPayoutLogs { get; set; }

        public DbSet<CustomInvoice> CustomInvoices { get; set; }

        public DbSet<PaymentRecord> PaymentRecords { get; set; }

        public DbSet<StaffAttendanceLog> StaffAttendanceLogs { get; set; }

        public DbSet<GymHoliday> GymHolidays { get; set; }

        public DbSet<GymAnnouncement> GymAnnouncements { get; set; }
        
        public DbSet<AnnouncementTemplate> AnnouncementTemplates { get; set; }
        
        public DbSet<UserNotification> UserNotifications { get; set; }

        public DbSet<Exercise> Exercises { get; set; }

        public DbSet<MasterExercise> MasterExercises { get; set; }

        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }

        public DbSet<WorkoutPlanDay> WorkoutPlanDays { get; set; }

        public DbSet<WorkoutPlanExercise> WorkoutPlanExercises { get; set; }
        
        public DbSet<WorkoutSessionLog> WorkoutSessionLogs { get; set; }
        
        public DbSet<LoggedExercise> LoggedExercises { get; set; }
        
        public DbSet<LoggedSet> LoggedSets { get; set; }
        
        public DbSet<MemberPlanAssignment> MemberPlanAssignments { get; set; }
       
        public DbSet<MemberWorkoutScheduleDay> MemberWorkoutScheduleDays { get; set; }
        
        public DbSet<DietPlan> DietPlans { get; set; }
        
        public DbSet<DietPlanMeal> DietPlanMeals { get; set; }

        public DbSet<MemberDietAssignment> MemberDietAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ValueConverter<DateTime, DateTime> utcConverter = new(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v.ToUniversalTime(), DateTimeKind.Utc));

            ValueConverter<DateTime?, DateTime?> nullableUtcConverter = new(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value.ToUniversalTime(), DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(utcConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableUtcConverter);
                    }
                }
            }

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasOne(rt => rt.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(rt => rt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(rt => rt.Token);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasOne(u => u.Profile)
                      .WithOne(p => p.User)
                      .HasForeignKey<UserProfile>(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(u => u.Preference)
                      .WithOne(p => p.User)
                      .HasForeignKey<UserPreference>(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(u => u.Security)
                      .WithOne(s => s.User)
                      .HasForeignKey<UserSecurity>(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Gym>(entity =>
            {
                entity.HasOne(g => g.Configuration)
                      .WithOne(c => c.Gym)
                      .HasForeignKey<GymConfiguration>(c => c.GymId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DietPlanMeal>()
                .Property(m => m.Protein)
                .HasColumnType("decimal(18,2)");
                
            modelBuilder.Entity<DietLog>()
                .Property(d => d.TargetProtein).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<DietLog>()
                .Property(d => d.TargetCarbs).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<DietLog>()
                .Property(d => d.TargetFats).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<DietLog>()
                .Property(d => d.TotalProtein).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<DietLog>()
                .Property(d => d.TotalCarbs).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<DietLog>()
                .Property(d => d.TotalFats).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MealLogEntry>()
                .Property(m => m.Protein).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MealLogEntry>()
                .Property(m => m.Carbs).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MealLogEntry>()
                .Property(m => m.Fats).HasColumnType("decimal(18,2)");
                
            modelBuilder.Entity<MealLogEntry>()
                .HasOne(m => m.DietLog)
                .WithMany(d => d.MealEntries)
                .HasForeignKey(m => m.DietLogId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Plan>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<GymPlan>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<GymPlan>()
                .Property(p => p.DiscountedPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SubscriptionRecord>()
                .Property(x => x.PriceAtPurchase)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaaSPaymentTransaction>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaaSConfiguration>(entity =>
            {
                entity.Property(x => x.TaxPercentage).HasPrecision(5, 2);
                entity.Property(x => x.YearlyRevenueTarget)
                      .HasColumnName("MonthlyRevenueTarget")
                      .HasPrecision(18, 2);
                entity.Property(x => x.SubscriptionTarget);
                entity.Property(x => x.UptimeThreshold).HasPrecision(5, 2);
            });

            modelBuilder.Entity<PTAssignment>(entity =>
            {
                entity.HasOne(pt => pt.Trainer)
                      .WithMany(s => s.PTAssignments)
                      .HasForeignKey(pt => pt.TrainerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pt => pt.Member)
                      .WithMany()
                      .HasForeignKey(pt => pt.MemberId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MemberMeasurement>(entity =>
            {
                entity.HasOne(mm => mm.Member)
                      .WithMany(m => m.Measurements)
                      .HasForeignKey(mm => mm.MemberId);

                entity.HasOne(mm => mm.RecordedBy)
                      .WithMany()
                      .HasForeignKey(mm => mm.RecordedById)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(mm => mm.User)
                      .WithMany()
                      .HasForeignKey(mm => mm.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(mm => mm.Weight).HasPrecision(5, 2);
                entity.Property(mm => mm.Height).HasPrecision(5, 2);
                entity.Property(mm => mm.BodyFatPercentage).HasPrecision(5, 2);
                entity.Property(mm => mm.BMI).HasPrecision(5, 2);
            });

            // Performance Indexes for Multi-tenancy
            modelBuilder.Entity<InventoryItem>().HasIndex(x => x.GymId);
            modelBuilder.Entity<Equipment>().HasIndex(x => x.GymId);
            modelBuilder.Entity<SaleTransaction>().HasIndex(x => x.GymId);
            modelBuilder.Entity<GymMember>().HasIndex(x => x.GymId);
            modelBuilder.Entity<Staff>().HasIndex(x => x.GymId);
            modelBuilder.Entity<StaffPayrollRule>().HasIndex(x => x.GymId);
            modelBuilder.Entity<StaffPayoutLog>().HasIndex(x => x.GymId);

            // Exercise indexes
            modelBuilder.Entity<Exercise>(entity =>
            {
                entity.HasIndex(x => x.Category);
                entity.HasIndex(x => x.Equipment);
                entity.HasIndex(x => x.Slug).IsUnique();
            });

            // MasterExercise indexes
            modelBuilder.Entity<MasterExercise>(entity =>
            {
                entity.HasIndex(x => x.Category);
                entity.HasIndex(x => x.Equipment);
                entity.HasIndex(x => x.Slug).IsUnique();
            });

            // Precision for payroll entities
            modelBuilder.Entity<StaffPayrollRule>()
                .Property(x => x.BaseSalary)
                .HasPrecision(18, 2);
            modelBuilder.Entity<StaffPayrollRule>()
                .Property(x => x.PTCommissionRate)
                .HasPrecision(5, 2);
            modelBuilder.Entity<StaffPayrollRule>()
                .Property(x => x.RehabCommissionRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<StaffPayoutLog>()
                .Property(x => x.BaseSalarySnapshot)
                .HasPrecision(18, 2);
            modelBuilder.Entity<StaffPayoutLog>()
                .Property(x => x.Commissions)
                .HasPrecision(18, 2);
            modelBuilder.Entity<StaffPayoutLog>()
                .Property(x => x.TotalPayout)
                .HasPrecision(18, 2);

            // Precision for member billing/payment ledger entities
            modelBuilder.Entity<MemberSubscription>()
                .Property(x => x.PricePaid)
                .HasPrecision(18, 2);
            modelBuilder.Entity<MemberSubscription>()
                .Property(x => x.AmountPaid)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PaymentRecord>(entity =>
            {
                entity.HasIndex(x => x.GymId);
                entity.HasIndex(x => new { x.SourceType, x.SourceId });
            });

            // AttendanceLog configuration
            modelBuilder.Entity<AttendanceLog>(entity =>
            {
                entity.HasOne(a => a.Member)
                      .WithMany()
                      .HasForeignKey(a => a.MemberId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Branch)
                      .WithMany()
                      .HasForeignKey(a => a.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(a => a.VerificationMethod)
                      .HasMaxLength(50)
                      .IsRequired();

                // Indexes for fast live-occupancy and log queries
                entity.HasIndex(a => new { a.BranchId, a.CheckOutTime });
                entity.HasIndex(a => a.MemberId);
                entity.HasIndex(a => a.CheckInTime);
            });

            // StaffAttendanceLog configuration
            modelBuilder.Entity<StaffAttendanceLog>(entity =>
            {
                entity.HasOne(a => a.Staff)
                      .WithMany()
                      .HasForeignKey(a => a.StaffId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Branch)
                      .WithMany()
                      .HasForeignKey(a => a.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Gym)
                      .WithMany()
                      .HasForeignKey(a => a.GymId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(a => a.GymId);
                entity.HasIndex(a => a.StaffId);
                entity.HasIndex(a => a.CheckInTime);
            });

            modelBuilder.Entity<GymHoliday>(entity =>
            {
                entity.HasIndex(x => x.GymId);
                entity.HasIndex(x => x.BranchId);
            });

            modelBuilder.Entity<GymAnnouncement>(entity =>
            {
                entity.HasIndex(x => x.GymId);
                entity.HasIndex(x => x.BranchId);
            });

            modelBuilder.Entity<AnnouncementTemplate>(entity =>
            {
                entity.HasIndex(x => x.GymId);
                entity.HasIndex(x => x.BranchId);
            });

            modelBuilder.Entity<UserNotification>(entity =>
            {
                entity.HasIndex(x => x.GymId);
                entity.HasIndex(x => x.BranchId);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => new { x.UserId, x.IsRead });

                entity.HasOne(un => un.User)
                      .WithMany()
                      .HasForeignKey(un => un.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // WorkoutPlan configurations
            modelBuilder.Entity<WorkoutPlan>(entity =>
            {
                entity.HasIndex(x => x.GymId);
                entity.HasIndex(x => new { x.CreatedBy, x.Type, x.IsDeleted });
                entity.HasQueryFilter(p => !p.IsDeleted);
                
                entity.HasMany(wp => wp.Days)
                      .WithOne(d => d.WorkoutPlan)
                      .HasForeignKey(d => d.WorkoutPlanId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WorkoutPlanDay>(entity =>
            {
                entity.HasIndex(x => x.WorkoutPlanId);
                
                entity.HasMany(d => d.Exercises)
                      .WithOne(e => e.WorkoutPlanDay)
                      .HasForeignKey(e => e.WorkoutPlanDayId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WorkoutPlanExercise>(entity =>
            {
                entity.HasIndex(x => x.WorkoutPlanDayId);
                entity.HasIndex(x => x.ExerciseId);

                entity.HasOne(e => e.Exercise)
                      .WithMany()
                      .HasForeignKey(e => e.ExerciseId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkoutSessionLog>(entity =>
            {
                entity.HasOne(l => l.Member)
                      .WithMany()
                      .HasForeignKey(l => l.MemberId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(l => l.User)
                      .WithMany()
                      .HasForeignKey(l => l.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(l => l.LoggedExercises)
                      .WithOne(le => le.WorkoutSessionLog)
                      .HasForeignKey(le => le.WorkoutSessionLogId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LoggedExercise>(entity =>
            {
                entity.HasMany(le => le.LoggedSets)
                      .WithOne(ls => ls.LoggedExercise)
                      .HasForeignKey(ls => ls.LoggedExerciseId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MemberPlanAssignment>(entity =>
            {
                entity.HasOne(mpa => mpa.Member)
                      .WithMany()
                      .HasForeignKey(mpa => mpa.MemberId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mpa => mpa.User)
                      .WithMany()
                      .HasForeignKey(mpa => mpa.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mpa => mpa.WorkoutPlan)
                      .WithMany()
                      .HasForeignKey(mpa => mpa.WorkoutPlanId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MemberWorkoutScheduleDay>(entity =>
            {
                entity.HasIndex(x => new { x.MemberPlanAssignmentId, x.DayOfWeek }).IsUnique();

                entity.HasOne(x => x.MemberPlanAssignment)
                      .WithMany(x => x.CustomScheduleDays)
                      .HasForeignKey(x => x.MemberPlanAssignmentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.WorkoutPlanDay)
                      .WithMany()
                      .HasForeignKey(x => x.WorkoutPlanDayId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed default settings
            modelBuilder.Entity<SaaSConfiguration>().HasData(new SaaSConfiguration
            {
                Id = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D"),
                PlatformName = "GymCrest",
                BillingEmail = "admin@gymcrest.com",
                TaxPercentage = 18.0m,
                GracePeriodDays = 7,
                Currency = "INR",
                CreatedOn = new DateTime(2026, 4, 25, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = Guid.Empty
            });

            // DietPlan configurations
            modelBuilder.Entity<DietPlan>(entity =>
            {
                entity.HasIndex(x => x.GymId);
                entity.HasIndex(x => new { x.CreatedBy, x.IsDeleted });
                entity.HasQueryFilter(p => !p.IsDeleted);
                
                entity.HasMany(wp => wp.Meals)
                      .WithOne(d => d.DietPlan)
                      .HasForeignKey(d => d.DietPlanId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DietPlanMeal>(entity =>
            {
                entity.HasIndex(x => x.DietPlanId);
            });

            modelBuilder.Entity<MemberDietAssignment>(entity =>
            {
                entity.HasIndex(x => x.MemberId);
                entity.HasIndex(x => new { x.MemberId, x.IsActive });

                entity.HasOne(mda => mda.Member)
                      .WithMany()
                      .HasForeignKey(mda => mda.MemberId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mda => mda.User)
                      .WithMany()
                      .HasForeignKey(mda => mda.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mda => mda.DietPlan)
                      .WithMany()
                      .HasForeignKey(mda => mda.DietPlanId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // DailyRoutine configuration
            modelBuilder.Entity<DailyRoutine>(entity =>
            {
                entity.HasIndex(x => x.UserId);
                entity.HasOne(dr => dr.User)
                      .WithMany()
                      .HasForeignKey(dr => dr.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // DailyRoutineCompletion configuration
            modelBuilder.Entity<DailyRoutineCompletion>(entity =>
            {
                entity.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
                entity.HasOne(drc => drc.User)
                      .WithMany()
                      .HasForeignKey(drc => drc.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(x => x.CompletedRoutineIds)
                      .HasConversion(
                          v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                          v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<Guid>()
                      );
            });
        }
    }
}
