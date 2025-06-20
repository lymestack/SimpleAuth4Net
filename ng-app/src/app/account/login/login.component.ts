import { Component, Inject, OnInit } from '@angular/core';
import {
  AppConfig,
  LoginModel,
  MfaMethod,
  SimpleAuthSettings,
} from '../../_api';
import { AuthService } from '../../core/_services/auth.service';
import { APP_CONFIG } from '../../core/_services/config-injection';
import { LoggerService } from '../../core/_services/logger.service';
import { Router } from '@angular/router';
import { CurrentUserService } from '../../core/_services/current-user.service';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrl: './login.component.scss',
    standalone: false
})
export class LoginComponent implements OnInit {
  model: LoginModel = new LoginModel();
  useBootstrap = false;
  enableLocalAccounts: boolean;
  enableGoogle: boolean;
  enableFacebook: boolean;
  allowRegistration: boolean;
  enableMicrosoft: boolean;
  simpleAuthSettings: SimpleAuthSettings;

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private auth: AuthService,
    private currentUser: CurrentUserService,
    private logger: LoggerService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.simpleAuthSettings = this.config.simpleAuth;
    this.enableGoogle = this.isSsoEnabled('Google');
    this.enableFacebook = this.isSsoEnabled('Facebook');
    this.enableMicrosoft = this.isSsoEnabled('Microsoft');
    this.allowRegistration = this.simpleAuthSettings.allowRegistration;
    this.enableLocalAccounts = this.simpleAuthSettings.enableLocalAccounts;

    if (this.auth.isLoggedIn()) {
      this.router.navigateByUrl('/');
    }
  }

  onSubmit() {
    if (this.simpleAuthSettings.requireUserVerification) {
      this.auth.userVerified(this.model.username).subscribe((data) => {
        if (!data) {
          this.router.navigateByUrl('/account/verification-pending');
          return;
        }

        this.performLogin();
      });
    } else {
      this.performLogin();
    }
  }

  private isSsoEnabled(provider: string): boolean {
    const providerSettings = this.simpleAuthSettings.ssoProviders.find(
      (x) => x.name === provider
    );

    return providerSettings?.enabled ?? false;
  }

  private performLogin() {
    this.auth.login(this.model).subscribe(
      (data: any) => {
        if (
          this.simpleAuthSettings.enableMfaViaEmail ||
          this.simpleAuthSettings.enableMfaViaSms ||
          this.simpleAuthSettings.enableMfaViaOtp
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
          this.currentUser.getAppUser(true).subscribe((data) => {
            console.log('Logged in');
            setTimeout(() => {
              window.location.reload();
            }, 250);
          });
        }
      },
      (err: any) => {
        this.logger.warning('Invalid login. Try again.');
      }
    );
  }
}
