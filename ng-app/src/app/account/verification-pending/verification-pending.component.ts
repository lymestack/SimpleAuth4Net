import { Component, Inject } from '@angular/core';
import { AppConfig, SimpleAuthSettings } from '../../_api';
import { APP_CONFIG } from '../../core/_services/config-injection';

@Component({
    selector: 'app-verification-pending',
    templateUrl: './verification-pending.component.html',
    styleUrl: './verification-pending.component.scss',
    standalone: false
})
export class VerificationPendingComponent {
  googleEnabled = false;
  facebookEnabled = false;

  constructor(@Inject(APP_CONFIG) public config: AppConfig) {
    const googleSsoSettings = this.config.simpleAuth.ssoProviders.find(
      (x) => x.name === 'Google'
    );
    const facebookSsoSettings = this.config.simpleAuth.ssoProviders.find(
      (x) => x.name === 'Facebook'
    );
    this.googleEnabled = googleSsoSettings?.enabled ?? false;
    this.facebookEnabled = facebookSsoSettings?.enabled ?? false;
  }
}
