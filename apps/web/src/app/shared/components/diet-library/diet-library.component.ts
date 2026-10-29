import { Component, OnInit, inject, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { DietTemplateCreatorComponent } from './diet-template-creator/diet-template-creator.component';
import { ConfirmationPopupComponent } from '../confirmation-popup/confirmation-popup.component';
import { DietPlanService } from '../../../core/services/diet-plan.service';
import { CONSTANTS } from '../../../core/constants/constants';
import { macroCalorieSplit } from '../../utils/nutrition';
import { DietType, dietTypeLabel, suitsDiet } from '../../constants/fitness-preferences';

@Component({
  selector: 'app-diet-library',
  standalone: true,
  imports: [CommonModule, DietTemplateCreatorComponent, ConfirmationPopupComponent],
  templateUrl: './diet-library.component.html',
  styleUrl: './diet-library.component.scss'
})
export class DietLibraryComponent implements OnInit {
  private notification = inject(NotificationService);
  private dietPlanService = inject(DietPlanService);
  private authService = inject(AuthApiService);

  @Input() currentUserId?: string;
  @Input() activeDietId?: string;
  @Input() isCardView: boolean = false;
  @Input() ownerMode: boolean = false;
  @Input() embedded: boolean = false;
  @Output() onAssign = new EventEmitter<any>();

  /** The member's diet; when set, plans that don't suit it are hidden until they choose to see all. */
  @Input() set dietPreference(value: DietType | null | undefined) {
    this.preference = value ?? null;
    this.applyDietFilter();
  }

  loggedInUserId: string = '';

  /** Every plan the viewer can see; dietPlans is the part currently shown. */
  private allPlans: any[] = [];
  private preference: DietType | null = null;
  showAllDiets = false;
  hiddenPlanCount = 0;
  readonly dietTypeLabel = dietTypeLabel;

  dietPlans: any[] = [];
  showCreateModal = false;
  editingPlan: any = null;
  drawerPlan: any = null;

  showDeleteConfirm = false;
  deletingPlanId: string | null = null;

  openMealDrawer(plan: any): void {
    this.drawerPlan = plan;
  }

  closeMealDrawer(): void {
    this.drawerPlan = null;
  }

  ngOnInit(): void {
    this.authService.userProfile$.subscribe(profile => {
      if (profile) {
        this.loggedInUserId = profile.id;
        this.loadDietPlans();
      }
    });
  }

  loadDietPlans(forceRefresh = false): void {
    if (!this.loggedInUserId) return;
    this.dietPlanService.getPlans(undefined, forceRefresh).subscribe({
      next: (plans) => {
        if (this.ownerMode || !this.currentUserId) {
          // Trainer/owner mode: see all non-custom templates in the gym
          this.allPlans = (plans || []).filter((p: any) => !p.isCustom);
        } else {
          // User mode: User sees global templates + their own plans
          this.allPlans = (plans || []).filter((p: any) => !p.isCustom || p.createdBy === this.currentUserId);
        }
        this.applyDietFilter();
      },
      error: (err) => {
        console.error('Failed to load diet templates:', err);
        this.notification.error(CONSTANTS.DIET_PLANNER_MODULE.LOAD_ERROR);
      }
    });
  }

  get preferenceLabel(): string {
    return dietTypeLabel(this.preference).toLowerCase();
  }

  toggleShowAllDiets(): void {
    this.showAllDiets = !this.showAllDiets;
    this.applyDietFilter();
  }

  private applyDietFilter(): void {
    const filterByDiet = !!this.preference && !this.showAllDiets;
    // The active plan always stays visible, even if it doesn't match.
    this.dietPlans = filterByDiet
      ? this.allPlans.filter(p => suitsDiet(p.dietType, this.preference) || p.id === this.activeDietId)
      : this.allPlans;
    this.hiddenPlanCount = this.preference
      ? this.allPlans.filter(p => !suitsDiet(p.dietType, this.preference) && p.id !== this.activeDietId).length
      : 0;
  }

  openCreateModal(): void {
    this.editingPlan = null;
    this.showCreateModal = true;
  }

  openEditModal(plan: any): void {
    if (!this.canEdit(plan)) {
      this.editingPlan = { ...plan, id: undefined, createdBy: this.currentUserId, isCustom: true };
    } else {
      this.editingPlan = plan;
    }
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.editingPlan = null;
  }

  onSaveTemplate(planData: any): void {
    if (this.editingPlan && this.editingPlan.id) {
      this.dietPlanService.updatePlan(this.editingPlan.id, planData).subscribe({
        next: () => {
          this.notification.success(CONSTANTS.DIET_PLANNER_MODULE.UPDATE_SUCCESS);
          this.loadDietPlans(true);
          this.closeCreateModal();
        },
        error: (err) => {
          console.error('Failed to update template:', err);
          this.notification.error(CONSTANTS.DIET_PLANNER_MODULE.UPDATE_ERROR);
        }
      });
    } else {
      this.dietPlanService.createPlan(planData).subscribe({
        next: () => {
          this.notification.success(CONSTANTS.DIET_PLANNER_MODULE.CREATE_SUCCESS);
          this.loadDietPlans(true);
          this.closeCreateModal();
        },
        error: (err) => {
          console.error('Failed to create template:', err);
          this.notification.error(CONSTANTS.DIET_PLANNER_MODULE.CREATE_ERROR);
        }
      });
    }
  }

  deleteTemplate(id: string, event: Event): void {
    event.stopPropagation();
    this.deletingPlanId = id;
    this.showDeleteConfirm = true;
  }

  confirmDelete(): void {
    if (!this.deletingPlanId) return;
    this.dietPlanService.deletePlan(this.deletingPlanId).subscribe({
      next: () => {
        this.notification.success(CONSTANTS.DIET_PLANNER_MODULE.DELETE_SUCCESS);
        this.loadDietPlans(true);
        this.closeDeleteConfirm();
      },
      error: (err) => {
        console.error('Failed to delete template:', err);
        this.notification.error(err.error?.message || CONSTANTS.DIET_PLANNER_MODULE.DELETE_ERROR);
        this.closeDeleteConfirm();
      }
    });
  }

  closeDeleteConfirm(): void {
    this.showDeleteConfirm = false;
    this.deletingPlanId = null;
  }

  assignTemplate(plan: any, event: Event): void {
    event.stopPropagation();
    this.onAssign.emit(plan);
  }

  canEdit(plan: any): boolean {
    if (this.ownerMode || !this.currentUserId) return true; // Trainer/owner mode
    return plan.createdBy && plan.createdBy.toLowerCase() === this.currentUserId.toLowerCase(); // User mode
  }

  getMacroPercentageForCard(plan: any, macroType: 'protein' | 'carbs' | 'fats'): number {
    return macroCalorieSplit(plan.protein, plan.carbs, plan.fats)[macroType];
  }

  getMealTime(meal: any): string {
    if (!meal)
      return '08:00 AM';

    if (meal.time)
      return meal.time;

    const name = meal.name || '';
    const match = name.match(/\(([^)]+)\)/);

    return match ? match[1] : '08:00 AM';
  }

  getMealTitle(meal: any): string {
    if (!meal)
      return 'Meal';

    if (meal.time)
      return meal.name;

    const name = meal.name || 'Meal';

    return name.replace(/\s*\([^)]+\)/, '').trim();
  }
}
