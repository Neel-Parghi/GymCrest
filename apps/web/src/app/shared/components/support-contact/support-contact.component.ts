import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { PlatformStatusService } from '../../../core/services/platform-status.service';

/** "Need help?" line with the support email and phone from platform settings. Renders nothing when neither is set. */
@Component({
  selector: 'app-support-contact',
  templateUrl: './support-contact.component.html',
  styleUrl: './support-contact.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SupportContactComponent {
  /** 'inline' for a one-line hint, 'footer' for a stacked list in a footer column. */
  readonly variant = input<'inline' | 'footer'>('inline');
  readonly label = input('Need help?');

  readonly status = toSignal(inject(PlatformStatusService).getStatus());

  telHref(phone: string): string {
    return 'tel:' + phone.replace(/[^\d+]/g, '');
  }
}
