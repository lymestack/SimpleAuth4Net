import { Component, Inject } from '@angular/core';
import { AuthService } from '../../core/_services/auth.service';
import { Router } from '@angular/router';
import { LoggerService } from '../../core/_services/logger.service';
import { AppConfig } from '../../_api';
import { APP_CONFIG } from '../../core/_services/config-injection';

@Component({
  selector: 'app-verify-account',
  templateUrl: './verify-account.component.html',
  styleUrl: './verify-account.component.scss',
})
export class VerifyAccountComponent {
  verificationCode: string = '';
  errors: string[] = [];

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private authService: AuthService,
    private logger: LoggerService,
    private router: Router
  ) {}

  canSubmit(): boolean {
    return !!this.verificationCode;
  }

  onSubmit(): void {
    this.authService.verifyAccount(this.verificationCode).subscribe({
      next: (data) => {
        if (location.href.includes('mfa')) {
          setTimeout(
            () => (window.location.href = this.config.environment.url),
            100
          );
        } else {
          this.logger.success('Account successfully verified.');
          this.router.navigateByUrl('/account/login');
        }
      },
      error: (error) => {
        this.errors = ['Invalid verification code'];
      },
    });
  }
}
