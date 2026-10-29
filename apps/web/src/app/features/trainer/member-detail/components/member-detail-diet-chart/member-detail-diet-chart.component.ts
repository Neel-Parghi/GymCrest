import { Component, Input, Output, EventEmitter, inject, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MemberService } from '../../../../../core/services/member.service';
import { NotificationService } from '../../../../../core/services/notification.service';
import { CONSTANTS } from '../../../../../core/constants/constants';
import { ConfirmationService } from '../../../../../core/services/confirmation.service';
import { DietTrackingService } from '../../../../../core/services/diet-tracking.service';
import { AuthApiService } from '../../../../../core/services/auth-api.service';
import { DietTemplateCreatorComponent } from '../../../../../shared/components/diet-library/diet-template-creator/diet-template-creator.component';

@Component({
  selector: 'app-member-detail-diet-chart',
  standalone: true,
  imports: [CommonModule, DietTemplateCreatorComponent],
  templateUrl: './member-detail-diet-chart.component.html',
  styleUrl: './member-detail-diet-chart.component.scss',
})
export class PTMemberDetailDietChartComponent implements OnChanges {
  private memberService = inject(MemberService);
  private notification = inject(NotificationService);
  private confirmationService = inject(ConfirmationService);
  private dietTrackingService = inject(DietTrackingService);
  private authService = inject(AuthApiService);

  @Input() activeDiet: any = null;
  @Input() memberId: string = '';
  @Input() memberInfo: any = null;

  @Output() openAssignModal = new EventEmitter<void>();
  @Output() customDietSaved = new EventEmitter<void>();

  showCreateCustomModal = false;
  customPlanData: any = null;

  // Collapsed by default on mobile - the macro breakdown expands on tap to cut down scroll before the meal schedule.
  macrosExpanded = false;

  // Diet Tracking Data
  // Diet Tracking Data
  dietLog: any = null;
  isUserRole: boolean = false;

  ngOnChanges(): void {
    this.isUserRole = this.authService.getUserRole() === 'User';
    if (this.memberId) {
      this.loadDietLog();
    }
  }

  loadDietLog(): void {
    if (this.isUserRole) return; // Tracker section is hidden for users - no need to fetch

    const today = new Date().toISOString().split('T')[0];
    this.dietTrackingService.getMemberDietLog(this.memberId, today).subscribe({
      next: (log) => this.dietLog = log,
      error: (err) => console.error('Failed to load member diet log', err)
    });
  }

  onOpenAssignModal(): void {
    this.openAssignModal.emit();
  }

  async onRemoveActiveDiet(): Promise<void> {
    const confirmed = await this.confirmationService.confirm({
      title: 'Remove Active Diet',
      message: 'Are you sure you want to remove the active diet plan from this member?',
      confirmText: 'Remove',
      cancelText: 'Cancel',
      type: 'danger'
    });

    if (confirmed) {
      this.memberService.unassignActiveDiet(this.memberId).subscribe({
        next: () => {
          this.notification.success('Active diet plan successfully removed.');
          this.customDietSaved.emit(); // Emitting this reloads the view
        },
        error: (err) => {
          this.notification.error(err.error?.message || 'Failed to remove active diet plan.');
        }
      });
    }
  }

  openCreateCustomModal(): void {
    if (this.activeDiet) {
      this.customPlanData = {
        name: this.activeDiet.planName,
        goal: this.activeDiet.goal,
        protein: this.activeDiet.macros?.protein,
        carbs: this.activeDiet.macros?.carbs,
        fats: this.activeDiet.macros?.fats,
        meals: (this.activeDiet.meals || []).map((m: any) => ({
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
      this.customPlanData = {
        name: `${this.memberInfo?.firstName || 'Member'}'s Diet Plan`,
        goal: 'Muscle Gain',
        protein: 150,
        carbs: 200,
        fats: 70,
        meals: []
      };
    }
    this.showCreateCustomModal = true;
  }

  closeCreateCustomModal(): void {
    this.showCreateCustomModal = false;
    this.customPlanData = null;
  }

  onSaveCustomDiet(planData: any): void {
    const finalPayload = {
      ...planData,
      isCustom: true,
      isTemplate: false,
      description: `Custom diet plan created for ${this.memberInfo?.firstName || 'member'}.`
    };

    this.memberService.assignCustomDiet(this.memberId, finalPayload).subscribe({
      next: () => {
        this.notification.success(
          CONSTANTS.MEMBER_DETAIL_MODULE.ASSIGN_DIET_SUCCESS_PREFIX +
          finalPayload.name +
          CONSTANTS.MEMBER_DETAIL_MODULE.ASSIGN_DIET_SUCCESS_MID +
          (this.memberInfo?.firstName || 'member') +
          CONSTANTS.MEMBER_DETAIL_MODULE.ASSIGN_DIET_SUCCESS_SUFFIX
        );
        this.closeCreateCustomModal();
        this.customDietSaved.emit();
      },
      error: () => {
        this.notification.error(CONSTANTS.MEMBER_DETAIL_MODULE.ASSIGN_DIET_ERROR);
      }
    });
  }

  getMacroPercentage(macroType: 'protein' | 'carbs' | 'fats'): number {
    if (!this.activeDiet || !this.activeDiet.macros) return 0;
    const protein = this.activeDiet.macros.protein || 0;
    const carbs = this.activeDiet.macros.carbs || 0;
    const fats = this.activeDiet.macros.fats || 0;

    const pKcal = protein * 4;
    const cKcal = carbs * 4;
    const fKcal = fats * 9;
    const totalKcal = pKcal + cKcal + fKcal;

    if (totalKcal === 0) return 0;

    if (macroType === 'protein') return Math.round((pKcal / totalKcal) * 100);
    if (macroType === 'carbs') return Math.round((cKcal / totalKcal) * 100);
    return Math.round((fKcal / totalKcal) * 100);
  }

  getCircumference(): number {
    return 377; // 2 * Math.PI * 60
  }

  getProteinOffset(): number {
    const pct = this.getMacroPercentage('protein');
    return this.getCircumference() - (pct / 100) * this.getCircumference();
  }

  getCarbsOffset(): number {
    const pct = this.getMacroPercentage('carbs');
    const proteinPct = this.getMacroPercentage('protein');
    return this.getCircumference() - ((pct + proteinPct) / 100) * this.getCircumference();
  }

  getFatsOffset(): number {
    const pct = this.getMacroPercentage('fats');
    const prevPct = this.getMacroPercentage('protein') + this.getMacroPercentage('carbs');
    return this.getCircumference() - ((pct + prevPct) / 100) * this.getCircumference();
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
