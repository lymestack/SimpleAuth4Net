/* eslint-disable @typescript-eslint/ban-ts-comment */
import { Component, Inject, NgZone, OnInit } from '@angular/core';
import { CredentialResponse, PromptMomentNotification } from 'google-one-tap';
import * as uuid from 'uuid';
import { AppConfig } from '../../_api';
import { APP_CONFIG } from '../../core/_services/config-injection';

@Component({
  selector: 'app-google-login-button',
  templateUrl: './google-login-button.component.html',
  styleUrls: ['./google-login-button.component.scss'],
})
export class GoogleLoginButtonComponent implements OnInit {
  myId = uuid.v4();

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private _ngZone: NgZone
  ) {}

  ngOnInit(): void {
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => this.initializeGoogleSignIn();
    document.head.appendChild(script);
  }

  initializeGoogleSignIn(): void {
    // @ts-ignore
    google.accounts.id.initialize({
      client_id: this.config.googleClientId,
      callback: this.handleCredentialResponse.bind(this),
      auto_select: false,
      cancel_on_tap_outside: true,
    });

    // @ts-ignore
    google.accounts.id.renderButton(
      // @ts-ignore
      document.getElementById('buttonDiv' + this.myId),
      {
        theme: 'filled_blue',
        size: 'large',
        width: 100,
      }
    );

    // @ts-ignore
    google.accounts.id.prompt((notification: PromptMomentNotification) => {});
  }

  async handleCredentialResponse(response: CredentialResponse) {
    debugger;
    // await this.currentUser.loginWithGoogle(response.credential).subscribe(
    //   (x) => {
    //     this.currentUser.setTokenValue(x.token);
    //     window.location.reload();
    //   },
    //   (error: any) => console.log('error', error)
    // );
  }
}
