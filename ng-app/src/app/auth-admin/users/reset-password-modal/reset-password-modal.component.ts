import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Clipboard } from '@angular/cdk/clipboard';
import { AuthService } from '../../../core/_services/auth.service';
import { LoggerService } from '../../../core/_services/logger.service';

@Component({
    selector: 'app-reset-password-modal',
    templateUrl: './reset-password-modal.component.html',
    standalone: false
})
export class ResetPasswordModalComponent implements OnInit {
  model: { username: string; verifyToken: string | null; newPassword: string };
  errorMessages: string[] = [];

  constructor(
    public dialogRef: MatDialogRef<ResetPasswordModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { username: string },
    private authService: AuthService,
    private clipboard: Clipboard,
    private logger: LoggerService
  ) {}

  ngOnInit(): void {
    this.model = {
      username: this.data.username,
      verifyToken: null,
      newPassword: this.generatePassword(),
    };
  }

  generatePassword(): string {
    const chars =
      'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+';
    return Array.from(
      { length: 12 },
      () => chars[Math.floor(Math.random() * chars.length)]
    ).join('');
  }

  regenerate(): void {
    this.model.newPassword = this.generatePassword();
  }

  copy(): void {
    this.clipboard.copy(this.model.newPassword);
    this.logger.info('Copied to clipboard...');
  }

  onSubmit(): void {
    this.authService
      .resetPassword(
        this.model.username,
        this.model.newPassword,
        this.model.verifyToken ?? ''
      )
      .subscribe({
        next: () => this.dialogRef.close(true),
        error: (err) => {
          this.errorMessages = err?.error?.errors || [
            'An unexpected error occurred.',
          ];
        },
      });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
