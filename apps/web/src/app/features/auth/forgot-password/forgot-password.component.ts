import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { RouterModule } from '@angular/router';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { BrandLogoComponent } from '../../../shared/components/brand-logo/brand-logo.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, BrandLogoComponent],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  readonly limits = FIELD_LIMITS;

  emailForm: FormGroup;
  isLoading = false;
  isSent = false;
  submittedEmail = '';

  constructor(
    private fb: FormBuilder,
    private authApi: AuthApiService,
    private notification: NotificationService
  ) {
    this.emailForm = this.fb.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(FIELD_LIMITS.EMAIL)]]
    });
  }

  sendLink() {
    if (this.emailForm.invalid) return;
    this.isLoading = true;
    this.submittedEmail = this.emailForm.value.email;

    this.authApi.forgotPassword({ email: this.submittedEmail }).subscribe({
      next: () => {
        this.isLoading = false;
        this.isSent = true;
      },
      error: () => {
        this.isLoading = false;
        this.isSent = true; // Still show success for security
      }
    });
  }
}
