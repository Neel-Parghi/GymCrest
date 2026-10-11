import { Component, OnInit, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { FIELD_LIMITS } from '../../../constants/validation.constants';
import { NotificationService } from '../../../../core/services/notification.service';
import { DropdownComponent } from '../../dropdown/dropdown.component';
import { DropdownOption } from '../../../../shared/models/dropdown.model';
import { DIET_TYPES } from '../../../../shared/constants/fitness-preferences';
import { CONSTANTS } from '../../../../core/constants/constants';
import { TimePickerComponent } from '../../time-picker/time-picker.component';
import { round2 } from '../../../utils/number';

@Component({
  selector: 'app-diet-template-creator',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, DropdownComponent, TimePickerComponent],
  templateUrl: './diet-template-creator.component.html',
  styleUrl: './diet-template-creator.component.scss'
})
export class DietTemplateCreatorComponent implements OnInit {
  readonly limits = FIELD_LIMITS;

  private fb = inject(FormBuilder);
  private notification = inject(NotificationService);

  @Input() plan: any = null;
  @Input() title: string = '';
  @Output() save = new EventEmitter<any>();
  @Output() close = new EventEmitter<void>();

  createForm!: FormGroup;
  activeFormTab: 'details' | 'meals' = 'details';
  expandedMealIndex = 0;

  goalOptions: DropdownOption[] = [
    { value: 'Muscle Gain', label: 'Muscle Gain (Bulking)', icon: 'fa-solid fa-dumbbell' },
    { value: 'Fat Loss', label: 'Fat Loss (Cutting)', icon: 'fa-solid fa-fire' },
    { value: 'Maintenance', label: 'Maintenance & Health', icon: 'fa-solid fa-heart' }
  ];

  dietTypeOptions: DropdownOption[] = [
    { value: '', label: 'Not specified', icon: 'fa-solid fa-utensils' },
    ...DIET_TYPES.map(d => ({ value: d.value, label: d.label, icon: d.icon }))
  ];

  ngOnInit(): void {
    this.initForm(this.plan);
  }

