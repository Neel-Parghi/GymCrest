import { Component, OnInit, inject, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { StaffService } from '../../../core/services/staff.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { WorkoutPlanService } from '../../../core/services/workout-plan.service';
import { MemberService } from '../../../core/services/member.service';
import { DietPlanService } from '../../../core/services/diet-plan.service';
import { WorkoutPlan, SplitPlanner, WeeklyPlanner, DailyPlanner } from '../../../shared/models/workout-plan.model';
import { CONSTANTS } from '../../../core/constants/constants';
import { PTMemberDetailOverviewComponent } from './components/member-detail-overview/member-detail-overview.component';
import { PTMemberDetailTrackPerformanceComponent } from './components/member-detail-track-performance/member-detail-track-performance.component';
import { PTMemberDetailWorkoutCalendarComponent } from './components/member-detail-workout-calendar/member-detail-workout-calendar.component';
import { PTMemberDetailDietChartComponent } from './components/member-detail-diet-chart/member-detail-diet-chart.component';
import { PTMemberDetailHealthTrackerComponent } from './components/member-detail-health-tracker/member-detail-health-tracker.component';
import { forkJoin } from 'rxjs';
import { AssignDailyRequest, AssignWorkoutModalComponent } from '../../../shared/components/assign-workout-modal/assign-workout-modal.component';

@Component({
  selector: 'app-member-detail',
  standalone: true,
  imports: [
    CommonModule,
    PTMemberDetailOverviewComponent,
    PTMemberDetailTrackPerformanceComponent,
    PTMemberDetailWorkoutCalendarComponent,
    PTMemberDetailDietChartComponent,
    PTMemberDetailHealthTrackerComponent,
    AssignWorkoutModalComponent
  ],
  templateUrl: './member-detail.component.html',
  styleUrl: './member-detail.component.scss'
})
export class PTMemberDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private staffService = inject(StaffService);
  private authService = inject(AuthApiService);
  private notification = inject(NotificationService);
  private workoutPlanService = inject(WorkoutPlanService);
  private memberService = inject(MemberService);
  private dietPlanService = inject(DietPlanService);

  memberId = '';
  currentUserId = '';
  memberInfo: any = null;
  workoutPlans: WorkoutPlan[] = [];
  workoutHistory: any[] = [];
  activeTab: 'overview' | 'progress' | 'workout-track' | 'workout-calendar' | 'diet-chart' = 'overview';
  previousTab: 'overview' | 'progress' | 'workout-track' | 'workout-calendar' | 'diet-chart' | '' = '';
  workoutOverrides: { [dateKey: string]: any } = {};
  loggingDate: Date = new Date();

  // Measurement progress logs
  measurements: any[] = [];
  isSubmittingProgress = false;
  isLoadingLogs = false;

  // Split Assignments & Plan Libraries
  showAssignSplitModal = false;
  showAssignDietModal = false;

  // Active workout split
  activeSplit: any = null;
  planAssignments: any[] = [];

  // Active Diet plan (loaded from API)
  activeDiet: any = null;
  dietPlans: any[] = [];

  // Live Sets tracker mock checklist
  todayWorkout: any = null;

  ngOnInit(): void {
    const routeTrainerId = this.route.snapshot.paramMap.get('trainerId');
    if (routeTrainerId) {
      this.currentUserId = routeTrainerId;
      this.setupMemberDataLoading();
    } else {
      this.authService.userProfile$.subscribe(profile => {
        if (profile) {
          this.currentUserId = profile.id;
          this.setupMemberDataLoading();
        }
      });
    }
  }

  setupMemberDataLoading(): void {
    this.route.paramMap.subscribe(params => {
      this.memberId = params.get('memberId') || '';
      if (this.memberId) {
        this.loadMemberInfo();
        this.loadMeasurementsLogs();
        this.loadWorkoutPlans();
        this.loadActivePlanAndWorkoutLogs();
        this.loadDietPlans();
        this.loadActiveDiet();
      }
    });
  }

  getTodayDateKey(): string {
    const d = new Date();
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  initializeTodayWorkout(): void {
    if (!this.activeSplit || !this.activeSplit.days || this.activeSplit.days.length === 0) {
      this.todayWorkout = null;
      return;
    }

    const todayStr = this.getTodayDateKey();
    let override = this.workoutOverrides[todayStr];

    if (!override && this.workoutHistory) {
      const existingSession = this.workoutHistory.find(h => {
        const d = new Date(h.date);
        return this.getDateKeyFor(d) === todayStr;
      });
      if (existingSession) {
        override = {
          dayName: existingSession.dayName,
          exercises: (existingSession.loggedExercises || existingSession.exercises || []).map((ex: any) => ({
            name: ex.name,
            sets: ex.loggedSets || ex.sets || [],
            reps: (ex.loggedSets || ex.sets || [])[0]?.reps?.toString() || '8-12 reps',
            isLoggedSession: true
          }))
        };
      }
    }

    if (override) {
      this.todayWorkout = {
        dayName: override.dayName,
        isRestDay: override.dayName.toLowerCase().includes('rest'),
        exercises: (override.exercises || []).map((ex: any) => {
          const isLogged = !!ex.isLoggedSession;
          const rawSets = isLogged ? ex.sets : [];
          const setsCount = isLogged ? rawSets.length : (typeof ex.sets === 'number' ? ex.sets : 3);

          return {
            name: ex.name,
            skipped: false,
            sets: Array.from({ length: setsCount }, (_, i) => {
              if (isLogged) {
                const s = rawSets[i];
                return {
                  setNo: s.setNo || (i + 1),
                  target: s.target || `${s.reps || 10} reps`,
                  weight: s.weight || 0,
                  reps: s.reps || 0,
                  completed: s.completed ?? true
                };
              } else {
                return {
                  setNo: i + 1,
                  target: typeof ex.reps === 'string' ? ex.reps : '8-12 reps',
                  weight: '',
                  reps: 10,
                  completed: false
                };
              }
            })
          };
        })
      };
      return;
    }

    const daysOfWeek = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    const todayName = daysOfWeek[new Date().getDay()];

    let templateDay = this.activeSplit.days.find((d: any) =>
      d.dayName && d.dayName.toLowerCase().includes(todayName.toLowerCase())
    );

    if (!templateDay && this.activeSplit.days.length > 0) {
      const isAbstractSplit = !this.activeSplit.days.some((d: any) =>
        daysOfWeek.some(w => d.dayName.toLowerCase().includes(w.toLowerCase()))
      );
      if (isAbstractSplit) {
        if (todayName === 'Monday') {
          templateDay = this.activeSplit.days[0];
        } else if (todayName === 'Wednesday' && this.activeSplit.days.length > 1) {
          templateDay = this.activeSplit.days[1];
        } else if (todayName === 'Friday' && this.activeSplit.days.length > 2) {
          templateDay = this.activeSplit.days[2];
        }
      }
    }

    if (templateDay) {
      this.todayWorkout = {
        dayName: templateDay.category ? `${templateDay.dayName} - ${templateDay.category}` : templateDay.dayName,
        isRestDay: templateDay.isRestDay || templateDay.dayName.toLowerCase().includes('rest'),
        exercises: templateDay.isRestDay ? [] : templateDay.exercises.map((ex: any) => ({
          name: ex.name,
          skipped: false,
          sets: Array.from({ length: typeof ex.sets === 'number' ? ex.sets : (parseInt(ex.reps) || 3) }, (_, i) => ({
            setNo: i + 1,
            target: typeof ex.reps === 'string' ? ex.reps : '8-12 reps',
            weight: '',
            reps: 10,
            completed: false
          }))
        }))
      };
    } else {
      this.todayWorkout = {
        dayName: 'Rest Day',
        isRestDay: true,
        exercises: []
      };
    }
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

      this.memberService.saveRecurringOverride(this.memberId, payload).subscribe({
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

      this.memberService.logWorkoutSession(this.memberId, payload).subscribe({
        next: () => {
          this.loadActivePlanAndWorkoutLogs();
          const todayStr = this.getDateKeyFor(new Date());
          if (event.dateKey === todayStr) {
            this.initializeTodayWorkout();
          }
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
    this.loggingDate = event.date;

    const dateStr = this.getDateKeyFor(event.date);
    const override = this.workoutOverrides[dateStr];

    let foundRoutine: any = null;
    if (override) {
      foundRoutine = override;
    } else if (this.activeSplit && this.activeSplit.days) {
      if (event.templateDayName) {
        foundRoutine = this.activeSplit.days.find((d: any) => d.dayName === event.templateDayName);
      }
      if (!foundRoutine) {
        foundRoutine = this.activeSplit.days.find((d: any) =>
          d.dayName === event.routineName ||
          d.category === event.routineName ||
          (d.category && event.routineName && d.category.toLowerCase().includes(event.routineName.toLowerCase()))
        );
      }
    }

    if (foundRoutine) {
      this.todayWorkout = {
        dayName: foundRoutine.category ? `${foundRoutine.dayName} - ${foundRoutine.category}` : foundRoutine.dayName,
        isRestDay: foundRoutine.isRestDay || foundRoutine.dayName.toLowerCase().includes('rest'),
        exercises: (foundRoutine.exercises || []).map((ex: any) => ({
          name: ex.name,
          skipped: false,
          sets: Array.from({ length: typeof ex.sets === 'number' ? ex.sets : 3 }, (_, i) => ({
            setNo: i + 1,
            target: typeof ex.reps === 'string' ? ex.reps : '8-12 reps',
            weight: '',
            reps: 10,
            completed: false
          }))
        }))
      };
    } else {
      this.todayWorkout = {
        dayName: event.routineName,
        isRestDay: event.routineName.toLowerCase().includes('rest'),
        exercises: []
      };
    }

    this.setTab('workout-track');
    this.notification.info(CONSTANTS.MEMBER_DETAIL_MODULE.LOG_WORKOUT_READY_PREFIX + event.date.toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric' }));
  }

  onEditWorkoutSession(session: any): void {
    this.loggingDate = new Date(session.date);

    const rawExercises = session.loggedExercises || session.exercises || [];
    const sortedExercises = [...rawExercises].sort((a: any, b: any) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
    this.todayWorkout = {
      dayName: session.dayName,
      isRestDay: session.dayName?.toLowerCase().includes('rest') || false,
      exercises: sortedExercises.map((ex: any) => {
        const sets = [...(ex.loggedSets || ex.sets || [])].sort((a: any, b: any) => (a.setNo || 0) - (b.setNo || 0));
        return {
          name: ex.name,
          skipped: ex.skipped || false,
          isCardio: ex.isCardio || false,
          sets: sets.map((s: any) => ({
            setNo: s.setNo,
            target: s.target || `${s.reps || 10} reps`,
            weight: s.weight || 0,
            reps: s.reps || 0,
            completed: s.completed ?? true
          }))
        };
      })
    };

    this.setTab('workout-track');
    this.notification.info(CONSTANTS.MEMBER_DETAIL_MODULE.EDIT_WORKOUT_PREFIX + this.loggingDate.toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric' }));
  }

  /** Daily workouts the trainer can put on a member's calendar days. */
  get dailyPlans(): DailyPlanner[] {
    return this.workoutPlans.filter((p): p is DailyPlanner => p.type === 'Daily');
  }

  loadWorkoutPlans(): void {
    if (!this.currentUserId) return;
    this.workoutPlanService.getPlans().subscribe({
      next: (plans) => {
        this.workoutPlans = (plans || []).filter(p => !p.isCustom && p.createdBy === this.currentUserId);
      },
      error: (err) => {
        console.error('Failed to load workout plans:', err);
      }
    });
  }

  setTab(tab: any): void {
    this.previousTab = this.activeTab;
    this.activeTab = tab;
  }

  onCancelWorkoutTrack(): void {
    this.loggingDate = new Date();
    this.initializeTodayWorkout();
    this.setTab('workout-calendar');
  }

  loadMemberInfo(): void {
    if (!this.currentUserId) return;
    this.staffService.getAssignedMembers(this.currentUserId).subscribe({
      next: (res: any) => {
        const list = res?.data || [];
        const assignmentInfo = list.find((m: any) => m.memberId === this.memberId) || null;
        if (assignmentInfo) {
          this.memberService.getMemberById(this.memberId).subscribe({
            next: (profileRes: any) => {
              const fullProfile = profileRes?.data || profileRes;
              this.memberInfo = {
                ...assignmentInfo,
                ...fullProfile
              };
            },
            error: (err) => {
              console.error('Error fetching full member details, falling back to assignment info:', err);
              this.memberInfo = assignmentInfo;
            }
          });
        } else {
          // Fallback to fetch member details directly
          this.memberService.getMemberById(this.memberId).subscribe({
            next: (profileRes: any) => {
              this.memberInfo = profileRes?.data || profileRes;
            },
            error: (err) => {
              console.error('Error fetching member details directly:', err);
              this.memberInfo = null;
            }
          });
        }
      },
      error: (err) => {
        console.error('Error fetching member details:', err);
        this.memberService.getMemberById(this.memberId).subscribe({
          next: (profileRes: any) => {
            this.memberInfo = profileRes?.data || profileRes;
          },
          error: () => {
            this.memberInfo = null;
          }
        });
      }
    });
  }

  loadMeasurementsLogs(): void {
    this.isLoadingLogs = true;
    this.staffService.getMemberMeasurements(this.memberId).subscribe({
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

  submitProgress(formVal: any): void {
    this.isSubmittingProgress = true;
    this.staffService.recordMeasurement(this.memberId, {
      weight: formVal.weight,
      height: formVal.height,
      bodyFatPercentage: formVal.bodyFatPercentage || undefined,
      bmi: formVal.bmi || undefined,
      notes: formVal.notes || undefined,
      isAdvanced: formVal.isAdvanced || false,
      neck: formVal.neck || undefined,
      shoulders: formVal.shoulders || undefined,
      chest: formVal.chest || undefined,
      leftBicep: formVal.leftBicep || undefined,
      rightBicep: formVal.rightBicep || undefined,
      leftForearm: formVal.leftForearm || undefined,
      rightForearm: formVal.rightForearm || undefined,
      upperAbs: formVal.upperAbs || undefined,
      lowerAbs: formVal.lowerAbs || undefined,
      waist: formVal.waist || undefined,
      hips: formVal.hips || undefined,
      leftThigh: formVal.leftThigh || undefined,
      rightThigh: formVal.rightThigh || undefined,
      leftCalf: formVal.leftCalf || undefined,
      rightCalf: formVal.rightCalf || undefined
    }).subscribe({
      next: () => {
        this.notification.success(CONSTANTS.MEMBER_DETAIL_MODULE.PROGRESS_RECORD_ADDED);
        this.loadMeasurementsLogs();
        this.isSubmittingProgress = false;
      },
      error: () => {
        this.notification.error(CONSTANTS.MEMBER_DETAIL_MODULE.PROGRESS_RECORD_ERROR);
        this.isSubmittingProgress = false;
      }
    });
  }

  loadActivePlanAndWorkoutLogs(): void {
    this.memberService.getActivePlan(this.memberId).subscribe({
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
          this.initializeTodayWorkout();
        } else {
          this.activeSplit = null;
          this.todayWorkout = null;
        }
      },
      error: () => {
        this.activeSplit = null;
        this.todayWorkout = null;
      }
    });

    this.memberService.getPlanAssignments(this.memberId).subscribe({
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

    this.memberService.getWorkoutLogs(this.memberId).subscribe({
      next: (res: any) => {
        this.workoutHistory = res?.data || [];
      }
    });
  }

  saveWorkoutSession(): void {
    const isRest = this.todayWorkout.isRestDay || this.todayWorkout.dayName?.toLowerCase().includes('rest');

    if (!isRest) {
      const nonSkippedExercises = this.todayWorkout.exercises.filter((ex: any) => !ex.skipped);
      const completedSetsCount = nonSkippedExercises.reduce(
        (sum: number, ex: any) => sum + ex.sets.filter((s: any) => s.completed).length, 0
      );

      if (completedSetsCount === 0) {
        this.notification.warning(CONSTANTS.MEMBER_DETAIL_MODULE.MIN_COMPLETED_SET_WARNING);
        return;
      }
    }

    const payload = {
      date: new Date(this.loggingDate),
      dayName: this.todayWorkout.dayName,
      status: isRest ? 'RestDay' : 'Completed',
      notes: isRest ? 'Rest Day logged from tracker' : '',
      loggedExercises: isRest ? [] : this.todayWorkout.exercises.map((ex: any, idx: number) => ({
        name: ex.name,
        skipped: ex.skipped || false,
        sortOrder: idx,
        isCardio: ex.isCardio || false,
        loggedSets: ex.skipped ? [] : ex.sets.map((s: any) => ({
          setNo: s.setNo,
          weight: s.weight || 0,
          reps: s.reps || 0,
          completed: s.completed
        }))
      }))
    };

    this.memberService.logWorkoutSession(this.memberId, payload).subscribe({
      next: () => {
        this.notification.success(CONSTANTS.MEMBER_DETAIL_MODULE.LOG_WORKOUT_SUCCESS);
        this.loadActivePlanAndWorkoutLogs();
        this.loggingDate = new Date();
        if (this.previousTab === 'workout-calendar') {
          this.setTab('workout-calendar');
        }
      },
      error: () => {
        this.notification.error(CONSTANTS.MEMBER_DETAIL_MODULE.LOG_WORKOUT_ERROR);
      }
    });
  }

  openAssignSplitModal(): void {
    if (!this.currentUserId) return;
    this.workoutPlanService.getPlans().subscribe({
      next: (res) => {
        const plans = res as any[];
        this.workoutPlans = (plans || []).filter((p: any) => !p.isCustom && p.createdBy === this.currentUserId);
        this.showAssignSplitModal = true;
      },
      error: () => {
        this.notification.warning('Failed to load workout plans.');
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
      this.memberService.saveRecurringOverride(this.memberId, { dayOfWeek, workoutPlanDayId: dayId, isRestDay: false })
    )).subscribe({
      next: () => {
        this.notification.success(`${request.plan.name} added on ${request.weekdays.join(', ')}.`);
        this.closeAssignSplitModal();
        this.loadActivePlanAndWorkoutLogs();
      },
      error: () => this.notification.error('Failed to add the daily workout. Please try again.')
    });
  }

  assignWorkoutSplit(plan: WorkoutPlan | string): void {
    if (typeof plan === 'string') {
      this.notification.warning(CONSTANTS.MEMBER_DETAIL_MODULE.CANNOT_ASSIGN_CUSTOM_PLAN_WARNING);
    } else {
      if (plan.id) {
        this.memberService.assignPlan(this.memberId, plan.id).subscribe({
          next: () => {
            this.notification.success(CONSTANTS.MEMBER_DETAIL_MODULE.ASSIGN_PLAN_SUCCESS_PREFIX + plan.name + CONSTANTS.MEMBER_DETAIL_MODULE.ASSIGN_PLAN_SUCCESS_SUFFIX);

            const todayStr = this.getTodayDateKey();
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
            this.notification.error(CONSTANTS.MEMBER_DETAIL_MODULE.ASSIGN_PLAN_ERROR);
          }
        });
      } else {
        this.notification.error(CONSTANTS.MEMBER_DETAIL_MODULE.INVALID_PLAN_ID_ERROR);
      }
    }
    this.closeAssignSplitModal();
  }

  loadDietPlans(): void {
    if (!this.currentUserId) return;
    this.dietPlanService.getPlans().subscribe({
      next: (plans) => {
        this.dietPlans = (plans || []).filter((p: any) => !p.isCustom && p.createdBy === this.currentUserId);
      },
      error: (err) => {
        console.error('Failed to load diet plans:', err);
        this.dietPlans = [];
      }
    });
  }

  loadActiveDiet(): void {
    this.memberService.getActiveDiet(this.memberId).subscribe({
      next: (res: any) => {
        const assignment = res?.data;
        if (assignment?.dietPlan) {
          const dp = assignment.dietPlan;
          this.activeDiet = {
            planName: dp.name,
            calories: dp.calories,
            macros: { protein: dp.protein, carbs: dp.carbs, fats: dp.fats },
            meals: (dp.meals || []).map((m: any) => ({
              name: m.name,
              time: m.time,
              calories: m.calories,
              protein: m.protein,
              carbs: m.carbs,
              fats: m.fats,
              items: m.items
            }))
          };
        } else {
          this.activeDiet = null;
        }
      },
      error: () => {
        this.activeDiet = null;
      }
    });
  }

  openAssignDietModal(): void {
    if (!this.currentUserId) return;
    this.dietPlanService.getPlans().subscribe({
      next: (res) => {
        const plans = res as any[];
        this.dietPlans = (plans || []).filter((p: any) => !p.isCustom && p.createdBy === this.currentUserId);
        if (this.dietPlans.length === 0) {
          this.notification.warning('No diet plans available. Please create a plan first.');
        } else {
          this.showAssignDietModal = true;
        }
      },
      error: () => {
        this.notification.warning('No diet plans available. Please create a plan first.');
      }
    });
  }

  closeAssignDietModal(): void {
    this.showAssignDietModal = false;
  }

  assignDietPlan(plan: any): void {
    this.memberService.assignDiet(this.memberId, plan.id).subscribe({
      next: () => {
        this.activeDiet = {
          planId: plan.id,
          planName: plan.name,
          calories: plan.calories,
          macros: { protein: plan.protein, carbs: plan.carbs, fats: plan.fats },
          meals: (plan.meals || []).map((m: any) => ({
            name: m.name,
            time: m.time,
            calories: m.calories,
            protein: m.protein,
            carbs: m.carbs,
            fats: m.fats,
            items: m.items
          }))
        };
        this.notification.success(CONSTANTS.MEMBER_DETAIL_MODULE.ASSIGN_DIET_SUCCESS_PREFIX + plan.name + CONSTANTS.MEMBER_DETAIL_MODULE.ASSIGN_DIET_SUCCESS_MID + this.memberInfo?.firstName + CONSTANTS.MEMBER_DETAIL_MODULE.ASSIGN_DIET_SUCCESS_SUFFIX);
        this.closeAssignDietModal();
      },
      error: (err) => {
        console.error('Failed to assign diet plan:', err);
        this.notification.error(CONSTANTS.MEMBER_DETAIL_MODULE.ASSIGN_DIET_ERROR);
      }
    });
  }

  goBack(): void {
    if (this.activeTab === 'workout-track' && this.previousTab === 'workout-calendar') {
      this.onCancelWorkoutTrack();
    } else {
      const fromSource = this.route.snapshot.queryParamMap.get('from');
      if (fromSource === 'directory') {
        this.router.navigate(['/gym-owner/members']);
        return;
      }

      const routeTrainerId = this.route.snapshot.paramMap.get('trainerId');
      if (routeTrainerId) {
        this.router.navigate([`/gym-owner/trainers/${routeTrainerId}/members`]);
      } else {
        this.router.navigate(['/trainer/members']);
      }
    }
  }
}
