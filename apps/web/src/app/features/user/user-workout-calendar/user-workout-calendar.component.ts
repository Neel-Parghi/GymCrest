import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PTMemberDetailWorkoutCalendarComponent } from '../../trainer/member-detail/components/member-detail-workout-calendar/member-detail-workout-calendar.component';
import { WorkoutPlanService } from '../../../core/services/workout-plan.service';
import { MemberService } from '../../../core/services/member.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { CONSTANTS } from '../../../core/constants/constants';
import { DailyPlanner } from '../../../shared/models/workout-plan.model';
import { AssignDailyRequest, AssignWorkoutModalComponent } from '../../../shared/components/assign-workout-modal/assign-workout-modal.component';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-user-workout-calendar',
  standalone: true,
  imports: [CommonModule, PTMemberDetailWorkoutCalendarComponent, AssignWorkoutModalComponent],
  templateUrl: './user-workout-calendar.component.html',
  styleUrl: './user-workout-calendar.component.scss',
})
export class UserWorkoutCalendarComponent implements OnInit {
  private workoutPlanService = inject(WorkoutPlanService);
  private memberService = inject(MemberService);
  private notification = inject(NotificationService);
  private authService = inject(AuthApiService);
  private router = inject(Router);

  userId = '';
  workoutHistory: any[] = [];
  activeSplit: any = null;
  planAssignments: any[] = [];
  workoutOverrides: { [dateKey: string]: any } = {};

  showAssignSplitModal = false;
  workoutPlans: any[] = [];
  memberInfo: any = { firstName: 'User' };

  ngOnInit() {
    this.authService.userProfile$.subscribe(profile => {
      if (profile) {
        this.userId = profile.id;
        this.memberInfo = { firstName: profile.firstName };
        this.loadActivePlanAndWorkoutLogs();
        this.loadWorkoutPlans();
      }
    });
  }

  loadActivePlanAndWorkoutLogs(): void {
    this.memberService.getActivePlan(this.userId).subscribe({
      next: (res: any) => {
        const plan = res?.data;
        if (plan) {
          this.activeSplit = {
            planId: plan.id,
            planName: plan.name,
            days: (plan.days || []).map((d: any) => ({
              id: d.id,
              dayName: d.isRestDay ? `${d.dayName || ('Day ' + d.dayIndex)} - Rest Day` : (d.dayName || ('Day ' + d.dayIndex)),
              isRestDay: d.isRestDay,
              category: d.category || '',
              exercises: (d.exercises || []).map((ex: any) => ({
                name: ex.exerciseName,
                sets: ex.sets,
                reps: ex.reps,
                notes: ex.notes || ''
              }))
            }))
          };
        } else {
          this.activeSplit = null;
        }
      },
      error: () => {
        this.activeSplit = null;
      }
    });

    this.memberService.getPlanAssignments(this.userId).subscribe({
      next: (res: any) => {
        this.planAssignments = (res?.data || []).map((assignment: any) => {
          const plan = assignment.workoutPlan;
          return {
            id: assignment.id,
            assignedAt: new Date(assignment.assignedAt),
            isActive: assignment.isActive,
            planName: plan.name,
            days: (plan.days || []).map((d: any) => ({
              id: d.id,
              dayName: d.isRestDay ? `${d.dayName || ('Day ' + d.dayIndex)} - Rest Day` : (d.dayName || ('Day ' + d.dayIndex)),
              isRestDay: d.isRestDay,
              category: d.category || '',
              exercises: (d.exercises || []).map((ex: any) => ({
                name: ex.exerciseName,
                sets: ex.sets,
                reps: ex.reps,
                notes: ex.notes || ''
              }))
            }))
          };
        });
      },
      error: () => {
        this.planAssignments = [];
      }
    });

    this.memberService.getWorkoutLogs(this.userId).subscribe({
      next: (res: any) => {
        this.workoutHistory = res?.data || [];
      }
    });
  }

  /** Plans the member can use; daily ones also feed the calendar's per-day override options. */
  loadWorkoutPlans(): void {
    this.workoutPlanService.getPlans().subscribe({
      next: plans => this.workoutPlans = (plans || []).filter(p => !p.isCustom || p.createdBy === this.userId),
      error: () => this.workoutPlans = []
    });
  }

  get dailyPlans(): DailyPlanner[] {
    return this.workoutPlans.filter((p): p is DailyPlanner => p.type === 'Daily');
  }

  openAssignSplitModal(): void {
    this.workoutPlanService.getPlans().subscribe({
      next: (plans) => {
        this.workoutPlans = (plans || []).filter(p => !p.isCustom || p.createdBy === this.userId);
        this.showAssignSplitModal = true;
      },
      error: () => {
        this.notification.warning('Failed to fetch workout plans.');
        this.workoutPlans = [];
        this.showAssignSplitModal = true;
      }
    });
  }

