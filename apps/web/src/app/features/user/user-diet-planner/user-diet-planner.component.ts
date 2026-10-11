import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PTMemberDetailDietChartComponent } from '../../trainer/member-detail/components/member-detail-diet-chart/member-detail-diet-chart.component';
import { DietLibraryComponent } from '../../../shared/components/diet-library/diet-library.component';
import { MemberService } from '../../../core/services/member.service';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SegmentedTabsComponent } from '../../../shared/components/segmented-tabs/segmented-tabs.component';
import { SegmentedTab } from '../../../shared/models/segmented-tab.model';
import { UserService } from '../../../core/services/user.service';
import { DietType } from '../../../shared/constants/fitness-preferences';

@Component({
  selector: 'app-user-diet-planner',
  standalone: true,
  imports: [CommonModule, PTMemberDetailDietChartComponent, DietLibraryComponent, SegmentedTabsComponent],
  templateUrl: './user-diet-planner.component.html',
  styleUrl: './user-diet-planner.component.scss'
})
export class UserDietPlannerComponent implements OnInit {
  private memberService = inject(MemberService);
  private authService = inject(AuthApiService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private userService = inject(UserService);

  readonly tabs: SegmentedTab[] = [
    { id: 'today', label: 'Today' },
    { id: 'active', label: 'My plan' },
    { id: 'library', label: 'Library' }
  ];

  userId = '';
  memberInfo: any = { firstName: 'User' };

  viewMode: 'active' | 'library' = 'active';
  activeDiet: any = null;
  dietPlans: any[] = [];
  showAssignDietModal = false;
  dietPreference: DietType | null = null;

  ngOnInit() {
    this.viewMode = this.route.snapshot.queryParamMap.get('view') === 'library' ? 'library' : 'active';

    this.userService.getPreferences().subscribe(res => {
      this.dietPreference = (res?.data as { dietPreference?: DietType | null } | null)?.dietPreference ?? null;
    });

    this.authService.userProfile$.subscribe(profile => {
      if (profile) {
        this.userId = profile.id;
        this.memberInfo = { firstName: profile.firstName };
        this.loadActiveDiet();
      }
    });
  }

  setView(tab: string): void {
    if (tab === 'today') {
      this.router.navigate(['/user/diet-tracker']);
      return;
    }
    this.viewMode = tab === 'library' ? 'library' : 'active';
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { view: this.viewMode === 'library' ? 'library' : null },
      replaceUrl: true
    });
  }

  loadActiveDiet(): void {
    this.memberService.getActiveDiet(this.userId).subscribe({
      next: (res: any) => {
        const assignment = res?.data;
        if (assignment?.dietPlan) {
          const dp = assignment.dietPlan;
          this.activeDiet = {
            planId: dp.id,
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

  assignDietPlan(plan: any): void {
    this.memberService.assignDiet(this.userId, plan.id).subscribe({
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
        this.notification.success(`Successfully assigned ${plan.name} to yourself!`);
        this.viewMode = 'active';
      },
      error: (err) => {
        console.error('Failed to assign diet plan:', err);
        this.notification.error('Failed to assign diet plan. Please try again later.');
      }
    });
  }
}
