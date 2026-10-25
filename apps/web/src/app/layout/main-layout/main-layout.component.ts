import { Component, inject, OnInit, HostListener, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { BreakpointObserver } from '@angular/cdk/layout';
import { AuthApiService } from '../../core/services/auth-api.service';
import { ThemeService } from '../../core/services/theme.service';
import { NavigationService } from '../../core/services/navigation.service';
import { NavItem } from '../../core/models/nav-Item.model';
import { ProfileService } from '../../core/services/profile.service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { BranchContextService } from '../../core/services/branch-context.service';
import { GymService } from '../../core/services/gym.service';
import { GymSettingsService } from '../../core/services/gym-settings.service';
import { AnnouncementService } from '../../core/services/announcement.service';
import { UserNotificationApiService } from '../../core/services/user-notification-api.service';
import { FeedbackDialogComponent } from '../../shared/components/feedback-dialog/feedback-dialog.component';
import { TourService } from '../../core/services/tour.service';
import { UserBottomNavComponent } from '../user-bottom-nav/user-bottom-nav.component';
import { forkJoin, of } from 'rxjs';
import { catchError, filter, map } from 'rxjs/operators';
import { BrandLogoComponent } from '../../shared/components/brand-logo/brand-logo.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, LoadingComponent, UserBottomNavComponent, BrandLogoComponent, FeedbackDialogComponent],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent implements OnInit {
  private authApiService = inject(AuthApiService);
  private profileService = inject(ProfileService);
  private themeService = inject(ThemeService);
  private router = inject(Router);
  private readonly navService = inject(NavigationService);
  private gymService = inject(GymService);
  private gymSettingsService = inject(GymSettingsService);
  private breakpointObserver = inject(BreakpointObserver);

  roleName: string = '';
  gymBrand: { name: string; logoUrl: string | null } | null = null;
  gymLogoFailed = false;

  get portalLabel(): string {
    return this.gymBrand?.name || this.roleName + ' Portal';
  }
  dashboardRoute: string = '/super-admin/dashboard';
  profileRoute: string = '/super-admin/profile';
  isSidebarCollapsed = false;
  isMobileSidebarOpen = false;
  menuItems: NavItem[] = [];
  userProfile$ = this.authApiService.userProfile$;

  isTabletOrBelow$ = this.breakpointObserver.observe('(max-width: 1023px)').pipe(map(r => r.matches));
  isMobile$ = this.breakpointObserver.observe('(max-width: 767px)').pipe(map(r => r.matches));

  private branchContextService = inject(BranchContextService);
  activeBranch$ = this.branchContextService.activeBranch$;

  isGymOwner = false;
  isUserRole = false;
  isStaffLocked = false;
  branches: any[] = [];
  isBranchDropdownOpen = false;
  feedbackOpen = false;

  isNotificationsOpen = false;
  announcements: any[] = [];
  private activeRole: string = '';
  private announcementService = inject(AnnouncementService);
  private userNotificationService = inject(UserNotificationApiService);
  private tourService = inject(TourService);
  unreadAnnouncementsCount = 0;

  @ViewChild('pageContent') private pageContent?: ElementRef<HTMLElement>;
  private lastPath = '';

  constructor() {
    this.menuItems = this.navService.getMenuItems();
  }

  ngOnInit(): void {
    this.setRoleName();
    this.profileService.getProfile().subscribe();

    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd)
    ).subscribe(event => {
      this.closeMobileSidebar();
      this.resetScrollOnPageChange(event.urlAfterRedirects);
    });


    const currentRole = this.authApiService.getUserRole();
    if (currentRole === 'GymOwner' || currentRole === 'Staff' || currentRole === 'Trainer') {
      this.loadGymBrand();
      this.gymService.myGymUpdated$.subscribe(() => this.loadGymBrand());
      this.gymSettingsService.loadSettings().subscribe({
        next: () => {
          this.menuItems = this.navService.getMenuItems();
        },
        error: (err) => console.error('Failed to load dynamic gym settings', err)
      });
    }

    const assignedBranchId = this.authApiService.getAssignedBranchId();
    if (assignedBranchId) {
      this.isStaffLocked = true;
      this.gymService.getMyBranches().subscribe({
        next: (res) => {
          this.branches = res.data || [];
          const matchedBranch = this.branches.find(b => b.id === assignedBranchId);
          if (matchedBranch) {
            this.branchContextService.setActiveBranch({ id: matchedBranch.id, name: matchedBranch.name });
          } else {
            this.branchContextService.setActiveBranch({ id: assignedBranchId, name: 'Assigned Location' });
          }
        }
      });
    } else if (this.isGymOwner) {
      this.loadBranches();
    }

    this.loadAnnouncements();
  }

  private loadGymBrand(): void {
    this.gymService.getMyGym().subscribe(res => {
      const gym = res?.data;
      this.gymBrand = gym?.gymName ? { name: gym.gymName, logoUrl: gym.logoUrl || null } : null;
      this.gymLogoFailed = false;
    });
  }

  private resetScrollOnPageChange(url: string): void {
    const path = url.split(/[?#]/)[0];
    if (path !== this.lastPath) {
      this.pageContent?.nativeElement.scrollTo({ top: 0, left: 0 });
    }
    this.lastPath = path;
  }

  private setRoleName(): void {
    const decoded = this.authApiService.decodeToken();
    const role = decoded?.role || '';
    this.roleName = role.replace(/([A-Z])/g, ' $1').trim();
    this.isGymOwner = role === 'GymOwner';
    this.isUserRole = role === 'User';

    if (role === 'GymOwner' || role === 'Staff') {
      this.dashboardRoute = '/gym-owner/dashboard';
      this.profileRoute = '/gym-owner/profile';
    } else if (role === 'Trainer') {
      this.dashboardRoute = '/trainer/dashboard';
      this.profileRoute = '/trainer/profile';
    } else if (role === 'User') {
      this.dashboardRoute = '/user/dashboard';
      this.profileRoute = '/user/profile';
    } else {
      this.dashboardRoute = '/super-admin/dashboard';
      this.profileRoute = '/super-admin/profile';
    }
  }

  loadBranches(): void {
    this.gymService.getMyBranches().subscribe({
      next: (res) => {
        this.branches = res.data || [];
      }
    });
  }

  selectBranch(branch: any | null): void {
    if (branch) {
      this.branchContextService.setActiveBranch({ id: branch.id, name: branch.name });
    } else {
      this.branchContextService.setActiveBranch(null);
    }
    this.isBranchDropdownOpen = false;
  }

  loadAnnouncements(): void {
    const role = this.authApiService.getUserRole();

    if (role === 'User') {
      forkJoin({
        announcements: this.announcementService.getMyGymAnnouncements().pipe(catchError(() => of([]))),
        notifications: this.userNotificationService.getMyNotifications().pipe(catchError(() => of([])))
      }).subscribe({
        next: (res) => {
          const announcementsRes = res.announcements as any;
          const announcementsList = announcementsRes?.data || (Array.isArray(announcementsRes) ? announcementsRes : []);
          const notificationsRes = res.notifications as any;
          const notificationsList = notificationsRes?.data || (Array.isArray(notificationsRes) ? notificationsRes : []);
          const merged = [...announcementsList, ...notificationsList]
            .sort((a, b) => new Date(b.createdOn).getTime() - new Date(a.createdOn).getTime());

          this.announcements = merged;
          this.unreadAnnouncementsCount = this.announcements.filter((n: any) => n.isRead === false || n.isRead === undefined).length;
        }
      });
    } else if (role === 'GymOwner' || role === 'Staff') {
      this.announcementService.getAnnouncements().subscribe({
        next: (res) => {
          const announcementsRes = res as any;
          this.announcements = announcementsRes?.data || (Array.isArray(announcementsRes) ? announcementsRes : []);
          this.unreadAnnouncementsCount = this.announcements.length;
        }
      });
    }
  }

  toggleNotifications(event: Event): void {
    event.stopPropagation();
    this.isNotificationsOpen = !this.isNotificationsOpen;
    if (this.isNotificationsOpen) {
      this.unreadAnnouncementsCount = 0;
      this.isBranchDropdownOpen = false;
    }
  }

  toggleUserGuide(event: Event): void {
    event.stopPropagation();
    this.isNotificationsOpen = false;
    this.isBranchDropdownOpen = false;
    this.tourService.startMainTour();
  }

  toggleSidebar() {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  toggleMobileSidebar() {
    this.isMobileSidebarOpen = !this.isMobileSidebarOpen;
  }

  closeMobileSidebar() {
    this.isMobileSidebarOpen = false;
  }

  toggleGroup(item: NavItem) {
    if (this.isSidebarCollapsed) {
      this.isSidebarCollapsed = false;
    }
    item.expanded = !item.expanded;
  }

  isGroupActive(item: NavItem): boolean {
    if (!item.children || item.children.length === 0) return false;
    const currentUrl = this.router.url.split(/[?#]/)[0].replace(/\/$/, "");
    return item.children.some(child => {
      if (!child.route) return false;
      const cleanRoute = child.route.replace(/\/$/, "");
      return currentUrl === cleanRoute || currentUrl.startsWith(cleanRoute + '/');
    });
  }

  getImageUrl(path: string | undefined): string | null {
    return this.profileService.getFullUrl(path);
  }

  get userInitials(): string {
    const profile = (this.authApiService as any).userProfileSubject?.value;
    if (profile) {
      const initials = `${profile.firstName?.[0] ?? ''}${profile.lastName?.[0] ?? ''}`.toUpperCase();
      if (initials) return initials;
    }

    const decoded = this.authApiService.decodeToken();
    if (!decoded) return 'SA';
    const name = (decoded as any)?.unique_name || (decoded as any)?.name || (decoded as any)?.email || 'Super Admin';
    return name
      .split(' ')
      .map((n: string) => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }

  logout() {
    this.authApiService.logout();
  }

  @HostListener('document:click')
  closeDropdowns() {
    this.isBranchDropdownOpen = false;
    this.isNotificationsOpen = false;
  }
}