  closeAssignSplitModal(): void {
    this.showAssignSplitModal = false;
  }

  /** Puts a daily workout on the chosen weekdays as weekly overrides of the member's program. */
  assignDailyWorkout(request: AssignDailyRequest): void {
    const dayId = request.plan.dayId;
    if (!dayId) {
      this.notification.error('This daily workout has no exercises saved yet.');
      return;
    }

    forkJoin(request.weekdays.map(dayOfWeek =>
      this.memberService.saveRecurringOverride(this.userId, { dayOfWeek, workoutPlanDayId: dayId, isRestDay: false })
    )).subscribe({
      next: () => {
        this.notification.success(`${request.plan.name} added on ${request.weekdays.join(', ')}.`);
        this.closeAssignSplitModal();
        this.loadActivePlanAndWorkoutLogs();
      },
      error: () => this.notification.error('Failed to add the daily workout. Please try again.')
    });
  }

  assignWorkoutSplit(plan: any): void {
    if (typeof plan === 'string') return;

    if (plan.id) {
      this.memberService.assignPlan(this.userId, plan.id).subscribe({
        next: () => {
          this.notification.success(`Successfully assigned ${plan.name} to yourself!`);

          const todayStr = this.getDateKeyFor(new Date());
          const todayDate = new Date(todayStr);
          const updatedOverrides = { ...this.workoutOverrides };
          for (const key of Object.keys(updatedOverrides)) {
            if (new Date(key) >= todayDate) {
              delete updatedOverrides[key];
            }
          }
          this.workoutOverrides = updatedOverrides;

          this.loadActivePlanAndWorkoutLogs();
        },
        error: () => {
          this.notification.error('Failed to assign workout plan.');
        }
      });
    }

    this.showAssignSplitModal = false;
  }

  onUpdateOverrides(event: { dateKey: string, workout: any, scope?: 'single' | 'recurring' }): void {
    const isRest = event.workout.dayName.toLowerCase().includes('rest');
    const hasValidPlanDayId = !isRest && event.workout.id;

    if (event.scope === 'recurring' && (isRest || hasValidPlanDayId)) {
      const weekdayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
      const dateParts = event.dateKey.split('-');
      const localDate = new Date(parseInt(dateParts[0]), parseInt(dateParts[1]) - 1, parseInt(dateParts[2]));
      const dayOfWeekName = weekdayNames[localDate.getDay()];

      const payload = {
        dayOfWeek: dayOfWeekName,
        workoutPlanDayId: isRest ? null : event.workout.id,
        isRestDay: isRest
      };

      this.memberService.saveRecurringOverride(this.userId, payload).subscribe({
        next: () => {
          this.loadActivePlanAndWorkoutLogs();
        },
        error: (err) => {
          console.error('Failed to save recurring override:', err);
          this.notification.error(CONSTANTS.MEMBER_DETAIL_MODULE.SAVE_RECURRING_OVERRIDE_ERROR);
        }
      });
    } else {
      if (event.scope === 'recurring' && !hasValidPlanDayId && !isRest) {
        this.notification.info('Custom workouts like Cardio can only be applied to this date.');
      }

      this.workoutOverrides = {
        ...this.workoutOverrides,
        [event.dateKey]: event.workout
      };

      const dateParts = event.dateKey.split('-');
      const localDate = new Date(parseInt(dateParts[0]), parseInt(dateParts[1]) - 1, parseInt(dateParts[2]), 12, 0, 0);

      const payload = {
        date: localDate,
        dayName: event.workout.dayName,
        status: isRest ? 'RestDay' : 'Planned',
        notes: 'Scheduled Override',
        loggedExercises: (event.workout.exercises || []).map((ex: any) => ({
          name: ex.name,
          loggedSets: Array.from({ length: ex.sets || 3 }, (_, i) => ({
            setNo: i + 1,
            weight: 0,
            reps: parseInt(ex.reps) || 10,
            completed: false
          }))
        }))
      };

      this.memberService.logWorkoutSession(this.userId, payload).subscribe({
        next: () => {
          this.loadActivePlanAndWorkoutLogs();
        },
        error: (err) => {
          console.error('Failed to save override log:', err);
          this.notification.error(CONSTANTS.MEMBER_DETAIL_MODULE.SAVE_OVERRIDE_ERROR);
        }
      });
    }
  }

  getDateKeyFor(date: Date): string {
    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const dd = String(date.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  onLogDateWorkout(event: { date: Date, routineName: string, templateDayName?: string }): void {
    this.router.navigate(['/user/performance'], {
      state: {
        loggingDate: event.date,
        routineName: event.routineName,
        templateDayName: event.templateDayName
      }
    });
  }

  onTrackToday(): void {
    this.router.navigate(['/user/performance']);
  }

  onEditWorkoutSession(session: any): void {
    this.router.navigate(['/user/performance'], {
      state: {
        sessionToEdit: session
      }
    });
  }
}
