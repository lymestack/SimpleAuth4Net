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
import { AppConfig, LoginModel } from '../../_api';
import { APP_CONFIG } from './config-injection';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  apiUrl: string;
  private refreshTokenInterval: Subscription | undefined;

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private httpClient: HttpClient
  ) {
    this.apiUrl = this.config.environment.api;
  }

  login(loginModel: LoginModel): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
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
    return this.httpClient
      .post<any>(
        `${this.apiUrl}Auth/LoginWithGoogle`,
        JSON.stringify(credentials),
        {
          headers: header,
          withCredentials: true,
        }
      )
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
        console.log('User logged out.');
        localStorage.removeItem('tokenExpiration');
      },
      (error) => console.error('Error revoking token:', error)
    );
  }

  refreshToken(): Observable<any> {
    return this.httpClient
      .get<any>(`${this.apiUrl}Auth/RefreshToken`, { withCredentials: true })
      .pipe(
        map((response: any) => {
          console.log('Token refreshed successfully');
          this.storeTokenExpiration(response.expires); // Update expiration
          this.scheduleTokenRefresh(response.expires);
          return response;
        }),
        catchError((error) => {
          console.error('Token refresh failed', error);
          this.clearRefreshToken();
          return throwError(() => new Error('Unauthorized'));
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
    return this.httpClient.delete(url, {
      headers: header,
      withCredentials: true,
    });
  }

  isLoggedIn(): boolean {
    const expires = this.getStoredTokenExpiration();
    if (!expires) {
      return false;
    }
    const now = new Date().getTime();
    return now < expires;
  }

  private storeTokenExpiration(expiration: string): void {
    const expirationTime = new Date(expiration).getTime();
    localStorage.setItem('tokenExpiration', expirationTime.toString());
  }

  private getStoredTokenExpiration(): number | null {
    const expiration = localStorage.getItem('tokenExpiration');
    return expiration ? parseInt(expiration, 10) : null;
  }

  private scheduleTokenRefresh(expiration: string): void {
    this.clearRefreshToken(); // Clear any existing interval
    const expirationTime = new Date(expiration).getTime();
    const now = new Date().getTime();
    const timeUntilRefresh = expirationTime - now - 60 * 1000; // Refresh 1 minute before expiration

    if (timeUntilRefresh > 0) {
      this.refreshTokenInterval = timer(timeUntilRefresh).subscribe(() => {
        this.refreshToken().subscribe(
          () => console.log('Token refreshed'),
          (error) => console.error('Error refreshing token:', error)
        );
      });
    }
  }

  private clearRefreshToken(): void {
    if (this.refreshTokenInterval) {
      this.refreshTokenInterval.unsubscribe();
    }
  }
}
