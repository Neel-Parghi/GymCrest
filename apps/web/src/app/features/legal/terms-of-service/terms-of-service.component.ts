import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LegalLayoutComponent } from '../legal-layout.component';
import { LEGAL } from '../legal.constants';

@Component({
  selector: 'app-terms-of-service',
  standalone: true,
  imports: [LegalLayoutComponent, RouterLink],
  templateUrl: './terms-of-service.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TermsOfServiceComponent {
  readonly legal = LEGAL;
}
