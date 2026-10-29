import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AnnouncementService } from '../../../core/services/announcement.service';
import { TemplateService } from '../../../core/services/template.service';
import { NotificationService } from '../../../core/services/notification.service';
import { RouterModule } from '@angular/router';
import { DataGrid } from '../../../shared/components/data-grid/data-grid.component';
import { SlideDrawerComponent } from '../../../shared/components/slide-drawer/slide-drawer.component';
import { DateTimePickerComponent } from '../../../shared/components/date-time-picker/date-time-picker.component';
import { DropdownComponent } from '../../../shared/components/dropdown/dropdown.component';
import { AppGridConfig } from '../../../shared/constants/grid-config';
import { CONSTANTS } from '../../../core/constants/constants';
import { ConfirmationService } from '../../../core/services/confirmation.service';

@Component({
  selector: 'app-announcements',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, DataGrid, SlideDrawerComponent, DateTimePickerComponent, DropdownComponent],
  templateUrl: './announcements.component.html',
  styleUrl: './announcements.component.scss',
})
export class AnnouncementsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private announcementService = inject(AnnouncementService);
  private templateService = inject(TemplateService);
  private notificationService = inject(NotificationService);
  private confirmationService = inject(ConfirmationService);

  announcements: any[] = [];
  templates: any[] = [];
  templateOptions: any[] = [];
  announcementForm: FormGroup;
  isSubmitting = false;
  isDrawerOpen = false;
  minDate = new Date();
  editingAnnouncementId: string | null = null;

  gridConfig = AppGridConfig['Announcements'];
  totalItems = 0;
  pageSize = 10;
  currentPage = 1;

  constructor() {
    this.announcementForm = this.fb.group({
      templateId: [null],
      title: ['', [Validators.required, Validators.maxLength(100)]],
      message: ['', [Validators.required, Validators.maxLength(1000)]],
      isActive: [true],
      validUntil: [null]
    });
  }

  ngOnInit(): void {
    this.setupFormListeners();
    this.loadAnnouncements();
    this.loadTemplates();
  }

  setupFormListeners(): void {
    this.announcementForm.get('templateId')?.valueChanges.subscribe(templateId => {
      if (templateId) {
        const template = this.templates.find(t => t.id === templateId);
        if (template) {
          this.announcementForm.patchValue({
            title: template.titleTemplate,
            message: template.messageTemplate
          });
        }
      }
    });
  }

  loadAnnouncements(): void {
    this.announcementService.getAnnouncements().subscribe({
      next: (res) => {
        this.announcements = this.unwrapArray(res);
        this.announcements.sort((a, b) => new Date(b.createdOn).getTime() - new Date(a.createdOn).getTime());
        this.totalItems = this.announcements.length;
      },
      error: (err) => {
        this.notificationService.error('Failed to load announcements');
        console.error(err);
      }
    });
  }

  loadTemplates(): void {
    this.templateService.getTemplates().subscribe({
      next: (res: any) => {
        this.templates = this.unwrapArray(res);
        this.templateOptions = this.templates.map(t => ({ value: t.id, label: t.name }));
      },
      error: (err) => {
        console.error('Failed to load templates for dropdown', err);
      }
    });
  }

  openDrawer(announcement: any = null): void {
    if (announcement) {
      this.editingAnnouncementId = announcement.id;
      this.announcementForm.patchValue({
        title: announcement.title,
        message: announcement.message,
        isActive: announcement.isActive,
        validUntil: announcement.validUntil ? new Date(announcement.validUntil) : null
      });
    } else {
      this.editingAnnouncementId = null;
      this.announcementForm.reset({ isActive: true, templateId: null }, { emitEvent: false });
    }
    this.isDrawerOpen = true;
  }

  closeDrawer(): void {
    this.isDrawerOpen = false;
  }

  onSubmit(): void {
    if (this.announcementForm.invalid) {
      this.announcementForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const request = this.announcementForm.value;

    if (this.editingAnnouncementId) {
      this.announcementService.updateAnnouncement(this.editingAnnouncementId, request).subscribe({
        next: () => {
          this.notificationService.success('Announcement updated successfully!');
          this.loadAnnouncements();
          this.closeDrawer();
          this.isSubmitting = false;
        },
        error: (err) => {
          this.notificationService.error(err.error?.message || 'Failed to update announcement');
          this.isSubmitting = false;
          console.error(err);
        }
      });
    } else {
      this.announcementService.createAnnouncement(request).subscribe({
        next: () => {
          this.notificationService.success('Announcement broadcasted successfully!');
          this.loadAnnouncements();
          this.closeDrawer();
          this.isSubmitting = false;
        },
        error: (err) => {
          this.notificationService.error(err.error?.message || 'Failed to create announcement');
          this.isSubmitting = false;
          console.error(err);
        }
      });
    }
  }

  onAction(event: { action: string, row: any }): void {
    if (event.action === CONSTANTS.ACTIONS.EDIT) {
      this.openDrawer(event.row);
    } else if (event.action === CONSTANTS.ACTIONS.DELETE) {
      this.deleteAnnouncement(event.row.id);
    }
  }

  onPageChanged(page: number) {
    this.currentPage = page;
  }

  onPageSizeChanged(size: number) {
    this.pageSize = size;
    this.currentPage = 1;
  }

  deleteAnnouncement(id: string): void {
    this.confirmationService.confirm({
      title: 'Delete Announcement?',
      message: 'Are you sure you want to delete this notification Announcement? This action cannot be undone.',
      confirmText: 'Delete',
      cancelText: 'Cancel',
      type: 'danger'
    }).then(confirmed => {
      if(confirmed){
        this.announcementService.deleteAnnouncement(id).
        subscribe({
          next: () => {
            this.notificationService.success('Announcement deleted');
            this.loadAnnouncements();
          },
          error: (err) => {
            this.notificationService.error(err.error?.message || 'Failed to delete announcement');
          }
        })
      }
    })
  }

  private unwrapArray(response: any): any[] {
    if (Array.isArray(response)) return response;
    if (Array.isArray(response?.data)) return response.data;
    return [];
  }
}
