import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { newPasswordValidators, existingPasswordValidators } from '../../../shared/validators/custom-validators';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { UserService } from '../../../core/services/user.service';
import { CONSTANTS } from '../../../core/constants/constants';
import { BrandLogoComponent } from '../../../shared/components/brand-logo/brand-logo.component';

@Component({
  selector: 'app-accept-invitation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BrandLogoComponent],
  templateUrl: './accept-invitation.component.html',
  styleUrls: ['./accept-invitation.component.scss']
})
export class AcceptInvitationComponent implements OnInit {
  readonly limits = FIELD_LIMITS;

  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private userService = inject(UserService);

  acceptForm: FormGroup;
  token: string | null = null;
  isValidating = true;
  isTokenValid = false;
  isSubmitting = false;
  showPassword = false;
  errorMessage = '';

  constructor() {
    this.acceptForm = this.fb.group({
      password: ['', newPasswordValidators],
      confirmPassword: ['', existingPasswordValidators]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token');

    if (this.token) {
      this.validateToken();
    } else {
      this.isValidating = false;
      this.isTokenValid = false;
      this.errorMessage = CONSTANTS.AUTH_ACCEPT_INVITATION.INVALID_TOKEN;
    }
  }

  validateToken(): void {
    this.userService.validateInvitation(this.token!).subscribe({
      next: (res: any) => {
        const data = res?.data || res;
        this.isValidating = false;
        this.isTokenValid = data.isValid;
        if (!this.isTokenValid) {
          this.errorMessage = CONSTANTS.AUTH_ACCEPT_INVITATION.EXPIRED_TOKEN;
        }
      },
      error: () => {
        this.isValidating = false;
        this.isTokenValid = false;
        this.errorMessage = CONSTANTS.AUTH_ACCEPT_INVITATION.VALIDATION_ERROR;
      }
    });
  }

  passwordMatchValidator(g: FormGroup) {
    return g.get('password')?.value === g.get('confirmPassword')?.value
      ? null : { mismatch: true };
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    if (this.acceptForm.valid && this.token) {
      this.isSubmitting = true;
      this.errorMessage = '';

      const payload = {
        token: this.token,
        password: this.acceptForm.value.password,
        confirmPassword: this.acceptForm.value.confirmPassword
      };

      this.userService.setPassword(payload).subscribe({
        next: () => {
          this.isSubmitting = false;

          this.router.navigate(['/login'], { queryParams: { setupSuccess: true } });
        },
        error: (err: any) => {
          this.isSubmitting = false;
          this.errorMessage = err.error?.message || CONSTANTS.AUTH_ACCEPT_INVITATION.SET_PASSWORD_ERROR;
        }
      });
    }
  }
}
