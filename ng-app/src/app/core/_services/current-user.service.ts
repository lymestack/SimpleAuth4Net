import { Inject, Injectable } from '@angular/core';
import { map, Observable, of } from 'rxjs';
import { RestService } from './rest.service';
import { AppConfig, AppUser } from '../../_api';
import { APP_CONFIG } from './config-injection';
import { AuthService } from './auth.service';
import { LocalStorageService } from './local-storage.service';

@Injectable()
export class CurrentUserService {
  localStorageKey = 'AppUser';

  constructor(
    private auth: AuthService,
    @Inject(APP_CONFIG) public config: AppConfig,
    private localStorage: LocalStorageService,
    private rest: RestService
  ) {}

  getAppUser(refresh: boolean = false): Observable<any> {
    // if (!this.isLoggedIn()) return of(null);
    let appUser: AppUser = this.localStorage.get(this.localStorageKey);
    if (!appUser || appUser.sessionId != this.config.sessionId) refresh = true;

    if (refresh) {
      return this.rest.getResource('AppUser/Me').pipe(
        map((data: AppUser) => {
          if (!data) return null;
          data.sessionId = this.config.sessionId;
          this.localStorage.save(this.localStorageKey, data);
          return data;
        })
      );
    } else {
      return of<AppUser>(appUser);
    }
  }

  // ZOMBIE: Doesn't work w/ the HTTP Only cookie:
  // isLoggedIn(): boolean {
  //   const token = this.getCookie('X-Access-Token');
  //   return !!token;
  // }

  private getCookie(name: string): string | null {
    const matches = document.cookie.match(
      new RegExp(
        '(?:^|; )' + name.replace(/([.$?*|{}()[\]\\/+^])/g, '\\$1') + '=([^;]*)'
      )
    );
    return matches ? decodeURIComponent(matches[1]) : null;
  }

  logout() {
    this.auth.logout();
  }

  isInRole(roleName: string): boolean {
    let appUser: AppUser = this.localStorage.get(this.localStorageKey);
    if (!appUser) return false;
    return appUser.roles.includes(roleName);
  }
}
