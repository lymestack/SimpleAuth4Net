import { Component } from '@angular/core';
import { AuthService } from '../../core/_services/auth.service';
import { LoggerService } from '../../core/_services/logger.service';
import { Router } from '@angular/router';

@Component({
    selector: 'app-reset-password',
    templateUrl: './reset-password.component.html',
    styleUrls: ['./reset-password.component.scss'],
    standalone: false
})
export class ResetPasswordComponent {
  verificationCode: string = '';
  newPassword: string = '';
  confirmPassword: string = '';
  errors: string[] = [];

  constructor(
    private authService: AuthService,
    private logger: LoggerService,
    private router: Router
  ) {}

  get passwordMismatch(): boolean {
    return (
      !!this.newPassword &&
      !!this.confirmPassword &&
      this.newPassword !== this.confirmPassword
    );
  }

  canSubmit(): boolean {
    return (
      !!this.verificationCode &&
      !!this.newPassword &&
      !!this.confirmPassword &&
      !this.passwordMismatch
    );
  }

  onSubmit(): void {
    this.authService
      .resetPassword(this.verificationCode, this.newPassword)
      .subscribe({
        next: (data) => {
          this.logger.success('Password reset successfully.');
          this.router.navigateByUrl('/account/login');
        },
        error: (error) => {
          // Log the error response for debugging
          console.error('Reset Password Error:', error);
          this.errors = error.error.errors;
        },
      });
  }
}
