import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../core/_services/auth.service';
import { LoggerService } from '../../core/_services/logger.service';
import { PasswordService } from '../../core/_services/password.service';
import { Router } from '@angular/router';

@Component({
    selector: 'app-reset-password',
    templateUrl: './reset-password.component.html',
    styleUrls: ['./reset-password.component.scss'],
    standalone: false
})
export class ResetPasswordComponent implements OnInit {
  verificationCode: string = '';
  newPassword: string = '';
  confirmPassword: string = '';
  errors: string[] = [];
  passwordErrors: string[] = [];
  passwordValid: boolean = false;
  passwordHint: string = '';
  isSubmitting: boolean = false;

  constructor(
    private authService: AuthService,
    private logger: LoggerService,
    private passwordService: PasswordService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.passwordHint = this.passwordService.getPasswordHint();
  }

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
      !this.passwordMismatch &&
      this.passwordValid &&
      !this.isSubmitting
    );
  }

  onPasswordChange(): void {
    const result = this.passwordService.validatePassword(this.newPassword);
    this.passwordErrors = result.errors;
    this.passwordValid = result.valid;
  }

  onSubmit(): void {
    if (!this.canSubmit()) return;
    
    this.isSubmitting = true;
    this.authService
      .resetPassword(this.verificationCode, this.newPassword)
      .subscribe({
        next: (data) => {
          this.logger.success('Password reset successfully.');
          this.router.navigateByUrl('/account/login');
        },
        error: (error) => {
          this.isSubmitting = false;
          // Log the error response for debugging
          console.error('Reset Password Error:', error);
          if (error?.error?.errors) {
            this.errors = error.error.errors;
          } else if (error?.error?.message) {
            this.errors = [error.error.message];
          } else if (typeof error?.error === 'string') {
            this.errors = [error.error];
          } else {
            this.errors = ['An unexpected error occurred.'];
          }
        },
      });
  }
}
