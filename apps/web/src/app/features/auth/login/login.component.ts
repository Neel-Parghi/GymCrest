import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { DEMO_ACCOUNTS, DemoAccountKey } from '../../../shared/constants/demo-accounts.constants';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CONSTANTS } from '../../../core/constants/constants';
import { BrandLogoComponent } from '../../../shared/components/brand-logo/brand-logo.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BrandLogoComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  readonly limits = FIELD_LIMITS;

  private fb = inject(FormBuilder);
  private authApiService = inject(AuthApiService);
  private route = inject(ActivatedRoute);
  private notification = inject(NotificationService);

  ngOnInit(): void {
    if (this.route.snapshot.queryParamMap.get('setupSuccess') === 'true') {
      this.notification.success(CONSTANTS.AUTH.PASSWORD_SET_SUCCESS);
    }

    const demo = this.route.snapshot.queryParamMap.get('demo');
    if (demo && demo in DEMO_ACCOUNTS) {
      this.loginForm.patchValue({ ...DEMO_ACCOUNTS[demo as DemoAccountKey], rememberMe: false });
    }
  }

  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(FIELD_LIMITS.EMAIL)]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.maxLength(30),
      Validators.pattern(/(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9])/)
    ]],
    rememberMe: [true]
  });

  showPassword = false;
  isLoading = false;

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  onSubmit() {
    if (this.loginForm.valid) {
      this.isLoading = true;
      const rememberMe = this.loginForm.value.rememberMe ?? false;
      const credentials = {
        email: this.loginForm.value.email || '',
        password: this.loginForm.value.password || ''
      };
      this.authApiService.login(credentials, rememberMe).subscribe({
        next: (response) => {
          const data = response?.data || response;
          const token = data?.accessToken;
          if (token) {
            // Parse token to check onboarding status
            const decodedToken = this.authApiService.decodeToken();
            const t = decodedToken as any;
            const isOnboarded = t?.isOnboarded === 'True' || t?.isOnboarded === true;
            const role = decodedToken?.role || t?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

            // Stay on "Signing in..." until the dashboard has loaded, not just until the token arrives
            this.authApiService.redirectUserByRole()
              .then(() => {
                if (!isOnboarded && role !== 'SuperAdmin') {
                  this.notification.info('Complete the onboarding');
                } else {
                  this.notification.success(CONSTANTS.AUTH.LOGIN_SUCCESS);
                }
              })
              .finally(() => this.isLoading = false);
            return;
          }
          console.error('No token received from login API', response);
          this.notification.error(CONSTANTS.AUTH.LOGIN_FAILED);
          this.isLoading = false;
        },
        error: (err) => {
          const errorMessage = err.error?.message || CONSTANTS.AUTH.LOGIN_ERROR;
          if (errorMessage === 'EMAIL_NOT_VERIFIED') {
            this.notification.error('Your email is not verified. Please register again to receive a new verification code.');
          } else {
            this.notification.error(errorMessage);
          }
          this.isLoading = false;
        },
      });
    } else {
      this.loginForm.markAllAsTouched();
    }
  }
}
