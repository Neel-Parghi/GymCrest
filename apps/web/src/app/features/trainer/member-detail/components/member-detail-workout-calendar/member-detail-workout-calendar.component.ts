import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { NotificationService } from '../../../../../core/services/notification.service';
import { DropdownComponent } from '../../../../../shared/components/dropdown/dropdown.component';
import { DropdownOption } from '../../../../../shared/models/dropdown.model';
import { CalendarDay } from '../../../../../shared/models/workout-plan.model';
import { CONSTANTS } from '../../../../../core/constants/constants';
import { DailyPlanner } from '../../../../../shared/models/workout-plan.model';
import { resolveScheduledDay } from '../../../../../shared/utils/workout-schedule';

/** Override dropdown values for daily workouts carry the workout's day id after this prefix. */
const DAILY_OPTION_PREFIX = 'daily:';

@Component({
  selector: 'app-member-detail-workout-calendar',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DropdownComponent],
  templateUrl: './member-detail-workout-calendar.component.html',
  styleUrl: './member-detail-workout-calendar.component.scss',
})
export class PTMemberDetailWorkoutCalendarComponent implements OnInit, OnChanges {
  private notification = inject(NotificationService);

  @Input() workoutHistory: any[] = [];
  @Input() activeSplit: any = null;
  @Input() planAssignments: any[] = [];
  @Input() workoutOverrides: { [dateKey: string]: any } = {};

  /** Daily workouts that can be put on a single date or weekday from the override dialog. */
  @Input() dailyPlans: DailyPlanner[] = [];
  @Output() openAssignModal = new EventEmitter<void>();
  @Output() changeTab = new EventEmitter<string>();
  @Output() updateOverrides = new EventEmitter<{ dateKey: string, workout: any, scope?: 'single' | 'recurring' }>();
  @Output() logDateWorkout = new EventEmitter<{ date: Date, routineName: string, templateDayName?: string }>();
  @Output() editWorkout = new EventEmitter<any>();

  currentDate = new Date();
  calendarDays: CalendarDay[] = [];
  weekdays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  // Detail viewing
  selectedSession: any = null;
  showDetailsModal = false;
  showMobileDaySheet = false;
  selectedMobileDay: any = null;

  // Override options modal
  showOverrideModal = false;
  overrideDateKey = '';
  overrideDateLabel = '';
  overrideOptions: DropdownOption[] = [];
  overrideValueControl = new FormControl('');
  overrideScopeControl = new FormControl<'single' | 'recurring'>('single');

