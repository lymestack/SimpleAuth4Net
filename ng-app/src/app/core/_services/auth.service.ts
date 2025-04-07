import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import {
  map,
  catchError,
  timer,
  Observable,
  throwError,
  Subscription,
  EMPTY,
  switchMap,
} from 'rxjs';
import {
  AppConfig,
  LoginModel,
  LoginWithSsoModel,
  MfaMethod,
  SsoProvider,
  VerifyTotpModel,
} from '../../_api';
import { APP_CONFIG } from './config-injection';
import { SelectMfaMethodDialogComponent } from '../../account/select-mfa-method-dialog/select-mfa-method-dialog.component';
import { MatDialog } from '@angular/material/dialog';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  apiUrl: string;
  private refreshTokenInterval: Subscription | undefined;
  private deviceId: string;
  private debug = false;

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private dialog: MatDialog,
    private httpClient: HttpClient
  ) {
    this.apiUrl = this.config.environment.api;
    this.deviceId = this.getOrGenerateDeviceId();
  }

  login(loginModel: LoginModel): Observable<any> {
    localStorage.setItem('verifyUsername', loginModel.username.trim());
    if (this.config.enableMfaViaEmail || this.config.enableMfaViaSms) {
      let mfaPreference = this.getStoredMfaPreference();
      if (!!mfaPreference) {
        loginModel.mfaMethod = mfaPreference;
        return this.performLogin(loginModel);
      }

      return this.showMfaChoiceDialog().pipe(
        switchMap((result) => {
          if (result) {
            loginModel.mfaMethod = result.method;
            if (result.remember) {
              this.storeMfaPreference(result.method);
            }
            return this.performLogin(loginModel);
          } else {
            return EMPTY;
          }
        })
      );
    } else {
      return this.performLogin(loginModel);
    }
  }

  private showMfaChoiceDialog(): Observable<
    { method: MfaMethod; remember: boolean } | undefined
  > {
    const dialogRef = this.dialog.open(SelectMfaMethodDialogComponent);
    return dialogRef.afterClosed();
  }

  private performLogin(loginModel: LoginModel): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    loginModel.deviceId = this.getOrGenerateDeviceId();

    return this.httpClient
      .post<any>(`${this.apiUrl}Auth/Login`, loginModel, {
        headers: header,
        withCredentials: true,
      })
      .pipe(
        map((response: any) => {
          if (!this.config.enableMfaViaEmail) {
            this.storeTokenExpiration(response.expires);
            this.scheduleTokenRefresh(response.expires);
          }
          return response;
        }),
        catchError((error) => {
          this.handleLoginError(error);
          return throwError(() => error);
        })
      );
  }

  private storeMfaPreference(method: MfaMethod): void {
    localStorage.setItem('preferredMfaMethod', method.toString());
  }

  private getStoredMfaPreference(): MfaMethod | null {
    const stored = localStorage.getItem('preferredMfaMethod');
    return stored ? (parseInt(stored) as MfaMethod) : null;
  }

  loginWithGoogle(credentials: string): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');

    const loginWithSsoModel: LoginWithSsoModel = {
      ssoProvider: SsoProvider.Google,
      credentialsFromProvider: credentials,
      deviceId: this.deviceId,
    };

    return this.httpClient
      .post<any>(`${this.apiUrl}Auth/LoginWithGoogle`, loginWithSsoModel, {
        headers: header,
        withCredentials: true,
      })
      .pipe(
        map((response) => {
          this.storeTokenExpiration(response.expires);
          this.scheduleTokenRefresh(response.expires);
          setTimeout(() => window.location.reload(), 100);
          return response;
        })
      );
  }

  // FUTURE: Consolidate loginWithFacebook and loginWithGoogle to loginWithSso after seeing if other providers match the same pattern of implementation.
  loginWithFacebook(credentials: string): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');

    const loginWithSsoModel: LoginWithSsoModel = {
      ssoProvider: SsoProvider.Facebook,
      credentialsFromProvider: credentials,
      deviceId: this.deviceId,
    };

    return this.httpClient
      .post<any>(`${this.apiUrl}Auth/LoginWithFacebook`, loginWithSsoModel, {
        headers: header,
        withCredentials: true,
      })
      .pipe(
        map((response) => {
          this.storeTokenExpiration(response.expires);
          this.scheduleTokenRefresh(response.expires);
          setTimeout(() => window.location.reload(), 100);
          return response;
        })
      );
  }

  loginWithMicrosoft(credentials: string): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');

    const loginWithSsoModel: LoginWithSsoModel = {
      ssoProvider: SsoProvider.Microsoft,
      credentialsFromProvider: credentials,
      deviceId: this.deviceId,
    };

    return this.httpClient
      .post<any>(`${this.apiUrl}Auth/LoginWithMicrosoft`, loginWithSsoModel, {
        headers: header,
        withCredentials: true,
      })
      .pipe(
        map((response) => {
          this.storeTokenExpiration(response.expires);
          this.scheduleTokenRefresh(response.expires);
          setTimeout(() => window.location.reload(), 100);
          return response;
        })
      );
  }

  logout(): Observable<any> {
    this.clearRefreshToken();
	localStorage.removeItem('tokenExpiration');
	localStorage.removeItem('AppUser');
	localStorage.removeItem('refreshTokenExpiration');

    return this.destroyCookieValues().pipe(
      map((response) => {
        this.log('User logged out.');
      }),
      catchError((error) => {
        return throwError(() => error);
      })
    );
  }

  userVerified(username: string): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    return this.httpClient.get<any>(
      `${this.apiUrl}Auth/UserVerified?username=${encodeURIComponent(
        username
      )}`,
      {
        headers: header,
        withCredentials: true,
      }
    );
  }

  forgotPassword(email: string): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    localStorage.setItem('verifyUsername', email);

    return this.httpClient.post<any>(
      `${this.apiUrl}Auth/ForgotPassword`,
      { email },
      {
        headers: header,
        withCredentials: true,
      }
    );
  }

  resetPassword(verifyToken: string, newPassword: string): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    let username = localStorage.getItem('verifyUsername');
    return this.httpClient.post<any>(
      `${this.apiUrl}Auth/ResetPassword`,
      { username, verifyToken, newPassword },
      {
        headers: header,
        withCredentials: true,
      }
    );
  }

  verifyAccount(verifyToken: string): Observable<any> {
    let mfaLogin = window.location.href.includes('mfa');
    let verifyEndpoint = 'Verify';
    if (mfaLogin) verifyEndpoint += mfaLogin ? 'Mfa' : 'Account';
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    let username = localStorage.getItem('verifyUsername');
    return this.httpClient
      .post<any>(
        `${this.apiUrl}Auth/${verifyEndpoint}`,
        { username, verifyToken, deviceId: this.deviceId },
        {
          headers: header,
          withCredentials: true,
        }
      )
      .pipe(
        map((response) => {
          if (!mfaLogin) return response;
          this.storeTokenExpiration(response.expires);
          this.scheduleTokenRefresh(response.expires);
          return response;
        })
      );
  }

  revokeToken(): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    return this.httpClient.delete(`${this.apiUrl}Auth/RevokeToken`, {
      headers: header,
      withCredentials: true,
    });
  }

  checkPasswordComplexity(password: string) {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    const url = `${
      this.apiUrl
    }Auth/CheckPasswordComplexity?password=${encodeURIComponent(password)}`;
    return this.httpClient.get(url, {
      headers: header,
      withCredentials: true,
    });
  }

  isLoggedIn(): boolean {
	const expiration = localStorage.getItem('tokenExpiration');
    if (!expiration) return false;
	
    const accessTokenExpires = this.getStoredTokenExpiration();
    const refreshTokenExpires = this.getStoredRefreshTokenExpiration();
    const now = new Date().getTime();

    if (accessTokenExpires && now < accessTokenExpires) return true;

    if (refreshTokenExpires && now < refreshTokenExpires) {
      this.log('Access token expired, but refresh token is valid.');
      return true;
    }

    return false;
  }

  refreshToken(): Observable<any> {
    return this.httpClient
      .get<any>(`${this.apiUrl}Auth/RefreshToken?deviceId=${this.deviceId}`, {
        withCredentials: true,
      })
      .pipe(
        map((response: any) => {
          this.log('Token refreshed successfully');
          this.storeTokenExpiration(
            response.expires,
            response.refreshTokenExpires
          ); // Update access and refresh token expirations
          this.scheduleTokenRefresh(response.expires);
          return response;
        }),
        catchError((error) => {
          this.error('Token refresh failed', error);
          this.clearRefreshToken();
          return throwError(() => new Error('Unauthorized'));
        })
      );
  }

  setupAuthenticator(username: string): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Auth/SetupAuthenticator?username=${encodeURIComponent(
        username
      )}`,
      null,
      { withCredentials: true }
    );
  }

  verifyAuthenticator(
    username: string,
    verificationCode: string
  ): Observable<any> {
    const verifyModel: VerifyTotpModel = {
      username: username,
      code: verificationCode,
      deviceId: this.deviceId,
    };

    return this.httpClient.post<any>(
      `${this.apiUrl}Auth/VerifyAuthenticatorCode`,
      verifyModel,
      { withCredentials: true }
    );
  }

  handleJwtResponse(response: any): void {
    if (response?.token) {
      this.storeTokenExpiration(response.expires, response.refreshTokenExpires);
      this.scheduleTokenRefresh(response.expires);
    }
  }

  getUserProfile(): Observable<any> {
    return this.httpClient.get<any>(`${this.apiUrl}AppUser/Me`, {
      withCredentials: true,
    });
  }

  updateUserProfile(user: any): Observable<any> {
    return this.httpClient.post<any>(`${this.apiUrl}Auth/AppUser`, user, {
      withCredentials: true,
    });
  }

  sendNewVerificationCode(
    username: string,
    mfaMethod: MfaMethod
  ): Observable<any> {
    return this.httpClient.post<any>(
      `${this.apiUrl}Auth/SendNewCode`,
      { username, mfaMethod },
      { withCredentials: true }
    );
  }

  private handleLoginError(error: any): void {
    if (
      error?.status === 401 &&
      (error.error.includes('locked') ||
        error.error?.message === 'The account is locked.')
    ) {
      alert(
        'Your account has been locked due to multiple failed login attempts. Please wait a few minutes to try again or contact support.'
      );
    } else {
      console.error('Login error:', error);
    }
  }

  private getStoredRefreshTokenExpiration(): number | null {
    const expiration = localStorage.getItem('refreshTokenExpiration');
    if (expiration) {
      const parsedExpiration = parseInt(expiration, 10);
      return !isNaN(parsedExpiration) ? parsedExpiration : null;
    }
    return null;
  }

  private storeTokenExpiration(
    accessExpiration: string,
    refreshExpiration?: string
  ): void {
    try {
      // Parse and store access token expiration
      const accessExpirationTime = Date.parse(accessExpiration);
      if (!isNaN(accessExpirationTime)) {
        localStorage.setItem(
          'tokenExpiration',
          accessExpirationTime.toString()
        );
      } else {
        this.error('Invalid access token expiration format:', accessExpiration);
      }

      // Parse and store refresh token expiration (optional)
      if (refreshExpiration) {
        const refreshExpirationTime = Date.parse(refreshExpiration);
        if (!isNaN(refreshExpirationTime)) {
          localStorage.setItem(
            'refreshTokenExpiration',
            refreshExpirationTime.toString()
          );
        } else {
          this.error(
            'Invalid refresh token expiration format:',
            refreshExpiration
          );
        }
      }
    } catch (error) {
      this.error('Error storing token expiration:', error);
    }
  }

  private getStoredTokenExpiration(): number | null {
    const expiration = localStorage.getItem('tokenExpiration');
    if (expiration) {
      const parsedExpiration = parseInt(expiration, 10);
      return !isNaN(parsedExpiration) ? parsedExpiration : null;
    }
    return null;
  }

  private destroyCookieValues(): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    return this.httpClient.delete(`${this.apiUrl}Auth/Logout`, {
      headers: header,
      withCredentials: true,
    });
  }

  private scheduleTokenRefresh(expiration: string): void {
    this.clearRefreshToken(); // Clear any existing interval

    const expirationTime = new Date(expiration).getTime();
    const now = new Date().getTime();
    const bufferTime = 30 * 1000; // 30 seconds buffer to account for clock skew
    const timeUntilRefresh = expirationTime - now - 60 * 1000 - bufferTime; // Refresh 1 minute before expiration

    this.log('Scheduling token refresh in:', timeUntilRefresh, 'ms');

    if (timeUntilRefresh > 0) {
      this.refreshTokenInterval = timer(timeUntilRefresh).subscribe(() => {
        this.refreshToken().subscribe(
          () => this.log('Token refreshed'),
          (error) => this.error('Error refreshing token:', error)
        );
      });
    } else {
      this.error('Invalid timeUntilRefresh:', timeUntilRefresh);
    }
  }

  private log(...args: any[]): void {
    if (this.debug) {
      this.log(...args);
    }
  }

  private error(...args: any[]): void {
    if (this.debug) {
      this.error(...args);
    }
  }

  private clearRefreshToken(): void {
    if (this.refreshTokenInterval) {
      this.refreshTokenInterval.unsubscribe();
    }
  }

  private getOrGenerateDeviceId(): string {
    let deviceId = localStorage.getItem('deviceId');
    if (!deviceId) {
      // Generate a new UUID and save it
      deviceId = this.generateUUID();
      localStorage.setItem('deviceId', deviceId);
    }
    return deviceId;
  }

  private generateUUID(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(
      /[xy]/g,
      function (c) {
        const r = (Math.random() * 16) | 0,
          v = c === 'x' ? r : (r & 0x3) | 0x8;
        return v.toString(16);
      }
    );
  }
}
