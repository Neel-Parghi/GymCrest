import { inject, Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { API_CONSTANTS } from '../constants/api-constants';
import { UserProfile } from '../../shared/models/user-profile.model';
import { JwtToken } from '../../shared/models/jwt-token.model';
import { ApiResponse } from '../../shared/models/api-response.model';
import {
  LoginRequestDto,
  RegisterRequestDto,
  VerifyOtpRequestDto,
  ResendOtpRequestDto,
  ForgotPasswordRequestDto,
  ResetPasswordRequestDto,
  TokenResponseDto,
  RegisterResponseDto,
  RefreshTokenRequestDto
} from '../../shared/models/auth.model';

@Injectable({
  providedIn: 'root',
})
export class AuthApiService extends BaseApiService {

  private readonly persistenceKey = 'authPersistence';
  private readonly refreshBufferMs = 60_000;
  private router = inject(Router);
  private userProfileSubject = new BehaviorSubject<UserProfile | null>(this.loadStoredProfile());
  public userProfile$ = this.userProfileSubject.asObservable();
  private refreshTimer: ReturnType<typeof setTimeout> | null = null;

  getUserProfile(): UserProfile | null {
    return this.userProfileSubject.value;
  }

  isDemoUser(): boolean {
    const profile = this.getUserProfile();
    if (!profile) return false;
    return ['demo@elevatyon.com', 'demouser@elevatyon.com', 'demo@gymforge.com', 'demouser@gymforge.com']
      .includes(profile.email?.toLowerCase());
  }

  constructor() {
    super();
    if (this.getToken()) 
      this.scheduleTokenRefresh();
  }

  private loadStoredProfile(): UserProfile | null {
    const stored = localStorage.getItem('userProfile') || sessionStorage.getItem('userProfile');
    if (stored) {
      try {
        return JSON.parse(stored);
      } catch (e) {
        console.error('Error parsing stored user profile', e);
        return null;
      }
    }
    return null;
  }

  login(credentials: LoginRequestDto, rememberMe: boolean = false): Observable<ApiResponse<TokenResponseDto>> {
    return this.post<ApiResponse<TokenResponseDto>>(API_CONSTANTS.AUTH.LOGIN, credentials).pipe(
      tap((res) => this.saveTokens(res, rememberMe))
    );
  }

  register(userData: RegisterRequestDto): Observable<ApiResponse<RegisterResponseDto>> {
    return this.post<ApiResponse<RegisterResponseDto>>(API_CONSTANTS.AUTH.REGISTER, userData);
  }

  verifyOtp(data: VerifyOtpRequestDto): Observable<ApiResponse<TokenResponseDto>> {
    return this.post<ApiResponse<TokenResponseDto>>(API_CONSTANTS.AUTH.VERIFY_OTP, data).pipe(
      tap((res) => this.saveTokens(res, true))
    );
  }

  resendOtp(data: ResendOtpRequestDto): Observable<ApiResponse<null>> {
    return this.post<ApiResponse<null>>(API_CONSTANTS.AUTH.RESEND_OTP, data);
  }

  forgotPassword(data: { email: string }): Observable<ApiResponse<null>> {
    const payload: ForgotPasswordRequestDto = { ...data, clientUri: window.location.origin + '/reset-password' };
    return this.post<ApiResponse<null>>(API_CONSTANTS.AUTH.FORGOT_PASSWORD, payload);
  }

  resetPassword(data: ResetPasswordRequestDto): Observable<ApiResponse<null>> {
    return this.post<ApiResponse<null>>(API_CONSTANTS.AUTH.RESET_PASSWORD, data);
  }

  getMe(): Observable<ApiResponse<UserProfile>> {
    return this.get<ApiResponse<UserProfile>>(API_CONSTANTS.AUTH.ME);
  }

  saveTokens(res: ApiResponse<TokenResponseDto> | { accessToken?: string, refreshToken?: string } | null | undefined, rememberMe: boolean = false) {
    const data = (res as any)?.data || res;
    const storage = rememberMe ? localStorage : sessionStorage;

    if (rememberMe) {
      sessionStorage.removeItem('token');
      sessionStorage.removeItem('refreshToken');
      sessionStorage.removeItem('userProfile');
      sessionStorage.removeItem(this.persistenceKey);
    } else {
      localStorage.removeItem('token');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('userProfile');
      localStorage.removeItem(this.persistenceKey);
    }

    if (data?.accessToken) storage.setItem('token', data.accessToken);
    if (data?.refreshToken) storage.setItem('refreshToken', data.refreshToken);
    if (data?.accessToken || data?.refreshToken) storage.setItem(this.persistenceKey, rememberMe ? 'local' : 'session');

    if (data?.accessToken) 
      this.scheduleTokenRefresh();
  }

  getToken(): string | null {
    return localStorage.getItem('token') || sessionStorage.getItem('token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken') || sessionStorage.getItem('refreshToken');
  }

  refreshToken(): Observable<ApiResponse<TokenResponseDto>> {
    const accessToken = this.getToken();
    const refreshToken = this.getRefreshToken();
    const rememberMe = this.getPersistenceStorage() === localStorage;
    const payload: RefreshTokenRequestDto = { accessToken: accessToken || '', refreshToken: refreshToken || '' };
    return this.post<ApiResponse<TokenResponseDto>>(API_CONSTANTS.AUTH.REFRESH, payload).pipe(
      tap(res => this.saveTokens(res, rememberMe))
    );
  }

  logout() {
    const refreshToken = this.getRefreshToken();
    const accessToken = this.getToken();

    if (refreshToken) {
      this.post(API_CONSTANTS.AUTH.LOGOUT, { accessToken, refreshToken }).subscribe({
        next: () => this.clearSession(),
        error: (err) => {
          this.clearSession();
        }
      });
    } else {
      this.clearSession();
    }
  }

  private clearSession() {
    this.clearRefreshTimer();
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('userProfile');
    localStorage.removeItem(this.persistenceKey);
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('refreshToken');
    sessionStorage.removeItem('userProfile');
    sessionStorage.removeItem(this.persistenceKey);
    this.userProfileSubject.next(null);
    window.location.replace('/login');
  }

  private clearRefreshTimer() {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
    }
  }

  private scheduleTokenRefresh() {
    this.clearRefreshTimer();

    if (!this.getRefreshToken()) 
      return;

    const decoded = this.decodeToken();
    if (!decoded?.exp) 
      return;

    const delay = decoded.exp * 1000 - Date.now() - this.refreshBufferMs;

    if (delay <= 0) {
      this.refreshToken().subscribe({ error: () => {} });
      return;
    }

    this.refreshTimer = setTimeout(() => {
      this.refreshToken().subscribe({ error: () => {} });
    }, delay);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token || token === 'undefined') return false;

    const decoded = this.decodeToken();
    if (!decoded) return false;

    if (!this.isTokenExpired(decoded)) return true;

    return !!this.getRefreshToken();
  }

  getUserRole(): string | null {
    const decoded: JwtToken | null = this.decodeToken();
    if (!decoded) return null;
    return decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || null;
  }

  getUserId(): string | null {
    const decoded: JwtToken | null = this.decodeToken();
    if (!decoded) return null;
    return decoded.userId || decoded.nameid || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || decoded.sub || decoded.id || null;
  }

  getGymId(): string | null {
    const decoded: JwtToken | null = this.decodeToken();
    if (!decoded) return null;
    return decoded.gymId || null;
  }

  getAssignedBranchId(): string | null {
    const decoded: JwtToken | null = this.decodeToken();
    if (!decoded) return null;
    return decoded.branchId || null;
  }

  decodeToken(): JwtToken | null {
    const token = this.getToken();
    if (!token || token === 'undefined' || token.split('.').length !== 3) {
      return null;
    }

    try {
      return jwtDecode(token);
    } catch (e) {
      console.error('Error decoding token', e);
      return null;
    }
  }

  private isTokenExpired(decoded: JwtToken): boolean {
    if (!decoded.exp) return false;
    return Date.now() >= decoded.exp * 1000;
  }

  private getPersistenceStorage(): Storage {
    const localPersistence = localStorage.getItem(this.persistenceKey);
    if (localPersistence === 'local' || (!localPersistence && (localStorage.getItem('token') || localStorage.getItem('refreshToken')))) {
      return localStorage;
    }
    return sessionStorage;
  }

  redirectUserByRole() {
    const role = this.getUserRole();
    switch (role) {
      case 'SuperAdmin':
        this.router.navigate(['/super-admin/dashboard']);
        break;
      case 'GymOwner':
      case 'Staff':
        this.router.navigate(['/gym-owner/dashboard']);
        break;
      case 'Admin':
        this.router.navigate(['/admin/dashboard']);
        break;
      case 'Trainer':
        this.router.navigate(['/trainer/dashboard']);
        break;
      case 'User':
        this.router.navigate(['/user/dashboard']);
        break;
      default:
        this.router.navigate(['/login']);
    }
  }

  setUserProfile(profile: UserProfile | null) {
    const rememberMe = this.getPersistenceStorage() === localStorage;
    const storage = rememberMe ? localStorage : sessionStorage;

    if (profile) {
      storage.setItem('userProfile', JSON.stringify(profile));
    } else {
      localStorage.removeItem('userProfile');
      sessionStorage.removeItem('userProfile');
    }
    this.userProfileSubject.next(profile);
  }
}
