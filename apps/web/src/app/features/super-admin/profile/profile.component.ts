import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormControl } from '@angular/forms';
import { ValidationMessage } from '../../../shared/components/validation-message/validation-message.component';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { phoneValidator, urlValidator, postalCodeValidator, newPasswordValidators, existingPasswordValidators } from '../../../shared/validators/custom-validators';
import { CommonModule } from '@angular/common';
import { ProfileService } from '../../../core/services/profile.service';
import { StaffService } from '../../../core/services/staff.service';
import { UserProfile } from '../../../shared/models/user-profile.model';
import { RouterModule } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { CONSTANTS } from '../../../core/constants/constants';
import { API_CONSTANTS } from '../../../core/constants/api-constants';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { UserService } from '../../../core/services/user.service';
import { ConfirmationService } from '../../../core/services/confirmation.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, ValidationMessage],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  readonly limits = FIELD_LIMITS;

  profile?: UserProfile;
  profileForm: FormGroup;
  passwordForm: FormGroup;
  isEditMode = false;
  isSaving = false;
  isChangingPassword = false;
  passwordChangeSuccess = false;
  passwordChangeError = '';
  activeTab: 'personal' | 'security' = 'personal';
  readonly demoRestrictionMessage = 'Not available in demo mode, please register and login to use feature';

  get isDemo(): boolean {
    return this.authApi.isDemoUser();
  }
  activeSecuritySubTab: 'change' | 'reset' | 'delete' = 'change';
  selectedFile: File | null = null;
  previewUrl: string | null = null;
  isDeletingAccount = false;

  isTrainer = false;
  isGymOwner = false;
  specControl = new FormControl('', Validators.maxLength(FIELD_LIMITS.SHORT_TEXT));

  constructor(
    private fb: FormBuilder,
    private profileService: ProfileService,
    private staffService: StaffService,
    private notification: NotificationService,
    private authApi: AuthApiService,
    private userService: UserService,
    private confirmationService: ConfirmationService
  ) {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.PERSON_NAME)]],
      lastName: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.PERSON_NAME)]],
      phone: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.PHONE), phoneValidator]],
      profilePictureUrl: [''],
      addressLine1: ['', [Validators.maxLength(FIELD_LIMITS.ADDRESS_LINE)]],
      addressLine2: ['', [Validators.maxLength(FIELD_LIMITS.ADDRESS_LINE)]],
      city: ['', [Validators.maxLength(FIELD_LIMITS.CITY)]],
      state: ['', [Validators.maxLength(FIELD_LIMITS.STATE)]],
      zipCode: ['', [Validators.maxLength(FIELD_LIMITS.POSTAL_CODE), postalCodeValidator]],
      bio: ['', [Validators.maxLength(FIELD_LIMITS.DESCRIPTION)]],
      experienceYears: [0],
      instagramUrl: ['', [Validators.maxLength(FIELD_LIMITS.URL), urlValidator]],
      portfolioUrl: ['', [Validators.maxLength(FIELD_LIMITS.URL), urlValidator]],
      shiftTimings: ['', [Validators.maxLength(FIELD_LIMITS.SHORT_TEXT)]],
      specializations: [[]]
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', existingPasswordValidators],
      newPassword: ['', newPasswordValidators],
      confirmPassword: ['', existingPasswordValidators]
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(g: FormGroup) {
    return g.get('newPassword')?.value === g.get('confirmPassword')?.value
      ? null : { mismatch: true };
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(forceRefresh = false) {
    this.profileService.getProfile(forceRefresh).subscribe({
      next: (response: any) => {
        const data = response.data || response;

        this.profile = data;
        this.isTrainer = data.role === 'Trainer';
        this.isGymOwner = data.role === 'GymOwner';

        this.profileForm.patchValue({
          firstName: data.firstName,
          lastName: data.lastName,
          phone: data.phone,
          profilePictureUrl: data.profilePictureUrl,
          addressLine1: data.addressLine1,
          addressLine2: data.addressLine2,
          city: data.city,
          state: data.state,
          zipCode: data.zipCode
        });

        if (this.isTrainer) {
          this.staffService.getStaffById(data.id).subscribe({
            next: (res: any) => {
              if (res?.data) {
                this.profileForm.patchValue({
                  bio: res.data.bio || '',
                  experienceYears: res.data.experienceYears || 0,
                  instagramUrl: res.data.instagramUrl || '',
                  portfolioUrl: res.data.portfolioUrl || '',
                  shiftTimings: res.data.shiftTimings || '',
                  specializations: res.data.specializations || []
                });
              }
            },
            error: (err) => console.error('Failed to load trainer details', err)
          });
        }

        this.selectedFile = null;
        this.previewUrl = null;
      },
      error: (err) => console.error('Failed to load profile', err)
    });
  }

  addSpecialization(): void {
    const val = (this.specControl.value || '').trim();
    const specs = this.profileForm.get('specializations')?.value || [];
    if (val && !specs.includes(val)) {
      specs.push(val);
      this.profileForm.patchValue({ specializations: specs });
      this.specControl.setValue('');
    }
  }

  removeSpecialization(index: number): void {
    const specs = this.profileForm.get('specializations')?.value || [];
    specs.splice(index, 1);
    this.profileForm.patchValue({ specializations: specs });
  }

  toggleEdit() {
    this.isEditMode = !this.isEditMode;
    if (!this.isEditMode) {
      this.loadProfile();
    }
  }

  async saveProfile() {
    if (this.profileForm.invalid) return;

    this.isSaving = true;

    try {
      if (this.selectedFile) {
        const uploadRes = (await this.profileService.uploadAvatar(this.selectedFile).toPromise()) as Record<string, any>;
        const resData = uploadRes?.['data'] as Record<string, any>;
        const newUrl = resData?.['url'] || resData?.['Url'] ||
          uploadRes?.['url'] || uploadRes?.['Url'];

        if (newUrl) {
          this.profileForm.patchValue({ profilePictureUrl: newUrl });
          if (this.profile) this.profile.profilePictureUrl = newUrl;
        }
      }

      await this.profileService.updateProfile(this.profileForm.value).toPromise();

      if (this.isTrainer && this.profile?.id) {
        const staffPayload = {
          firstName: this.profileForm.value.firstName,
          lastName: this.profileForm.value.lastName,
          email: this.profile?.email || '',
          phoneNumber: this.profileForm.value.phone,
          role: (this.profile as any).roleId || 1,
          branchId: (this.profile as any).branchId || undefined,
          bio: this.profileForm.value.bio,
          experienceYears: this.profileForm.value.experienceYears,
          instagramUrl: this.profileForm.value.instagramUrl,
          portfolioUrl: this.profileForm.value.portfolioUrl,
          shiftTimings: this.profileForm.value.shiftTimings,
          specializations: this.profileForm.value.specializations
        };
        await this.staffService.updateStaff(this.profile.id, staffPayload).toPromise();
      }

      this.isSaving = false;
      this.isEditMode = false;
      this.selectedFile = null;
      this.previewUrl = null;
      this.notification.success(CONSTANTS.PROFILE_UPDATE_SUCCESS_MESSAGE);

      this.loadProfile(true);
    } catch (error: any) {
      this.isSaving = false;
      this.notification.error(error.error?.message || CONSTANTS.PROFILE_UPDATE_ERROR_MESSAGE);
    }
  }

  submitPasswordChange() {
    if (this.passwordForm.invalid) return;

    this.isChangingPassword = true;
    this.passwordChangeError = '';
    this.passwordChangeSuccess = false;

    const payload = {
      currentPassword: this.passwordForm.value.currentPassword,
      newPassword: this.passwordForm.value.newPassword,
      confirmPassword: this.passwordForm.value.confirmPassword
    };

    this.profileService.changePassword(payload).subscribe({
      next: () => {
        this.isChangingPassword = false;
        this.passwordChangeSuccess = true;
        this.passwordForm.reset();
        this.notification.success('Password updated successfully.');
        setTimeout(() => this.passwordChangeSuccess = false, 3000);
      },
      error: (err: any) => {
        this.isChangingPassword = false;
        const msg = err?.error?.data?.message || err?.error?.message || 'Failed to change password. Please check your current password.';
        this.passwordChangeError = msg;
        this.notification.error(msg);
      }
    });
  }

  getImageUrl(path: string | undefined): string | null {
    if (this.previewUrl) return this.previewUrl;
    if (!path) return null;
    if (path.startsWith('http')) return path;
    const baseUrl = API_CONSTANTS.BASE_URL.replace('/api', '');
    return `${baseUrl}${path}`;
  }

  onFileSelected(event: any) {
    const file: File = event.target.files[0];
    if (file) {
      this.selectedFile = file;

      const reader = new FileReader();
      reader.onload = () => {
        this.previewUrl = reader.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  requestReset() {
    if (!this.profile?.email) {
      this.notification.error('Email not found.');
      return;
    }

    this.authApi.forgotPassword({ email: this.profile.email }).subscribe({
      next: () => {
        this.notification.success('Verification link sent to your email.');
      },
      error: () => {
        this.notification.success('If the email is registered, a verification code has been sent.');
      }
    });
  }

  triggerAvatarUpload() {
    const fileInput = document.getElementById('avatarInput') as HTMLInputElement;
    fileInput?.click();
  }

  async scheduleAccountDeletion() {
    const confirmed = await this.confirmationService.confirm({
      message: 'Are you sure you want to delete your account? This action cannot be undone.',
      confirmText: 'Schedule Deletion',
      cancelText: 'Cancel',
      type: 'danger'
    });

    if (!confirmed) return;

    this.isDeletingAccount = true;
    this.userService.scheduleAccountDeletion().subscribe({
      next: (res: any) => {
        this.isDeletingAccount = false;
        this.notification.success(res?.data?.message || res?.message || 'Account deletion scheduled successfully.');
      },
      error: (err) => {
        this.isDeletingAccount = false;
        this.notification.error('Failed to schedule account deletion. ' + (err.error?.message || ''));
      }
    });
  }
}
