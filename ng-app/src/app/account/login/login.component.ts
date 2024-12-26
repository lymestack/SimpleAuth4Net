import { Component, Inject, OnInit } from '@angular/core';
import { AppConfig, LoginModel } from '../../_api';
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
  allowRegistration: boolean;

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private auth: AuthService,
    private logger: LoggerService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.enableGoogle = this.config.enableGoogle;
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
          if (this.config.enableMfaViaEmail) {
            let verifyRoute =
              this.model.mfaMethod == 1
                ? '/account/verify-mfa-email'
                : '/account/verify-mfa-sms';
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
