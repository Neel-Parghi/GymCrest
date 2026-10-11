import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { postalCodeValidator, newPasswordValidators, existingPasswordValidators } from '../../../shared/validators/custom-validators';
import { firstValueFrom, map } from 'rxjs';
import { ProfileService } from '../../../core/services/profile.service';
import { UserService } from '../../../core/services/user.service';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmationService } from '../../../core/services/confirmation.service';
import { CONSTANTS } from '../../../core/constants/constants';
import { UpdateUserProfile, UserProfile } from '../../../shared/models/user-profile.model';
import { MyGymMembership } from '../../../shared/models/my-gym-membership.model';
import { SegmentedTabsComponent } from '../../../shared/components/segmented-tabs/segmented-tabs.component';
import { SegmentedTab } from '../../../shared/models/segmented-tab.model';
import { AccountLink, PasswordForm, ProfileForm } from '../../../core/models/user-account.model';

const passwordsMatch = (group: AbstractControl): ValidationErrors | null =>
  group.get('newPassword')?.value === group.get('confirmPassword')?.value ? null : { mismatch: true };

@Component({
  selector: 'app-user-account',
  standalone: true,
  imports: [DatePipe, RouterLink, ReactiveFormsModule, SegmentedTabsComponent],
  templateUrl: './user-account.component.html',
  styleUrl: './user-account.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserAccountComponent implements OnInit {
  readonly limits = FIELD_LIMITS;

  private profileService = inject(ProfileService);
  private userService = inject(UserService);
  private authApi = inject(AuthApiService);
  private notification = inject(NotificationService);
  private confirmation = inject(ConfirmationService);
  private destroyRef = inject(DestroyRef);

  readonly demoMessage = 'Not available in demo mode. Register and log in to use this feature.';
  readonly isDemo = this.authApi.isDemoUser();

  readonly tabs: SegmentedTab[] = [
    { id: 'personal', label: 'Personal' },
    { id: 'security', label: 'Security' }
  ];

  /**
   * Pages the bottom nav has no tab for. Exercise progress (Training) and Settings (header gear)
   * are reachable elsewhere; on desktop the sidebar covers all of them, so the list is hidden there.
   */
  readonly links: AccountLink[] = [
    { label: 'Billing & payments', hint: 'Plans, invoices, payment history', icon: 'fa-solid fa-file-invoice-dollar', tone: 'info', route: '/user/billing' },
    { label: 'Health tracker', hint: 'Weight, body fat, measurements', icon: 'fa-solid fa-heart-pulse', tone: 'good', route: '/user/health-tracker' }
  ];

  readonly profile = signal<UserProfile | null>(null);
  readonly membership = signal<MyGymMembership | null>(null);
  readonly activeTab = signal<'personal' | 'security'>('personal');
  readonly isEditing = signal(false);
  readonly isSaving = signal(false);
  readonly isChangingPassword = signal(false);
  readonly isDeleting = signal(false);

  readonly avatarPreview = signal<string | null>(null);
  private selectedAvatar: File | null = null;
  readonly isUploadingAvatar = signal(false);

  readonly profileForm = new FormGroup<ProfileForm>({
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50)] }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50)] }),
    phone: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(20)] }),
    addressLine1: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(FIELD_LIMITS.ADDRESS_LINE)] }),
    addressLine2: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(FIELD_LIMITS.ADDRESS_LINE)] }),
    city: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(FIELD_LIMITS.CITY)] }),
    state: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(FIELD_LIMITS.STATE)] }),
    zipCode: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(FIELD_LIMITS.POSTAL_CODE), postalCodeValidator] })
  });

  readonly passwordForm = new FormGroup<PasswordForm>({
    currentPassword: new FormControl('', { nonNullable: true, validators: existingPasswordValidators }),
    newPassword: new FormControl('', { nonNullable: true, validators: newPasswordValidators }),
    confirmPassword: new FormControl('', { nonNullable: true, validators: existingPasswordValidators })
  }, { validators: passwordsMatch });

  readonly initials = computed(() => {
    const p = this.profile();
    return ((p?.firstName?.charAt(0) ?? '') + (p?.lastName?.charAt(0) ?? '')).toUpperCase() || '?';
  });

  readonly avatarUrl = computed(() => this.avatarPreview() ?? this.profileService.getFullUrl(this.profile()?.profilePictureUrl));

  readonly addressLines = computed(() => {
    const p = this.profile();
    if (!p) return [];
    const cityLine = [p.city, p.state, p.zipCode].filter(Boolean).join(', ');
    return [p.addressLine1, p.addressLine2, cityLine].filter((line): line is string => !!line);
  });

  ngOnInit(): void {
    this.loadProfile();
    this.userService.getMyGym().pipe(
      map(res => ((res as { data?: MyGymMembership })?.data ?? null) as MyGymMembership | null),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({ next: m => this.membership.set(m), error: () => this.membership.set(null) });
  }

  private loadProfile(forceRefresh = false): void {
    this.profileService.getProfile(forceRefresh).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: response => {
        const data = ((response as unknown as { data?: UserProfile })?.data ?? response) as UserProfile;
        this.profile.set(data);
        this.resetProfileForm(data);
      },
      error: err => console.error('Failed to load profile', err)
    });
  }

  private resetProfileForm(p: UserProfile): void {
    this.profileForm.reset({
      firstName: p.firstName ?? '',
      lastName: p.lastName ?? '',
      phone: p.phone ?? '',
      addressLine1: p.addressLine1 ?? '',
      addressLine2: p.addressLine2 ?? '',
      city: p.city ?? '',
      state: p.state ?? '',
      zipCode: p.zipCode ?? ''
    });
  }

  setTab(tab: string): void {
    this.activeTab.set(tab === 'security' ? 'security' : 'personal');
  }

  startEdit(): void {
    if (this.isDemo) return;
    this.isEditing.set(true);
  }

  cancelEdit(): void {
    const p = this.profile();
    if (p) this.resetProfileForm(p);
    this.isEditing.set(false);
  }

  async saveProfile(): Promise<void> {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }
    this.isSaving.set(true);
    try {
      await this.persistProfile(this.profile()?.profilePictureUrl ?? '');
      this.isEditing.set(false);
      this.notification.success(CONSTANTS.PROFILE_UPDATE_SUCCESS_MESSAGE);
    } catch (error) {
      this.notification.error((error as { error?: { message?: string } })?.error?.message || CONSTANTS.PROFILE_UPDATE_ERROR_MESSAGE);
    } finally {
      this.isSaving.set(false);
    }
  }

  private async persistProfile(profilePictureUrl: string): Promise<void> {
    const values = this.profileForm.getRawValue();
    const payload: UpdateUserProfile = {
      ...values,
      firstName: values.firstName.trim(),
      lastName: values.lastName.trim(),
      phone: values.phone.trim(),
      profilePictureUrl
    };
    await firstValueFrom(this.profileService.updateProfile(payload));
    this.loadProfile(true);
  }

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      this.notification.error('Please choose an image file.');
      return;
    }

    this.selectedAvatar = file;
    const reader = new FileReader();
    reader.onload = () => this.avatarPreview.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  cancelAvatar(): void {
    this.selectedAvatar = null;
    this.avatarPreview.set(null);
  }

  async saveAvatar(): Promise<void> {
    if (!this.selectedAvatar || this.isDemo) return;
    this.isUploadingAvatar.set(true);
    try {
      const upload = await firstValueFrom(this.profileService.uploadAvatar(this.selectedAvatar));
      const body = upload as unknown as { data?: { url?: string; Url?: string }; url?: string; Url?: string };
      const url = body?.data?.url || body?.data?.Url || body?.url || body?.Url;
      if (!url) throw new Error('Upload returned no URL');

      // Keep the stored details as they are; only the picture changes.
      const p = this.profile();
      if (p) this.resetProfileForm(p);
      await this.persistProfile(url);
      this.cancelAvatar();
      this.notification.success('Profile photo updated.');
    } catch {
      this.notification.error('Could not update your photo. Please try again.');
    } finally {
      this.isUploadingAvatar.set(false);
    }
  }

  changePassword(): void {
    if (this.passwordForm.invalid || this.isDemo) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    this.isChangingPassword.set(true);
    this.profileService.changePassword(this.passwordForm.getRawValue()).subscribe({
      next: () => {
        this.isChangingPassword.set(false);
        this.passwordForm.reset();
        this.notification.success('Password updated successfully.');
      },
      error: err => {
        this.isChangingPassword.set(false);
        this.notification.error(err?.error?.data?.message || err?.error?.message || 'Failed to change password. Please check your current password.');
      }
    });
  }

  sendResetLink(): void {
    const email = this.profile()?.email;
    if (!email || this.isDemo) return;
    this.authApi.forgotPassword({ email }).subscribe({
      next: () => this.notification.success('Reset link sent to your email.'),
      // Same message either way so we don't reveal whether an address is registered.
      error: () => this.notification.success('If the email is registered, a reset link has been sent.')
    });
  }

  async deleteAccount(): Promise<void> {
    if (this.isDemo) return;
    const confirmed = await this.confirmation.confirm({
      title: 'Delete your account?',
      message: 'Your profile, logs and history will be permanently removed. This cannot be undone.',
      confirmText: 'Delete account',
      cancelText: 'Cancel',
      type: 'danger'
    });
    if (!confirmed) return;

    this.isDeleting.set(true);
    this.userService.scheduleAccountDeletion().subscribe({
      next: res => {
        this.isDeleting.set(false);
        if ((res?.data as { deleted?: boolean } | null)?.deleted) {
          this.notification.success('Your account has been deleted.');
          this.authApi.logout();
          return;
        }
        this.notification.success('Account deletion scheduled. You will receive an email confirmation.');
      },
      error: err => {
        this.isDeleting.set(false);
        this.notification.error('Failed to schedule account deletion. ' + (err?.error?.message || ''));
      }
    });
  }

  logout(): void {
    this.authApi.logout();
  }
}
