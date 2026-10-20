import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, OnInit, PLATFORM_ID, computed, effect, inject, signal, viewChild } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BrandLogoComponent } from '../../shared/components/brand-logo/brand-logo.component';
import { HeroPreviewComponent } from './hero-preview/hero-preview.component';
import { PricingService } from '../../core/services/pricing.service';
import { PricingPlan } from '../../shared/models/pricing.model';
import { TestPaymentNoticeComponent } from '../../shared/components/test-payment-notice/test-payment-notice.component';
import { PAYMENTS_TEST_MODE } from '../../core/constants/constants';
import { PlatformStatusService } from '../../core/services/platform-status.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { SwipeDotsDirective } from './swipe-dots.directive';

interface HeroMessage {
  lead: string;
  highlight: string;
  subtext: string;
}

interface Faq {
  question: string;
  answer: string;
}

type PlansState = 'loading' | 'ready' | 'error';

interface TourVideo {
  src: string;
  poster: string;
  vertical: boolean;
}

/** Phones get the 9:16 cut, wider screens the 16:9 one. */
const TOUR_VIDEOS: Record<'vertical' | 'landscape', TourVideo> = {
  vertical: { src: 'videos/gymcrest-tour-vertical.mp4', poster: 'images/landing/tour-poster-vertical.webp', vertical: true },
  landscape: { src: 'videos/gymcrest-tour.mp4', poster: 'images/landing/tour-poster.webp', vertical: false }
};

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, BrandLogoComponent, HeroPreviewComponent, TestPaymentNoticeComponent, SwipeDotsDirective],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown.escape)': 'menuOpen.set(false); closeTour()' }
})
export class LandingComponent implements OnInit {
  private readonly pricingService = inject(PricingService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  /** Support email and phone from platform settings, for the footer. */
  readonly platformStatus = toSignal(inject(PlatformStatusService).getStatus());

  readonly year = new Date().getFullYear();
  readonly menuOpen = signal(false);

  readonly heroMessages: HeroMessage[] = [
    {
      lead: 'Run your whole gym',
      highlight: 'in one place',
      subtext: 'Members, plans, billing, attendance, staff and inventory, with automatic renewal reminders that keep members coming back.'
    },
    {
      lead: 'Train smarter.',
      highlight: 'See your progress.',
      subtext: 'Plan workouts and meals, log every session and watch your progress add up. Free for every member, on your own or with your gym.'
    }
  ];
  readonly heroIndex = signal(0);
  private heroTimer?: ReturnType<typeof setInterval>;

  readonly faqs: Faq[] = [
    ...(PAYMENTS_TEST_MODE ? [{
      question: 'Do gym owners have to pay right now?',
      answer: "Not yet. GymCrest is in its launch phase, so it's free for gym owners too. Checkout runs in test mode and no money is charged. We'll let you know well before real billing starts."
    }] : []),
    {
      question: 'Is GymCrest really free for members?',
      answer: 'Yes. Anyone can create a member account and use workouts, diet tracking and progress for free, with or without a gym. Only gyms pay for a plan.'
    },
    {
      question: 'How do my members pay me?',
      answer: 'Connect your own Razorpay account and members can pay for their membership online, straight to you. You can also record cash or other offline payments at the desk.'
    },
    {
      question: 'Will I be charged automatically when my plan ends?',
      answer: 'No. Gym plans are paid upfront through Razorpay with no auto-debit. You choose when to renew, and your dashboard shows how many days are left.'
    },
    {
      question: 'Can I run more than one branch?',
      answer: 'Yes. Add your branches to one gym account and manage members, staff and plans across all of them. Your plan shows how many branches it includes.'
    },
    {
      question: 'My members already use GymCrest. Can I link them?',
      answer: 'Yes. When you add a member with the email they signed up with, their gym membership appears in their existing account automatically.'
    },
    {
      question: 'Is my data safe, and can I delete it?',
      answer: 'Each gym\'s data is kept separate and only your team can see it. You can delete your account from the Account page. The Privacy Policy explains exactly what we store and why.'
    }
  ];

  /** The tour video only loads once the visitor opens it, so it never slows the page down. */
  readonly tour = signal<TourVideo | null>(null);
  private readonly tourClose = viewChild<ElementRef<HTMLButtonElement>>('tourClose');

  readonly plansState = signal<PlansState>('loading');
  readonly plans = signal<PricingPlan[]>([]);
  readonly hasTrial = computed(() => this.plans().some(p => p.isTrial));
  readonly paidPlans = computed(() => this.plans().filter(p => !p.isTrial));

  constructor() {
    effect(() => this.tourClose()?.nativeElement.focus());
  }

  openTour(): void {
    const narrow = this.isBrowser && window.matchMedia('(max-width: 700px)').matches;
    this.menuOpen.set(false);
    this.tour.set(TOUR_VIDEOS[narrow ? 'vertical' : 'landscape']);
  }

  closeTour(): void {
    this.tour.set(null);
  }

  telHref(phone: string): string {
    return 'tel:' + phone.replace(/[^\d+]/g, '');
  }

  ngOnInit(): void {
    this.loadPlans();
    this.startHeroRotation();
    this.destroyRef.onDestroy(() => clearInterval(this.heroTimer));
  }

  loadPlans(): void {
    this.plansState.set('loading');
    this.pricingService.getAllPlans(true).subscribe({
      next: res => {
        const active = (res?.data ?? [])
          .filter(p => p.isActive)
          .sort((a, b) => a.durationInDays - b.durationInDays || a.price - b.price);
        this.plans.set(active);
        this.plansState.set('ready');
      },
      error: () => this.plansState.set('error')
    });
  }

  periodLabel(plan: PricingPlan): string {
    const days = plan.durationInDays;
    if (days >= 360 && days <= 366) return '/year';
    if (days >= 28 && days <= 31) return '/month';
    if (days % 30 === 0) return `/${days / 30} months`;
    return `/${days} days`;
  }

  billingNote(plan: PricingPlan): string {
    const period = this.periodLabel(plan);
    if (period === '/month') return 'Billed monthly';
    const billed = period === '/year' ? 'billed yearly' : 'billed upfront';
    if (plan.price <= 0) return billed.charAt(0).toUpperCase() + billed.slice(1);
    const perMonth = Math.round(plan.price / (plan.durationInDays / 30.4));
    return `≈ ₹${this.formatInr(perMonth)}/month, ${billed}`;
  }

  limitsLabel(plan: PricingPlan): string {
    const parts: string[] = [];
    if (plan.maxMembers) parts.push(`Up to ${this.formatInr(plan.maxMembers)} members`);
    if (plan.maxBranches) parts.push(`${plan.maxBranches} ${plan.maxBranches === 1 ? 'branch' : 'branches'}`);
    return parts.join(' · ');
  }

  formatInr(value: number): string {
    return value.toLocaleString('en-IN');
  }

  scrollTo(id: string, event?: Event): void {
    event?.preventDefault();
    this.menuOpen.set(false);
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  toggleMenu(): void {
    this.menuOpen.update(open => !open);
  }

  selectHero(index: number): void {
    this.heroIndex.set(index);
    if (this.heroTimer) {
      clearInterval(this.heroTimer);
      this.startHeroRotation();
    }
  }

  private startHeroRotation(): void {
    if (!this.isBrowser || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    const DISPLAY_MS = 6000;
    this.heroTimer = setInterval(() => {
      this.heroIndex.update(i => (i + 1) % this.heroMessages.length);
    }, DISPLAY_MS);
  }
}
