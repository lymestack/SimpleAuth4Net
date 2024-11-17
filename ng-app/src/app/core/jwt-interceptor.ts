import { Injectable, Injector } from '@angular/core';
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

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(private inject: Injector, private router: Router) {}

  ctr = 0;

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError((err: HttpErrorResponse) => {
        // if (err instanceof HttpErrorResponse) {
        // }

        this.handleAuthError(err);
        return next.handle(request);
      })
    );
  }

  private handleAuthError(err: HttpErrorResponse) {
    if (err && err.status === 401 && this.ctr != 1) {
      this.ctr++;
      let service = this.inject.get(AuthService);

      service.refreshToken().subscribe({
        next: (x: any) => {
          console.log('Tokens refreshed, try again');
          return of(
            'We refreshed the token no do again what you were trying to do.'
          );
        },
        error: (err: any) => {
          service.revokeToken().subscribe({
            next: (x: any) => {
              this.router.navigateByUrl('/');
              return of(err.message);
            },
          });
        },
      });

      return of('Attempting to refresh tokens.');
    } else {
      this.ctr = 0;
      return throwError(() => new Error('Non auth error'));
    }
  }
}
