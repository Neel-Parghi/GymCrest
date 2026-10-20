import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ValidationMessage } from '../../../shared/components/validation-message/validation-message.component';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { phoneValidator, urlValidator, gstValidator } from '../../../shared/validators/custom-validators';
import { ActivatedRoute } from '@angular/router';
import { ConfigurationService } from '../../../core/services/configuration.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CONSTANTS } from '../../../core/constants/constants';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { ConfirmationService } from '../../../core/services/confirmation.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ValidationMessage],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit {
  readonly limits = FIELD_LIMITS;

  private fb = inject(FormBuilder);
  private configService = inject(ConfigurationService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private authService = inject(AuthApiService);
  private confirmation = inject(ConfirmationService);

  settingsForm: FormGroup;
  activeTab: 'general' | 'strategy' | 'financial' | 'legal' | 'status' = 'general';
  loading = true;
  saving = false;
  resettingDemo = false;
  isAdmin = false;

  constructor() {
    this.settingsForm = this.fb.group({
      platformName: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.PLATFORM_NAME)]],
      billingEmail: ['', [Validators.required, Validators.email, Validators.maxLength(FIELD_LIMITS.EMAIL)]],
      taxPercentage: [18, [Validators.required, Validators.min(0), Validators.max(100)]],
      gracePeriodDays: [7, [Validators.required, Validators.min(0)]],
      yearlyRevenueTarget: [0, [Validators.required, Validators.min(0)]],
      subscriptionTarget: [0, [Validators.required, Validators.min(0)]],
      uptimeThreshold: [99.9, [Validators.required, Validators.min(0), Validators.max(100)]],
      currency: [CONSTANTS.DASHBOARD.CURRENCY],
      supportPhone: ['', [Validators.maxLength(FIELD_LIMITS.PHONE), phoneValidator]],
      supportEmail: ['', [Validators.email, Validators.maxLength(FIELD_LIMITS.EMAIL)]],
      isMaintenanceMode: [false],
      maintenanceStartTime: [null],
      maintenanceEndTime: [null],
      termsUrl: ['', [Validators.maxLength(FIELD_LIMITS.URL), urlValidator]],
      privacyUrl: ['', [Validators.maxLength(FIELD_LIMITS.URL), urlValidator]],
      billingAddress: ['', [Validators.maxLength(FIELD_LIMITS.ADDRESS_BLOCK)]],
      gstNo: ['', [Validators.maxLength(FIELD_LIMITS.GST_NUMBER), gstValidator]]
    });
  }

  ngOnInit() {
    this.isAdmin = this.authService.getUserRole() === 'SuperAdmin';
    this.checkQueryParams();
    this.loadSettings();
    if (!this.isAdmin) {
      this.settingsForm.disable();
    }
  }

  private checkQueryParams() {
    this.route.queryParams.subscribe(params => {
      if (params['tab']) {
        const tab = params['tab'];
        if (tab === 'strategy' || tab === 'legal' || tab === 'general' || tab === 'financial' || tab === 'status') {
          this.activeTab = tab as any;
        }
      }
    });
  }

  formatDateForInput(dateString: any): string | null {
    if (!dateString) return null;
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return null;
    
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  loadSettings() {
    this.configService.getConfig().subscribe({
      next: (res) => {
        if (res.data) {
          const config = { ...res.data } as Record<string, any>;
          if (config['maintenanceStartTime']) {
            config['maintenanceStartTime'] = this.formatDateForInput(config['maintenanceStartTime']);
          }
          if (config['maintenanceEndTime']) {
            config['maintenanceEndTime'] = this.formatDateForInput(config['maintenanceEndTime']);
          }
          this.settingsForm.patchValue(config);
        }
        this.loading = false;
      }
    });
  }

  saveSettings() {
    if (this.settingsForm.invalid) return;

    this.saving = true;
    this.configService.updateConfig(this.settingsForm.value).subscribe({
      next: () => {
        this.notification.success(CONSTANTS.SETTINGS.UPDATE_SUCCESS);
        this.saving = false;
      },
      error: () => {
        this.notification.error(CONSTANTS.SETTINGS.UPDATE_ERROR);
        this.saving = false;
      }
    });
  }

  resetDemoMember() {
    this.confirmation.confirm({
      title: 'Reset demo member',
      message: 'This replaces everything in the demo member account with a fresh plan and two weeks of workouts, meals and check-ins. Anything visitors logged will be removed.',
      type: 'danger'
    }).then(confirmed => {
      if (!confirmed) return;
      this.resettingDemo = true;
      this.configService.resetDemoMember().subscribe({
        next: (res) => {
          const r = res.data;
          this.notification.success(r
            ? `Demo member reset: ${r.workoutsLogged} workouts, ${r.daysOfMealsLogged} days of meals, ${r.healthCheckIns} check-ins.`
            : 'Demo member reset.');
          this.resettingDemo = false;
        },
        error: (err) => {
          this.notification.error(err.error?.message || 'Could not reset the demo member.');
          this.resettingDemo = false;
        }
      });
    });
  }
}
