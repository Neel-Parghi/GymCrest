import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { GymOwnerStats } from '../../../core/models/dashboard.model';
import { GymOwnerDashboardService } from '../../../core/services/gym-owner-dashboard.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CONSTANTS } from '../../../core/constants/constants';
import { Router, RouterLink } from '@angular/router';
import { BranchContextService } from '../../../core/services/branch-context.service';
import { GymService } from '../../../core/services/gym.service';
import { GymListResponse } from '../../../shared/models/gym.model';
import { GymPlanService } from '../../../core/services/gym-plan.service';
import { PlatformStatusService } from '../../../core/services/platform-status.service';

@Component({
  selector: 'app-gym-owner-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(GymOwnerDashboardService);
  private gymService = inject(GymService);
  private notification = inject(NotificationService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private branchContextService = inject(BranchContextService);
  private planService = inject(GymPlanService);
  private platformStatus = inject(PlatformStatusService);

  stats: GymOwnerStats | null = null;
  gymData: GymListResponse | null = null;
  daysToExpiry: number | null = null;
  /** Days of access left after the plan ended (the platform grace period); null when not in grace. */
  graceDaysLeft: number | null = null;
  private gracePeriodDays = 0;
  isLoading = true;
  selectedDensityRange: 'today' | 'week' = 'today';
  todayDate = new Date();
  showDensityDropdown = false;
  plansCount: number = 0;
  isFirstTimeSetup = false;

  toggleDensityDropdown() {
    this.showDensityDropdown = !this.showDensityDropdown;
  }

  selectDensityRange(range: 'today' | 'week') {
    this.selectedDensityRange = range;
    this.showDensityDropdown = false;
  }

  ngOnInit(): void {
    this.platformStatus.getStatus().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(status => {
      this.gracePeriodDays = status.gracePeriodDays;
      this.calculateExpiryDays();
    });
    this.branchContextService.activeBranch$.pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => {
      this.loadStats();
      this.loadGymData();
      this.loadPlansCount();
    });
  }

  loadGymData(): void {
    this.gymService.getMyGym().subscribe({
      next: (res) => {
        if (res.data) {
          this.gymData = res.data;
          this.calculateExpiryDays();
        }
      }
    });
  }

  calculateExpiryDays(): void {
    if (this.gymData && this.gymData.subscriptionExpiry) {
      const expiryDate = new Date(this.gymData.subscriptionExpiry);
      const today = new Date();
      const diffTime = expiryDate.getTime() - today.getTime();
      this.daysToExpiry = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
      const left = this.daysToExpiry + this.gracePeriodDays;
      this.graceDaysLeft = this.daysToExpiry < 0 && left >= 0 ? left : null;
    }
  }

  loadStats(): void {
    this.isLoading = true;
    this.dashboardService.getStats().subscribe({
      next: (res: any) => {
        const data = res?.data || res;
        this.stats = data;
        this.checkSetupStatus();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading dashboard stats:', err);
        this.notification.error(CONSTANTS.DASHBOARD.LOAD_STATS_ERROR);
        this.isLoading = false;
      }
    });
  }

  getAttendancePercentage(): number {
    if (!this.stats) return 0;
    return Math.round((this.stats.todayAttendance / this.stats.activeMembers) * 100);
  }

  navigateToInventory(filter?: string): void {
    const queryParams: any = {};
    if (filter === 'lowStock') {
      queryParams.tab = 'inventory';
      queryParams.filter = 'lowStock';
    } else if (filter === 'inStock') {
      queryParams.tab = 'inventory';
      queryParams.filter = 'inStock';
    } else if (filter === 'maintenance') {
      queryParams.tab = 'equipment';
      queryParams.filter = 'maintenance';
    }
    this.router.navigate(['/gym-owner/inventory'], { queryParams });
  }

  navigateToUpgrade(): void {
    this.router.navigate(['/gym-owner/my-gyms'], { queryParams: { action: 'upgrade' } });
  }

  navigateToMembers(): void {
    this.router.navigate(['/gym-owner/members']);
  }

  formatTotalMembers(): string {
    const count = this.stats?.totalMembers || 0;
    if (count >= 1000) {
      return (count / 1000).toFixed(1) + 'k';
    }
    return count.toString();
  }

  getDistributionCircleDash(index: number): string {
    if (!this.stats || !this.stats.membershipDistribution || !this.stats.membershipDistribution[index]) {
      return '0 377';
    }
    const item = this.stats.membershipDistribution[index];
    const circumference = 2 * Math.PI * 60;
    const length = (item.percentage / 100) * circumference;
    const remaining = circumference - length;
    return `${length} ${remaining}`;
  }

  getDistributionCircleOffset(index: number): number {
    if (!this.stats || !this.stats.membershipDistribution) {
      return 0;
    }
    const circumference = 2 * Math.PI * 60;
    let offset = 0;
    for (let i = 0; i < index; i++) {
      const item = this.stats.membershipDistribution[i];
      offset += (item.percentage / 100) * circumference;
    }
    return -offset;
  }

  getBarChartData() {
    if (this.selectedDensityRange === 'week') {
      if (!this.stats || !this.stats.weeklyOccupancy || this.stats.weeklyOccupancy.length === 0) {
        return [
          { hour: 'Mon', occupancyCount: 0, isActivePeak: false },
          { hour: 'Tue', occupancyCount: 0, isActivePeak: false },
          { hour: 'Wed', occupancyCount: 0, isActivePeak: false },
          { hour: 'Thu', occupancyCount: 0, isActivePeak: false },
          { hour: 'Fri', occupancyCount: 0, isActivePeak: false },
          { hour: 'Sat', occupancyCount: 0, isActivePeak: false },
          { hour: 'Sun', occupancyCount: 0, isActivePeak: false }
        ];
      }
      const maxVal = Math.max(...this.stats.weeklyOccupancy.map(d => d.occupancyCount), 1);
      return this.stats.weeklyOccupancy.map(item => ({
        hour: item.day,
        occupancyCount: item.occupancyCount,
        isActivePeak: item.occupancyCount > 0 && item.occupancyCount === maxVal
      }));
    }

    if (!this.stats || !this.stats.hourlyOccupancy || this.stats.hourlyOccupancy.length === 0) {
      return [
        { hour: '6 AM', occupancyCount: 0, isActivePeak: false },
        { hour: '8 AM', occupancyCount: 0, isActivePeak: false },
        { hour: '10 AM', occupancyCount: 0, isActivePeak: false },
        { hour: '12 PM', occupancyCount: 0, isActivePeak: false },
        { hour: '2 PM', occupancyCount: 0, isActivePeak: false },
        { hour: '4 PM', occupancyCount: 0, isActivePeak: false },
        { hour: '6 PM', occupancyCount: 0, isActivePeak: false },
        { hour: '8 PM', occupancyCount: 0, isActivePeak: false },
        { hour: '10 PM', occupancyCount: 0, isActivePeak: false }
      ];
    }

    const maxVal = Math.max(...this.stats.hourlyOccupancy.map(d => d.occupancyCount), 1);
    return this.stats.hourlyOccupancy.map(item => ({
      hour: item.hour,
      occupancyCount: item.occupancyCount,
      isActivePeak: item.occupancyCount === maxVal
    }));
  }

  getBarHeight(count: number): number {
    const data = this.getBarChartData();
    const maxVal = Math.max(...data.map(d => d.occupancyCount), 1);
    const heightVal = (count / maxVal) * 120;
    return heightVal > 15 ? heightVal : 15;
  }

  getMemberTier(planName: string): string {
    if (!planName) return '1 MONTH';
    const name = planName.toLowerCase();
    if (name.includes('12') || name.includes('year') || name.includes('annual')) {
      return '12 MONTH';
    }
    if (name.includes('6') || name.includes('semi')) {
      return '6 MONTH';
    }
    return '1 MONTH';
  }

  loadPlansCount(): void {
    this.planService.getGymPlans().subscribe({
      next: (res) => {
        this.plansCount = res.data?.length || 0;
        this.checkSetupStatus();
      },
      error: (err) => {
        console.error('Error loading plans count:', err);
      }
    });
  }

  checkSetupStatus(): void {
    if (!this.stats) return;
    const hasPlans = this.plansCount > 0;
    const hasStaff = (this.stats.activeTrainers + this.stats.supportStaffCount) > 0;
    const hasMembers = this.stats.totalMembers > 0;
    this.isFirstTimeSetup = !hasPlans || !hasStaff || !hasMembers;
  }

  getSetupProgress(): number {
    let completed = 0;
    if (this.plansCount > 0) completed++;
    if (this.stats && (this.stats.activeTrainers + this.stats.supportStaffCount) > 0) completed++;
    if (this.stats && this.stats.totalMembers > 0) completed++;
    return Math.round((completed / 3) * 100);
  }

  isPlansSetup(): boolean {
    return this.plansCount > 0;
  }

  isStaffSetup(): boolean {
    return !!(this.stats && (this.stats.activeTrainers + this.stats.supportStaffCount) > 0);
  }

  isMembersSetup(): boolean {
    return !!(this.stats && this.stats.totalMembers > 0);
  }
}
