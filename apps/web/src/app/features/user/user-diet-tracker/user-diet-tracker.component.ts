import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, map, tap } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { DietTrackingService } from '../../../core/services/diet-tracking.service';
import { PlatformStatusService } from '../../../core/services/platform-status.service';
import { ConfirmationService } from '../../../core/services/confirmation.service';
import { AssignedMealDto, DietLogDto, MealLogEntryDto } from '../../../shared/models/diet-tracking.model';
import { SegmentedTabsComponent } from '../../../shared/components/segmented-tabs/segmented-tabs.component';
import { SegmentedTab } from '../../../shared/models/segmented-tab.model';
import { mealTimeToMinutes } from '../../../shared/utils/meal-time';
import { NUTRITION_TARGET_FALLBACK, percentOf } from '../../../shared/utils/nutrition';
import { toDateKey } from '../../../shared/utils/workout-schedule';
import { FoodSearchItem, FoodSearchResult, MealForm, SelectedFoodItem, MealSheetMode, PlannedMeal } from '../../../core/models/user-nutrition.model';

const RING_CIRCUMFERENCE = 2 * Math.PI * 80;
const PORTION_STEP = 0.5;
const MAX_PORTION = 10;
const SOURCE_LABELS: Record<string, string> = { indb: 'Indian food DB', usda: 'USDA', web: 'Web estimate', ai: 'AI estimate' };

type MealFormValue = ReturnType<FormGroup<MealForm>['getRawValue']>;

