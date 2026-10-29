import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FIELD_LIMITS } from '../../../../shared/constants/validation.constants';
import { phoneValidator } from '../../../../shared/validators/custom-validators';
import { SlideDrawerComponent } from "../../../../shared/components/slide-drawer/slide-drawer.component";
import { ValidationMessage } from '../../../../shared/components/validation-message/validation-message.component';
import { GymOwnerResponse } from '../../../../shared/models/gym.model';

@Component({
  selector: 'app-owner-details-drawer',
  standalone: true,
  imports: [CommonModule, SlideDrawerComponent, ReactiveFormsModule, ValidationMessage],
  providers: [DatePipe],
  templateUrl: './owner-details-drawer.component.html',
  styleUrl: './owner-details-drawer.component.scss'
})
export class OwnerDetailsDrawerComponent implements OnChanges {
  readonly limits = FIELD_LIMITS;

  @Input() isOpen = false;
  @Input() isEditing = false;
  @Input() ownerContext: GymOwnerResponse | null = null;

  @Output() closeDrawer = new EventEmitter<void>();
  @Output() resendInvite = new EventEmitter<string>();
  @Output() saveOwner = new EventEmitter<any>();
  @Output() isEditingChange = new EventEmitter<boolean>();

  editForm!: FormGroup;

  private fb = inject(FormBuilder);

  constructor() {
    this.editForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(FIELD_LIMITS.PERSON_NAME)]],
      lastName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(FIELD_LIMITS.PERSON_NAME)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(FIELD_LIMITS.EMAIL)]],
      phone: ['', [Validators.required, Validators.maxLength(FIELD_LIMITS.PHONE), phoneValidator]]
    });
    this.editForm.disable();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['ownerContext'] && this.ownerContext) {
      this.editForm.patchValue({
        firstName: this.ownerContext?.firstName || '',
        lastName: this.ownerContext?.lastName || '',
        email: this.ownerContext?.email || '',
        phone: this.ownerContext?.phone || ''
      });
    }

    if (changes['isEditing'] || changes['ownerContext']) {
      if (this.isEditing) {
        this.editForm.enable();
      } else {
        this.editForm.disable();
      }
    }
  }

  toggleEdit() {
    this.isEditing = true;
    this.isEditingChange.emit(true);
    this.editForm.enable();
  }

  cancelEdit() {
    this.isEditing = false;
    this.isEditingChange.emit(false);
    this.editForm.patchValue({
      firstName: this.ownerContext?.firstName || '',
      lastName: this.ownerContext?.lastName || '',
      email: this.ownerContext?.email || '',
      phone: this.ownerContext?.phone || ''
    });
    this.editForm.disable();
  }

  save() {
    if (this.editForm.valid && this.isEditing) {
      this.saveOwner.emit({
        id: this.ownerContext?.id,
        status: this.ownerContext?.status,
        ...this.editForm.value
      });
      this.isEditing = false;
      this.isEditingChange.emit(false);
      this.editForm.disable();
    }
  }

  onClose() {
    this.cancelEdit();
    this.closeDrawer.emit();
  }

  onResendInvite() {
    if (this.ownerContext?.id) {
      this.resendInvite.emit(this.ownerContext.id);
    }
  }
}
