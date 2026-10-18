import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LegalLayoutComponent } from '../legal-layout.component';
import { LEGAL } from '../legal.constants';

@Component({
  selector: 'app-privacy-policy',
  standalone: true,
  imports: [LegalLayoutComponent, RouterLink],
  templateUrl: './privacy-policy.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PrivacyPolicyComponent {
  readonly legal = LEGAL;
}
