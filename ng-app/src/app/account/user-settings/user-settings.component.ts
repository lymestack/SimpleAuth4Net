import { Component, Inject, OnInit } from '@angular/core';
import { AuthService } from '../../core/_services/auth.service';
import { LoggerService } from '../../core/_services/logger.service';
import { AppConfig, AppUser } from '../../_api';
import { APP_CONFIG } from '../../core/_services/config-injection';

@Component({
  selector: 'app-user-settings',
  templateUrl: './user-settings.component.html',
  styleUrls: ['./user-settings.component.scss'],
})
export class UserSettingsComponent implements OnInit {
  user: AppUser;

  otpEnabled = false;

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private authService: AuthService,
    private logger: LoggerService
  ) {}

  ngOnInit(): void {
    this.otpEnabled = this.config.simpleAuth.enableMfaViaOtp;

    this.authService.getUserProfile().subscribe({
      next: (data: AppUser) => {
        this.user = data;
      },
      error: (err: any) => {
        this.logger.error('Failed to load user profile', err);
      },
    });
  }

  onSubmit(): void {
    this.authService.updateUserProfile(this.user).subscribe({
      next: () => {
        this.logger.success('Profile updated successfully!');
      },
      error: (err) => {
        this.logger.error('Failed to update profile', err);
      },
    });
  }
}
