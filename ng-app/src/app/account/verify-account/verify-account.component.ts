import { Component } from '@angular/core';
import { AuthService } from '../../core/_services/auth.service';
import { Router } from '@angular/router';
import { LoggerService } from '../../core/_services/logger.service';

@Component({
  selector: 'app-verify-account',
  templateUrl: './verify-account.component.html',
  styleUrl: './verify-account.component.scss',
})
export class VerifyAccountComponent {
  verificationCode: string = '';
  errors: string[] = [];

  constructor(
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
        this.logger.success('Account successfully verified.');
        this.router.navigateByUrl('/account/login');
      },
      error: (error) => {
        this.errors = ['Invalid verification code'];
      },
    });
  }
}
