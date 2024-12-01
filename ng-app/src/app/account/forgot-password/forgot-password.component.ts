import { Component } from '@angular/core';
import { AuthService } from '../../core/_services/auth.service';
import { LoggerService } from '../../core/_services/logger.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss',
})
export class ForgotPasswordComponent {
  email: string = '';

  constructor(
    private authService: AuthService,
    private logger: LoggerService,
    private router: Router
  ) {}

  onSubmit(): void {
    this.authService.forgotPassword(this.email).subscribe((data) => {
      this.logger.success('Verification code sent to your email.');
      this.router.navigateByUrl('/account/reset-password');
    });
  }
}
