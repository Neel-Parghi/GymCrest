import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PricingService } from '../../../core/services/pricing.service';
import { PlanCheckoutService } from '../../../core/services/plan-checkout.service';
import { GymService } from '../../../core/services/gym.service';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CONSTANTS } from '../../../core/constants/constants';
import { TestPaymentNoticeComponent } from '../../../shared/components/test-payment-notice/test-payment-notice.component';
import { PricingPlan } from '../../../shared/models/pricing.model';
import { GymListResponse } from '../../../shared/models/gym.model';

@Component({
  selector: 'app-subscription-expired',
  standalone: true,
  imports: [CommonModule, TestPaymentNoticeComponent],
  templateUrl: './subscription-expired.component.html',
  styleUrl: './subscription-expired.component.scss'
})
export class SubscriptionExpiredComponent implements OnInit {
  private pricingService = inject(PricingService);
  private planCheckout = inject(PlanCheckoutService);
  private gymService = inject(GymService);
  private authService = inject(AuthApiService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  /** Only the owner can renew; trainers and staff are told to contact them. */
  readonly isOwner = this.authService.getUserRole() === 'GymOwner';

  plans: PricingPlan[] = [];
  gym: GymListResponse | null = null;
  loading = true;

  /** Plan picked for the confirm step; payment only starts from there. */
  selectedPlan: PricingPlan | null = null;
  isPaying = false;
  checkoutMessage: { tone: 'error' | 'info'; text: string } | null = null;

  ngOnInit(): void {
    if (!this.isOwner) {
      this.loading = false;
      return;
    }
    this.loadGymAndPlans();
  }

  loadGymAndPlans() {
    this.loading = true;
    this.gymService.getMyGym(true).subscribe({
      next: (gymRes) => {
        this.gym = gymRes?.data || null;
        this.pricingService.getAllPlans(true).subscribe({
          next: (plansRes) => {
            this.plans = plansRes?.data?.filter(p => p.isActive && !p.isTrial) || [];
            this.loading = false;
          },
          error: () => {
            this.loading = false;
          }
        });
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  choosePlan(plan: PricingPlan): void {
    this.selectedPlan = plan;
    this.checkoutMessage = null;
  }

  cancelChoice(): void {
    if (this.isPaying) return;
    this.selectedPlan = null;
    this.checkoutMessage = null;
  }

  /** The plan is expired, so a renewal runs from today. */
  activeUntil(plan: PricingPlan): Date {
    const until = new Date();
    until.setDate(until.getDate() + (plan.durationInDays > 0 ? plan.durationInDays : 30));
    return until;
  }

  pay(): void {
    const plan = this.selectedPlan;
    if (!plan || !this.gym?.id || this.isPaying) return;

    this.isPaying = true;
    this.checkoutMessage = null;
    this.planCheckout.checkout({
      gymId: this.gym.id,
      planId: plan.id,
      description: `${plan.name} plan renewal`,
      prefill: { name: this.gym.ownerName, email: this.gym.email }
    }).subscribe(outcome => {
      this.isPaying = false;
      if (outcome.status === 'paid') {
        this.notification.success(CONSTANTS.SUBSCRIPTION_EXPIRED.ACCESS_RESTORED.replace('{name}', plan.name));
        this.gymService.clearMyGymCache();
        this.router.navigate(['/gym-owner/dashboard']);
      } else if (outcome.status === 'dismissed') {
        this.checkoutMessage = { tone: 'info', text: 'Payment was cancelled. Your plan has not been renewed.' };
      } else {
        this.checkoutMessage = { tone: 'error', text: outcome.message ?? 'Payment failed. Please try again.' };
      }
    });
  }

  logout() {
    this.authService.logout();
  }
}
