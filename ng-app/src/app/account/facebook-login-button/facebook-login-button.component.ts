import { Component } from '@angular/core';
import { AuthService } from '../../core/_services/auth.service';
import { Router } from '@angular/router';

declare const FB: any;

@Component({
    selector: 'app-facebook-login-button',
    templateUrl: './facebook-login-button.component.html',
    styleUrl: './facebook-login-button.component.scss',
    standalone: false
})
export class FacebookLoginButtonComponent {
  constructor(private authService: AuthService, private router: Router) {}

  async login() {
    FB.login(
      async (result: any) => {
        await this.authService
          .loginWithFacebook(result.authResponse.accessToken)
          .subscribe(
            (x: any) => {
              this.router.navigate(['/logout']);
            },
            (error: any) => {
              console.log(error);
            }
          );
      },
      { scope: 'email' }
    );
  }
}