  initForm(plan?: any): void {
    this.createForm = this.fb.group({
      name: [plan?.name || '', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
      description: [plan?.description || '', [Validators.maxLength(300)]],
      goal: [plan?.goal || 'Muscle Gain', [Validators.required]],
      dietType: [plan?.dietType || ''],
      protein: [plan?.protein ?? null, [Validators.required, Validators.min(0)]],
      carbs: [plan?.carbs ?? null, [Validators.min(0)]],
      fats: [plan?.fats ?? null, [Validators.min(0)]],
      meals: this.fb.array([])
    });

    if (plan?.meals && plan.meals.length > 0) {
      plan.meals.forEach((m: any) => this.addMeal(m));
    } else {
      this.addMeal();
    }
  }

  get meals(): FormArray {
    return this.createForm.get('meals') as FormArray;
  }

  addMeal(meal?: any): void {
    const rawName: string = meal?.name || '';
    const rawTime: string = meal?.time || '';
    const parsedTime = rawTime ? this.to24h(rawTime) : '08:00';

    const mealGroup = this.fb.group({
      mealTitle: [rawName, [Validators.required, Validators.maxLength(30)]],
      mealTime: [parsedTime, [Validators.required]],
      calories: [meal?.calories ?? null, [Validators.required, Validators.min(0), Validators.max(5000)]],
      protein: [meal?.protein ?? null, [Validators.required, Validators.min(0), Validators.max(300)]],
      carbs: [meal?.carbs ?? null, [Validators.min(0), Validators.max(500)]],
      fats: [meal?.fats ?? null, [Validators.min(0), Validators.max(200)]],
      items: [meal?.items || '', [Validators.maxLength(FIELD_LIMITS.NOTES)]]
    });

    mealGroup.valueChanges.subscribe(changes => {
      let newCalories = changes.calories;
      let newProtein = changes.protein;
      let newCarbs = changes.carbs;
      let newFats = changes.fats;
      let updated = false;

      if (newCalories !== null && newCalories !== undefined) {
        if (newCalories > 5000) { newCalories = 5000; updated = true; }
        if (newCalories < 0) { newCalories = 0; updated = true; }
      }
      if (newProtein !== null && newProtein !== undefined) {
        if (newProtein > 300) { newProtein = 300; updated = true; }
        if (newProtein < 0) { newProtein = 0; updated = true; }
      }
      if (newCarbs !== null && newCarbs !== undefined) {
        if (newCarbs > 500) { newCarbs = 500; updated = true; }
        if (newCarbs < 0) { newCarbs = 0; updated = true; }
      }
      if (newFats !== null && newFats !== undefined) {
        if (newFats > 200) { newFats = 200; updated = true; }
        if (newFats < 0) { newFats = 0; updated = true; }
      }

      if (updated) {
        mealGroup.patchValue({ calories: newCalories, protein: newProtein, carbs: newCarbs, fats: newFats }, { emitEvent: false });
      }
    });

    this.meals.push(mealGroup);
    this.expandedMealIndex = this.meals.length - 1;

    setTimeout(() => {
      const mealElements = document.querySelectorAll('.meal-builder-item');
      if (mealElements && mealElements.length > 0) {
        const lastMeal = mealElements[mealElements.length - 1];
        lastMeal.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
      }
    }, 100);
  }

  private to24h(timeStr: string): string {
    if (!timeStr)
      return '';

    if (/^\d{1,2}:\d{2}$/.test(timeStr.trim()))
      return timeStr.trim().padStart(5, '0');

    const m = timeStr.trim().match(/(\d{1,2}):(\d{2})\s*(AM|PM)/i);

    if (!m)
      return timeStr.trim();

    let h = parseInt(m[1], 10);
    const min = m[2];
    const period = m[3].toUpperCase();

    if (period === 'PM' && h !== 12)
      h += 12;

    if (period === 'AM' && h === 12)
      h = 0;

    return `${String(h).padStart(2, '0')}:${min}`;
  }

  private to12h(timeStr: string): string {
    if (!timeStr)
      return '08:00 AM';

    const [hStr, min] = timeStr.split(':');
    let h = parseInt(hStr, 10);
    const period = h >= 12 ? 'PM' : 'AM';

    if (h === 0)
      h = 12;
    else if (h > 12)
      h -= 12;

    return `${String(h).padStart(2, '0')}:${min} ${period}`;
  }

  toggleMealExpand(index: number, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.expandedMealIndex = this.expandedMealIndex === index ? -1 : index;
  }

  removeMeal(index: number, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    if (this.meals.length <= 1) {
      this.notification.warning(CONSTANTS.DIET_PLANNER_MODULE.MIN_MEAL_WARNING);
      return;
    }
    this.meals.removeAt(index);
    if (this.expandedMealIndex === index) {
      this.expandedMealIndex = Math.max(0, index - 1);
    } else if (this.expandedMealIndex > index) {
      this.expandedMealIndex--;
    }
  }

  get computedCalories(): number {
    if (!this.createForm) return 0;
    const p = this.createForm.get('protein')?.value || 0;
    const c = this.createForm.get('carbs')?.value || 0;
    const f = this.createForm.get('fats')?.value || 0;
    return round2((p * 4) + (c * 4) + (f * 9));
  }

  get macroPercentages(): { protein: number, carbs: number, fats: number } {
    const p = this.createForm?.get('protein')?.value || 0;
    const c = this.createForm?.get('carbs')?.value || 0;
    const f = this.createForm?.get('fats')?.value || 0;

    const pKcal = p * 4;
    const cKcal = c * 4;
    const fKcal = f * 9;
    const total = pKcal + cKcal + fKcal;

    if (total === 0) return { protein: 0, carbs: 0, fats: 0 };
    return {
      protein: Math.round((pKcal / total) * 100),
      carbs: Math.round((cKcal / total) * 100),
      fats: Math.round((fKcal / total) * 100)
    };
  }

  get mealsTotalCalories(): number {
    const mealList = this.meals.value || [];
    return round2(mealList.reduce((sum: number, m: any) => sum + (m.calories || 0), 0));
  }

  getCircumference(): number {
    return 377;
  }

  getProteinOffset(): number {
    const pct = this.macroPercentages.protein;
    return this.getCircumference() - (pct / 100) * this.getCircumference();
  }

  getCarbsOffset(): number {
    const pct = this.macroPercentages.carbs;
    const proteinPct = this.macroPercentages.protein;
    return this.getCircumference() - ((pct + proteinPct) / 100) * this.getCircumference();
  }

  getFatsOffset(): number {
    const pct = this.macroPercentages.fats;
    const prevPct = this.macroPercentages.protein + this.macroPercentages.carbs;
    return this.getCircumference() - ((pct + prevPct) / 100) * this.getCircumference();
  }

  onClose(): void {
    this.close.emit();
  }

  onSubmit(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const val = this.createForm.getRawValue();
    const planData = {
      id: this.plan?.id,
      name: val.name,
      description: val.description || '',
      calories: this.computedCalories,
      protein: val.protein,
      carbs: val.carbs ?? 0,
      fats: val.fats ?? 0,
      goal: val.goal,
      dietType: val.dietType || null,
      meals: val.meals.map((m: any, idx: number) => ({
        name: m.mealTitle.trim(),
        time: this.to12h(m.mealTime),
        calories: m.calories,
        protein: m.protein,
        carbs: m.carbs ?? 0,
        fats: m.fats ?? 0,
        items: m.items,
        sortOrder: idx
      }))
    };

    this.save.emit(planData);
  }
}
