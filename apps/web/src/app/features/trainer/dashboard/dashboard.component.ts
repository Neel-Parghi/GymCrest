import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { StaffService } from '../../../core/services/staff.service';
import { BillingService } from '../../../core/services/billing.service';
import { ToastrService } from 'ngx-toastr';
import { dayPart, greetingFor } from '../../../shared/utils/greeting';

@Component({
  selector: 'app-trainer-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class TrainerDashboardComponent implements OnInit {
  private authService = inject(AuthApiService);
  private staffService = inject(StaffService);
  private billingService = inject(BillingService);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  trainerName = '';
  greeting = 'Welcome back';
  greetingIcon = 'fa-sun';
  greetingTheme = 'theme-morning';
  todayDate = new Date();
  activeClientsCount = 0;
  completedSessions = 0;
  totalSessions = 0;
  isCheckedIn = false;
  userId = '';

  stats = [
    { title: 'Active Clients', value: '0', icon: 'fa-users', color: 'blue' },
    { title: 'Today\'s Sessions', value: '0 / 0', icon: 'fa-calendar-check', color: 'green' },
    { title: 'Hours Logged', value: '0.0 hrs', icon: 'fa-clock', color: 'purple' },
    { title: 'PT Commissions', value: '₹0', icon: 'fa-indian-rupee-sign', color: 'amber' }
  ];

  todaySessions: any[] = [];

  ngOnInit(): void {
    const part = dayPart();
    this.greeting = greetingFor();
    this.greetingIcon = { morning: 'fa-sun', afternoon: 'fa-cloud-sun', evening: 'fa-moon' }[part];
    this.greetingTheme = `theme-${part}`;

    this.authService.userProfile$.subscribe(profile => {
      if (profile) {
        this.userId = profile.id;
        this.trainerName = `${profile.firstName} ${profile.lastName}`;
        this.loadTrainerData(profile.id);
        this.loadStaffStatus();
        this.loadHoursLogged();
        this.loadCommissions();
      }
    });
  }

  loadStaffStatus(): void {
    this.staffService.getStaffById(this.userId).subscribe({
      next: (res: any) => {
        if (res?.data) {
          this.isCheckedIn = res.data.isCheckedIn;
        }
      },
      error: (err) => console.error('Error loading staff status:', err)
    });
  }

  loadHoursLogged(): void {
    this.staffService.getStaffAttendanceLogs({ pageNumber: 1, pageSize: 50 }).subscribe({
      next: (res: any) => {
        if (res?.data?.items) {
          const total = res.data.items
            .filter((x: any) => x.hoursWorked != null)
            .reduce((sum: number, x: any) => sum + x.hoursWorked, 0);
          this.stats[2].value = `${total.toFixed(1)} hrs`;
        }
      },
      error: (err) => console.error('Error loading attendance logs:', err)
    });
  }

  toggleClock(): void {
    if (this.isCheckedIn) {
      this.staffService.checkOutStaff(this.userId).subscribe({
        next: () => {
          this.isCheckedIn = false;
          this.loadHoursLogged();
        },
        error: (err) => console.error(err)
      });
    } else {
      this.staffService.checkInStaff(this.userId).subscribe({
        next: () => {
          this.isCheckedIn = true;
          this.loadHoursLogged();
        },
        error: (err) => console.error(err)
      });
    }
  }

  loadTrainerData(trainerId: string): void {
    this.staffService.getAssignedMembers(trainerId).subscribe({
      next: (res: any) => {
        const list = res?.data || [];
        this.activeClientsCount = list.filter((x: any) => x.isActive || x.status === 'Active').length;
        this.stats[0].value = this.activeClientsCount.toString();

        // Map todaySessions
        this.todaySessions = list.map((member: any) => {
          return {
            time: member.assignedSlot || 'Flexible',
            memberName: `${member.firstName} ${member.lastName}`,
            planName: member.activePlanName || 'Personal Training',
            status: 'Upcoming', // Defaulting to upcoming, as real status tracking for sessions might require a workout log API
            initials: (member.firstName?.[0] || '') + (member.lastName?.[0] || '')
          };
        });

        // Simple sort to put flexible at the end, and sort times
        this.todaySessions.sort((a, b) => {
          if (a.time === 'Flexible') return 1;
          if (b.time === 'Flexible') return -1;
          return a.time.localeCompare(b.time);
        });

        this.totalSessions = this.todaySessions.length;
        this.completedSessions = 0; // Without real workout log tracking, assume 0 for now
        this.stats[1].value = `${this.completedSessions} / ${this.totalSessions}`;
      },
      error: (err) => {
        console.error('Error loading trainer assigned members:', err);
      }
    });
  }

  loadCommissions(): void {
    const today = new Date();
    const monthKey = `${today.getFullYear()}-${(today.getMonth() + 1).toString().padStart(2, '0')}`;

    this.billingService.getStaffPayrollOverview(monthKey).subscribe({
      next: (res: any) => {
        if (res?.data?.payouts && res.data.payouts.length > 0) {
          const payout = res.data.payouts[0];
          const formatter = new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 });
          this.stats[3].value = formatter.format(payout.netEarnings || 0);
        }
      },
      error: (err) => console.error('Error loading commissions:', err)
    });
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }

  completeSession(session: any, event: Event): void {
    event.stopPropagation();
    if (session.status === 'Completed') return;

    session.status = 'Completed';
    this.completedSessions++;
    this.stats[1].value = `${this.completedSessions} / ${this.totalSessions}`;
    this.toastr.success(`Marked session with ${session.memberName} as completed!`, 'Session Completed');
  }
}
