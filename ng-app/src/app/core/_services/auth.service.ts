import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { map, catchError, of, timer } from 'rxjs';
import { Observable } from 'rxjs';
import { AppConfig, LoginModel } from '../../_api';
import { APP_CONFIG } from './config-injection';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  apiUrl: string;
  private refreshTokenInterval: any;

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,

    private httpClient: HttpClient
  ) {
    this.apiUrl = this.config.environment.api;
  }

  loginWithGoogle(credentials: string): Observable<any> {
    const header = new HttpHeaders().set('Content-type', 'application/json');
    return this.httpClient.post(
      this.apiUrl + 'LoginWithGoogle',
      JSON.stringify(credentials),
      {
        headers: header,
        withCredentials: true,
      }
    );
  }

  refreshToken(): Observable<any> {
    const header = new HttpHeaders().set('Content-type', 'application/json');
    return this.httpClient
      .get(this.apiUrl + 'RefreshToken', {
        headers: header,
        withCredentials: true,
      })
      .pipe(
        catchError((error) => {
          this.clearRefreshToken();
          return of(null);
        })
      );
  }

  revokeToken(): Observable<any> {
    // FUTURE: Fix this:
    let username = '';
    const header = new HttpHeaders().set('Content-type', 'application/json');
    return this.httpClient.delete(this.apiUrl + 'RevokeToken/' + username, {
      headers: header,
      withCredentials: true,
    });
  }

  signOutExternal() {
    this.clearRefreshToken();
    console.log('Logged out and token deleted');
  }

  // registerUser(username: string) {
  //   let url = this.apiUrl + 'Register';
  //   let postData = {
  //     username: username,
  //     password: '12345',
  //     confirmPassword: '12345',
  //   };

  //   return this.httpClient.post(url, postData);
  // }

  login(model: LoginModel) {
    throw new Error('Method not implemented.');
  }

  // ZOMBIE: Debug code:
  // checkToken(): Observable<boolean> {
  //   const header = new HttpHeaders().set('Content-type', 'application/json');
  //   return this.httpClient.get<boolean>(this.apiUrl + 'CheckTokenCookie', {
  //     headers: header,
  //     withCredentials: true,
  //   });
  // }

  // getSecureResource(url: string): Observable<any> {
  //   const header = new HttpHeaders().set('Content-type', 'application/json');
  //   return this.httpClient.get(url, { headers: header, withCredentials: true });
  // }

  private clearRefreshToken() {
    if (this.refreshTokenInterval) {
      this.refreshTokenInterval.unsubscribe();
    }
  }

  isLoggedIn(): boolean {
    const token = this.getCookie('X-Access-Token');
    return !!token;
  }

  private getCookie(name: string): string | null {
    const matches = document.cookie.match(
      new RegExp(
        '(?:^|; )' + name.replace(/([.$?*|{}()[\]\\/+^])/g, '\\$1') + '=([^;]*)'
      )
    );
    return matches ? decodeURIComponent(matches[1]) : null;
  }
}
