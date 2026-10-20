import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { PAYMENTS_TEST_MODE, RAZORPAY_TEST_DETAILS_URL } from '../../../core/constants/constants';

/**
 * Launch-phase notice shown wherever a gym plan can be bought, while checkout runs on a Razorpay test key.
 * Renders nothing once a live key is configured.
 */
@Component({
  selector: 'app-test-payment-notice',
  templateUrl: './test-payment-notice.component.html',
  styleUrl: './test-payment-notice.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TestPaymentNoticeComponent {
  /** 'public' for visitors on the website, 'owner' inside the gym owner portal. */
  readonly audience = input<'public' | 'owner'>('owner');

  readonly show = PAYMENTS_TEST_MODE;
  readonly testDetailsUrl = RAZORPAY_TEST_DETAILS_URL;
}
