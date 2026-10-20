import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthApiService } from '../services/auth-api.service';
import { catchError, switchMap, throwError, BehaviorSubject, Subject, filter, take, race, Observable } from 'rxjs';

/** Pages that stay usable during maintenance; they handle a failed request on their own. */
const PUBLIC_PAGES = ['/', '/privacy-policy', '/terms-of-service', '/maintenance'];

let isRefreshing = false;
const refreshTokenSubject: BehaviorSubject<string | null> = new BehaviorSubject<string | null>(null);
const refreshFailedSubject = new Subject<unknown>();

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthApiService);
  const router = inject(Router);
  const token = authService.getToken();

  let authReq = req;
  if (token) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error) => {
      const isAuthRequest = req.url.includes('/auth/login') || req.url.includes('/auth/refresh') || req.url.includes('/auth/logout');
      
      if (error instanceof HttpErrorResponse && error.status === 401 && !isAuthRequest) {
        return handle401Error(authService, authReq, next);
      }
      
      // Gym's SaaS plan expired (owners and staff; members are exempt server-side)
      if (error instanceof HttpErrorResponse && error.status === 402 && !router.url.includes('/subscription-expired')) {
        router.navigate(['/subscription-expired']);
      }

      // Platform in maintenance mode: the public website keeps working, the app shows the maintenance page
      if (error instanceof HttpErrorResponse && error.status === 503 && error.error?.code === 'MAINTENANCE'
        && !PUBLIC_PAGES.includes(router.url.split(/[?#]/)[0])) {
        router.navigate(['/maintenance']);
      }

      return throwError(() => error);
    })
  );
};

function isSessionRejected(err: unknown, authService: AuthApiService): boolean {
  if (!authService.getRefreshToken()) return true;
  return err instanceof HttpErrorResponse && err.status === 401;
}

function handle401Error(authService: AuthApiService, request: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    return authService.refreshToken().pipe(
      switchMap((res: any) => {
        const accessToken = res?.data?.accessToken || res?.accessToken;
        isRefreshing = false;
        refreshTokenSubject.next(accessToken);
        
        return next(request.clone({
          setHeaders: {
            Authorization: `Bearer ${accessToken}`
          }
        }));
      }),
      catchError((err) => {
        isRefreshing = false;
        refreshFailedSubject.next(err);
        if (isSessionRejected(err, authService)) {
          authService.logout();
        }
        return throwError(() => err);
      })
    );
  } else {
    return race(
      refreshTokenSubject.pipe(
        filter(token => token != null),
        take(1),
        switchMap(jwt => {
          return next(request.clone({
            setHeaders: {
              Authorization: `Bearer ${jwt}`
            }
          }));
        })
      ),
      refreshFailedSubject.pipe(
        take(1),
        switchMap(err => throwError(() => err))
      )
    );
  }
}
