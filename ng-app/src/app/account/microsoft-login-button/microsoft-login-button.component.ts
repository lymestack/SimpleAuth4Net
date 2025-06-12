import { Component, Inject } from '@angular/core';
import { AppConfig } from '../../_api';
import { APP_CONFIG } from '../../core/_services/config-injection';

@Component({
  selector: 'app-microsoft-login-button',
  templateUrl: './microsoft-login-button.component.html',
  styleUrl: './microsoft-login-button.component.scss',
})
export class MicrosoftLoginButtonComponent {
  constructor(@Inject(APP_CONFIG) public config: AppConfig) {}

  loginWithMicrosoft() {
    const ssoSettings = this.config.simpleAuth.ssoProviders.find(
      (x) => x.name === 'Microsoft' && x.enabled
    )!;

    const authUrl =
      `https://login.microsoftonline.com/${ssoSettings.tenantId}/oauth2/v2.0/authorize?` +
      `client_id=${ssoSettings.appId}` +
      `&response_type=code` +
      `&redirect_uri=${encodeURIComponent(ssoSettings.redirectUri)}` +
      `&scope=${encodeURIComponent('openid profile email offline_access')}`;

    window.location.href = authUrl;
  }
}
