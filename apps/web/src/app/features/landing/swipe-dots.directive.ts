import { AfterViewInit, DestroyRef, Directive, NgZone, inject, input, signal } from '@angular/core';

/**
 * Position dots for a swipeable card row. Put it on the dots element and pass the row:
 *   <div class="swipe-dots" [appSwipeDots]="row" #dots="swipeDots">
 *     @for (i of dots.indexes(); track i) { <span [class.active]="i === dots.active()"></span> }
 *   </div>
 * Shows nothing while the row fits on screen (desktop grids), so no breakpoint logic is needed here.
 */
@Directive({
  selector: '[appSwipeDots]',
  standalone: true,
  exportAs: 'swipeDots'
})
export class SwipeDotsDirective implements AfterViewInit {
  readonly row = input.required<HTMLElement>({ alias: 'appSwipeDots' });

  readonly indexes = signal<number[]>([]);
  readonly active = signal(0);

  private readonly zone = inject(NgZone);
  private readonly destroyRef = inject(DestroyRef);

  ngAfterViewInit(): void {
    const row = this.row();
    const update = () => this.update(row);

    this.zone.runOutsideAngular(() => {
      row.addEventListener('scroll', update, { passive: true });
      const resize = new ResizeObserver(update);
      resize.observe(row);
      // Pricing cards arrive after the plans load
      const mutations = new MutationObserver(update);
      mutations.observe(row, { childList: true });
      this.destroyRef.onDestroy(() => {
        row.removeEventListener('scroll', update);
        resize.disconnect();
        mutations.disconnect();
      });
    });
    update();
  }

  private update(row: HTMLElement): void {
    const cards = Array.from(row.children as HTMLCollectionOf<HTMLElement>).filter(c => c.offsetWidth > 0);
    const count = row.scrollWidth > row.clientWidth + 1 ? cards.length : 0;
    if (count !== this.indexes().length) {
      this.indexes.set(Array.from({ length: count }, (_, i) => i));
    }
    if (!count) return;

    // At the far right the last card can't snap to the start, so treat "scrolled to the end" as the last card
    if (row.scrollLeft + row.clientWidth >= row.scrollWidth - 2) {
      this.active.set(count - 1);
      return;
    }
    const start = row.getBoundingClientRect().left + parseFloat(getComputedStyle(row).paddingLeft);
    let nearest = 0;
    cards.forEach((card, i) => {
      if (Math.abs(card.getBoundingClientRect().left - start) < Math.abs(cards[nearest].getBoundingClientRect().left - start)) {
        nearest = i;
      }
    });
    this.active.set(nearest);
  }
}
