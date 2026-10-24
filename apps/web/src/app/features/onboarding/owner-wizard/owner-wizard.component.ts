import { Component, ElementRef, OnInit, inject, NgZone, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { phoneValidator, urlValidator, gstValidator, postalCodeValidator, registrationNumberValidator } from '../../../shared/validators/custom-validators';
import { Router } from '@angular/router';
import { PricingService } from '../../../core/services/pricing.service';
import { PricingPlan, planLimitsLabel, planPeriodLabel } from '../../../shared/models/pricing.model';
import { ToastrService } from 'ngx-toastr';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { UserService } from '../../../core/services/user.service';
import { GymService } from '../../../core/services/gym.service';
import { ProfileService } from '../../../core/services/profile.service';
import { PaymentService } from '../../../core/services/payment.service';
import { ValidationMessage } from "../../../shared/components/validation-message/validation-message.component";
import { DateTimePickerComponent } from '../../../shared/components/date-time-picker/date-time-picker.component';
import { TimePickerComponent } from '../../../shared/components/time-picker/time-picker.component';
import { CONSTANTS } from '../../../core/constants/constants';
import { STORAGE_KEYS } from '../../../shared/utils/storage-keys';
import { take } from 'rxjs';

declare var Razorpay: any;

@Component({
  selector: 'app-owner-wizard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ValidationMessage, DateTimePickerComponent, TimePickerComponent],
  templateUrl: './owner-wizard.component.html',
  styleUrls: ['./owner-wizard.component.scss']
})
export class OwnerWizardComponent implements OnInit {
  readonly limits = FIELD_LIMITS;

  currentStep = 1;
  ownerFirstName = '';
  totalSteps = 6;
  collapsedBranches: boolean[] = [];

  showRazorpayModal = false;

  plans: PricingPlan[] = [];
  isLoadingPlans = false;
  selectedPlanId: string | null = null;
  isProcessingPayment = false;
  isSubmitting = false;
  createdGymId: string | null = null;

  gymForm!: FormGroup;

  private fb = inject(FormBuilder);
  private pricingService = inject(PricingService);
  private authService = inject(AuthApiService);
  private router = inject(Router);
  private toastr = inject(ToastrService);
  private userService = inject(UserService);
  private gymService = inject(GymService);
  private profileService = inject(ProfileService);
  private paymentService = inject(PaymentService);
  private ngZone = inject(NgZone);
  @ViewChild('scroller') private scroller?: ElementRef<HTMLElement>;

  ngOnInit(): void {
    this.initForm();
    this.loadDraft();
    this.prefillFromAccount();
    const user = this.authService.getUserProfile();

    if (user && user.currentOnboardingStep) {
      const draft = localStorage.getItem(STORAGE_KEYS.OWNER_WIZARD_DRAFT);
      if (draft || user.currentOnboardingStep === 1) {
        this.currentStep = user.currentOnboardingStep;
      } else {
        this.currentStep = 1;
      }
    }

    if (this.currentStep >= 4) {
      this.loadPlans();
    }

    this.gymForm.valueChanges.subscribe(() => this.saveDraft());
  }

  private prefillFromAccount(): void {
    const tokenEmail = String(this.authService.decodeToken()?.['email'] ?? '');
    this.applyAccountDetails(this.authService.getUserProfile()?.email || tokenEmail);

    this.profileService.getProfile().pipe(take(1)).subscribe({
      next: profile => {
        this.ownerFirstName = profile?.firstName || '';
        this.applyAccountDetails(profile?.email, profile?.phone);
      },
      error: () => { }
    });
  }

  private applyAccountDetails(email?: string, phone?: string): void {
    const gymInfo = this.gymForm.get('gymInfo');

    if (email) gymInfo?.get('email')?.setValue(email);
    if (phone && !gymInfo?.get('phone')?.value) gymInfo?.get('phone')?.setValue(phone);
  }

  saveDraft() {
    localStorage.setItem(STORAGE_KEYS.OWNER_WIZARD_DRAFT, JSON.stringify({
      form: this.gymForm.value,
      selectedPlanId: this.selectedPlanId
    }));
  }

  loadDraft() {
    const draftStr = localStorage.getItem(STORAGE_KEYS.OWNER_WIZARD_DRAFT);
    if (draftStr) {
      try {
        const draft = JSON.parse(draftStr);
        if (draft.form) {
          if (draft.form.gymInfo) {
            this.gymForm.get('gymInfo')?.patchValue(draft.form.gymInfo);
          }
          if (draft.form.branches && draft.form.branches.length > 0) {
            this.branches.clear();
            this.collapsedBranches = [];
            draft.form.branches.forEach((branch: any) => {
              const branchForm = this.fb.group({
                name: [branch.name || '', [Validators.required, Validators.maxLength(FIELD_LIMITS.BRANCH_NAME)]],
                address: this.fb.group({
                  line1: [branch.address?.line1 || '', [Validators.required, Validators.maxLength(FIELD_LIMITS.ADDRESS_LINE)]],
                  line2: [branch.address?.line2 || '', [Validators.maxLength(FIELD_LIMITS.ADDRESS_LINE)]],
                  city: [branch.address?.city || '', [Validators.required, Validators.maxLength(FIELD_LIMITS.CITY)]],
                  state: [branch.address?.state || '', [Validators.required, Validators.maxLength(FIELD_LIMITS.STATE)]],
                  country: [branch.address?.country || '', [Validators.required, Validators.maxLength(FIELD_LIMITS.COUNTRY)]],
                  postalCode: [branch.address?.postalCode || '', [Validators.required, Validators.maxLength(FIELD_LIMITS.POSTAL_CODE), postalCodeValidator]]
                }),
                contactNumber: [branch.contactNumber || '', [Validators.required, Validators.maxLength(FIELD_LIMITS.PHONE), phoneValidator]],
                openTime: [branch.openTime || '06:00'],
                closeTime: [branch.closeTime || '22:00']
              });
              this.branches.push(branchForm);
              this.collapsedBranches.push(true);
            });
          }
        }
        if (draft.selectedPlanId) {
          this.selectedPlanId = draft.selectedPlanId;
        }
      } catch (e) {
        console.error('Failed to load draft', e);
      }
    }
  }

  private initForm() {
    this.gymForm = this.fb.group({
      gymInfo: this.fb.group({
        name: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.GYM_NAME)]],
        brandName: ['', [Validators.maxLength(FIELD_LIMITS.BRAND_NAME)]],
        description: ['', [Validators.maxLength(FIELD_LIMITS.DESCRIPTION)]],
        establishedDate: [''],
        registrationNumber: ['', [Validators.maxLength(FIELD_LIMITS.REGISTRATION_NUMBER), registrationNumberValidator]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(FIELD_LIMITS.EMAIL)]],
        phone: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.PHONE), phoneValidator]],
        gstNumber: ['', [Validators.maxLength(FIELD_LIMITS.GST_NUMBER), gstValidator]],
        websiteUrl: ['', [Validators.maxLength(FIELD_LIMITS.URL), urlValidator]],
        logoUrl: [''],
        coverImageUrl: [''],
        address: this.fb.group({
          line1: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.ADDRESS_LINE)]],
          line2: ['', [Validators.maxLength(FIELD_LIMITS.ADDRESS_LINE)]],
          city: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.CITY)]],
          state: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.STATE)]],
          country: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.COUNTRY)]],
          postalCode: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.POSTAL_CODE), postalCodeValidator]]
        })
      }),
      branches: this.fb.array([])
    });

    this.addBranch();
  }

  get branches(): FormArray {
    return this.gymForm.get('branches') as FormArray;
  }

  addBranch() {
    const branchForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.BRANCH_NAME)]],
      address: this.fb.group({
        line1: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.ADDRESS_LINE)]],
        line2: ['', [Validators.maxLength(FIELD_LIMITS.ADDRESS_LINE)]],
        city: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.CITY)]],
        state: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.STATE)]],
        country: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.COUNTRY)]],
        postalCode: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.POSTAL_CODE), postalCodeValidator]]
      }),
      contactNumber: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.PHONE), phoneValidator]],
      openTime: ['06:00'],
      closeTime: ['22:00']
    });
    this.branches.push(branchForm);
    this.collapsedBranches.push(false);
  }

  removeBranch(index: number) {
    if (this.branches.length > 1) {
      this.branches.removeAt(index);
      this.collapsedBranches.splice(index, 1);
    }
  }

  toggleBranchCollapse(index: number) {
    this.collapsedBranches[index] = !this.collapsedBranches[index];
  }

  copyGymAddressToBranch(event: any, index: number): void {
    const isChecked = event.target.checked;
    if (isChecked) {
      const gymAddress = this.gymForm.get('gymInfo.address')?.value;
      const gymPhone = this.gymForm.get('gymInfo.phone')?.value;
      if (gymAddress) {
        this.branches.at(index).get('address')?.patchValue({
          line1: gymAddress.line1,
          line2: gymAddress.line2,
          city: gymAddress.city,
          state: gymAddress.state,
          country: gymAddress.country,
          postalCode: gymAddress.postalCode
        });
      }
      if (gymPhone) {
        this.branches.at(index).get('contactNumber')?.setValue(gymPhone);
      }
    } else {
      this.branches.at(index).get('address')?.patchValue({
        line1: '',
        line2: '',
        city: '',
        state: '',
        country: '',
        postalCode: ''
      });
      this.branches.at(index).get('contactNumber')?.setValue('');
    }
  }

  loadPlans() {
    this.isLoadingPlans = true;
    this.pricingService.getAllPlans().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.plans = res.data.filter(p => p.isActive);
          this.applyIntendedPlan();
        }
        this.isLoadingPlans = false;
      },
      error: () => {
        this.toastr.error('Failed to load pricing plans');
        this.isLoadingPlans = false;
      }
    });
  }

  readonly periodLabel = planPeriodLabel;
  readonly limitsLabel = planLimitsLabel;

  selectPlan(id: string) {
    this.selectedPlanId = id;
    this.saveDraft();
  }

  /** Preselects the plan picked on the landing pricing card, unless a plan was already chosen in this wizard. */
  private applyIntendedPlan(): void {
    try {
      const intended = localStorage.getItem(STORAGE_KEYS.INTENDED_PLAN);
      if (!intended) return;
      localStorage.removeItem(STORAGE_KEYS.INTENDED_PLAN);
      if (!this.selectedPlanId && this.plans.some(p => p.id === intended)) this.selectPlan(intended);
    } catch { /* storage blocked */ }
  }

  isCurrentStepValid(): boolean {
    if (this.currentStep === 1) {
      return true;
    }
    if (this.currentStep === 2) {
      return this.gymForm.get('gymInfo')?.valid || false;
    }
    if (this.currentStep === 3) {
      return this.branches.length > 0 && this.branches.valid;
    }
    if (this.currentStep === 4) {
      return !!this.selectedPlanId;
    }
    return true;
  }

  nextStep() {
    if (!this.isCurrentStepValid()) {
      if (this.currentStep === 4 && !this.selectedPlanId) {
        this.toastr.warning('Please select a subscription plan to continue.');
      } else {
        this.gymForm.markAllAsTouched();
      }
      return;
    }

    if (this.currentStep < this.totalSteps) {
      this.currentStep++;
      this.scrollToTop();
      if (this.currentStep === 4) {
        this.loadPlans();
      }
      this.saveStep(this.currentStep);
    }
  }

  prevStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.scrollToTop();
      this.saveDraft();
    }
  }

  private scrollToTop() {
    this.scroller?.nativeElement.scrollTo({ top: 0 });
    window.scrollTo({ top: 0 });
  }

  logout() {
    localStorage.removeItem(STORAGE_KEYS.OWNER_WIZARD_DRAFT);
    this.authService.logout();
  }

  saveStep(step: number) {
    this.userService.saveOnboardingStep(step).subscribe();
  }

  getSelectedPlanName(): string {
    return this.plans.find(p => p.id === this.selectedPlanId)?.name || 'Unknown Plan';
  }

  getSelectedPlanPrice(): number {
    return this.plans.find(p => p.id === this.selectedPlanId)?.price || 0;
  }
  getSelectedPlanPeriod(): string {
    const plan = this.plans.find(p => p.id === this.selectedPlanId);
    return plan ? planPeriodLabel(plan) : '';
  }


  processPayment() {
    this.isProcessingPayment = true;

    if (this.createdGymId) {
      this.initiateRazorpayOrder();
      return;
    }

    this.isSubmitting = true;

    const ownerId = this.authService.getUserId() || this.authService.getUserProfile()?.id;
    if (!ownerId) {
      this.toastr.error('User profile not found. Please log in again.');
      this.isSubmitting = false;
      this.isProcessingPayment = false;
      return;
    }

    const formValue = this.gymForm.value;
    const payload = {
      ...formValue.gymInfo,
      branches: formValue.branches,
      assignedOwnerId: ownerId,
      planId: this.selectedPlanId,
      isTrial: false
    };

    this.gymService.onboardGym(payload).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        this.createdGymId = res.data.gymId;
        this.initiateRazorpayOrder();
      },
      error: () => {
        this.toastr.error('Failed to set up your gym. Please try again.');
        this.isSubmitting = false;
        this.isProcessingPayment = false;
      }
    });
  }

  private initiateRazorpayOrder() {
    if (!this.createdGymId || !this.selectedPlanId) {
      this.isProcessingPayment = false;
      return;
    }

    this.paymentService.initiatePayment({ gymId: this.createdGymId, planId: this.selectedPlanId }).subscribe({
      next: (res) => this.openRazorpayCheckout(res.data),
      error: () => {
        this.toastr.error('Failed to start payment. Please try again.');
        this.isProcessingPayment = false;
      }
    });
  }

  private openRazorpayCheckout(order: { razorpayOrderId: string; amount: number }) {
    const options = {
      key: CONSTANTS.PAYMENT.RAZORPAY.KEY_ID,
      amount: order.amount,
      currency: CONSTANTS.PAYMENT.RAZORPAY.CURRENCY,
      name: CONSTANTS.PAYMENT.RAZORPAY.COMPANY_NAME,
      description: `Subscription for ${this.getSelectedPlanName()}`,
      order_id: order.razorpayOrderId,
      handler: (response: any) => {
        this.ngZone.run(() => {
          this.verifyAndFinish(response);
        });
      },
      prefill: {
        name: this.gymForm.get('gymInfo.name')?.value || 'Gym Owner',
        email: this.gymForm.get('gymInfo.email')?.value || 'owner@example.com',
        contact: this.gymForm.get('gymInfo.phone')?.value || ''
      },
      theme: { color: CONSTANTS.PAYMENT.RAZORPAY.THEME_COLOR },
      modal: {
        ondismiss: () => {
          this.ngZone.run(() => this.checkActivatedAfterClose());
        }
      }
    };

    const rzp = new (window as any).Razorpay(options);
    rzp.open();
  }

  private verifyAndFinish(response: any) {
    this.isSubmitting = true;

    this.paymentService.verifyPayment({
      orderId: response.razorpay_order_id,
      paymentId: response.razorpay_payment_id,
      signature: response.razorpay_signature
    }).subscribe({
      next: () => this.finishOnboarding(),
      // The webhook can still activate the plan when this check fails, so ask before reporting a failure
      error: () => this.gymService.confirmSubscriptionActive().subscribe(active => {
        if (active) {
          this.finishOnboarding();
          return;
        }
        this.toastr.error('Payment verification failed. Please try again or contact support.');
        this.isSubmitting = false;
        this.isProcessingPayment = false;
      })
    });
  }

  /**
   * The popup closed without a success callback. Some methods (UPI/QR) still complete and the webhook activates
   * the plan, so check before letting the owner pay again (which would charge them twice).
   */
  private checkActivatedAfterClose() {
    this.isSubmitting = true;
    this.gymService.confirmSubscriptionActive().subscribe(active => {
      if (active) {
        this.finishOnboarding();
        return;
      }
      this.isSubmitting = false;
      this.isProcessingPayment = false;
    });
  }

  /** Plan is paid: fetch a token that carries the new gym, then open the dashboard. */
  private finishOnboarding() {
    this.isSubmitting = true;
    localStorage.removeItem(STORAGE_KEYS.OWNER_WIZARD_DRAFT);
    this.toastr.success('Welcome to GymCrest!');
    this.gymService.clearMyGymCache();
    const openDashboard = () => this.router.navigate(['/gym-owner/dashboard']);
    this.authService.refreshToken().subscribe({
      next: () => this.profileService.getProfile(true).subscribe({ next: openDashboard, error: openDashboard }),
      // Without a fresh token the guards would send the owner back here; signing in again picks up the new gym
      error: () => {
        this.toastr.info('Your plan is active. Please sign in again to open your dashboard.');
        this.authService.logout();
      }
    });
  }
}
