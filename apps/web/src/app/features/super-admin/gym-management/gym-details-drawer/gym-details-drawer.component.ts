import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FIELD_LIMITS } from '../../../../shared/constants/validation.constants';
import { phoneValidator, urlValidator, gstValidator, registrationNumberValidator } from '../../../../shared/validators/custom-validators';
import { NotificationService } from '../../../../core/services/notification.service';
import { GymListResponse } from '../../../../shared/models/gym.model';
import { SlideDrawerComponent } from "../../../../shared/components/slide-drawer/slide-drawer.component";
import { ValidationMessage } from "../../../../shared/components/validation-message/validation-message.component";
import { AddBranchModalComponent } from '../add-branch-modal/add-branch-modal.component';
import { GymService } from '../../../../core/services/gym.service';

@Component({
  selector: 'app-gym-details-drawer',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SlideDrawerComponent, ValidationMessage, AddBranchModalComponent],
  templateUrl: './gym-details-drawer.component.html',
  styleUrl: './gym-details-drawer.component.scss'
})
export class GymDetailsDrawerComponent implements OnChanges {
  readonly limits = FIELD_LIMITS;

  private fb = inject(FormBuilder);
  private notification = inject(NotificationService);
  private gymService = inject(GymService);

  @Input() isOpen = false;
  @Input() isEditing = false;
  @Input() set gym(value: GymListResponse | undefined) {
    this._gym = value;
    if (value) {
      this.initForm(value);
    }
  }

  get gym() { return this._gym; }
  private _gym?: GymListResponse;

  get ownerInitials(): string {
    const parts = (this._gym?.ownerName || '').trim().split(/\s+/).filter(Boolean);
    return parts.slice(0, 2).map(p => p[0].toUpperCase()).join('') || '?';
  }

  get addressText(): string {
    const a = this._gym?.address;
    if (!a) return '';
    return [a.line1, a.line2, a.city, a.state, a.postalCode, a.country].filter(Boolean).join(', ');
  }

  @Output() closeDrawer = new EventEmitter<void>();
  @Output() gymUpdated = new EventEmitter<any>();

  activeTab: 'overview' | 'branches' = 'overview';
  branches: any[] = [];
  isLoadingBranches = false;
  isAddBranchModalOpen = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isEditing'] || changes['gym']) {
      if (this.isEditing) {
        this.editForm.enable();
      } else {
        this.editForm.disable();
      }
    }
    if (changes['isOpen'] && this.isOpen) {
      this.activeTab = 'overview';
      if (this.gym?.id) {
        this.loadBranches();
      }
    }
  }

  editForm = this.fb.group({
    id: [''],
    gymName: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.GYM_NAME)]],
    brandName: ['', [Validators.maxLength(FIELD_LIMITS.BRAND_NAME)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(FIELD_LIMITS.EMAIL)]],
    phone: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.PHONE), phoneValidator]],
    websiteUrl: ['', [Validators.maxLength(FIELD_LIMITS.URL), urlValidator]],
    description: ['', [Validators.maxLength(FIELD_LIMITS.DESCRIPTION)]],
    gstNumber: ['', [Validators.maxLength(FIELD_LIMITS.GST_NUMBER), gstValidator]],
    registrationNumber: ['', [Validators.maxLength(FIELD_LIMITS.REGISTRATION_NUMBER), registrationNumberValidator]],
    establishedDate: [''],
    invoicePrefix: ['', [Validators.required, Validators.maxLength(10)]],
    defaultTaxRate: [18 as number | null, [Validators.required, Validators.min(0), Validators.max(50)]],
    overdueGraceDays: [7 as number | null, [Validators.required, Validators.min(0), Validators.max(365)]],
    autoEmailReceipts: [true],
    isActive: [true],
    isVerified: [false]
  });

  initForm(gym: GymListResponse) {
    this.editForm.patchValue({
      id: gym.id,
      gymName: gym.gymName,
      brandName: gym.brandName,
      email: gym.email,
      phone: gym.phone,
      websiteUrl: gym.websiteUrl,
      description: gym.description,
      gstNumber: gym.gstNumber,
      registrationNumber: gym.registrationNumber,
      establishedDate: gym.establishedDate ? gym.establishedDate.substring(0, 10) : '',
      invoicePrefix: gym.invoicePrefix || 'GF-',
      defaultTaxRate: gym.defaultTaxRate ?? 18,
      overdueGraceDays: gym.overdueGraceDays ?? 7,
      autoEmailReceipts: gym.autoEmailReceipts ?? true,
      isActive: gym.isActive,
      isVerified: gym.isVerified
    });
  }

  toggleEdit() {
    this.isEditing = true;
    this.editForm.enable();
  }

  cancelEdit() {
    this.isEditing = false;
    if (this._gym) {
      this.initForm(this._gym);
    }
    this.editForm.disable();
  }

  onClose() {
    this.cancelEdit();
    this.closeDrawer.emit();
  }

  save() {
    if (this.editForm.valid && this._gym?.id) {
      const value = this.editForm.getRawValue();
      this.gymUpdated.emit({ ...value, establishedDate: value.establishedDate || null });
      this.isEditing = false;
      this.editForm.disable();
      this.onClose();
    }
  }

  loadBranches() {
    if (!this.gym?.id) return;
    this.isLoadingBranches = true;
    this.gymService.getGymBranches(this.gym.id).subscribe({
      next: (res) => {
        this.branches = res.data || [];
        this.isLoadingBranches = false;
      },
      error: () => this.isLoadingBranches = false
    });
  }

  switchTab(tab: 'overview' | 'branches') {
    this.activeTab = tab;
    if (tab === 'branches') {
      this.loadBranches();
    }
  }
}
