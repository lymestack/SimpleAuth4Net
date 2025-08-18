import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Clipboard } from '@angular/cdk/clipboard';
import { AuthService } from '../../../core/_services/auth.service';
import { LoggerService } from '../../../core/_services/logger.service';
import { PasswordService } from '../../../core/_services/password.service';

@Component({
    selector: 'app-reset-password-modal',
    templateUrl: './reset-password-modal.component.html',
    standalone: false
})
export class ResetPasswordModalComponent implements OnInit {
  model: { username: string; verifyToken: string | null; newPassword: string };
  errorMessages: string[] = [];
  passwordValid: boolean = false;
  isSubmitting: boolean = false;
  passwordHint: string = '';

  constructor(
    public dialogRef: MatDialogRef<ResetPasswordModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { username: string },
    private authService: AuthService,
    private clipboard: Clipboard,
    private logger: LoggerService,
    private passwordService: PasswordService
  ) {}

  ngOnInit(): void {
    this.passwordHint = this.passwordService.getPasswordHint();
    this.model = {
      username: this.data.username,
      verifyToken: null,
      newPassword: this.generatePassword(),
    };
    this.validatePassword();
  }

  generatePassword(): string {
    return this.passwordService.generatePassword();
  }

  validatePassword(): void {
    const result = this.passwordService.validatePassword(this.model.newPassword);
    this.errorMessages = result.errors;
    this.passwordValid = result.valid;
  }

  onPasswordChange(): void {
    this.validatePassword();
  }

  regenerate(): void {
    this.model.newPassword = this.generatePassword();
    this.validatePassword();
  }

  copy(): void {
    this.clipboard.copy(this.model.newPassword);
    this.logger.info('Copied to clipboard...');
  }

  onSubmit(): void {
    if (!this.passwordValid) {
      this.validatePassword();
      return;
    }
    
    this.isSubmitting = true;
    this.authService
      .resetPassword(
        this.model.username,
        this.model.newPassword,
        this.model.verifyToken ?? ''
      )
      .subscribe({
        next: () => this.dialogRef.close(true),
        error: (err) => {
          this.isSubmitting = false;
          if (err?.error?.errors) {
            this.errorMessages = err.error.errors;
          } else if (err?.error?.message) {
            this.errorMessages = [err.error.message];
          } else if (typeof err?.error === 'string') {
            this.errorMessages = [err.error];
          } else {
            this.errorMessages = ['An unexpected error occurred.'];
          }
        },
      });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
