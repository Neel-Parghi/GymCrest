import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { Exercise } from '../../../../../shared/models/exercise.model';
import { DropdownComponent } from '../../../../../shared/components/dropdown/dropdown.component';
import { DropdownOption } from '../../../../../shared/models/dropdown.model';
import { NotificationService } from '../../../../../core/services/notification.service';
import { SplitPlanner } from '../../../../../shared/models/workout-plan.model';
import { CONSTANTS } from '../../../../../core/constants/constants';
import { AuthApiService } from '../../../../../core/services/auth-api.service';
import { FIELD_LIMITS } from '../../../../../shared/constants/validation.constants';

@Component({
  selector: 'app-split-planner-creator',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, DropdownComponent],
  templateUrl: './split-planner-creator.component.html',
  styleUrl: './split-planner-creator.component.scss'
})
export class SplitPlannerCreatorComponent implements OnInit {
  readonly limits = FIELD_LIMITS;
  private fb = inject(FormBuilder);
  private notification = inject(NotificationService);
  private authService = inject(AuthApiService);

  get isStandaloneUser(): boolean {
    return this.authService.getUserRole() === 'User';
  }

  @Input() categories: string[] = [];
  @Input() exercisesMap: { [category: string]: Exercise[] } = {};
  @Input() editData: SplitPlanner | null = null;

  @Output() save = new EventEmitter<SplitPlanner>();
  @Output() close = new EventEmitter<void>();

  createSplitForm!: FormGroup;
  activeStep: 'details' | 'plan' = 'details';
  activeDayTab = 0;
  copiedWorkout: any = null;

  levelOptions: DropdownOption[] = [
    { label: 'Beginner', value: 'Beginner' },
    { label: 'Intermediate', value: 'Intermediate' },
    { label: 'Advanced', value: 'Advanced' }
  ];

  goalOptions: DropdownOption[] = [
    { label: 'Hypertrophy', value: 'Hypertrophy' },
    { label: 'Strength & Power', value: 'Strength & Power' },
    { label: 'Fat Loss', value: 'Fat Loss' }
  ];

  getCategoryDropdownOptions(): DropdownOption[] {
    return this.categories.map(c => ({ label: c, value: c }));
  }

  getCategoryForExercise(exerciseName: string): string | null {
    if (!exerciseName) return null;
    for (const cat of Object.keys(this.exercisesMap)) {
      const found = this.exercisesMap[cat].some(e => e.name === exerciseName);
      if (found) return cat;
    }
    return null;
  }

  getExercisesDropdownOptions(dayIdx: number, currentExIdx?: number): DropdownOption[] {
    const day = this.splitDays.at(dayIdx);
    const targetCats = day?.get('targetCategories')?.value || ['Back'];
    const exercises = this.getExercisesForCategories(targetCats);

    const usedNames = new Set<string>();
    const exercisesFormArray = this.getSplitExercises(dayIdx);
    let currentSelectedName = '';
    if (currentExIdx !== undefined) {
      const currentCtrl = exercisesFormArray.at(currentExIdx);
      currentSelectedName = currentCtrl?.get('exerciseName')?.value || '';
    }

    exercisesFormArray.controls.forEach((ctrl, idx) => {
      if (idx !== currentExIdx) {
        const name = ctrl.get('exerciseName')?.value;
        if (name) usedNames.add(name);
      }
    });

    const options = exercises
      .filter(e => !usedNames.has(e.name))
      .map(e => ({
        label: `${e.name} (${e.equipment})`,
        value: e.name
      }));

    if (currentSelectedName && !options.some(opt => opt.value === currentSelectedName)) {
      let exerciseDetail: Exercise | undefined;
      for (const cat of Object.keys(this.exercisesMap)) {
        const found = this.exercisesMap[cat].find(e => e.name === currentSelectedName);
        if (found) {
          exerciseDetail = found;
          break;
        }
      }
      options.unshift({
        label: exerciseDetail ? `${exerciseDetail.name} (${exerciseDetail.equipment})` : currentSelectedName,
        value: currentSelectedName
      });
    }

    return options;
  }

  nextStep(): void {
    if (this.createSplitForm.get('name')?.invalid) {
      this.createSplitForm.get('name')?.markAsTouched();
      this.notification.warning(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_INVALID_NAME);
      return;
    }
    if (this.createSplitForm.get('description')?.invalid) {
      this.createSplitForm.get('description')?.markAsTouched();
      this.notification.warning(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_DESCRIPTION_REQUIRED);
      return;
    }
    this.activeStep = 'plan';
  }

  prevStep(): void {
    this.activeStep = 'details';
  }

