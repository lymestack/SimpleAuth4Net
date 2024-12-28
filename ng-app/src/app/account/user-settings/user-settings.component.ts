import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../core/_services/auth.service';
import { LoggerService } from '../../core/_services/logger.service';

@Component({
  selector: 'app-user-settings',
  templateUrl: './user-settings.component.html',
  styleUrls: ['./user-settings.component.scss'],
})
export class UserSettingsComponent implements OnInit {
  user = {
    firstName: '',
    lastName: '',
    emailAddress: '',
  };

  constructor(
    private authService: AuthService,
    private logger: LoggerService
  ) {}

  ngOnInit(): void {
    this.authService.getUserProfile().subscribe({
      next: (data: any) => {
        this.user = {
          firstName: data.firstName,
          lastName: data.lastName,
          emailAddress: data.emailAddress,
        };
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
