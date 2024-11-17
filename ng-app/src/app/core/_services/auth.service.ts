import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, catchError, of, timer } from 'rxjs';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  path = '';
  username = '';
  private refreshTokenInterval: any;

  constructor(private httpClient: HttpClient) {}

  loginWithGoogle(credentials: string): Observable<any> {
    const header = new HttpHeaders().set('Content-type', 'application/json');
    return this.httpClient.post(
      this.path + 'LoginWithGoogle',
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
      .get(this.path + 'RefreshToken', {
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
    const header = new HttpHeaders().set('Content-type', 'application/json');
    return this.httpClient.delete(this.path + 'RevokeToken/' + this.username, {
      headers: header,
      withCredentials: true,
    });
  }

  checkToken(): Observable<boolean> {
    const header = new HttpHeaders().set('Content-type', 'application/json');
    return this.httpClient.get<boolean>(this.path + 'CheckTokenCookie', {
      headers: header,
      withCredentials: true,
    });
  }

  signOutExternal() {
    this.clearRefreshToken();
    console.log('Logged out and token deleted');
  }

  registerUser(username: string) {
    let url = this.path + 'Register';
    let postData = {
      username: username,
      password: '12345',
      confirmPassword: '12345',
    };

    return this.httpClient.post(url, postData);
  }

  getSecureResource(url: string): Observable<any> {
    const header = new HttpHeaders().set('Content-type', 'application/json');
    return this.httpClient.get(url, { headers: header, withCredentials: true });
  }

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
