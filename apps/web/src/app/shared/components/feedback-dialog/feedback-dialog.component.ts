import { ChangeDetectionStrategy, Component, ElementRef, inject, output, signal, viewChild, AfterViewInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { FeedbackService } from '../../../core/services/feedback.service';
import { FeedbackCategory } from '../../models/feedback.model';
import { NotificationService } from '../../../core/services/notification.service';

/** "Send feedback" dialog: bug / idea / other plus a message. The current page is attached automatically. */
@Component({
  selector: 'app-feedback-dialog',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './feedback-dialog.component.html',
  styleUrl: './feedback-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown.escape)': 'close()' }
})
export class FeedbackDialogComponent implements AfterViewInit {
  private feedbackService = inject(FeedbackService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  readonly closed = output<void>();
  private readonly messageBox = viewChild<ElementRef<HTMLTextAreaElement>>('messageBox');

  readonly categories: { value: FeedbackCategory; label: string; icon: string }[] = [
    { value: 'Bug', label: 'Something broke', icon: 'fa-bug' },
    { value: 'Idea', label: 'Idea', icon: 'fa-lightbulb' },
    { value: 'Other', label: 'Other', icon: 'fa-comment' }
  ];
  readonly maxLength = 2000;

  readonly category = signal<FeedbackCategory>('Bug');
  readonly message = signal('');
  readonly sending = signal(false);

  ngAfterViewInit(): void {
    this.messageBox()?.nativeElement.focus();
  }

  send(): void {
    const text = this.message().trim();
    if (text.length < 5 || this.sending()) return;

    this.sending.set(true);
    this.feedbackService.submit(this.category(), text, this.router.url).subscribe({
      next: () => {
        this.notification.success('Thanks! Your feedback was sent.');
        this.closed.emit();
      },
      error: (err) => {
        this.sending.set(false);
        const message = err?.status === 429
          ? 'You have sent a few already. Please try again in a few minutes.'
          : err?.error?.message || 'Could not send your feedback. Please try again.';
        this.notification.error(message);
      }
    });
  }

  close(): void {
    if (!this.sending()) this.closed.emit();
  }
}
