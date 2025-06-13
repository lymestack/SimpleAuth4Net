import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../core/_services/auth.service';
import { LoggerService } from '../../core/_services/logger.service';

@Component({
    selector: 'app-setup-authenticator',
    templateUrl: './setup-authenticator.component.html',
    styleUrls: ['./setup-authenticator.component.scss'],
    standalone: false
})
export class SetupAuthenticatorComponent implements OnInit {
  qrCodeBase64: string | null = null;
  totpSecret: string | null = null;

  constructor(
    private authService: AuthService,
    private logger: LoggerService
  ) {}

  ngOnInit(): void {
    const username = localStorage.getItem('verifyUsername');
    if (username) {
      this.authService.setupAuthenticator(username).subscribe({
        next: (data: any) => {
          this.qrCodeBase64 = data.qrCodeBase64;
          this.totpSecret = data.totpSecret;
        },
        error: (error) => {
          this.logger.error(
            'Failed to load QR code for authenticator setup.',
            error
          );
        },
      });
    } else {
      this.logger.error('No username found in local storage.');
    }
  }

  onCompleteSetup(): void {
    this.logger.success('Authenticator setup completed.');
  }
}
