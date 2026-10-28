import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NotificationService } from '../../../core/services/notification.service';
import { WorkoutMasterService } from '../../../core/services/workout-master.service';
import { Exercise } from '../../../shared/models/exercise.model';
import { TimetableModalComponent } from './components/timetable-modal/timetable-modal.component';
import { SplitPlannerCreatorComponent } from './components/split-planner-creator/split-planner-creator.component';
import { WeeklyPlannerCreatorComponent } from './components/weekly-planner-creator/weekly-planner-creator.component';
import { DailyPlannerCreatorComponent } from './components/daily-planner-creator/daily-planner-creator.component';
import { WorkoutPlanService } from '../../../core/services/workout-plan.service';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { ConfirmationService } from '../../../core/services/confirmation.service';
import { CONSTANTS } from '../../../core/constants/constants';
import {
  WorkoutPlan,
  SplitPlanner,
  WeeklyPlanner,
  DailyPlanner
} from '../../../shared/models/workout-plan.model';

@Component({
  selector: 'app-workout-planner',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TimetableModalComponent,
    SplitPlannerCreatorComponent,
    WeeklyPlannerCreatorComponent,
    DailyPlannerCreatorComponent
  ],
  templateUrl: './workout-planner.component.html',
  styleUrl: './workout-planner.component.scss'
})
export class WorkoutPlannerComponent implements OnInit {
  private notification = inject(NotificationService);
  private workoutMasterService = inject(WorkoutMasterService);
  private workoutPlanService = inject(WorkoutPlanService);
  private confirmationService = inject(ConfirmationService);
  private authService = inject(AuthApiService);
  private cdr = inject(ChangeDetectorRef);

  currentUserId = '';
  activeTab: 'splits' | 'weekly' | 'daily' = 'weekly';

  @Input() ownerMode: boolean = false;
  @Input() embedded: boolean = false;
  @Output() onAssign = new EventEmitter<any>();

  // Modal toggle states
  showCreateSplitModal = false;
  showCreateWeeklyModal = false;
  showCreateDailyModal = false;
  showDetailsModal = false;

  // Editing template states
  editingSplitPlan: SplitPlanner | null = null;
  editingWeeklyPlan: WeeklyPlanner | null = null;
  editingDailyPlan: DailyPlanner | null = null;

  // Timetable Detail Modal State
  selectedPlanner: WorkoutPlan | SplitPlanner | null = null;
  selectedPlannerType: 'split' | 'weekly' | 'daily' = 'split';

  // Master Exercise References loaded for sub-components
  categories: string[] = [];
  exercisesMap: { [category: string]: Exercise[] } = {};

  splitPlanners: SplitPlanner[] = [];
  weeklyPlanners: WeeklyPlanner[] = [];
  dailyPlanners: DailyPlanner[] = [];

  ngOnInit(): void {
    this.authService.userProfile$.subscribe(profile => {
      if (profile) {
        this.currentUserId = profile.id;
        this.loadPlanners();
      }
    });

    // Fetch exercise reference lists from Master API
    this.workoutMasterService.getCategories().subscribe(cats => {
      this.categories = cats;
      this.cdr.markForCheck();
    });
    this.workoutMasterService.getExercises().subscribe(exercises => {
      const grouped: { [category: string]: Exercise[] } = {};
      exercises.forEach(ex => {
        if (!grouped[ex.category]) {
          grouped[ex.category] = [];
        }
        grouped[ex.category].push(ex);
      });
      this.exercisesMap = grouped;
      this.cdr.markForCheck();
    });
  }

