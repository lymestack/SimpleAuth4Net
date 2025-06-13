import { Component, Inject } from '@angular/core';
import { RestService } from '../../core/_services/rest.service';
import { LoggerService } from '../../core/_services/logger.service';
import { Router } from '@angular/router';
import { AuthService } from '../../core/_services/auth.service';
import { AppConfig } from '../../_api';
import { APP_CONFIG } from '../../core/_services/config-injection';

@Component({
    selector: 'app-register',
    templateUrl: './register.component.html',
    styleUrls: ['./register.component.scss'],
    standalone: false
})
export class RegisterComponent {
  model = {
    firstName: '',
    lastName: '',
    emailAddress: '',
    username: '',
    password: '',
    confirmPassword: '',
  };

  errors: string[] = [];
  usernameAvailable = false;
  checkingUsername = false;
  checkedUsername = false;
  usernameValid = false;
  checkedComplexity = false;
  metComplexityStandard = false;

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private auth: AuthService,
    private logger: LoggerService,
    private rest: RestService,
    private router: Router
  ) {}

  onEmailAddressChanged(newEmailAddress: string) {
    this.model.username = newEmailAddress;
    this.usernameValid = validateEmail(newEmailAddress);
    if (!this.usernameValid) return;

    this.checkingUsername = true;
    let url = 'Auth/UserExists?username=' + newEmailAddress;
    this.rest.getResource(url).subscribe((data: any) => {
      this.checkedUsername = true;
      this.checkingUsername = false;
      this.usernameAvailable = !data.exists;
    });

    function validateEmail(email: any) {
      // tslint:disable-next-line:max-line-length
      const re =
        /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
      return re.test(String(email).toLowerCase());
    }
  }

  onPasswordChanged() {
    this.auth
      .checkPasswordComplexity(this.model.password)
      .subscribe((data: any) => {
        this.checkedComplexity = true;
        this.metComplexityStandard = data.success;
        this.errors = data.success ? [] : data.errors;
      });
  }

  onSubmit() {
    console.log('Registration form submitted:', this.model);
    let url = 'Auth/Register';
    this.rest.postResource(url, this.model).subscribe(
      (data: any) => {
        let route = '/account/register-confirmation';
        if (this.config.simpleAuth.requireUserVerification) {
          localStorage.setItem('verifyUsername', this.model.username);
          route = '/account/verify';
        }

        this.router.navigateByUrl(route);
        console.log('Done:', data);
      },
      (err: any) => {
        this.logger.error('An error occurred. ' + err);
      }
    );
  }
}