  ngOnInit(): void {
    this.generateCalendar();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['workoutHistory'] || changes['activeSplit'] || changes['planAssignments'] || changes['workoutOverrides']) {
      this.generateCalendar();
    }
  }

  prevMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() - 1, 1);
    this.generateCalendar();
  }

  nextMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1, 1);
    this.generateCalendar();
  }

  goToToday(): void {
    this.currentDate = new Date();
    this.generateCalendar();
  }

  getMonthLabel(): string {
    return this.currentDate.toLocaleString('default', { month: 'long', year: 'numeric' });
  }

  generateCalendar(): void {
    const year = this.currentDate.getFullYear();
    const month = this.currentDate.getMonth();

    const firstDayIndex = new Date(year, month, 1).getDay();
    const totalDays = new Date(year, month + 1, 0).getDate();
    const prevMonthTotalDays = new Date(year, month, 0).getDate();

    const days: any[] = [];

    for (let i = firstDayIndex - 1; i >= 0; i--) {
      const dayNum = prevMonthTotalDays - i;
      const date = new Date(year, month - 1, dayNum);
      days.push(this.createDayObject(date, false));
    }

    for (let i = 1; i <= totalDays; i++) {
      const date = new Date(year, month, i);
      days.push(this.createDayObject(date, true));
    }

    const remainingCells = 35 - days.length;
    for (let i = 1; i <= remainingCells; i++) {
      const date = new Date(year, month + 1, i);
      days.push(this.createDayObject(date, false));
    }

    this.calendarDays = days;
  }

  createDayObject(date: Date, isCurrentMonth: boolean): any {
    const dateKey = this.getDateKey(date);

    const todayStr = this.getDateKey(new Date());
    const isToday = dateKey === todayStr;

    const comparisonDate = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const comparisonToday = new Date();
    comparisonToday.setHours(0, 0, 0, 0);
    const isPast = comparisonDate.getTime() < comparisonToday.getTime();

    const historySession = this.workoutHistory.find(h => {
      const d = new Date(h.date);
      return this.getDateKey(d) === dateKey && h.status === 'Completed';
    });

    let override = this.workoutOverrides ? this.workoutOverrides[dateKey] : null;
    if (!override) {
      const plannedSession = this.workoutHistory.find(h => {
        const d = new Date(h.date);
        return this.getDateKey(d) === dateKey && (h.status === 'Planned' || h.status === 'RestDay');
      });
      if (plannedSession) {
        override = {
          dayName: plannedSession.dayName,
          templateDayName: plannedSession.dayName.split(' - ')[0],
          exercises: (plannedSession.loggedExercises || plannedSession.exercises || []).map((ex: any) => ({
            name: ex.name,
            sets: (ex.loggedSets || ex.sets || []).length,
            reps: (ex.loggedSets || ex.sets || [])[0]?.reps?.toString() || '8-12 reps'
          }))
        };
      }
    }

    let scheduledWorkout: any = null;
    const targetTime = comparisonDate.getTime();
    const activeAssignment = this.planAssignments
      .filter(a => new Date(a.assignedAt).setHours(0, 0, 0, 0) <= targetTime)
      .slice(-1)[0];

    const planToUse = activeAssignment || (!isPast ? this.activeSplit : null);

    if (planToUse && planToUse.days) {
      // Same weekday rules as the dashboard, reminders and streak.
      const templateDay = resolveScheduledDay<any>(planToUse.days, comparisonDate);

      if (templateDay) {
        scheduledWorkout = {
          dayName: templateDay.dayName,
          category: templateDay.category,
          exercises: templateDay.exercises,
          isRestDay: templateDay.isRestDay || templateDay.dayName.toLowerCase().includes('rest')
        };
      }
    }

    let routineName = 'Rest Day';
    let isRestDay = true;
    let exercisesCount = 0;
    let templateDayName = '';

    if (override) {
      routineName = override.dayName;
      isRestDay = routineName.toLowerCase().includes('rest');
      exercisesCount = override.exercises ? override.exercises.length : 0;
      templateDayName = override.templateDayName || '';
    } else if (scheduledWorkout) {
      routineName = scheduledWorkout.isRestDay ? 'Rest Day' : (scheduledWorkout.category || scheduledWorkout.dayName);
      isRestDay = scheduledWorkout.isRestDay;
      exercisesCount = scheduledWorkout.exercises ? scheduledWorkout.exercises.length : 0;
      templateDayName = scheduledWorkout.dayName;
    }

    return {
      date: new Date(date),
      dateKey,
      dayNum: date.getDate(),
      isCurrentMonth,
      isToday,
      isPast,
      historySession,
      override,
      scheduledWorkout,
      routineName,
      isRestDay,
      exercisesCount,
      templateDayName
    };
  }

  getDateKey(date: Date): string {
    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const dd = String(date.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  openSessionDetails(session: any): void {
    const rawExercises = session.loggedExercises || session.exercises || [];
    const sortedExercises = [...rawExercises].sort((a: any, b: any) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
    const exercises = sortedExercises.map((ex: any) => {
      const rawSets = ex.loggedSets || ex.sets || [];
      const sets = [...rawSets].sort((a: any, b: any) => (a.setNo || 0) - (b.setNo || 0));
      return {
        ...ex,
        isCardio: ex.isCardio || false,
        sets
      };
    });
    this.selectedSession = {
      ...session,
      exercises
    };
    this.showDetailsModal = true;
  }

  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedSession = null;
  }

  isCardioExercise(name: string, target?: string): boolean {
    if (!name) return false;
    const nameLower = name.toLowerCase().trim();

    if (CONSTANTS.MEMBER_DETAIL_MODULE.TRACK_PERFORMANCE.CARDIO_KEYWORDS.some(keyword => nameLower.includes(keyword))) {
      return true;
    }

    if (CONSTANTS.MEMBER_DETAIL_MODULE.TRACK_PERFORMANCE.CARDIO_NAME_REGEXP.test(nameLower)) {
      if (nameLower.includes('farmer')) {
        return false;
      }
      return true;
    }

    if (target) {
      const targetLower = target.toLowerCase().trim();
      if (CONSTANTS.MEMBER_DETAIL_MODULE.TRACK_PERFORMANCE.CARDIO_TARGET_REGEXP.test(targetLower)) {
        return true;
      }
    }

    return false;
  }

  openOverrideDialog(dayObj: any): void {
    if (dayObj.isPast && !dayObj.isToday) {
      this.notification.warning(CONSTANTS.MEMBER_DETAIL_MODULE.WORKOUT_CALENDAR.CANNOT_OVERRIDE_PAST);
      return;
    }

    this.overrideDateKey = dayObj.dateKey;
    this.overrideDateLabel = dayObj.date.toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric' });
    this.overrideScopeControl.setValue('single');

    const options: DropdownOption[] = [];
    if (this.activeSplit && this.activeSplit.days) {
      this.activeSplit.days.forEach((d: any) => {
        const labelText = d.category ? `${d.dayName} - ${d.category}` : d.dayName;
        options.push({ label: labelText, value: d.id || d.dayName, icon: 'fa-solid fa-dumbbell' });
      });
    }
    this.dailyPlans.filter(p => p.dayId).forEach(p => {
      options.push({ label: `Daily · ${p.name}`, value: `${DAILY_OPTION_PREFIX}${p.dayId}`, icon: 'fa-solid fa-calendar-day' });
    });
    options.push({ label: 'Rest Day', value: 'Rest Day', icon: 'fa-solid fa-bed' });
    options.push({ label: 'Cardio Workout', value: 'Cardio Workout', icon: 'fa-solid fa-person-running' });

    this.overrideOptions = options;

    let initialVal = '';
    if (dayObj.override) {
      const matchedDay = this.activeSplit?.days?.find((d: any) =>
        d.dayName === (dayObj.override.templateDayName || dayObj.routineName) ||
        d.category === dayObj.routineName
      );
      initialVal = matchedDay?.id || dayObj.override.templateDayName || dayObj.routineName;
      if (dayObj.routineName === 'Rest Day' || dayObj.routineName === 'Cardio Workout') {
        initialVal = dayObj.routineName;
      }
    } else {
      const matchedDay = this.activeSplit?.days?.find((d: any) =>
        d.dayName === (dayObj.templateDayName || dayObj.routineName) ||
        d.category === dayObj.routineName
      );
      initialVal = matchedDay?.id || dayObj.templateDayName || dayObj.routineName;
      if (dayObj.routineName === 'Rest Day' || dayObj.routineName === 'Cardio Workout') {
        initialVal = dayObj.routineName;
      }
    }
    this.overrideValueControl.setValue(initialVal);
    this.showOverrideModal = true;
  }

  closeOverrideModal(): void {
    this.showOverrideModal = false;
  }

  confirmOverride(): void {
    let selectedDay: any = null;
    const selectedOverrideValue = this.overrideValueControl.value;

    if (selectedOverrideValue === 'Rest Day') {
      selectedDay = { dayName: 'Rest Day', exercises: [] };
    } else if (selectedOverrideValue === 'Cardio Workout') {
      selectedDay = {
        dayName: 'Cardio Workout',
        exercises: [
          { name: 'Treadmill Run', sets: 1, reps: '30 mins', notes: 'Keep moderate intensity.' },
          { name: 'Stationary Bike', sets: 1, reps: '15 mins', notes: 'Warm down.' }
        ]
      };
    } else if (selectedOverrideValue?.startsWith(DAILY_OPTION_PREFIX)) {
      const dayId = selectedOverrideValue.slice(DAILY_OPTION_PREFIX.length);
      const daily = this.dailyPlans.find(p => p.dayId === dayId);
      if (daily) {
        selectedDay = {
          id: dayId,
          dayName: daily.name,
          exercises: daily.exercises,
          templateDayName: daily.name
        };
      }
    } else {
      const match = this.activeSplit?.days.find((d: any) => d.id === selectedOverrideValue)
        || this.activeSplit?.days.find((d: any) => d.dayName === selectedOverrideValue);
      if (match) {
        const displayName = match.category || match.dayName;
        selectedDay = {
          id: match.id,
          dayName: displayName,
          exercises: match.exercises,
          templateDayName: match.dayName
        };
      }
    }

    if (selectedDay) {
      this.updateOverrides.emit({
        dateKey: this.overrideDateKey,
        workout: selectedDay,
        scope: this.overrideScopeControl.value || 'single'
      });
      this.notification.success(`Updated routine for ${this.overrideDateLabel}!`);
    }

    this.closeOverrideModal();
  }

  onAssignSplit(): void {
    this.openAssignModal.emit();
  }

  trackTodayLive(): void {
    this.changeTab.emit('workout-track');
  }

  logPastWorkout(dayObj: any): void {
    this.logDateWorkout.emit({
      date: dayObj.date,
      routineName: dayObj.routineName,
      templateDayName: dayObj.templateDayName
    });
  }

  editWorkoutSession(session: any): void {
    this.editWorkout.emit(session);
  }

  openMobileDaySheet(dayObj: any): void {
    this.selectedMobileDay = dayObj;
    this.showMobileDaySheet = true;
  }

  closeMobileDaySheet(): void {
    this.showMobileDaySheet = false;
    this.selectedMobileDay = null;
  }

  mobileViewLoggedWorkout(): void {
    if (this.selectedMobileDay?.historySession) {
      this.openSessionDetails(this.selectedMobileDay.historySession);
    }
    this.closeMobileDaySheet();
  }

  mobileEditLoggedWorkout(): void {
    if (this.selectedMobileDay?.historySession) {
      this.editWorkoutSession(this.selectedMobileDay.historySession);
    }
    this.closeMobileDaySheet();
  }

  mobileLogWorkout(): void {
    const dayObj = this.selectedMobileDay;
    this.closeMobileDaySheet();
    if (!dayObj) return;
    if (dayObj.isToday) {
      this.trackTodayLive();
    } else {
      this.logPastWorkout(dayObj);
    }
  }

  mobileChangePlan(): void {
    const dayObj = this.selectedMobileDay;
    this.closeMobileDaySheet();
    if (dayObj) {
      this.openOverrideDialog(dayObj);
    }
  }
}
