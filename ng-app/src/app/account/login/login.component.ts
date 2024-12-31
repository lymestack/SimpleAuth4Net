import { Component, Inject, OnInit } from '@angular/core';
import { AppConfig, LoginModel, MfaMethod } from '../../_api';
import { AuthService } from '../../core/_services/auth.service';
import { APP_CONFIG } from '../../core/_services/config-injection';
import { LoggerService } from '../../core/_services/logger.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent implements OnInit {
  model: LoginModel = new LoginModel();
  useBootstrap = false;
  enableLocalAccounts: boolean;
  enableGoogle: boolean;
  enableFacebook: boolean;
  allowRegistration: boolean;

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private auth: AuthService,
    private logger: LoggerService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.enableGoogle = this.config.enableGoogleSso;
    this.enableFacebook = this.config.enableFacebookSso;
    this.allowRegistration = this.config.allowRegistration;
    this.enableLocalAccounts = this.config.enableLocalAccounts;

    if (this.auth.isLoggedIn()) {
      this.router.navigateByUrl('/');
    }
  }

  onSubmit() {
    this.auth.userVerified(this.model.username).subscribe((data) => {
      if (!data) {
        this.router.navigateByUrl('/account/verification-pending');
        return;
      }

      this.auth.login(this.model).subscribe(
        (data: any) => {
          if (
            this.config.enableMfaViaEmail ||
            this.config.enableMfaViaSms ||
            this.config.enableMfaViaOtp
          ) {
            let verifyRoute: string;
            if (this.model.mfaMethod === MfaMethod.Email) {
              verifyRoute = '/account/verify-mfa-email';
            } else if (this.model.mfaMethod === MfaMethod.Sms) {
              verifyRoute = '/account/verify-mfa-sms';
            } else if (this.model.mfaMethod === MfaMethod.Otp) {
              verifyRoute = '/account/verify-mfa-otp';
            } else {
              this.logger.error('Invalid MFA method selected.');
              return;
            }
            this.router.navigateByUrl(verifyRoute);
          } else {
            console.log('Logged in');
            setTimeout(() => window.location.reload(), 1000);
          }
        },
        (err: any) => {
          this.logger.warning('Invalid login. Try again.');
        }
      );
    });
  }
}
