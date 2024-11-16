import { Component } from '@angular/core';
import { RestService } from '../../core/_services/rest.service';

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
  captchaResolved = false;
  isCaptchaEnabled = false;

  constructor(private rest: RestService) {}

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
    this.rest.getResource(url).subscribe((userExists: any) => {
      this.checkedUsername = true;
      this.checkingUsername = false;
      this.usernameAvailable = !userExists;
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
    // Add your registration logic here
  }
}
