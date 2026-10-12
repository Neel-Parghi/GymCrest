import {
  Component, EventEmitter, inject, Input, OnChanges, OnInit, Output, SimpleChanges, ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SlideDrawerComponent } from '../../../../shared/components/slide-drawer/slide-drawer.component';
import { MemberService } from '../../../../core/services/member.service';
import { GymService } from '../../../../core/services/gym.service';
import { GymMember, MemberSubscription, RenewSubscriptionRequest } from '../../../../shared/models/member.model';
import { GymPlan } from '../../../../shared/models/gym-plan.model';
import { MemberStatus, PaymentStatus } from '../../../../shared/enums/member-enums';

@Component({
  selector: 'app-member-detail-drawer',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SlideDrawerComponent],
  templateUrl: './member-detail-drawer.component.html',
  styleUrl: './member-detail-drawer.component.scss'
})
export class MemberDetailDrawer implements OnChanges, OnInit {
  @Input() isOpen = false;
  @Input() member: GymMember | null = null;
  @Input() plans: GymPlan[] = [];
  @Input() branches: any[] = [];
  @Output() drawerClose = new EventEmitter<void>();
  @Output() manage = new EventEmitter<string>();
  @Output() freeze = new EventEmitter<string>();
  @Output() unfreeze = new EventEmitter<string>();
  @Output() toggle = new EventEmitter<string>();
  @Output() renew = new EventEmitter<{ memberId: string; request: RenewSubscriptionRequest }>();

  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);
  private memberService = inject(MemberService);
  private gymService = inject(GymService);

  MemberStatus = MemberStatus;
  PaymentStatus = PaymentStatus;
  subscriptionHistory: MemberSubscription[] = [];
  loadingHistory = false;
  gymName = 'YOUR GYM';

  ngOnInit(): void {
    this.loadGymName();
  }

  loadGymName(): void {
    this.gymService.getMyGym().subscribe({
      next: (res) => {
        const gym = res?.data || res;
        if (gym && gym.gymName) {
          this.gymName = gym.gymName;
          this.cdr.detectChanges();
        }
      },
      error: () => { }
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['member'] && this.member) {
      this.loadHistory();
      this.cdr.detectChanges();
    }

    if (changes['isOpen'] && this.isOpen) {
      if (this.member) this.loadHistory();
      this.cdr.detectChanges();
    }
  }

  loadHistory(): void {
    if (!this.member) return;
    this.loadingHistory = true;
    this.memberService.getSubscriptionHistory(this.member.id).subscribe({
      next: (response) => {
        this.subscriptionHistory = response.data;
        this.loadingHistory = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingHistory = false;
        this.cdr.detectChanges();
      }
    });
  }

  get sub(): MemberSubscription | undefined {
    return this.member?.currentSubscription;
  }

  get fullName(): string {
    return `${this.member?.firstName ?? ''} ${this.member?.lastName ?? ''}`.trim();
  }

  get initials(): string {
    const f = this.member?.firstName?.[0] ?? '';
    const l = this.member?.lastName?.[0] ?? '';
    return (f + l).toUpperCase();
  }

  get daysRemaining(): number {
    if (!this.sub?.endDate) return 0;
    const diff = new Date(this.sub.endDate).getTime() - Date.now();
    return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  }

  get totalDays(): number {
    if (!this.sub) return 0;
    return (this.sub.durationMonths + (this.sub.extendedMonths ?? 0)) * 30;
  }

  get progressPercent(): number {
    if (!this.totalDays) return 0;
    return Math.max(0, Math.min(100, (this.daysRemaining / this.totalDays) * 100));
  }

  get progressClass(): string {
    if (this.daysRemaining <= 0) return 'expired';
    if (this.daysRemaining <= 30) return 'warning';
    return 'healthy';
  }

  get statusClass(): string {
    switch (this.member?.status) {
      case MemberStatus.Active: return 'active';
      case MemberStatus.Inactive: return 'inactive';
      case MemberStatus.Freeze: return 'frozen';
      case MemberStatus.Expired: return 'expired';
      default: return '';
    }
  }

  get statusLabel(): string {
    switch (this.member?.status) {
      case MemberStatus.Active: return 'Active';
      case MemberStatus.Inactive: return 'Inactive';
      case MemberStatus.Freeze: return 'Frozen';
      case MemberStatus.Expired: return 'Expired';
      default: return 'Unknown';
    }
  }

  get isFrozen(): boolean {
    return this.member?.status === MemberStatus.Freeze;
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

  getPaymentStatusClass(status: PaymentStatus): string {
    switch (status) {
      case PaymentStatus.Paid: return 'paid';
      case PaymentStatus.Pending: return 'pending';
      case PaymentStatus.Partial: return 'partial';
      case PaymentStatus.Refunded: return 'refunded';
      default: return 'unpaid';
    }
  }

  onFreezeToggle(): void {
    if (!this.member) return;
    if (this.isFrozen) this.unfreeze.emit(this.member.id);
    else this.freeze.emit(this.member.id);
  }

  getBranchName(branchId?: string): string {
    if (!branchId) return 'No Branch (General)';
    const branch = this.branches.find(b => b.id === branchId);
    return branch ? branch.name : 'Unknown Branch';
  }
}
