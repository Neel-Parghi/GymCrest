import { Component, EventEmitter, Output, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { FIELD_LIMITS } from '../../../../shared/constants/validation.constants';
import { phoneValidator, urlValidator, gstValidator, postalCodeValidator, registrationNumberValidator } from '../../../../shared/validators/custom-validators';
import { NotificationService } from '../../../../core/services/notification.service';
import { GymService } from '../../../../core/services/gym.service';
import { ApiResponse } from '../../../../shared/models/api-response.model';
import { GymOwnerResponse, OnboardGymRequest } from '../../../../shared/models/gym.model';
import { ConfirmationPopupComponent } from "../../../../shared/components/confirmation-popup/confirmation-popup.component";
import { PricingService } from '../../../../core/services/pricing.service';
import { ValidationMessage } from "../../../../shared/components/validation-message/validation-message.component";
import { PricingPlan } from '../../../../shared/models/pricing.model';
import { DropdownComponent } from '../../../../shared/components/dropdown/dropdown.component';
import { DateTimePickerComponent } from '../../../../shared/components/date-time-picker/date-time-picker.component';
import { TimePickerComponent } from '../../../../shared/components/time-picker/time-picker.component';
import { DropdownOption } from '../../../../shared/models/dropdown.model';
import { CONSTANTS } from '../../../../core/constants/constants';

@Component({
  selector: 'app-gym-onboarding-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ConfirmationPopupComponent, ValidationMessage, DropdownComponent, DateTimePickerComponent, TimePickerComponent],
  templateUrl: './gym-onboarding-modal.component.html',
  styleUrl: './gym-onboarding-modal.component.scss'
})
export class GymOnboardingModalComponent implements OnInit {
  readonly limits = FIELD_LIMITS;

  @Output() close = new EventEmitter<void>();

  currentStep = 1;
  totalSteps = 5;
  collapsedBranches: boolean[] = [];

  onboardingForm!: FormGroup;
  isSubmitting = false;
  isConfirmCancelOpen = false;

  private fb = inject(FormBuilder);
  private gymService = inject(GymService);
  private notification = inject(NotificationService);

  steps = [
    { id: 1, label: 'Gym Info', key: 'gymInfo' },
    { id: 2, label: 'Branches', key: 'branches' },
    { id: 3, label: 'Assign Owner', key: 'assignedOwnerId' },
    { id: 4, label: 'Plan & Billing', key: 'plan' },
    { id: 5, label: 'Review', key: null }
  ];

  gymOwners: GymOwnerResponse[] = [];
  pricingService = inject(PricingService);
  plans: any[] = [];

  /** Trial length from the active trial plan in Pricing; the API falls back to 14 days when there is none. */
  get trialDays(): number {
    const trial = this.plans.find(p => p.isTrial && p.isActive && p.durationInDays > 0);
    return trial?.durationInDays ?? 14;
  }

  get ownerOptions(): DropdownOption[] {
    return this.gymOwners.map(owner => ({
      label: `${owner.name} (${owner.email})`,
      value: owner.id,
      icon: 'fa-solid fa-user-tie'
    }));
  }

  get planOptions(): DropdownOption[] {
    const options: DropdownOption[] = [{ label: 'No Plan Selected (Trialing)', value: '' }];
    this.plans.forEach(plan => {
      options.push({
        label: `${plan.name || plan.Name} - $${plan.price}`,
        value: plan.id,
        icon: 'fa-solid fa-gem'
      });
    });
    return options;
  }

  constructor() { }

  ngOnInit(): void {
    this.initForm();
    this.getGymOwners();
    this.loadPlans();
  }

  private initForm() {
    this.onboardingForm = this.fb.group({
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
      branches: this.fb.array([]),
      assignedOwnerId: ['', Validators.required],
      plan: this.fb.group({
        subscriptionId: [''],
        isTrial: [true]
      })
    });

    this.addBranch();

    this.onboardingForm.get('plan.isTrial')?.valueChanges.subscribe(isTrial => {
      if (isTrial) {
        this.onboardingForm.get('plan.subscriptionId')?.setValue('', { emitEvent: false });
      }
    });

    this.onboardingForm.get('plan.subscriptionId')?.valueChanges.subscribe(subId => {
      if (subId) {
        this.onboardingForm.get('plan.isTrial')?.setValue(false, { emitEvent: false });
      } else {
        this.onboardingForm.get('plan.isTrial')?.setValue(true, { emitEvent: false });
      }
    });
  }

  get branches(): FormArray {
    return this.onboardingForm.get('branches') as FormArray;
  }

  loadPlans() {
    this.pricingService.getAllPlans().subscribe((res: ApiResponse<PricingPlan[]>) => {
      this.plans = res?.data || [];
    });
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
      const gymAddress = this.onboardingForm.get('gymInfo.address')?.value;
      const gymPhone = this.onboardingForm.get('gymInfo.phone')?.value;
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

  isCurrentStepValid(): boolean {
    if (this.currentStep === 1) {
      return this.onboardingForm.get('gymInfo')?.valid || false;
    }
    if (this.currentStep === 2) {
      return this.branches.length > 0 && this.branches.valid;
    }
    if (this.currentStep === 3) {
      return this.onboardingForm.get('assignedOwnerId')?.valid || false;
    }
    if (this.currentStep === 4) {
      const planGroup = this.onboardingForm.get('plan');
      const isTrial = planGroup?.get('isTrial')?.value;
      const subscriptionId = planGroup?.get('subscriptionId')?.value;
      if (!isTrial && !subscriptionId) {
        return false;
      }
      return planGroup?.valid || false;
    }
    return true;
  }

  nextStep() {
    if (this.isCurrentStepValid() && this.currentStep < this.totalSteps) {
      this.currentStep++;
    }
  }

  prevStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  closeModal() {
    this.close.emit();
  }

  onTopClose() {
    if (this.onboardingForm.dirty) {
      this.isConfirmCancelOpen = true;
    } else {
      this.closeModal();
    }
  }

  submit() {
    if (this.onboardingForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;

      const formValue = this.onboardingForm.value;
      const payload: OnboardGymRequest = {
        ...formValue.gymInfo,
        branches: formValue.branches,
        assignedOwnerId: formValue.assignedOwnerId || null,
        planId: formValue.plan.subscriptionId || null,
        isTrial: formValue.plan.isTrial
      };

      this.gymService.onboardGym(payload).subscribe({
        next: () => {
          this.notification.success(CONSTANTS.GYM_ONBOARD_SUCCESS_MESSAGE);
          this.isSubmitting = false;
          this.closeModal();
        },
        error: (error) => {
          this.notification.error(error.error?.message || CONSTANTS.GYM_ONBOARD_ERROR_MESSAGE);
          this.isSubmitting = false;
        }
      });
    }
  }

  get f() {
    return this.onboardingForm.value;
  }

  getSelectedOwnerName(): string {
    const ownerId = this.onboardingForm.get('assignedOwnerId')?.value;
    const owner = this.gymOwners.find(o => o.id === ownerId);
    return owner ? owner.name : 'Not Assigned';
  }

  getSelectedPlanName(): string {
    const planId = this.onboardingForm.get('plan.subscriptionId')?.value;
    const plan = this.plans.find(p => p.id === planId);
    return plan ? (plan.name || plan.Name) : 'No Plan Selected (Trial)';
  }

  getGymOwners() {
    this.gymService.getGymOwnersList().subscribe({
      next: (res) => {
        this.gymOwners = res.data.items;
      },
      error: (err: any) => {
        this.notification.error(err.error?.message || CONSTANTS.GYM_OWNER_LOAD_ERROR_MESSAGE);
      }
    });
  }
}