  copyWorkout(dayIdx: number): void {
    const dayGroup = this.splitDays.at(dayIdx);
    const categories = dayGroup.get('targetCategories')?.value || ['Back'];
    const exercisesData = this.getSplitExercises(dayIdx).value.map((ex: any) => ({
      name: ex.exerciseName,
      sets: ex.targetSets,
      reps: ex.targetReps,
      notes: ex.notes,
      isCustomName: ex.isCustomName
    }));

    this.copiedWorkout = {
      categories: [...categories],
      exercises: exercisesData
    };

    localStorage.setItem('gymforge_copied_workout_split', JSON.stringify(this.copiedWorkout));
    this.notification.success(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_COPY_SUCCESS);
  }

  pasteWorkout(dayIdx: number): void {
    const raw = localStorage.getItem('gymforge_copied_workout_split');
    if (!raw) {
      this.notification.warning(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_NO_CLIPBOARD);
      return;
    }

    try {
      const data = JSON.parse(raw);
      if (!data.exercises || data.exercises.length === 0) return;

      const dayGroup = this.splitDays.at(dayIdx);
      let categories = ['Back'];
      if (data.categories) {
        categories = data.categories;
      } else if (data.category) {
        categories = data.category.split(' + ');
      }
      dayGroup.patchValue({
        targetCategories: categories
      });

      const exArray = this.getSplitExercises(dayIdx);
      exArray.clear();

      data.exercises.forEach((ex: any) => {
        exArray.push(this.fb.group({
          exerciseName: [ex.name || '', [Validators.required]],
          targetSets: [ex.sets || 3, [Validators.required, Validators.min(1), Validators.max(200)]],
          targetReps: [ex.reps || '10-12 Reps', [Validators.required, Validators.maxLength(15)]],
          notes: [ex.notes || '', [Validators.maxLength(FIELD_LIMITS.EXERCISE_NOTES)]],
          isCustomName: [ex.isCustomName ?? !this.getCategoryForExercise(ex.name)]
        }));
      });

      this.notification.success(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_PASTE_SUCCESS);
    } catch (e) {
      console.error(e);
      this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_PASTE_ERROR);
    }
  }

  ngOnInit(): void {
    this.initSplitForm();
    if (this.editData) {
      this.populateFormWithEditData(this.editData);
    }
  }

  get splitDays(): FormArray {
    return this.createSplitForm.get('days') as FormArray;
  }

  getSplitExercises(dayIndex: number): FormArray {
    return this.splitDays.at(dayIndex).get('exercises') as FormArray;
  }

  private initSplitForm(): void {
    this.createSplitForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
      description: ['', [Validators.maxLength(200)]],
      level: ['Beginner'],
      goal: ['Hypertrophy'],
      isCustom: [false],
      days: this.fb.array([])
    });

    if (!this.editData) {
      this.addSplitDay();
    }
  }

  private populateFormWithEditData(data: any): void {
    this.createSplitForm.patchValue({
      name: data.name,
      description: data.description,
      level: data.level,
      goal: data.goal,
      isCustom: data.isCustom ?? false
    });

    const daysArray = this.splitDays;
    daysArray.clear();

    (data.days || []).forEach((day: any) => {
      const categoriesList = day.category ? day.category.split(' + ') : [this.categories[0] || 'Back'];
      const dayGroup = this.fb.group({
        name: [day.name, [Validators.required, Validators.maxLength(25)]],
        targetCategories: [categoriesList],
        exercises: this.fb.array(
          (day.exercises || []).map((ex: any) => this.fb.group({
            exerciseName: [ex.name || '', [Validators.required]],
            targetSets: [ex.sets || 3, [Validators.required, Validators.min(1), Validators.max(200)]],
            targetReps: [ex.reps || '10-12', [Validators.required, Validators.maxLength(10)]],
            notes: [ex.notes || '', [Validators.maxLength(FIELD_LIMITS.EXERCISE_NOTES)]],
            isCustomName: [!this.getCategoryForExercise(ex.name)]
          }))
        )
      });

      daysArray.push(dayGroup);
    });
  }

  addSplitDay(): void {
    const defaultCat = this.categories[0] || 'Back';
    const dayGroup = this.fb.group({
      name: [`Day ${this.splitDays.length + 1}`, [Validators.required, Validators.maxLength(25)]],
      targetCategories: [[defaultCat]],
      exercises: this.fb.array([])
    });

    this.splitDays.push(dayGroup);
    const newDayIndex = this.splitDays.length - 1;
    this.addSplitExercise(newDayIndex);
  }

  removeSplitDay(index: number): void {
    if (this.splitDays.length > 1) {
      this.splitDays.removeAt(index);
      if (this.activeDayTab >= this.splitDays.length) {
        this.activeDayTab = this.splitDays.length - 1;
      }
    }
  }

  addSplitExercise(dayIndex: number): void {
    const exGroup = this.fb.group({
      exerciseName: ['', [Validators.required]],
      targetSets: [3, [Validators.required, Validators.min(1), Validators.max(200)]],
      targetReps: ['10-12', [Validators.required, Validators.maxLength(10)]],
      notes: ['', [Validators.maxLength(FIELD_LIMITS.EXERCISE_NOTES)]],
      isCustomName: [false]
    });

    this.getSplitExercises(dayIndex).push(exGroup);
    setTimeout(() => {
      const wrapper = document.querySelector('.exercise-table-scroll-wrapper');
      if (wrapper) {
        wrapper.scrollTop = wrapper.scrollHeight;
      }
    }, 200);
  }

  removeSplitExercise(dayIdx: number, exIdx: number): void {
    const exArray = this.getSplitExercises(dayIdx);
    exArray.removeAt(exIdx);
  }

  toggleCustomExerciseName(dayIdx: number, exIdx: number): void {
    const ctrl = this.getSplitExercises(dayIdx).at(exIdx).get('isCustomName');
    ctrl?.setValue(!ctrl.value);
  }

  isCustomExerciseName(dayIdx: number, exIdx: number): boolean {
    return !!this.getSplitExercises(dayIdx).at(exIdx).get('isCustomName')?.value;
  }

  enforceMaxSets(exGroup: any, event: Event): void {
    const input = event.target as HTMLInputElement;
    if (parseInt(input.value) > 200) {
      input.value = '200';
      exGroup.get('targetSets')?.setValue(200);
    }
  }

  toggleCategory(dayIdx: number, cat: string): void {
    const dayGroup = this.splitDays.at(dayIdx);
    const ctrl = dayGroup.get('targetCategories');
    if (!ctrl) return;
    const current = [...(ctrl.value || [])];
    const idx = current.indexOf(cat);
    if (idx > -1) {
      if (current.length > 1) {
        current.splice(idx, 1);
      } else {
        this.notification.warning(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_MIN_CATEGORY);
        return;
      }
    } else {
      if (current.length >= 4) {
        this.notification.warning('Maximum 4 muscle groups can be selected.');
        return;
      }
      current.push(cat);
    }
    ctrl.setValue(current);
  }

  isCategorySelected(dayIdx: number, cat: string): boolean {
    const dayGroup = this.splitDays.at(dayIdx);
    const current = dayGroup?.get('targetCategories')?.value || [];
    return current.includes(cat);
  }

  getExercisesForCategory(category: string): Exercise[] {
    if (!category) return [];
    if (this.exercisesMap[category]) {
      return this.exercisesMap[category];
    }
    const key = Object.keys(this.exercisesMap).find(k => k.toLowerCase() === category.toLowerCase());
    return key ? this.exercisesMap[key] : [];
  }

  getExercisesForCategories(categories: string[]): Exercise[] {
    let list: Exercise[] = [];
    categories.forEach(cat => {
      const exs = this.getExercisesForCategory(cat);
      list = [...list, ...exs];
    });
    return list.sort((a, b) => a.name.localeCompare(b.name));
  }

  onSubmitSplit(): void {
    if (this.createSplitForm.invalid) {
      this.createSplitForm.markAllAsTouched();

      let hasInvalidExercises = false;
      for (let i = 0; i < this.splitDays.length; i++) {
        const exercises = this.getSplitExercises(i);
        if (exercises.controls.some(ctrl => ctrl.get('exerciseName')?.invalid)) {
          hasInvalidExercises = true;
          break;
        }
      }

      if (hasInvalidExercises) {
        this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.SELECT_EXERCISE_WARNING);
      } else {
        this.notification.error(CONSTANTS.WORKOUT_PLANNER_MODULE.SPLIT_INVALID_FORM);
      }
      return;
    }

    const val = this.createSplitForm.getRawValue();
    const totalExs = val.days.reduce((sum: number, d: any) => sum + d.exercises.length, 0);

    const splitData: SplitPlanner = {
      ...(this.editData ? { id: this.editData.id } : {}),
      name: val.name,
      description: val.description,
      level: val.level,
      goal: val.goal,
      type: 'Split',
      isCustom: val.isCustom ?? false,
      daysCount: val.days.length,
      exercisesCount: totalExs,
      days: val.days.map((d: any) => {
        // Auto-detect categories based on selected exercises to ensure none are missed
        const categoriesSet = new Set<string>(d.targetCategories || []);
        d.exercises.forEach((ex: any) => {
          if (ex.exerciseName) {
            const cat = this.getCategoryForExercise(ex.exerciseName);
            if (cat) categoriesSet.add(cat);
          }
        });
        return {
          name: d.name,
          category: Array.from(categoriesSet).join(' + '),
          exercises: d.exercises.map((e: any) => ({
            name: e.exerciseName,
            sets: e.targetSets,
            reps: e.targetReps,
            notes: e.notes
          }))
        };
      })
    };

    this.save.emit(splitData);
  }

  onClose(): void {
    this.close.emit();
  }
}
