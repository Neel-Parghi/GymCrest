import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { BrandLogoComponent } from '../../../shared/components/brand-logo/brand-logo.component';

/** The landing hero's app previews: gym-owner dashboard (0) or member app (1), plus the switch between them. */
@Component({
  selector: 'app-hero-preview',
  standalone: true,
  imports: [BrandLogoComponent],
  templateUrl: './hero-preview.component.html',
  styleUrl: './hero-preview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeroPreviewComponent {
  readonly active = input(0);
  readonly viewChange = output<number>();
}
