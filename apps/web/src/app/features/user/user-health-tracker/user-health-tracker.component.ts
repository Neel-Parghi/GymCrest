import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PTMemberDetailHealthTrackerComponent } from '../../trainer/member-detail/components/member-detail-health-tracker/member-detail-health-tracker.component';
import { StaffService } from '../../../core/services/staff.service';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { UserService } from '../../../core/services/user.service';

@Component({
  selector: 'app-user-health-tracker',
  standalone: true,
  imports: [CommonModule, PTMemberDetailHealthTrackerComponent],
  templateUrl: './user-health-tracker.component.html',
  styleUrl: './user-health-tracker.component.scss',
})
export class UserHealthTrackerComponent implements OnInit {
  private staffService = inject(StaffService);
  private authService = inject(AuthApiService);
  private notification = inject(NotificationService);
  private userService = inject(UserService);

  userId = '';
  measurements: any[] = [];
  isLoadingLogs = false;
  isSubmittingProgress = false;

  ngOnInit() {
    this.authService.userProfile$.subscribe(profile => {
      if (profile) {
        this.userId = profile.id;
        this.loadMeasurementsLogs();
      }
    });
  }

  loadMeasurementsLogs(): void {
    this.isLoadingLogs = true;
    this.staffService.getMemberMeasurements(this.userId).subscribe({
      next: (res: any) => {
        this.measurements = res?.data || [];
        this.measurements.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
        this.isLoadingLogs = false;
      },
      error: () => {
        this.measurements = [];
        this.isLoadingLogs = false;
      }
    });
  }

  onRecordMeasurement(payload: any) {
    this.isSubmittingProgress = true;
    this.staffService.recordMeasurement(this.userId, {
      weight: payload.weight,
      height: payload.height,
      bodyFatPercentage: payload.bodyFatPercentage || undefined,
      bmi: payload.bmi || undefined,
      notes: payload.notes || undefined,
      isAdvanced: payload.isAdvanced || false,
      neck: payload.neck || undefined,
      shoulders: payload.shoulders || undefined,
      chest: payload.chest || undefined,
      leftBicep: payload.leftBicep || undefined,
      rightBicep: payload.rightBicep || undefined,
      leftForearm: payload.leftForearm || undefined,
      rightForearm: payload.rightForearm || undefined,
      upperAbs: payload.upperAbs || undefined,
      lowerAbs: payload.lowerAbs || undefined,
      waist: payload.waist || undefined,
      hips: payload.hips || undefined,
      leftThigh: payload.leftThigh || undefined,
      rightThigh: payload.rightThigh || undefined,
      leftCalf: payload.leftCalf || undefined,
      rightCalf: payload.rightCalf || undefined
    }).subscribe({
      next: () => {
        this.notification.success('Health progress logged successfully.');
        // The dashboard caches its summary; drop it so weight and goal progress show the new entry.
        this.userService.clearCache();
        this.loadMeasurementsLogs();
        this.isSubmittingProgress = false;
      },
      error: () => {
        this.notification.error('Failed to log health progress.');
        this.isSubmittingProgress = false;
      }
    });
  }
}
