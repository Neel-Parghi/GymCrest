import { Component, OnInit, ViewChild, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { finalize } from 'rxjs';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MemberService } from '../../../core/services/member.service';
import { GymPlanService } from '../../../core/services/gym-plan.service';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { GymService } from '../../../core/services/gym.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmationService } from '../../../core/services/confirmation.service';
import { DataGrid, GridCellDirective } from '../../../shared/components/data-grid/data-grid.component';
import { AppGridConfig } from '../../../shared/constants/grid-config';
import { StaffService } from '../../../core/services/staff.service';
import { RouterLink } from '@angular/router';
import { BranchContextService } from '../../../core/services/branch-context.service';
import { OnboardMemberModal } from './onboard-member-modal/onboard-member-modal.component';
import { MemberDetailDrawer } from './member-detail-drawer/member-detail-drawer.component';
import { FreezeMemberModalComponent } from './freeze-member-modal/freeze-member-modal.component';
import { GymMember, MemberStatus, OnboardMemberRequest, RenewSubscriptionRequest } from '../../../shared/models/member.model';
import { GymPlan } from '../../../shared/models/gym-plan.model';
import { PaymentStatus } from '../../../shared/enums/member-enums';
import { DropdownComponent } from '../../../shared/components/dropdown/dropdown.component';
import { DropdownOption } from '../../../shared/models/dropdown.model';
import { CONSTANTS } from '../../../core/constants/constants';

@Component({
  selector: 'app-members-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, DataGrid, GridCellDirective, RouterLink, OnboardMemberModal, MemberDetailDrawer, FreezeMemberModalComponent, DropdownComponent],
  templateUrl: './members-list.component.html',
  styleUrl: './members-list.component.scss'
})
export class MembersListComponent implements OnInit {
  readonly limits = FIELD_LIMITS;

  private memberService = inject(MemberService);
  private gymPlanService = inject(GymPlanService);
  private authService = inject(AuthApiService);
  private gymService = inject(GymService);
  private notificationService = inject(NotificationService);
  private confirmationService = inject(ConfirmationService);
  private fb = inject(FormBuilder);
  private staffService = inject(StaffService);

  memberTrainerMap = new Map<string, { trainerId: string, trainerName: string }>();
  isLoadingTrainersMap = false;
  private destroyRef = inject(DestroyRef);
  private branchContextService = inject(BranchContextService);

  @ViewChild(OnboardMemberModal) onboardModal!: OnboardMemberModal;

  // State
  members: GymMember[] = [];
  filteredMembers: GymMember[] = [];
  activePlans: GymPlan[] = [];
  allPlans: GymPlan[] = [];
  branches: any[] = [];
  loading = false;
  gymId: string | null = null;
  gymOwnerId: string | null = null;
  totalItems = 0;

  // Overall dashboard stats counts
  dashboardTotalCount = 0;
  dashboardActiveCount = 0;
  dashboardFrozenCount = 0;
  dashboardExpiredCount = 0;

  // Mock dashboard widgets data
  atRiskMembers = [
    { name: 'John Doe', plan: '12 Month Premium', lastActive: '12 days ago', initials: 'JD', severity: 'critical' },
    { name: 'Sarah Smith', plan: '6 Month Plan', lastActive: '10 days ago', initials: 'SS', severity: 'critical' },
    { name: 'Amit Patel', plan: '1 Month Plan', lastActive: '14 days ago', initials: 'AP', severity: 'danger' },
    { name: 'Rohan Sharma', plan: '3 Month Plan', lastActive: '8 days ago', initials: 'RS', severity: 'warning' }
  ];

  funnelData = {
    expired: 45,
    reminded: 32,
    renewed: 21,
    conversionRate: 46.6
  };

