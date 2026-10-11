import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * The Elevatyon wordmark as inline vector paths. Letters use currentColor so the
 * logo follows the surrounding text colour (light, dark mode, over photos);
 * the "y" uses --brand-accent.
 */
@Component({
  selector: 'app-brand-logo',
  standalone: true,
  templateUrl: './brand-logo.component.html',
  styleUrl: './brand-logo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BrandLogoComponent {
  readonly label = input<string>('Elevatyon');
}
