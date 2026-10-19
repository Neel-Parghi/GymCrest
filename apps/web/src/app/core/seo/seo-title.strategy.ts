import { Injectable, inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { Meta, Title } from '@angular/platform-browser';
import { ActivatedRouteSnapshot, RouterStateSnapshot, TitleStrategy } from '@angular/router';

export interface SeoData {
  description: string;
  index?: boolean;
}

/** Public origin used for canonical URLs; swap when the custom domain goes live. */
export const SITE_URL = 'https://gymcrest.vercel.app';

export const DEFAULT_DESCRIPTION =
  'GymCrest is gym management software for Indian gyms (members, billing, attendance, staff) with a free workout, diet and progress tracker for members.';

@Injectable({ providedIn: 'root' })
export class SeoTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);

  override updateTitle(state: RouterStateSnapshot): void {
    const pageTitle = this.buildTitle(state);
    this.title.setTitle(pageTitle ? `${pageTitle} · GymCrest` : 'GymCrest');

    const seo = this.deepestSeo(state.root);
    this.meta.updateTag({ name: 'description', content: seo?.description ?? DEFAULT_DESCRIPTION });
    this.meta.updateTag({ name: 'robots', content: seo?.index ? 'index, follow' : 'noindex, nofollow' });
    this.setCanonical(seo?.index ? SITE_URL + state.url.split(/[?#]/)[0] : null);
  }

  /** Points search engines at one URL per public page; removed on noindex pages. */
  private setCanonical(href: string | null): void {
    let link = this.document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!href) {
      link?.remove();
      return;
    }
    if (!link) {
      link = this.document.createElement('link');
      link.rel = 'canonical';
      this.document.head.appendChild(link);
    }
    link.href = href === SITE_URL + '/' ? href : href.replace(/\/$/, '');
  }

  private deepestSeo(route: ActivatedRouteSnapshot): SeoData | undefined {
    let seo: SeoData | undefined;
    for (let r: ActivatedRouteSnapshot | null = route; r; r = r.firstChild) {
      seo = (r.data['seo'] as SeoData | undefined) ?? seo;
    }
    return seo;
  }
}