  streakLeaderboard = [
    { name: 'Raj Kumar', streak: '28 Days', plan: '12 Month Plan', rank: 1, initials: 'RK' },
    { name: 'Sophia Loren', streak: '24 Days', plan: '6 Month Plan', rank: 2, initials: 'SL' },
    { name: 'Vikram Singh', streak: '21 Days', plan: '12 Month Plan', rank: 3, initials: 'VS' },
    { name: 'Elena Gilbert', streak: '19 Days', plan: '3 Month Plan', rank: 4, initials: 'EG' },
    { name: 'Dev Patel', streak: '18 Days', plan: '1 Month Plan', rank: 5, initials: 'DP' }
  ];


  // Filter Form
  filterForm!: FormGroup;

  // Pagination
  currentPage = 1;
  pageSize = 10;

  // DataGrid config
  gridConfig = AppGridConfig['GymMembers'];

  // Status filter
  activeFilter: MemberStatus | 'all' = 'all';
  selectedPlan: string = 'all';
  selectedPaymentStatus: string = 'all';
  // Filter options
  get planOptions(): DropdownOption[] {
    const options: DropdownOption[] = [{ label: 'All Plans', value: 'all' }];
    this.activePlans.forEach(p => options.push({ label: p.name, value: p.id }));
    return options;
  }

  readonly paymentOptions: DropdownOption[] = [
    { label: 'All Payments', value: 'all' },
    { label: 'Pending', value: '1' },
    { label: 'Paid', value: '2' },
    { label: 'Partial', value: '3' },
    { label: 'Refunded', value: '4' }
  ];

  // View Mode
  viewMode: 'list' | 'dashboard' = 'list';

  toggleViewMode(): void {
    this.viewMode = this.viewMode === 'list' ? 'dashboard' : 'list';
    this.currentPage = 1;
    this.loadMembers(false);
  }

  // Modal / Drawer state
  isOnboardOpen = false;
  isEditMode = false;
  isDrawerOpen = false;
  selectedMember: GymMember | null = null;
  showFreezeModal = false;
  memberIdPendingFreeze: string | null = null;

  // Stats
  get totalCount(): number {
    return this.viewMode === 'dashboard' ? this.dashboardTotalCount : this.totalItems;
  }
  get activeCount(): number {
    return this.viewMode === 'dashboard' ? this.dashboardActiveCount : this.members.filter(m => m.status === MemberStatus.Active).length;
  }
  get frozenCount(): number {
    return this.viewMode === 'dashboard' ? this.dashboardFrozenCount : this.members.filter(m => m.status === MemberStatus.Freeze).length;
  }
  get expiredCount(): number {
    return this.viewMode === 'dashboard' ? this.dashboardExpiredCount : this.members.filter(m => m.status === MemberStatus.Expired).length;
  }
  get totalPages(): number { return Math.ceil(this.filteredMembers.length / this.pageSize); }

  readonly filterTabs = [
    { label: 'All', value: 'all' as const },
    { label: 'Active', value: MemberStatus.Active as const },
    { label: 'Inactive', value: MemberStatus.Inactive as const },
    { label: 'Frozen', value: MemberStatus.Freeze as const },
    { label: 'Expired', value: MemberStatus.Expired as const },
  ];

