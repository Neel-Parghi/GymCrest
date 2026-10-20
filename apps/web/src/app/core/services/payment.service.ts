import { Injectable } from '@angular/core';
import { BaseApiService } from './base-api.service';
import { Observable, shareReplay, tap } from 'rxjs';
import { ApiResponse } from '../../shared/models/api-response.model';
import { CreatePaymentRequest, GymSubscriptionStatus, InitiatePaymentResponse, PaymentStats, PaymentTransaction, VerifyPaymentRequest } from '../../shared/models/payment.model';
import { API_CONSTANTS } from '../constants/api-constants';

@Injectable({
  providedIn: 'root',
})
export class PaymentService extends BaseApiService {

  private statsCache$?: Observable<ApiResponse<PaymentStats>>;
  private transactionsCache$?: Observable<ApiResponse<PaymentTransaction[]>>;
  private subscriptionStatusCache$?: Observable<ApiResponse<GymSubscriptionStatus>>;
  private subscriptionHistoryCache$?: Observable<ApiResponse<PaymentTransaction[]>>;

  constructor() {
    super();
  }

  clearStatsCache(): void {
    this.statsCache$ = undefined;
  }

  clearTransactionsCache(): void {
    this.transactionsCache$ = undefined;
  }

  clearSubscriptionCache(): void {
    this.subscriptionStatusCache$ = undefined;
    this.subscriptionHistoryCache$ = undefined;
  }

  getStats(forceRefresh = false): Observable<ApiResponse<PaymentStats>> {
    if (!this.statsCache$ || forceRefresh) {
      this.statsCache$ = this.get<ApiResponse<PaymentStats>>(API_CONSTANTS.PAYMENTS.STATS).pipe(
        shareReplay(1)
      );
    }
    return this.statsCache$;
  }

  getTransactions(forceRefresh = false): Observable<ApiResponse<PaymentTransaction[]>> {
    if (!this.transactionsCache$ || forceRefresh) {
      this.transactionsCache$ = this.get<ApiResponse<PaymentTransaction[]>>(API_CONSTANTS.PAYMENTS.TRANSACTIONS).pipe(
        shareReplay(1)
      );
    }
    return this.transactionsCache$;
  }

  initiatePayment(payload: CreatePaymentRequest): Observable<ApiResponse<InitiatePaymentResponse>> {
    return this.post<ApiResponse<InitiatePaymentResponse>>(API_CONSTANTS.PAYMENTS.INITIATE, payload).pipe(
      tap(() => {
        this.clearStatsCache();
        this.clearTransactionsCache();
        this.clearSubscriptionCache();
      })
    );
  }

  verifyPayment(payload: VerifyPaymentRequest): Observable<ApiResponse<{ message: string }>> {
    return this.post<ApiResponse<{ message: string }>>(API_CONSTANTS.PAYMENTS.VERIFY, payload).pipe(
      tap(() => {
        this.clearStatsCache();
        this.clearTransactionsCache();
        this.clearSubscriptionCache();
      })
    );
  }

  getSubscriptionStatus(forceRefresh = false): Observable<ApiResponse<GymSubscriptionStatus>> {
    if (!this.subscriptionStatusCache$ || forceRefresh) {
      this.subscriptionStatusCache$ = this.get<ApiResponse<GymSubscriptionStatus>>(API_CONSTANTS.PAYMENTS.SUBSCRIPTION).pipe(
        shareReplay(1)
      );
    }
    return this.subscriptionStatusCache$;
  }

  getSubscriptionHistory(forceRefresh = false): Observable<ApiResponse<PaymentTransaction[]>> {
    if (!this.subscriptionHistoryCache$ || forceRefresh) {
      this.subscriptionHistoryCache$ = this.get<ApiResponse<PaymentTransaction[]>>(API_CONSTANTS.PAYMENTS.HISTORY).pipe(
        shareReplay(1)
      );
    }
    return this.subscriptionHistoryCache$;
  }

}
