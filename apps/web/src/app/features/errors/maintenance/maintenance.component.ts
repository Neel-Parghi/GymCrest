import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { interval, startWith, switchMap } from 'rxjs';
import { BrandLogoComponent } from '../../../shared/components/brand-logo/brand-logo.component';
import { SupportContactComponent } from '../../../shared/components/support-contact/support-contact.component';
import { PlatformStatusService } from '../../../core/services/platform-status.service';

const RECHECK_MS = 60_000;

/** Shown while the super admin has the platform in maintenance mode; returns to the app once it ends. */
@Component({
  selector: 'app-maintenance',
  imports: [DatePipe, BrandLogoComponent, SupportContactComponent],
  templateUrl: './maintenance.component.html',
  styleUrl: './maintenance.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MaintenanceComponent implements OnInit {
  private readonly statusService = inject(PlatformStatusService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly endTime = signal<string | null>(null);
  readonly checking = signal(false);

  ngOnInit(): void {
    interval(RECHECK_MS).pipe(
      startWith(0),
      switchMap(() => this.statusService.getStatus(true)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(status => {
      this.endTime.set(status.maintenanceEndTime);
      if (!status.maintenanceActive) this.leave();
    });
  }

  tryAgain(): void {
    this.checking.set(true);
    this.statusService.getStatus(true).subscribe(status => {
      this.checking.set(false);
      this.endTime.set(status.maintenanceEndTime);
      if (!status.maintenanceActive) this.leave();
    });
  }

  private leave(): void {
    // /login sends signed-in people straight to their dashboard
    this.router.navigateByUrl('/login');
  }
}
