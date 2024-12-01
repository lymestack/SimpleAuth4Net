import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import {
  map,
  catchError,
  timer,
  Observable,
  throwError,
  Subscription,
} from 'rxjs';
import { AppConfig, LoginModel, LoginWithGoogleModel } from '../../_api';
import { APP_CONFIG } from './config-injection';

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
    private httpClient: HttpClient
  ) {
    this.apiUrl = this.config.environment.api;
    this.deviceId = this.getOrGenerateDeviceId();
  }

  login(loginModel: LoginModel): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    loginModel.deviceId = this.getOrGenerateDeviceId();

    return this.httpClient
      .post<any>(`${this.apiUrl}Auth/Login`, loginModel, {
        headers: header,
        withCredentials: true,
      })
      .pipe(
        map((response) => {
          this.storeTokenExpiration(response.expires); // Store expiration in localStorage or memory
          this.scheduleTokenRefresh(response.expires);
          return response;
        })
      );
  }

  loginWithGoogle(credentials: string): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');

    const loginWithGoogleModel: LoginWithGoogleModel = {
      credentialsFromGoogle: credentials,
      deviceId: this.deviceId,
    };

    return this.httpClient
      .post<any>(`${this.apiUrl}Auth/LoginWithGoogle`, loginWithGoogleModel, {
        headers: header,
        withCredentials: true,
      })
      .pipe(
        map((response) => {
          this.storeTokenExpiration(response.expires); // Store expiration in localStorage or memory
          this.scheduleTokenRefresh(response.expires);
          return response;
        })
      );
  }

  logout(): void {
    this.clearRefreshToken();
    this.destroyCookieValues().subscribe(
      () => {
        this.log('User logged out.');
        localStorage.removeItem('tokenExpiration');
      },
      (error) => this.error('Error revoking token:', error)
    );
  }

  revokeToken(): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    return this.httpClient.delete(`${this.apiUrl}Auth/RevokeToken`, {
      headers: header,
      withCredentials: true,
    });
  }

  destroyCookieValues(): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    return this.httpClient.delete(`${this.apiUrl}Auth/Logout`, {
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
    const accessTokenExpires = this.getStoredTokenExpiration();
    const refreshTokenExpires = this.getStoredRefreshTokenExpiration();

    const now = new Date().getTime();

    // Check access token expiration
    if (accessTokenExpires && now < accessTokenExpires) {
      return true; // Access token is still valid
    }

    // If the access token is expired, check refresh token expiration
    if (refreshTokenExpires && now < refreshTokenExpires) {
      this.log('Access token expired, but refresh token is valid.');
      return true; // Refresh token is still valid
    }

    // If neither token is valid, user is not logged in
    return false;
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

  private getStoredTokenExpiration(): number | null {
    const expiration = localStorage.getItem('tokenExpiration');
    if (expiration) {
      const parsedExpiration = parseInt(expiration, 10);
      return !isNaN(parsedExpiration) ? parsedExpiration : null;
    }
    return null;
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
