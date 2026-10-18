import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { ActivatedRouteSnapshot, RouterStateSnapshot, TitleStrategy } from '@angular/router';

export interface SeoData {
  description: string;
  index?: boolean;
}

export const DEFAULT_DESCRIPTION =
  'GymCrest is gym management software for Indian gyms (members, billing, attendance, staff) with a free workout, diet and progress tracker for members.';

@Injectable({ providedIn: 'root' })
export class SeoTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  override updateTitle(state: RouterStateSnapshot): void {
    const pageTitle = this.buildTitle(state);
    this.title.setTitle(pageTitle ? `${pageTitle} · GymCrest` : 'GymCrest');

    const seo = this.deepestSeo(state.root);
    this.meta.updateTag({ name: 'description', content: seo?.description ?? DEFAULT_DESCRIPTION });
    this.meta.updateTag({ name: 'robots', content: seo?.index ? 'index, follow' : 'noindex, nofollow' });
  }

  private deepestSeo(route: ActivatedRouteSnapshot): SeoData | undefined {
    let seo: SeoData | undefined;
    for (let r: ActivatedRouteSnapshot | null = route; r; r = r.firstChild) {
      seo = (r.data['seo'] as SeoData | undefined) ?? seo;
    }
    return seo;
  }
}