  ngOnInit(): void {
    const user = this.authService.getUserProfile();
    this.gymId = user?.gymId || this.authService.getGymId();
    this.gymOwnerId = user?.id || null;

    this.initFilters();

    if (this.gymId) {
      this.loadPlans();
      this.loadTrainersAndAssignments();
      this.gymService.getMyBranches().subscribe({
        next: (res) => this.branches = res.data || [],
        error: () => { }
      });
    }

    this.branchContextService.activeBranch$.pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadMembers(false);
    });
  }

  private initFilters() {
    this.filterForm = this.fb.group({
      search: [''],
      plan: ['all'],
      payment: ['all']
    });

    this.filterForm.valueChanges.pipe(
      debounceTime(1000),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadMembers();
    });
  }

  // Data Loading
  loadMembers(refresh = false): void {
    if (!this.gymId) return;

    if (this.viewMode === 'dashboard') {
      this.loadDashboardData(refresh);
      return;
    }

    this.loading = true;
    const { search, plan, payment } = this.filterForm.value;
    const bypass = false;

    this.memberService.getGymMembers(this.currentPage, this.pageSize, search, this.activeFilter.toString(), plan, payment, refresh, bypass)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (res) => {
          this.totalItems = res.data.totalCount;
          this.members = res.data.items.map(m => ({
            ...m,
            statusLabel: this.getStatusLabel(m.status),
            currentSubscription: m.currentSubscription ? {
              ...m.currentSubscription,
              paymentStatusLabel: this.getPaymentStatusLabel(m.currentSubscription.paymentStatus)
            } : undefined
          }));
          this.mapTrainersToMembers();
        },
        error: () => this.notificationService.error(CONSTANTS.MEMBERS_MODULE.LOAD_ERROR)
      });
  }

  loadDashboardData(refresh = false): void {
    this.loading = true;
    this.memberService.getMemberDashboardData(refresh)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (res) => {
          if (res.data) {
            const data = res.data as any;
            this.atRiskMembers = data.atRiskMembers || [];
            this.funnelData = data.renewalFunnel || { expired: 0, reminded: 0, renewed: 0, conversionRate: 0 };
            this.streakLeaderboard = data.streakLeaderboard || [];
            this.dashboardTotalCount = data.totalCount || 0;
            this.dashboardActiveCount = data.activeCount || 0;
            this.dashboardFrozenCount = data.frozenCount || 0;
            this.dashboardExpiredCount = data.expiredCount || 0;
          }
        },
        error: () => { }
      });
  }

  loadPlans(): void {
    if (!this.gymOwnerId) return;
    this.gymPlanService.getGymPlans().subscribe({
      next: (plans) => {
        this.allPlans = plans.data || [];
        this.activePlans = this.allPlans.filter(p => p.isActive);
      },
      error: () => { }
    });
  }

  exportToCsv(): void {
    if (!this.gymId) return;
    this.memberService.exportMembers().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `GymCrest_Members_${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.notificationService.success(CONSTANTS.MEMBERS_MODULE.EXPORT_SUCCESS);
      },
      error: () => this.notificationService.error(CONSTANTS.MEMBERS_MODULE.EXPORT_ERROR)
    });
  }

  // Filtering
  applyLocalFilters(): void {
    // Local filtering is no longer needed as we're filtering on the backend
    // Just map members to filteredMembers directly
    this.filteredMembers = [...this.members];
  }

  setFilter(f: MemberStatus | 'all'): void {
    this.activeFilter = f;
    this.currentPage = 1;
    this.loadMembers();
  }

  // Grid events
  onAction(event: { action: string; row: GymMember }): void {
    if (event.action === 'view') {
      this.viewDetails(event.row.id);
    } else if (event.action === 'edit') {
      this.openEditModal(event.row.id, event.row);
    } else if (event.action === 'delete') {
      this.handleDeleteMember(event.row);
    }
  }

  handleDeleteMember(member: GymMember): void {
    this.confirmationService.confirm({
      title: CONSTANTS.MEMBERS_MODULE.DELETE_CONFIRM_TITLE,
      message: CONSTANTS.MEMBERS_MODULE.DELETE_CONFIRM_MSG.replace('{name}', `${member.firstName} ${member.lastName}`),
      confirmText: CONSTANTS.MEMBERS_MODULE.REMOVE_MEMBER,
      type: 'danger'
    }).then(confirmed => {
      if (confirmed) {
        this.memberService.deleteMember(member.id).subscribe({
          next: () => {
            this.notificationService.success(CONSTANTS.MEMBERS_MODULE.DELETE_SUCCESS);
            this.memberService.clearCache();
            this.loadMembers();
          },
          error: () => this.notificationService.error(CONSTANTS.MEMBERS_MODULE.DELETE_ERROR)
        });
      }
    });
  }

  onPageChange(page: number): void { this.currentPage = page; this.loadMembers(); }
  onPageSizeChange(size: number): void { this.pageSize = size; this.currentPage = 1; this.loadMembers(); }

  openOnboardModal(): void {
    this.isEditMode = false;
    this.selectedMember = null;
    this.isOnboardOpen = true;
  }

  openEditModal(memberId: string, memberData?: GymMember): void {
    this.isEditMode = true;
    this.isDrawerOpen = false;
    this.selectedMember = null;

    if (memberData) {
      this.selectedMember = { ...memberData };
      this.isOnboardOpen = true;
    } else {
      this.memberService.getMemberById(memberId).subscribe(m => {
        this.selectedMember = m.data;
        this.isOnboardOpen = true;
      });
    }
  }

  handleOnboard(payload: OnboardMemberRequest): void {
    if (!this.gymId) return;
    this.memberService.onboardMember(payload).subscribe({
      next: () => {
        this.notificationService.success(CONSTANTS.MEMBERS_MODULE.ONBOARD_SUCCESS);
        this.isOnboardOpen = false;
        this.memberService.clearCache();
        this.loadMembers();
      },
      error: (err) => {
        const msg = err?.error?.message ?? CONSTANTS.MEMBERS_MODULE.ONBOARD_ERROR;
        this.onboardModal?.setError(msg);
      }
    });
  }

  handleUpdate(payload: OnboardMemberRequest): void {
    if (!this.selectedMember) return;
    this.memberService.updateMember(this.selectedMember.id, payload).subscribe({
      next: () => {
        this.notificationService.success(CONSTANTS.MEMBERS_MODULE.UPDATE_SUCCESS);
        this.isOnboardOpen = false;
        this.memberService.clearCache();
        this.loadMembers();
      },
      error: (err) => {
        const msg = err?.error?.message ?? CONSTANTS.MEMBERS_MODULE.UPDATE_ERROR;
        this.onboardModal?.setError(msg);
      }
    });
  }

  // Detail Drawer
  viewDetails(memberId: string): void {
    this.selectedMember = null;
    this.memberService.getMemberById(memberId).subscribe({
      next: (m) => {
        if (m.success && m.data) {
          this.selectedMember = { ...m.data, statusLabel: this.getStatusLabel(m.data.status) };
          this.isDrawerOpen = true;
        } else {
          this.notificationService.error(CONSTANTS.MEMBERS_MODULE.DETAIL_LOAD_ERROR_PREFIX + m.message);
        }
      },
      error: (err) => {
        console.error('Error fetching member details:', err);
        this.notificationService.error(CONSTANTS.MEMBERS_MODULE.DETAIL_LOAD_ERROR);
      }
    });
  }

  handleFreeze(memberId: string): void {
    this.memberIdPendingFreeze = memberId;
    this.showFreezeModal = true;
  }

  closeFreezeModal(): void {
    this.showFreezeModal = false;
    this.memberIdPendingFreeze = null;
  }

  confirmFreeze(freezeUntil: Date): void {
    if (!this.memberIdPendingFreeze) return;
    const memberId = this.memberIdPendingFreeze;

    this.memberService.freezeMember(memberId, freezeUntil.toISOString()).subscribe({
      next: () => {
        this.notificationService.success(CONSTANTS.MEMBERS_MODULE.FREEZE_SUCCESS);
        this.memberService.clearCache();
        this.refreshDrawer(memberId);
        this.closeFreezeModal();
      },
      error: (err) => {
        const msg = err?.error?.message ?? CONSTANTS.MEMBERS_MODULE.FREEZE_ERROR;
        this.notificationService.error(msg);
      }
    });
  }

  handleUnfreeze(memberId: string): void {
    this.memberService.unfreezeMember(memberId).subscribe({
      next: () => {
        this.notificationService.success(CONSTANTS.MEMBERS_MODULE.UNFREEZE_SUCCESS);
        this.memberService.clearCache();
        this.refreshDrawer(memberId);
      },
      error: () => this.notificationService.error(CONSTANTS.MEMBERS_MODULE.UNFREEZE_ERROR)
    });
  }

  handleToggle(memberId: string): void {
    this.memberService.toggleMemberStatus(memberId).subscribe({
      next: () => {
        this.notificationService.success(CONSTANTS.MEMBERS_MODULE.STATUS_UPDATE_SUCCESS);
        this.memberService.clearCache();
        this.refreshDrawer(memberId);
      },
      error: () => this.notificationService.error(CONSTANTS.MEMBERS_MODULE.STATUS_UPDATE_ERROR)
    });
  }

  handleRenew(event: { memberId: string; request: RenewSubscriptionRequest }): void {
    this.memberService.renewSubscription(event.memberId, event.request).subscribe({
      next: () => {
        this.notificationService.success(CONSTANTS.MEMBERS_MODULE.RENEW_SUCCESS);
        this.memberService.clearCache();
        this.refreshDrawer(event.memberId);
      },
      error: () => this.notificationService.error(CONSTANTS.MEMBERS_MODULE.RENEW_ERROR)
    });
  }

  private refreshDrawer(memberId: string): void {
    this.loadMembers();
    this.memberService.getMemberById(memberId).subscribe({
      next: (m) => this.selectedMember = { ...m.data, statusLabel: this.getStatusLabel(m.data.status) },
      error: () => { }
    });
  }

  // Helpers
  getStatusLabel(status: MemberStatus): string {
    switch (status) {
      case MemberStatus.Active: return 'Active';
      case MemberStatus.Inactive: return 'Inactive';
      case MemberStatus.Expired: return 'Expired';
      case MemberStatus.Freeze: return 'Frozen';
      default: return 'Unknown';
    }
  }

  getPaymentStatusLabel(status: PaymentStatus): string {
    switch (status) {
      case PaymentStatus.Paid: return 'Paid';
      case PaymentStatus.Pending: return 'Pending';
      case PaymentStatus.Partial: return 'Partial';
      case PaymentStatus.Refunded: return 'Refunded';
      default: return 'Unpaid';
    }
  }

  sendNotification(member: any): void {
    this.notificationService.success(CONSTANTS.MEMBERS_MODULE.ALERT_SUCCESS.replace('{name}', member.name));
  }

  loadTrainersAndAssignments(): void {
    this.isLoadingTrainersMap = true;
    this.staffService.getUnscopedGymStaff(1, 100).subscribe({
      next: (res: any) => {
        const staffList = res?.data?.items || res?.data || [];
        const trainers = staffList.filter((s: any) => s.role === 1 || s.roleName?.toLowerCase() === 'trainer');

        let completedCalls = 0;
        if (trainers.length === 0) {
          this.isLoadingTrainersMap = false;
          return;
        }

        trainers.forEach((trainer: any) => {
          this.staffService.getAssignedMembers(trainer.id).subscribe({
            next: (membersRes: any) => {
              const assignedMembers = membersRes?.data || [];
              assignedMembers.forEach((member: any) => {
                this.memberTrainerMap.set(member.memberId, {
                  trainerId: trainer.id,
                  trainerName: `${trainer.firstName} ${trainer.lastName}`
                });
              });
            },
            complete: () => {
              completedCalls++;
              if (completedCalls === trainers.length) {
                this.isLoadingTrainersMap = false;
                this.mapTrainersToMembers();
              }
            }
          });
        });
      },
      error: () => {
        this.isLoadingTrainersMap = false;
      }
    });
  }

  mapTrainersToMembers(): void {
    this.members.forEach(member => {
      const assignment = this.memberTrainerMap.get(member.id);
      if (assignment) {
        (member as any).personalTrainer = assignment.trainerName;
        (member as any).personalTrainerId = assignment.trainerId;
      } else {
        (member as any).personalTrainer = 'Unassigned';
      }
    });
    this.filteredMembers = [...this.members];
  }
}
