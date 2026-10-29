import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { NotificationService } from '../../../core/services/notification.service';
import { DropdownComponent } from '../../../shared/components/dropdown/dropdown.component';
import { DropdownOption } from '../../../shared/models/dropdown.model';
import { DateTimePickerComponent } from '../../../shared/components/date-time-picker/date-time-picker.component';
import { GymSettingsService } from '../../../core/services/gym-settings.service';
import { GymService } from '../../../core/services/gym.service';
import { CONSTANTS } from '../../../core/constants/constants';
import { Holiday } from '../../../shared/models/gym.model';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, DropdownComponent, DateTimePickerComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit {
  readonly limits = FIELD_LIMITS;

  private fb = inject(FormBuilder);
  private notification = inject(NotificationService);
  private settingsService = inject(GymSettingsService);
  private gymService = inject(GymService);

  settingsForm!: FormGroup;
  holidayForm!: FormGroup;
  activeTab: 'permissions' | 'notifications' | 'calendar' = 'permissions';
  saving = false;

  // Dropdown Options
  expiryWarningOptions: DropdownOption[] = [
    { label: '3 Days Before', value: 3 },
    { label: '5 Days Before', value: 5 },
    { label: '7 Days Before', value: 7 },
    { label: '10 Days Before', value: 10 },
    { label: '14 Days Before', value: 14 }
  ];

  branchOptions: DropdownOption[] = [];

  // RBAC Config Schema
  roles = [
    { key: 'Staff', label: 'Front Desk Staff' }
  ];

  modules = [
    { key: 'dashboard', label: 'Dashboard', icon: 'fa-solid fa-house' },
    { key: 'members', label: 'Members Management', icon: 'fa-solid fa-users' },
    { key: 'attendance', label: 'Attendance Counter', icon: 'fa-solid fa-calendar-check' },
    { key: 'staff', label: 'Staff & Trainers', icon: 'fa-solid fa-user-tie' },
    { key: 'plans', label: 'Gym Plans & Pricing', icon: 'fa-solid fa-receipt' },
    { key: 'inventory', label: 'Inventory Assets', icon: 'fa-solid fa-boxes-stacked' },
    { key: 'billing', label: 'Billing & Invoices', icon: 'fa-solid fa-file-invoice-dollar' }
  ];

  holidays: Holiday[] = [];

  ngOnInit() {
    this.initForm();
    this.loadSettings();
    this.loadBranches();
    this.loadHolidays();
  }

  private initForm(savedRights: any = {}, savedSettings: any = {}) {
    const matrixGroup: any = {};
    this.roles.forEach(role => {
      this.modules.forEach(mod => {
        const key = `${role.key}_${mod.key}`;

        // Define fallback defaults if no settings are present
        let defaultValue = true;
        if (role.key === 'Trainer') {
          defaultValue = mod.key === 'dashboard' || mod.key === 'members' || mod.key === 'attendance';
        } else if (role.key === 'Staff') {
          defaultValue = mod.key === 'dashboard' || mod.key === 'members' || mod.key === 'attendance' || mod.key === 'inventory';
        }

        const value = savedRights[key] !== undefined ? savedRights[key] : defaultValue;
        matrixGroup[key] = [value];
      });
    });

    const defaults = {
      expiryWarningDays: 7,
      ...savedSettings
    };

    matrixGroup['expiryWarningDays'] = [defaults.expiryWarningDays, [Validators.required]];

    this.settingsForm = this.fb.group(matrixGroup);

    this.holidayForm = this.fb.group({
      newHolidayName: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.SHORT_TEXT)]],
      newHolidayDate: ['', Validators.required],
      newHolidayBranch: ['All Locations', Validators.required]
    });
  }

  private loadSettings() {
    // 1. Check if settings are already preloaded in cache
    const cache = this.settingsService.getSettingsSync();
    if (cache) {
      this.initForm(cache['roleRights'] || {}, cache['operations'] || {});
      return;
    }

    this.settingsService.loadSettings().subscribe({
      next: () => {
        const data = this.settingsService.getSettingsSync();
        if (data) {
          this.initForm(data['roleRights'] || {}, data['operations'] || {});
        }
      },
      error: () => this.notification.error(CONSTANTS.GYM_OWNER_SETTINGS.LOAD_ERROR)
    });
  }

  saveSettings() {
    if (this.settingsForm.invalid) {
      this.notification.error(CONSTANTS.GYM_OWNER_SETTINGS.VALIDATION_ERROR);
      return;
    }

    this.saving = true;

    // Gather Role Rights Matrix
    const rightsMatrix: any = {};
    this.roles.forEach(role => {
      this.modules.forEach(mod => {
        const key = `${role.key}_${mod.key}`;
        rightsMatrix[key] = this.settingsForm.value[key];
      });
    });

    // Gather Automations Settings
    const automationsSettings = {
      expiryWarningDays: this.settingsForm.value.expiryWarningDays
    };

    const payload = {
      roleRightsMatrixJson: JSON.stringify(rightsMatrix),
      planExpirationTriggerDays: this.settingsForm.value.expiryWarningDays
    };

    this.settingsService.updateSettings(payload, rightsMatrix, automationsSettings).subscribe({
      next: () => {
        this.saving = false;
        this.notification.success(CONSTANTS.GYM_OWNER_SETTINGS.UPDATE_SUCCESS);
        this.settingsForm.markAsPristine();
      },
      error: (err) => {
        this.saving = false;
        this.notification.error(CONSTANTS.GYM_OWNER_SETTINGS.UPDATE_ERROR);
        console.error('Error saving settings', err);
      }
    });
  }

  loadBranches() {
    this.gymService.getMyBranches().subscribe({
      next: (res) => {
        const branchesList = res.data || res || [];
        const options: DropdownOption[] = [{ label: 'All Locations', value: 'All Locations' }];
        branchesList.forEach((b: any) => {
          options.push({ label: b.name, value: b.id });
        });
        this.branchOptions = options;
      },
      error: () => { }
    });
  }

  loadHolidays() {
    this.settingsService.getHolidays().subscribe({
      next: (res: any) => {
        this.holidays = (res.data as any[]) || (res as any[]) || [];
      },
      error: (err) => console.error('Failed to load holidays closures', err)
    });
  }

  addHoliday() {
    if (this.holidayForm.invalid) {
      this.notification.error(CONSTANTS.GYM_OWNER_SETTINGS.HOLIDAY_VALIDATION_ERROR);
      return;
    }

    const formVal = this.holidayForm.value;

    // Resolve branchId (if 'All Locations', it is null)
    const branchId = formVal.newHolidayBranch === 'All Locations' ? null : formVal.newHolidayBranch;

    const payload = {
      name: formVal.newHolidayName,
      date: formVal.newHolidayDate,
      branchId: branchId
    };

    this.settingsService.addHoliday(payload).subscribe({
      next: () => {
        this.notification.success(CONSTANTS.GYM_OWNER_SETTINGS.HOLIDAY_CREATE_SUCCESS);
        this.holidayForm.reset({
          newHolidayName: '',
          newHolidayDate: '',
          newHolidayBranch: 'All Locations'
        });
        this.loadHolidays();
      },
      error: () => this.notification.error(CONSTANTS.GYM_OWNER_SETTINGS.HOLIDAY_CREATE_ERROR)
    });
  }

  deleteHoliday(id: string) {
    this.settingsService.deleteHoliday(id).subscribe({
      next: () => {
        this.notification.success(CONSTANTS.GYM_OWNER_SETTINGS.HOLIDAY_DELETE_SUCCESS);
        this.loadHolidays();
      },
      error: (err) => this.notification.error(err.error?.message || CONSTANTS.GYM_OWNER_SETTINGS.HOLIDAY_DELETE_ERROR)
    });
  }
}
