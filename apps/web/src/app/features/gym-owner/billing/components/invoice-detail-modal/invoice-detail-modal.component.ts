import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../../../../core/services/notification.service';
import { CONSTANTS } from '../../../../../core/constants/constants';

@Component({
  selector: 'app-invoice-detail-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './invoice-detail-modal.component.html',
  styleUrl: './invoice-detail-modal.component.scss'
})
export class InvoiceDetailModalComponent {
  private notification = inject(NotificationService);

  @Input() invoice!: any;
  @Input() gymDetails: any = null;
  @Input() platformConfig: any = null;

  @Output() close = new EventEmitter<void>();

  isSaaSInvoice(invoice: any): boolean {
    return !!invoice && (!!invoice.planName || invoice.id?.toString().includes('SaaS') || invoice.id?.toString().startsWith('pay_'));
  }

  getTaxableAmount(amount: number, taxRate: number = 18): number {
    return amount / (1 + (taxRate / 100));
  }

  getTaxAmount(amount: number, taxRate: number = 18): number {
    return amount - this.getTaxableAmount(amount, taxRate);
  }

  getFormattedInvoiceId(invoice: any): string {
    return invoice?.id ?? '';
  }

  downloadInvoicePdf(invoice: any, event: Event): void {
    event.stopPropagation();

    setTimeout(() => {
      const printableElement = document.getElementById('printable-receipt');
      if (!printableElement) {
        this.notification.error(CONSTANTS.BILLING_MODULE.RECEIPT_TEMPLATE_ERROR);
        return;
      }

      const printFrame = document.createElement('iframe');
      printFrame.setAttribute('style', 'position: absolute; width: 1024px; height: 768px; top: -9999px; left: -9999px; visibility: hidden;');

      printFrame.onload = () => {
        printFrame.contentWindow?.postMessage({
          type: 'PRINT_INVOICE',
          invoiceId: invoice.id || '',
          html: printableElement.innerHTML
        }, window.location.origin);
      };

      printFrame.src = '/assets/templates/invoice-print.html';
      document.body.appendChild(printFrame);

      const handlePrintComplete = (msgEvent: MessageEvent) => {
        if (msgEvent.origin === window.location.origin && msgEvent.data?.type === 'PRINT_COMPLETE') {
          if (printFrame.parentNode) {
            printFrame.parentNode.removeChild(printFrame);
          }
          window.removeEventListener('message', handlePrintComplete);
          this.notification.success(CONSTANTS.BILLING_MODULE.RECEIPT_PDF_SUCCESS.replace('{name}', invoice.id || invoice.memberName || 'GymCrest License'));
        }
      };

      window.addEventListener('message', handlePrintComplete);
    }, 50);
  }
}
