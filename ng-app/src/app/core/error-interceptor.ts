import { Inject, Injectable, Injector } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from './_services/auth.service';
import { AppConfig } from '../_api';
import { APP_CONFIG } from './_services/config-injection';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private inject: Injector,
    private router: Router
  ) {}

  ctr = 0;

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError((err: HttpErrorResponse) => {
        this.handleAuthError(err);
        return next.handle(request);
      })
    );
  }

  private handleAuthError(err: HttpErrorResponse) {
    if (err && err.status === 401 && this.ctr != 1) {
      let service = this.inject.get(AuthService);
      service.logout().subscribe(() => {
        this.router.navigateByUrl('/account/login');
      });

      return of('Signing out and redirecting to login page...');
    } else {
      this.ctr = 0;
      return throwError(() => new Error('Non auth error'));
    }
  }
}
