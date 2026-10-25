import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FeedbackService } from '../../../core/services/feedback.service';
import { FeedbackItem } from '../../../shared/models/feedback.model';
import { NotificationService } from '../../../core/services/notification.service';

type FeedbackFilter = 'open' | 'resolved' | 'all';

@Component({
  selector: 'app-feedback-list',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './feedback-list.component.html',
  styleUrl: './feedback-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FeedbackListComponent implements OnInit {
  private feedbackService = inject(FeedbackService);
  private notification = inject(NotificationService);

  readonly items = signal<FeedbackItem[]>([]);
  readonly loading = signal(true);
  readonly filter = signal<FeedbackFilter>('open');

  readonly openCount = computed(() => this.items().filter(i => !i.isResolved).length);
  readonly visible = computed(() => {
    const f = this.filter();
    return this.items().filter(i => f === 'all' || (f === 'open' ? !i.isResolved : i.isResolved));
  });

  readonly categoryIcon: Record<string, string> = { Bug: 'fa-bug', Idea: 'fa-lightbulb', Other: 'fa-comment' };

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.feedbackService.getAll().subscribe({
      next: res => {
        this.items.set(res?.data ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notification.error('Could not load feedback.');
      }
    });
  }

  toggleResolved(item: FeedbackItem): void {
    const value = !item.isResolved;
    this.feedbackService.setResolved(item.id, value).subscribe({
      next: () => this.items.update(list => list.map(i => i.id === item.id ? { ...i, isResolved: value } : i)),
      error: () => this.notification.error('Could not update this report.')
    });
  }

  device(userAgent?: string): string {
    if (!userAgent) return '';
    const browser = /Edg\//.test(userAgent) ? 'Edge' : /Chrome\//.test(userAgent) ? 'Chrome'
      : /Firefox\//.test(userAgent) ? 'Firefox' : /Safari\//.test(userAgent) ? 'Safari' : 'Browser';
    const os = /Android/.test(userAgent) ? 'Android' : /iPhone|iPad/.test(userAgent) ? 'iOS'
      : /Windows/.test(userAgent) ? 'Windows' : /Mac OS/.test(userAgent) ? 'macOS' : /Linux/.test(userAgent) ? 'Linux' : '';
    return os ? `${browser} on ${os}` : browser;
  }
}
