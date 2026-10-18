import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { AcceptInvitationComponent } from './features/auth/accept-invitation/accept-invitation.component';
import { ForgotPasswordComponent } from './features/auth/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './features/auth/reset-password/reset-password.component';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { DashboardComponent } from './features/super-admin/dashboard/dashboard.component';
import { GymOwners } from './features/super-admin/gym-management/gym-owners/gym-owners.component';
import { GymList } from './features/super-admin/gym-management/gym-list/gym-list.component';
import { PricingList } from './features/super-admin/pricing/pricing-list/pricing-list.component';
import { authGuard } from './core/guards/auth-guard';
import { loggedInGuard } from './core/guards/logged-in.guard';
import { subscriptionGuard } from './core/guards/subscription.guard';
import { roleGuard } from './core/guards/role-guard';
import { onboardingGuard } from './core/guards/onboarding.guard';


export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    // Public website; signed-in visitors go straight to their dashboard
    loadComponent: () => import('./features/landing/landing.component').then(m => m.LandingComponent),
    canActivate: [loggedInGuard]
  },
  { path: 'login', component: LoginComponent, canActivate: [loggedInGuard] },
  { path: 'register', component: RegisterComponent, canActivate: [loggedInGuard] },
  { path: 'accept-invitation', component: AcceptInvitationComponent, canActivate: [loggedInGuard] },
  { path: 'forgot-password', component: ForgotPasswordComponent, canActivate: [loggedInGuard] },
  { path: 'reset-password', component: ResetPasswordComponent },

  {
    path: 'onboarding',
    loadChildren: () => import('./features/onboarding/onboarding.routes').then(m => m.ONBOARDING_ROUTES),
    canActivate: [authGuard, onboardingGuard]
  },
  {
    path: 'super-admin',
    component: MainLayoutComponent,
    canActivate: [authGuard, onboardingGuard],
    children: [
      {
        path: 'users',
        loadComponent: () => import('./features/super-admin/users/users-list/users-list.component').then(m => m.UsersListComponent)
      },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'gym-owners', component: GymOwners },
      { path: 'gym-list', component: GymList },
      { path: 'pricing', component: PricingList },
      {
        path: 'payments',
        loadComponent: () => import('./features/super-admin/payments/payments.component').then(m => m.PaymentsComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/super-admin/settings/settings.component').then(m => m.SettingsComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/super-admin/profile/profile.component').then(m => m.ProfileComponent)
      }
    ]
  },
  {
    path: 'gym-owner',
    component: MainLayoutComponent,
    canActivate: [authGuard, onboardingGuard, subscriptionGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/gym-owner/dashboard/dashboard.component').then(m => m.DashboardComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/super-admin/profile/profile.component').then(m => m.ProfileComponent)
      },
      {
        path: 'my-gyms',
        loadComponent: () => import('./features/gym-owner/my-gyms/my-gyms.component').then(m => m.MyGymsComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'plans',
        loadComponent: () => import('./features/gym-owner/gym-plans/gym-plans.component').then(m => m.GymPlansComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'members',
        loadComponent: () => import('./features/gym-owner/members/members-list.component').then(m => m.MembersListComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'staff',
        loadComponent: () => import('./features/gym-owner/staff/staff-list.component').then(m => m.StaffListComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'attendance',
        loadComponent: () => import('./features/gym-owner/attendance/attendance.component').then(m => m.AttendanceComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'inventory',
        loadComponent: () => import('./features/gym-owner/inventory/inventory.component').then(m => m.InventoryComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'billing',
        loadComponent: () => import('./features/gym-owner/billing/billing.component').then(m => m.BillingComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/gym-owner/settings/settings.component').then(m => m.SettingsComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'announcements',
        loadComponent: () => import('./features/gym-owner/announcements/announcements.component').then(m => m.AnnouncementsComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'announcements/templates',
        loadComponent: () => import('./features/gym-owner/announcements/templates/templates.component').then(m => m.TemplatesComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'trainers/:trainerId/members',
        loadComponent: () => import('./features/trainer/members/members.component').then(m => m.PTMembersTrackComponent)
      },
      {
        path: 'trainers/:trainerId/members/:memberId',
        loadComponent: () => import('./features/trainer/member-detail/member-detail.component').then(m => m.PTMemberDetailComponent)
      },
      {
        path: 'resources/workout-library',
        loadComponent: () => import('./features/gym-owner/resources/workout-library/workout-library.component').then(m => m.GymOwnerWorkoutLibraryComponent),
        canActivate: [roleGuard]
      },
      {
        path: 'resources/diet-library',
        loadComponent: () => import('./features/gym-owner/resources/diet-library/diet-library.component').then(m => m.GymOwnerDietLibraryComponent),
        canActivate: [roleGuard]
      }
    ]
  },
  {
    path: 'trainer',
    component: MainLayoutComponent,
    canActivate: [authGuard, onboardingGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/trainer/dashboard/dashboard.component').then(m => m.TrainerDashboardComponent)
      },
      {
        path: 'members',
        loadComponent: () => import('./features/trainer/members/members.component').then(m => m.PTMembersTrackComponent)
      },
      {
        path: 'members/:memberId',
        loadComponent: () => import('./features/trainer/member-detail/member-detail.component').then(m => m.PTMemberDetailComponent)
      },
      {
        path: 'workout-planner',
        loadComponent: () => import('./features/trainer/workout-planner/workout-planner.component').then(m => m.WorkoutPlannerComponent)
      },
      {
        path: 'diet-planner',
        loadComponent: () => import('./features/trainer/diet-planner/diet-planner.component').then(m => m.PTDietPlannerComponent)
      },

      {
        path: 'settings',
        loadComponent: () => import('./features/trainer/trainer-settings/trainer-settings.component').then(m => m.TrainerSettingsComponent)
      },
      {
        path: 'attendance',
        loadComponent: () => import('./features/trainer/trainer-attendance/trainer-attendance.component').then(m => m.TrainerAttendanceComponent)
      },
      {
        path: 'billing',
        loadComponent: () => import('./features/trainer/trainer-billing/trainer-billing.component').then(m => m.TrainerBillingComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/super-admin/profile/profile.component').then(m => m.ProfileComponent)
      }
    ]
  },
  {
    path: 'user',
    component: MainLayoutComponent,
    canActivate: [authGuard, onboardingGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/user/user-dashboard/user-dashboard.component').then(m => m.UserDashboardComponent)
      },
      {
        path: 'performance',
        loadComponent: () => import('./features/user/user-performance/user-performance.component').then(m => m.UserPerformanceComponent)
      },
      {
        path: 'workout-calendar',
        loadComponent: () => import('./features/user/user-workout-calendar/user-workout-calendar.component').then(m => m.UserWorkoutCalendarComponent)
      },
      {
        path: 'exercise-progress',
        loadComponent: () => import('./features/user/user-exercise-progress/user-exercise-progress.component').then(m => m.UserExerciseProgressComponent)
      },
      {
        path: 'exercise-progress/breakdown',
        loadComponent: () => import('./features/user/user-exercise-progress-overview/user-exercise-progress-overview.component').then(m => m.UserExerciseProgressOverviewComponent)
      },
      {
        path: 'exercise-progress/breakdown/:exerciseName',
        loadComponent: () => import('./features/user/user-exercise-progress-detail/user-exercise-progress-detail.component').then(m => m.UserExerciseProgressDetailComponent)
      },
      {
        path: 'workout-planner',
        loadComponent: () => import('./features/user/user-workout-planner/user-workout-planner.component').then(m => m.UserWorkoutPlannerComponent)
      },
      {
        path: 'diet-planner',
        loadComponent: () => import('./features/user/user-diet-planner/user-diet-planner.component').then(m => m.UserDietPlannerComponent)
      },
      {
        path: 'health-tracker',
        loadComponent: () => import('./features/user/user-health-tracker/user-health-tracker.component').then(m => m.UserHealthTrackerComponent)
      },
      {
        path: 'diet-tracker',
        loadComponent: () => import('./features/user/user-diet-tracker/user-diet-tracker.component').then(m => m.UserDietTrackerComponent)
      },
      {
        path: 'winter-arc',
        loadChildren: () => import('./features/user/winter-arc/winter-arc.routes').then(m => m.WINTER_ARC_ROUTES)
      },
      {
        path: 'billing',
        loadComponent: () => import('./features/user/user-billing/user-billing.component').then(m => m.UserBillingComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/user/user-settings/user-settings.component').then(m => m.UserSettingsComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/user/user-account/user-account.component').then(m => m.UserAccountComponent)
      }
    ]
  },
  {
    path: 'subscription-expired',
    loadComponent: () => import('./features/gym-owner/subscription-expired/subscription-expired.component').then(m => m.SubscriptionExpiredComponent),
    canActivate: [authGuard]
  },
  {
    path: 'privacy-policy',
    loadComponent: () => import('./features/legal/privacy-policy/privacy-policy.component').then(m => m.PrivacyPolicyComponent)
  },
  {
    path: 'terms-of-service',
    loadComponent: () => import('./features/legal/terms-of-service/terms-of-service.component').then(m => m.TermsOfServiceComponent)
  },
  {
    path: '404',
    loadComponent: () => import('./features/errors/not-found/not-found.component').then(m => m.NotFoundComponent)
  },
  {
    path: '**',
    redirectTo: '404'
  }
];
