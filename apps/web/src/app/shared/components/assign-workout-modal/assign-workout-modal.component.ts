import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { SegmentedTabsComponent } from '../segmented-tabs/segmented-tabs.component';
import { SegmentedTab } from '../../models/segmented-tab.model';
import { DailyPlanner, WorkoutPlan } from '../../models/workout-plan.model';
import { ConfirmationService } from '../../../core/services/confirmation.service';

export interface AssignDailyRequest {
  plan: DailyPlanner;
  weekdays: string[];
}

type AssignView = 'programs' | 'daily';

const WEEKDAYS = [
  { key: 'Monday', short: 'Mon' }, { key: 'Tuesday', short: 'Tue' }, { key: 'Wednesday', short: 'Wed' },
  { key: 'Thursday', short: 'Thu' }, { key: 'Friday', short: 'Fri' }, { key: 'Saturday', short: 'Sat' }, { key: 'Sunday', short: 'Sun' }
];

/**
 * Assigns workouts to a member: a program (split or weekly) sets their whole week, while a daily workout is put on
 * chosen weekdays on top of the program.
 */
@Component({
  selector: 'app-assign-workout-modal',
  standalone: true,
  imports: [SegmentedTabsComponent],
  templateUrl: './assign-workout-modal.component.html',
  styleUrl: './assign-workout-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssignWorkoutModalComponent {
  private confirmation = inject(ConfirmationService);

  readonly plans = input<WorkoutPlan[]>([]);
  readonly activePlanId = input<string | null>(null);
  readonly activePlanName = input<string | null>(null);
  /** The member's first name when a trainer assigns; left empty when members assign to themselves. */
  readonly memberName = input<string | null>(null);

  readonly assignProgram = output<WorkoutPlan>();
  readonly assignDaily = output<AssignDailyRequest>();
  readonly closed = output<void>();

  readonly weekdays = WEEKDAYS;
  readonly tabs: SegmentedTab[] = [
    { id: 'programs', label: 'Programs' },
    { id: 'daily', label: 'Daily workouts' }
  ];

  readonly view = signal<AssignView>('programs');
  readonly selectedDailyId = signal<string | null>(null);
  readonly selectedWeekdays = signal<string[]>([]);

  readonly programGroups = computed(() => {
    const plans = this.plans();
    return [
      { label: 'Split programs', hint: 'Days rotate through the week', plans: plans.filter(p => p.type === 'Split') },
      { label: 'Weekly programs', hint: 'A set workout for each weekday', plans: plans.filter(p => p.type === 'Weekly') }
    ].filter(g => g.plans.length);
  });

  /** "your" for members, "Asha's" for a trainer assigning to Asha. */
  readonly ownerPhrase = computed(() => {
    const name = this.memberName()?.trim();
    return name ? `${name}'s` : 'your';
  });

  readonly dailyPlans = computed(() => this.plans().filter((p): p is DailyPlanner => p.type === 'Daily'));

  readonly canAddDaily = computed(() =>
    !!this.activePlanId() && !!this.selectedDailyId() && this.selectedWeekdays().length > 0);

  setView(view: string): void {
    this.view.set(view === 'daily' ? 'daily' : 'programs');
  }

  programMeta(plan: WorkoutPlan): string {
    const days = plan.type === 'Weekly' ? plan.activeDaysCount : plan.type === 'Split' ? plan.days.length : plan.daysCount;
    return [days ? `${days} ${days === 1 ? 'day' : 'days'}` : null, plan.level, plan.goal].filter(Boolean).join(' · ');
  }

  dailyMeta(plan: DailyPlanner): string {
    const count = plan.exercises?.length ?? 0;
    return [plan.targetCategory || null, `${count} ${count === 1 ? 'exercise' : 'exercises'}`].filter(Boolean).join(' · ');
  }

  async chooseProgram(plan: WorkoutPlan): Promise<void> {
    if (plan.id === this.activePlanId()) return;

    const current = this.activePlanName();
    if (current) {
      const confirmed = await this.confirmation.confirm({
        title: 'Replace current program?',
        message: `${plan.name} will replace ${current}. Changes made to upcoming days, including daily workouts, will be cleared.`,
        confirmText: 'Replace program',
        type: 'warning'
      });
      if (!confirmed) return;
    }
    this.assignProgram.emit(plan);
  }

  toggleWeekday(day: string): void {
    this.selectedWeekdays.update(days => days.includes(day) ? days.filter(d => d !== day) : [...days, day]);
  }

  addDaily(): void {
    const plan = this.dailyPlans().find(p => p.id === this.selectedDailyId());
    if (!plan || !this.canAddDaily()) return;
    const order = WEEKDAYS.map(d => d.key);
    const weekdays = [...this.selectedWeekdays()].sort((a, b) => order.indexOf(a) - order.indexOf(b));
    this.assignDaily.emit({ plan, weekdays });
  }
}
