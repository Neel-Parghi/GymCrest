export interface LoginRequestDto {
  email: string;
  password?: string;
}

export interface RegisterRequestDto {
  firstName: string;
  lastName?: string;
  email: string;
  phone?: string;
  password?: string;
  role?: number | string;
}

export interface RegisterResponseDto {
  message: string;
  requiresOtp: boolean;
  email: string;
}

export interface VerifyOtpRequestDto {
  email: string;
  otpCode: string;
}

export interface ResendOtpRequestDto {
  email: string;
}

export interface ForgotPasswordRequestDto {
  email: string;
}

export interface ResetPasswordRequestDto {
  email: string;
  token: string;
  newPassword?: string;
  confirmPassword?: string;
}

export interface TokenResponseDto {
  accessToken: string;
  refreshToken: string;
}

export interface RefreshTokenRequestDto {
  accessToken: string;
  refreshToken: string;
}

export interface ChangePasswordRequestDto {
  currentPassword?: string;
  newPassword?: string;
}

export interface SetPasswordRequestDto {
  token: string;
  password?: string;
  confirmPassword?: string;
}
