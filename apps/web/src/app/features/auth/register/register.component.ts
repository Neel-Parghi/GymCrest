import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, FormsModule, Validators, FormControl } from '@angular/forms';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { Router, RouterLink } from '@angular/router';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CONSTANTS } from '../../../core/constants/constants';
import { BrandLogoComponent } from '../../../shared/components/brand-logo/brand-logo.component';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink, BrandLogoComponent]
})
export class RegisterComponent {
  readonly limits = FIELD_LIMITS;

  private fb = inject(FormBuilder);
  private authApiService = inject(AuthApiService);
  private router = inject(Router);
  private notification = inject(NotificationService);

  registerForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(30)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(FIELD_LIMITS.EMAIL)]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.maxLength(30),
      Validators.pattern(/(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9])/)
    ]],
  });

  showPassword = false;
  isLoading = false;
  selectedRole = 'User'; // default

  isOtpStep = false;
  otpCode = '';
  otpControls: FormControl[] = Array.from({ length: 6 }, () => new FormControl(''));
  registeredEmail = '';
  resendCooldown = 0;
  resendTimer: any;

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  setRole(role: string) {
    this.selectedRole = role;
  }

  onSubmit() {
    if (this.registerForm.valid) {
      this.isLoading = true;

      const formValue = this.registerForm.value;
      const nameParts = (formValue.name || '').trim().split(' ');
      const firstName = nameParts[0];
      const lastName = nameParts.length > 1 ? nameParts.slice(1).join(' ') : '';

      const payload = {
        firstName,
        lastName,
        email: formValue.email || '',
        password: formValue.password || '',
        role: this.selectedRole
      };

      this.authApiService.register(payload).subscribe({
        next: (res) => {
          const data = res?.data || res;
          if (data?.requiresOtp) {
            this.isOtpStep = true;
            this.registeredEmail = data.email;
            this.notification.success(data.message || 'OTP sent successfully.');
            this.startResendCooldown();
          } else {
            this.notification.success(CONSTANTS.AUTH.REGISTER_SUCCESS);
            this.authApiService.redirectUserByRole();
          }
          this.isLoading = false;
        },
        error: (err) => {
          this.notification.error(err.error?.message || CONSTANTS.AUTH.REGISTER_FAILED);
          this.isLoading = false;
        },
      });
    }
  }

  verifyOtp() {
    this.updateOtpCode();
    if (this.otpCode.length === 6) {
      this.isLoading = true;
      this.authApiService.verifyOtp({ email: this.registeredEmail, otpCode: this.otpCode }).subscribe({
        next: () => {
          this.notification.success('Email verified successfully!');
          this.authApiService.redirectUserByRole();
          this.isLoading = false;
        },
        error: (err) => {
          this.notification.error(err.error?.message || 'Verification failed.');
          this.isLoading = false;
        }
      });
    }
  }

  onOtpKeyup(event: KeyboardEvent, index: number) {
    const input = event.target as HTMLInputElement;
    if (event.key === 'Backspace' || event.key === 'Tab' || event.key === 'Shift') return;

    if (input.value.length === 1 && index < 5) {
      const nextInput = document.getElementById(`otp-${index + 1}`) as HTMLInputElement;
      if (nextInput) nextInput.focus();
    }
    this.updateOtpCode();
  }

  onOtpKeydown(event: KeyboardEvent, index: number) {
    const input = event.target as HTMLInputElement;
    if (event.key === 'Backspace' && input.value === '' && index > 0) {
      const prevInput = document.getElementById(`otp-${index - 1}`) as HTMLInputElement;
      if (prevInput) {
        prevInput.focus();
        this.otpControls[index - 1].setValue('');
      }
    }
    this.updateOtpCode();
  }

  onOtpPaste(event: ClipboardEvent) {
    event.preventDefault();
    const pastedData = event.clipboardData?.getData('text');
    if (pastedData) {
      const digits = pastedData.replace(/\D/g, '').split('').slice(0, 6);
      digits.forEach((digit, index) => {
        this.otpControls[index].setValue(digit);
      });
      if (digits.length > 0) {
        const nextIndex = Math.min(digits.length, 5);
        const nextInput = document.getElementById(`otp-${nextIndex}`) as HTMLInputElement;
        if (nextInput) nextInput.focus();
      }
      this.updateOtpCode();
    }
  }

  updateOtpCode() {
    this.otpCode = this.otpControls.map(c => c.value).join('');
  }

  trackByIndex(index: number, obj: any): any {
    return index;
  }

  resendOtp() {
    if (this.resendCooldown > 0) return;

    this.isLoading = true;
    this.authApiService.resendOtp({ email: this.registeredEmail }).subscribe({
      next: (res) => {
        this.notification.success(res?.message || 'OTP resent successfully.');
        this.startResendCooldown();
        this.isLoading = false;
      },
      error: (err) => {
        this.notification.error(err.error?.message || 'Failed to resend OTP.');
        this.isLoading = false;
      }
    });
  }

  startResendCooldown() {
    this.resendCooldown = 60;
    if (this.resendTimer) clearInterval(this.resendTimer);

    this.resendTimer = setInterval(() => {
      this.resendCooldown--;
      if (this.resendCooldown <= 0) {
        clearInterval(this.resendTimer);
      }
    }, 1000);
  }

  ngOnDestroy() {
    if (this.resendTimer) clearInterval(this.resendTimer);
  }
}

