import { Component, Inject } from '@angular/core';
import { AppConfig } from '../../_api';
import { APP_CONFIG } from '../../core/_services/config-injection';

@Component({
  selector: 'app-verification-pending',
  templateUrl: './verification-pending.component.html',
  styleUrl: './verification-pending.component.scss',
})
export class VerificationPendingComponent {
  googleEnabled = false;
  facebookEnabled = false;

  constructor(@Inject(APP_CONFIG) public config: AppConfig) {
    this.googleEnabled = config.enableGoogleSso;
    this.facebookEnabled = config.enableFacebookSso;
  }
}
