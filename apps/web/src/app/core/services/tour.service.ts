import { inject, Injectable } from '@angular/core';
import { driver, DriveStep } from 'driver.js';
import { AuthApiService } from './auth-api.service';

@Injectable({
  providedIn: 'root'
})
export class TourService {
  private authService = inject(AuthApiService);

  startMainTour() {
    const role = this.authService.getUserRole();
    let steps: DriveStep[] = [];

    if (role === 'Trainer') {
      steps = [
        { element: '#nav-workout-planner', popover: { title: '<i class="fa-solid fa-calendar-check"></i> Workout Planner', description: 'Create and customize daily training routines for your clients.', side: 'right', align: 'start' } },
        { element: '#nav-diet-planner', popover: { title: '<i class="fa-solid fa-apple-whole"></i> Diet Planner', description: 'Add custom meals and nutrition macros for your clients.', side: 'right', align: 'start' } },
        { element: '#nav-pt-members', popover: { title: '<i class="fa-solid fa-users"></i> PT Members', description: 'Manage your clients and assign plans to them.', side: 'right', align: 'start' } },
        { element: '#nav-dashboard', popover: { title: '<i class="fa-solid fa-house"></i> Dashboard', description: 'Monitor client gains and statistics from the dashboard.', side: 'right', align: 'start' } }
      ];
    } else if (role === 'GymOwner') {
      steps = [
        { element: '#nav-members', popover: { title: '<i class="fa-solid fa-users"></i> Member Management', description: 'Manage all gym members and their subscriptions.', side: 'right', align: 'start' } },
        { element: '#nav-gym-plans', popover: { title: '<i class="fa-solid fa-receipt"></i> Gym Plans', description: 'Create subscription plans and memberships for your gym.', side: 'right', align: 'start' } },
        { element: '#nav-billing-and-invoices', popover: { title: '<i class="fa-solid fa-file-invoice-dollar"></i> Finances', description: 'Track gym revenue, bills, and payouts.', side: 'right', align: 'start' } },
        { element: '#nav-dashboard', popover: { title: '<i class="fa-solid fa-house"></i> Dashboard', description: 'View high-level statistics and financial summaries.', side: 'right', align: 'start' } }
      ];
    } else if (role === 'Staff') {
      steps = [
        { element: '#nav-members', popover: { title: '<i class="fa-solid fa-users"></i> Member Management', description: 'Manage gym members and record attendance.', side: 'right', align: 'start' } },
        { element: '#nav-attendance', popover: { title: '<i class="fa-solid fa-calendar-check"></i> Attendance', description: 'Track member attendance and gym visits.', side: 'right', align: 'start' } },
        { element: '#nav-inventory', popover: { title: '<i class="fa-solid fa-boxes-stacked"></i> Inventory', description: 'Manage gym equipment and supplements.', side: 'right', align: 'start' } },
        { element: '#nav-dashboard', popover: { title: '<i class="fa-solid fa-house"></i> Dashboard', description: 'View your staff dashboard overview.', side: 'right', align: 'start' } }
      ];
    } else {
      // Regular User
      steps = [
        { element: '#nav-workout-planner', popover: { title: '<i class="fa-solid fa-calendar-check"></i> Workout Planner', description: 'Create and customize your daily training routines.', side: 'right', align: 'start' } },
        { element: '#nav-diet-planner', popover: { title: '<i class="fa-solid fa-utensils"></i> Diet Planner', description: 'Log meals and track your nutrition macros.', side: 'right', align: 'start' } },
        { element: '#nav-track-performance', popover: { title: '<i class="fa-solid fa-chart-line"></i> Track Performance', description: 'Monitor your gains and statistics over time.', side: 'right', align: 'start' } },
        { element: '#nav-dashboard', popover: { title: '<i class="fa-solid fa-house"></i> Dashboard', description: 'Get a quick summary of your daily progress.', side: 'right', align: 'start' } }
      ];
    }

    const driverObj = driver({
      showProgress: true,
      animate: true,
      popoverClass: 'gymcrest-tour-popover',
      popoverOffset: 40,
      steps: steps
    });

    driverObj.drive();
  }
}