  loadPlanners(): void {
    if (!this.currentUserId) return;
    this.workoutPlanService.getPlans().subscribe({
      next: (plans) => {
        const templates = (plans || []).filter(p => !p.isCustom || p.createdBy === this.currentUserId);
        this.splitPlanners = templates.filter(p => p.type === 'Split');
        this.weeklyPlanners = templates.filter(p => p.type === 'Weekly');
        this.dailyPlanners = templates.filter(p => p.type === 'Daily');
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.LOAD_ERROR);
        console.error(err);
      }
    });
  }

  setTab(tab: 'splits' | 'weekly' | 'daily'): void {
    this.activeTab = tab;
  }

  getCategoryIcon(category: string): string {
    switch (category?.toLowerCase()) {
      case 'chest': return 'fa-dumbbell';
      case 'back': return 'fa-angles-up';
      case 'shoulders': return 'fa-arrows-up-to-line';
      case 'legs': return 'fa-shoe-prints';
      case 'biceps': return 'fa-hand-fist';
      case 'triceps': return 'fa-hand-fist';
      case 'core': return 'fa-shield-halved';
      case 'cardio': return 'fa-person-running';
      default: return 'fa-dumbbell';
    }
  }

  openDetailsModal(planner: WorkoutPlan, type: 'split' | 'weekly' | 'daily'): void {
    if (type === 'daily') {
      const p = planner as DailyPlanner;
      this.selectedPlanner = {
        id: p.id,
        name: p.name,
        description: p.description,
        level: p.level,
        goal: p.goal,
        type: 'Split',
        daysCount: 1,
        exercisesCount: p.exercises.length,
        days: [
          {
            name: 'Workout Session',
            category: p.targetCategory,
            exercises: p.exercises
          }
        ]
      } as SplitPlanner;
      this.selectedPlannerType = 'daily';
    } else {
      this.selectedPlanner = planner;
      this.selectedPlannerType = type;
    }
    this.showDetailsModal = true;
  }

  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedPlanner = null;
  }

  openCreatorModal(): void {
    if (this.activeTab === 'splits') {
      this.openCreateSplitModal();
    } else if (this.activeTab === 'weekly') {
      this.openCreateWeeklyModal();
    } else {
      this.openCreateDailyModal();
    }
  }

  openCreateSplitModal(): void { this.showCreateSplitModal = true; }
  
  closeCreateSplitModal(): void {
    this.showCreateSplitModal = false;
    this.editingSplitPlan = null;
  }

  openCreateWeeklyModal(): void { this.showCreateWeeklyModal = true; }
  
  closeCreateWeeklyModal(): void {
    this.showCreateWeeklyModal = false;
    this.editingWeeklyPlan = null;
  }

  openCreateDailyModal(): void { this.showCreateDailyModal = true; }
  
  closeCreateDailyModal(): void {
    this.showCreateDailyModal = false;
    this.editingDailyPlan = null;
  }

  canEdit(plan: any): boolean {
    if (this.ownerMode || !this.currentUserId) return true;
    return plan.createdBy && plan.createdBy.toLowerCase() === this.currentUserId.toLowerCase();
  }

  assignPlanner(plan: WorkoutPlan, event: Event): void {
    event.stopPropagation();
    this.onAssign.emit(plan);
  }

  editPlanner(plan: WorkoutPlan, type: 'split' | 'weekly' | 'daily', event?: Event): void {
    if (event) {
      event.stopPropagation();
    }

    let planToEdit = plan;
    if (!this.canEdit(plan)) {
      planToEdit = { ...plan, id: undefined, createdBy: this.currentUserId, isCustom: true };
    }

    if (type === 'split') {
      this.editingSplitPlan = planToEdit as SplitPlanner;
      this.showCreateSplitModal = true;
    } else if (type === 'weekly') {
      this.editingWeeklyPlan = planToEdit as WeeklyPlanner;
      this.showCreateWeeklyModal = true;
    } else {
      this.editingDailyPlan = planToEdit as DailyPlanner;
      this.showCreateDailyModal = true;
    }
  }

  onEditFromTimetableModal(): void {
    const planner = this.selectedPlanner;
    const type = this.selectedPlannerType;
    this.closeDetailsModal();

    if (!planner) return;

    if (type === 'daily') {
      const actual = this.dailyPlanners.find(p => p.id === planner.id);
      if (actual) {
        this.editPlanner(actual, 'daily');
      }
    } else {
      this.editPlanner(planner, type);
    }
  }

  onSplitCreated(plan: SplitPlanner): void {
    if (plan.id) {
      this.workoutPlanService.updatePlan(plan.id, plan).subscribe({
        next: (updatedPlan) => {
          const index = this.splitPlanners.findIndex(p => p.id === plan.id);
          if (index !== -1) {
            this.splitPlanners[index] = updatedPlan as SplitPlanner;
            this.splitPlanners = [...this.splitPlanners];
          }
          this.notification.success(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_UPDATE_SUCCESS);
          this.cdr.markForCheck();
          this.closeCreateSplitModal();
        },
        error: (err) => {
          this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_UPDATE_ERROR);
          console.error(err);
        }
      });
    } else {
      this.workoutPlanService.createPlan(plan).subscribe({
        next: (createdPlan) => {
          this.splitPlanners.unshift(createdPlan as SplitPlanner);
          this.splitPlanners = [...this.splitPlanners];
          this.notification.success(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_CREATE_SUCCESS);
          this.cdr.markForCheck();
          this.closeCreateSplitModal();
        },
        error: (err) => {
          this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_CREATE_ERROR);
          console.error(err);
        }
      });
    }
  }

  onWeeklyCreated(plan: WeeklyPlanner): void {
    if (plan.id) {
      this.workoutPlanService.updatePlan(plan.id, plan).subscribe({
        next: (updatedPlan) => {
          const index = this.weeklyPlanners.findIndex(p => p.id === plan.id);
          if (index !== -1) {
            this.weeklyPlanners[index] = updatedPlan as WeeklyPlanner;
            this.weeklyPlanners = [...this.weeklyPlanners];
          }
          this.notification.success(CONSTANTS.WORKOUT_PLANNER_MODULE.WEEKLY_UPDATE_SUCCESS);
          this.cdr.markForCheck();
          this.closeCreateWeeklyModal();
        },
        error: (err) => {
          this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.WEEKLY_UPDATE_ERROR);
          console.error(err);
        }
      });
    } else {
      this.workoutPlanService.createPlan(plan).subscribe({
        next: (createdPlan) => {
          this.weeklyPlanners.unshift(createdPlan as WeeklyPlanner);
          this.weeklyPlanners = [...this.weeklyPlanners];
          this.notification.success(CONSTANTS.WORKOUT_PLANNER_MODULE.WEEKLY_CREATE_SUCCESS);
          this.cdr.markForCheck();
          this.closeCreateWeeklyModal();
        },
        error: (err) => {
          this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.WEEKLY_CREATE_ERROR);
          console.error(err);
        }
      });
    }
  }

  onDailyCreated(plan: DailyPlanner): void {
    if (plan.id) {
      this.workoutPlanService.updatePlan(plan.id, plan).subscribe({
        next: (updatedPlan) => {
          const index = this.dailyPlanners.findIndex(p => p.id === plan.id);
          if (index !== -1) {
            this.dailyPlanners[index] = updatedPlan as DailyPlanner;
            this.dailyPlanners = [...this.dailyPlanners];
          }
          this.notification.success(CONSTANTS.WORKOUT_PLANNER_MODULE.DAILY_UPDATE_SUCCESS);
          this.cdr.markForCheck();
          this.closeCreateDailyModal();
        },
        error: (err) => {
          this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.DAILY_UPDATE_ERROR);
          console.error(err);
        }
      });
    } else {
      this.workoutPlanService.createPlan(plan).subscribe({
        next: (createdPlan) => {
          this.dailyPlanners.unshift(createdPlan as DailyPlanner);
          this.dailyPlanners = [...this.dailyPlanners];
          this.notification.success(CONSTANTS.WORKOUT_PLANNER_MODULE.DAILY_CREATE_SUCCESS);
          this.cdr.markForCheck();
          this.closeCreateDailyModal();
        },
        error: (err) => {
          this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.DAILY_CREATE_ERROR);
          console.error(err);
        }
      });
    }
  }

  async deletePlanner(id: string | undefined, type: 'split' | 'weekly' | 'daily', event: Event): Promise<void> {
    event.stopPropagation();
    if (!id) return;
    
    const confirmed = await this.confirmationService.confirm({
      title: 'Delete Workout Plan',
      message: CONSTANTS.WORKOUT_PLANNER_MODULE.DELETE_CONFIRM,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      type: 'danger'
    });

    if (confirmed) {
      this.workoutPlanService.deletePlan(id).subscribe({
        next: () => {
          if (type === 'split') {
            this.splitPlanners = this.splitPlanners.filter(p => p.id !== id);
          } else if (type === 'weekly') {
            this.weeklyPlanners = this.weeklyPlanners.filter(p => p.id !== id);
          } else {
            this.dailyPlanners = this.dailyPlanners.filter(p => p.id !== id);
          }
          this.notification.success(CONSTANTS.WORKOUT_PLANNER_MODULE.DELETE_SUCCESS);
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.DELETE_ERROR);
          console.error(err);
        }
      });
    }
  }

  onPlannerChanged(): void {
    if (!this.selectedPlanner) return;

    let targetPlan: WorkoutPlan | null = null;

    if (this.selectedPlannerType === 'daily') {
      const dailyIdx = this.dailyPlanners.findIndex(p => p.id === this.selectedPlanner!.id);
      if (dailyIdx !== -1) {
        const virtualSplit = this.selectedPlanner as SplitPlanner;
        this.dailyPlanners[dailyIdx].exercises = virtualSplit.days[0].exercises;
        targetPlan = this.dailyPlanners[dailyIdx];
      }
    } else if (this.selectedPlannerType === 'split') {
      targetPlan = this.selectedPlanner;
    } else if (this.selectedPlannerType === 'weekly') {
      targetPlan = this.selectedPlanner;
    }

    if (targetPlan && targetPlan.id) {
      this.workoutPlanService.updatePlan(targetPlan.id, targetPlan).subscribe({
        next: (updatedPlan) => {
          if (this.selectedPlannerType === 'daily') {
            const idx = this.dailyPlanners.findIndex(p => p.id === targetPlan!.id);
            if (idx !== -1) this.dailyPlanners[idx] = updatedPlan as DailyPlanner;
          } else if (this.selectedPlannerType === 'split') {
            const idx = this.splitPlanners.findIndex(p => p.id === targetPlan!.id);
            if (idx !== -1) this.splitPlanners[idx] = updatedPlan as SplitPlanner;
          } else if (this.selectedPlannerType === 'weekly') {
            const idx = this.weeklyPlanners.findIndex(p => p.id === targetPlan!.id);
            if (idx !== -1) this.weeklyPlanners[idx] = updatedPlan as WeeklyPlanner;
          }
          this.splitPlanners = [...this.splitPlanners];
          this.weeklyPlanners = [...this.weeklyPlanners];
          this.dailyPlanners = [...this.dailyPlanners];
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.PLAN_SAVE_ERROR);
          console.error(err);
        }
      });
    }
  }
}