@Component({
  selector: 'app-user-diet-tracker',
  standalone: true,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule, SegmentedTabsComponent],
  templateUrl: './user-diet-tracker.component.html',
  styleUrl: './user-diet-tracker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserDietTrackerComponent implements OnInit {
  readonly limits = FIELD_LIMITS;

  private dietTrackingService = inject(DietTrackingService);
  private confirmation = inject(ConfirmationService);
  private toastr = inject(ToastrService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private platformStatus = toSignal(inject(PlatformStatusService).getStatus());

  readonly tabs: SegmentedTab[] = [
    { id: 'today', label: 'Today' },
    { id: 'plan', label: 'My plan' },
    { id: 'library', label: 'Library' }
  ];

  readonly currentDate = signal(new Date());
  readonly dietLog = signal<DietLogDto | null>(null);
  readonly isLoading = signal(true);

  readonly showSheet = signal(false);
  readonly sheetMode = signal<MealSheetMode>('add');
  readonly sheetPlannedMeal = signal<AssignedMealDto | null>(null);
  readonly editingEntry = signal<MealLogEntryDto | null>(null);
  readonly isSaving = signal(false);
  readonly saveError = signal('');
  readonly quickLoggingId = signal<string | null>(null);

  readonly isSearching = signal(false);
  /** Off when the super admin has switched food search off, or the API says it is. */
  private readonly searchTurnedOff = signal(false);
  readonly foodSearchAvailable = computed(() => this.platformStatus()?.foodSearchEnabled !== false && !this.searchTurnedOff());
  readonly searchError = signal('');
  readonly searchResult = signal<FoodSearchResult | null>(null);
  readonly searchItems = signal<SelectedFoodItem[]>([]);
  readonly searchRows = computed(() => this.searchItems().map(({ item, portion }) => ({
    ...this.scaleItem(item, portion),
    portion,
    sourceLabel: SOURCE_LABELS[item.source ?? ''] ?? '',
    isEstimate: item.source === 'ai' || item.source === 'web'
  })));
  private sheetInitialValues: MealFormValue | null = null;

  readonly searchQueryControl = new FormControl('', { nonNullable: true });
  readonly mealForm = new FormGroup<MealForm>({
    foodName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(FIELD_LIMITS.SHORT_TEXT)] }),
    calories: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    protein: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    carbs: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    fats: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    sourceDietPlanMealId: new FormControl<string | null>(null)
  });

  readonly isToday = computed(() => toDateKey(this.currentDate()) === toDateKey(new Date()));

  readonly sheetTitle = computed(() => {
    const planned = this.sheetPlannedMeal();
    switch (this.sheetMode()) {
      case 'plan': return `Log ${planned?.name ?? 'meal'}`;
      case 'edit': return planned ? `Edit ${planned.name}` : 'Edit food';
      default: return 'Add food';
    }
  });

  readonly totals = computed(() => {
    const log = this.dietLog();
    const target = log?.targetCalories || NUTRITION_TARGET_FALLBACK.calories;
    const eaten = log?.totalCalories ?? 0;
    return {
      eaten,
      target,
      left: Math.max(target - eaten, 0),
      over: Math.max(eaten - target, 0),
      ringDash: `${(RING_CIRCUMFERENCE * Math.min(eaten / target, 1)).toFixed(1)} ${RING_CIRCUMFERENCE.toFixed(1)}`
    };
  });

  readonly macros = computed(() => {
    const log = this.dietLog();
    const macro = (label: string, tone: string, value: number | undefined, target: number | undefined, fallback: number) => {
      const t = target || fallback;
      const v = Math.round((value ?? 0) * 10) / 10;
      return { label, tone, value: v, target: t, pct: percentOf(v, t) };
    };
    return [
      macro('Protein', 'protein', log?.totalProtein, log?.targetProtein, NUTRITION_TARGET_FALLBACK.protein),
      macro('Carbs', 'carbs', log?.totalCarbs, log?.targetCarbs, NUTRITION_TARGET_FALLBACK.carbs),
      macro('Fats', 'fats', log?.totalFats, log?.targetFats, NUTRITION_TARGET_FALLBACK.fats)
    ];
  });

  readonly plannedMeals = computed<PlannedMeal[]>(() => {
    const log = this.dietLog();
    const entries = log?.mealEntries ?? [];
    const sorted = [...(log?.assignedMeals ?? [])].sort((a, b) => mealTimeToMinutes(a.time) - mealTimeToMinutes(b.time));
    const withEntries = sorted.map(meal => ({ meal, entry: entries.find(e => e.sourceDietPlanMealId === meal.id) ?? null }));
    const nextId = withEntries.find(m => !m.entry)?.meal.id;
    return withEntries.map(m => ({
      ...m,
      isNext: m.meal.id === nextId,
      isAdjusted: !!m.entry && (
        m.entry.calories !== m.meal.calories || m.entry.protein !== m.meal.protein ||
        m.entry.carbs !== m.meal.carbs || m.entry.fats !== m.meal.fats
      )
    }));
  });

  readonly loggedPlannedCount = computed(() => this.plannedMeals().filter(m => m.entry).length);

  /** Food logged that isn't one of the planned meals. */
  readonly otherEntries = computed(() => {
    const plannedIds = new Set((this.dietLog()?.assignedMeals ?? []).map(m => m.id));
    return (this.dietLog()?.mealEntries ?? []).filter(e => !e.sourceDietPlanMealId || !plannedIds.has(e.sourceDietPlanMealId));
  });

  ngOnInit(): void {
    this.loadLogForDate(this.currentDate());
  }

  loadLogForDate(date: Date): void {
    this.isLoading.set(true);
    this.dietTrackingService.getUserDietLog(toDateKey(date))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.dietLog.set((res?.data ?? res) as DietLogDto);
          this.isLoading.set(false);
        },
        error: err => {
          console.error('Failed to load diet log', err);
          this.dietLog.set(null);
          this.isLoading.set(false);
        }
      });
  }

  goToTab(tab: string): void {
    if (tab === 'plan') this.router.navigate(['/user/diet-planner']);
    if (tab === 'library') this.router.navigate(['/user/diet-planner'], { queryParams: { view: 'library' } });
  }

  shiftDay(offset: number): void {
    if (offset > 0 && this.isToday()) return;
    const d = new Date(this.currentDate());
    d.setDate(d.getDate() + offset);
    this.currentDate.set(d);
    this.loadLogForDate(d);
  }


  openAddSheet(): void {
    this.openSheet('add', null, null, { foodName: '', calories: 0, protein: 0, carbs: 0, fats: 0, sourceDietPlanMealId: null });
  }

  openPlannedSheet(meal: AssignedMealDto): void {
    this.openSheet('plan', meal, null, this.planValues(meal));
  }

  openEditSheet(entry: MealLogEntryDto, meal: AssignedMealDto | null = null): void {
    this.openSheet('edit', meal, entry, {
      foodName: entry.foodName,
      calories: entry.calories,
      protein: entry.protein,
      carbs: entry.carbs,
      fats: entry.fats,
      sourceDietPlanMealId: entry.sourceDietPlanMealId ?? null
    });
  }

  resetToPlan(): void {
    const meal = this.sheetPlannedMeal();
    if (meal) this.mealForm.patchValue(this.planValues(meal));
  }

  closeSheet(): void {
    if (this.isSaving()) return;
    this.showSheet.set(false);
    this.resetSearch();
    this.saveError.set('');
  }

  searchFood(): void {
    const query = this.searchQueryControl.value.trim();
    if (!query || this.isSearching() || !this.foodSearchAvailable()) return;
    this.isSearching.set(true);
    this.searchError.set('');
    this.searchResult.set(null);
    this.searchItems.set([]);
    this.dietTrackingService.searchFood(query).subscribe({
      next: result => {
        const food = ((result as { data?: FoodSearchResult })?.data ?? result) as FoodSearchResult;
        this.searchResult.set(food);
        const items = food.items?.length ? food.items : [{ ...food, quantity: '' }];
        this.applySearchItems(items.map(item => ({ item, portion: 1 })));
        this.isSearching.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isSearching.set(false);
        if (err.status === 503 && err.error?.code === 'FOOD_SEARCH_OFF') {
          this.searchTurnedOff.set(true);
          return;
        }
        this.searchError.set(err.status === 429
          ? 'Too many searches. Please wait a minute and try again.'
          : "Couldn't find that. Try describing it differently, or enter the details manually.");
      }
    });
  }

  removeSearchItem(index: number): void {
    this.applySearchItems(this.searchItems().filter((_, i) => i !== index));
  }

  changePortion(index: number, direction: 1 | -1): void {
    this.applySearchItems(this.searchItems().map((entry, i) => i !== index ? entry : {
      ...entry,
      portion: Math.min(Math.max(entry.portion + direction * PORTION_STEP, PORTION_STEP), MAX_PORTION)
    }));
  }

  clearSearch(): void {
    this.resetSearch();
    if (this.sheetInitialValues) this.mealForm.reset(this.sheetInitialValues);
  }

  quickLog(meal: AssignedMealDto): void {
    if (this.quickLoggingId()) return;
    this.quickLoggingId.set(meal.id);
    this.persist(this.dietTrackingService.addMealEntry({
      logDate: toDateKey(this.currentDate()),
      mealType: 'Assigned',
      ...this.planValues(meal),
      sourceDietPlanMealId: meal.id
    })).subscribe({
      next: () => this.quickLoggingId.set(null),
      error: () => {
        this.quickLoggingId.set(null);
        this.toastr.error(`Couldn't log ${meal.name}. Please try again.`);
      }
    });
  }

  saveMeal(): void {
    if (this.mealForm.invalid || this.isSaving()) return;

    const { sourceDietPlanMealId, ...values } = this.mealForm.getRawValue();
    const editing = this.editingEntry();
    const request$ = editing
      ? this.dietTrackingService.updateMealEntry(editing.id, values)
      : this.dietTrackingService.addMealEntry({
          logDate: toDateKey(this.currentDate()),
          mealType: sourceDietPlanMealId ? 'Assigned' : 'Custom',
          ...values,
          ...(sourceDietPlanMealId ? { sourceDietPlanMealId } : {})
        });

    this.isSaving.set(true);
    this.saveError.set('');
    this.persist(request$).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeSheet();
      },
      error: () => {
        this.isSaving.set(false);
        this.saveError.set("Couldn't save. Check your connection and try again.");
      }
    });
  }

  async removeEditingEntry(): Promise<void> {
    const entry = this.editingEntry();
    if (!entry || this.isSaving()) return;

    const confirmed = await this.confirmation.confirm({
      title: 'Remove from log?',
      message: `${entry.foodName} (${entry.calories} kcal) will be removed from this day.`,
      confirmText: 'Remove',
      type: 'danger'
    });
    if (!confirmed) return;

    this.isSaving.set(true);
    this.saveError.set('');
    this.persist(this.dietTrackingService.removeMealEntry(entry.id)).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeSheet();
      },
      error: () => {
        this.isSaving.set(false);
        this.saveError.set("Couldn't remove. Please try again.");
      }
    });
  }

  private openSheet(mode: MealSheetMode, meal: AssignedMealDto | null, entry: MealLogEntryDto | null, values: MealFormValue): void {
    this.sheetMode.set(mode);
    this.sheetPlannedMeal.set(meal);
    this.editingEntry.set(entry);
    this.saveError.set('');
    this.resetSearch();
    this.sheetInitialValues = values;
    this.mealForm.reset(values);
    this.showSheet.set(true);
  }

  private scaleItem(item: FoodSearchItem, portion: number): FoodSearchItem {
    const round1 = (n: number) => Math.round(n * portion * 10) / 10;
    return {
      ...item,
      calories: Math.round(item.calories * portion),
      protein: round1(item.protein),
      carbs: round1(item.carbs),
      fats: round1(item.fats)
    };
  }

  private resetSearch(): void {
    this.searchQueryControl.reset('');
    this.searchError.set('');
    this.searchResult.set(null);
    this.searchItems.set([]);
  }

  private applySearchItems(entries: SelectedFoodItem[]): void {
    this.searchItems.set(entries);
    if (!entries.length) {
      this.searchResult.set(null);
      return;
    }
    const items = entries.map(({ item, portion }) => this.scaleItem(item, portion));
    const sum = (pick: (i: FoodSearchItem) => number) => items.reduce((total, i) => total + pick(i), 0);
    this.mealForm.patchValue({
      foodName: items.map(i => i.name).join(', ').slice(0, FIELD_LIMITS.SHORT_TEXT),
      calories: Math.round(sum(i => i.calories)),
      protein: Math.round(sum(i => i.protein) * 10) / 10,
      carbs: Math.round(sum(i => i.carbs) * 10) / 10,
      fats: Math.round(sum(i => i.fats) * 10) / 10
    });
  }

  private planValues(meal: AssignedMealDto): MealFormValue {
    return {
      foodName: meal.items || meal.name,
      calories: meal.calories,
      protein: meal.protein,
      carbs: meal.carbs,
      fats: meal.fats,
      sourceDietPlanMealId: meal.id
    };
  }

  private persist(request$: Observable<unknown>): Observable<void> {
    const dateKey = toDateKey(this.currentDate());
    return request$.pipe(
      tap({
        next: updatedLog => {
          this.dietTrackingService.invalidateDietLogCache(dateKey);
          this.applyUpdatedLog(updatedLog);
        },
        error: err => console.error(err)
      }),
      map(() => undefined)
    );
  }

  private applyUpdatedLog(response: unknown): void {
    const body = response as (DietLogDto & { data?: DietLogDto }) | null;
    this.dietLog.set(body?.data ?? body);
  }

}
