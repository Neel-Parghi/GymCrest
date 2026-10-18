import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BrandLogoComponent } from '../../shared/components/brand-logo/brand-logo.component';

/** Shared public page shell for the legal documents (privacy policy, terms of service). */
@Component({
  selector: 'app-legal-layout',
  standalone: true,
  imports: [RouterLink, BrandLogoComponent],
  templateUrl: './legal-layout.component.html',
  styleUrl: './legal-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LegalLayoutComponent {
  readonly title = input.required<string>();
  readonly lastUpdated = input.required<string>();
}
