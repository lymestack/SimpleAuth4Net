import { Component } from '@angular/core';
import { RestService } from '../../core/_services/rest.service';
import { LoggerService } from '../../core/_services/logger.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
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

  usernameAvailable = false;
  checkingUsername = false;
  checkedUsername = false;
  usernameValid = false;

  // Replace with your reCAPTCHA site key
  captchaSiteKey = 'YOUR_RECAPTCHA_SITE_KEY';
  captchaResolved = true;
  isCaptchaEnabled = false;

  constructor(
    private logger: LoggerService,
    private rest: RestService,
    private router: Router
  ) {}

  onCaptchaResolved(captchaResponse: any) {
    console.log('Captcha resolved:', captchaResponse);
    this.captchaResolved = !!captchaResponse;
  }

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

  onSubmit() {
    if (this.isCaptchaEnabled && !this.captchaResolved) {
      console.error('CAPTCHA must be resolved.');
      return;
    }

    console.log('Registration form submitted:', this.model);
    let url = 'Auth/Register';
    this.rest.postResource(url, this.model).subscribe(
      (data: any) => {
        this.router.navigateByUrl('/account/register-confirmation');

        console.log('Done:', data);
      },
      (err: any) => {
        this.logger.error('An error occurred. ' + err);
      }
    );
  }

  showSuccess() {
    this.logger.success('Hey!');
  }

  showWarning() {
    this.logger.warning('Hey!');
  }

  showDanger() {
    this.logger.error('Hey!');
  }
}
