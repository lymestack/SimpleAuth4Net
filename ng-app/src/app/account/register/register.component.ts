import { Component } from '@angular/core';
import { RestService } from '../../core/_services/rest.service';
import { SnackbarService } from '../../core/_services/snackbar.service';

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
    private rest: RestService,
    private snackbarService: SnackbarService
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
        debugger;
        // TODO: Redirect to a confirmation page.

        console.log('Done:', data);
      },
      (err: any) => {
        this.snackbarService.openSnackBar(
          'An error occurred! ' + err,
          'danger',
          'error'
        );
      }
    );
  }

  // showSuccess() {
  //   this.snackbarService.openSnackBar(
  //     'Operation successful!',
  //     'success',
  //     'check_circle'
  //   );
  // }

  // showWarning() {
  //   this.snackbarService.openSnackBar('Be cautious!', 'warning', 'warning');
  // }

  // showDanger() {
  //   this.snackbarService.openSnackBar('An error occurred!', 'danger', 'error');
  // }
}
