import { Component, EventEmitter, inject, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FIELD_LIMITS } from '../../../../shared/constants/validation.constants';
import { PricingService } from '../../../../core/services/pricing.service';
import { ValidationMessage } from '../../../../shared/components/validation-message/validation-message.component';
import { ConfirmationPopupComponent } from "../../../../shared/components/confirmation-popup/confirmation-popup.component";
import { ApiResponse } from '../../../../shared/models/api-response.model';
import { PricingPlan, textToFeatures } from '../../../../shared/models/pricing.model';

@Component({
  selector: 'app-add-pricing',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ValidationMessage, ConfirmationPopupComponent],
  templateUrl: './add-pricing.component.html',
  styleUrl: './add-pricing.component.scss',
})
export class AddPricing {
  readonly limits = FIELD_LIMITS;

  @Output() close = new EventEmitter<void>();
  @Output() planAdded = new EventEmitter<any>();

  pricingForm: FormGroup;
  isSubmitting = false;
  isConfirmCancelOpen = false;

  private fb = inject(FormBuilder);
  private pricingService = inject(PricingService);

  constructor() {
    this.pricingForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.PLAN_NAME)]],
      description: ['', [Validators.maxLength(FIELD_LIMITS.DESCRIPTION)]],
      price: [null, [Validators.required, Validators.min(0)]],
      durationInDays: [30, [Validators.required, Validators.min(1)]],
      maxBranches: [null],
      maxMembers: [null],
      featuresText: [''],
      isTrial: [false],
      isActive: [true]
    });
  }

  onSubmit() {
    if (this.pricingForm.invalid) {
      this.pricingForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const { featuresText, ...rest } = this.pricingForm.value;
    const payload = { ...rest, features: textToFeatures(featuresText) };

    this.pricingService.addPlan(payload).subscribe({
      next: (res: ApiResponse<PricingPlan>) => {
        this.isSubmitting = false;
        this.planAdded.emit(res);
        this.closeModal();
      },
      error: (err) => {
        console.error(err);
        this.isSubmitting = false;
      }
    });
  }

  closeModal() {
    this.close.emit();
  }

  onTopClose() {
    if (this.pricingForm.dirty) {
      this.isConfirmCancelOpen = true;
    } else {
      this.closeModal();
    }
  }
}
