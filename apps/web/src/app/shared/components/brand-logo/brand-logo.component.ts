import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * The GymCrest wordmark (Sora ExtraBold, outlined) as inline vector paths. Letters
 * use currentColor so the logo follows the surrounding text colour (light, dark
 * mode, over photos).
 */
@Component({
  selector: 'app-brand-logo',
  standalone: true,
  templateUrl: './brand-logo.component.html',
  styleUrl: './brand-logo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BrandLogoComponent {
  readonly label = input<string>('GymCrest');
}
