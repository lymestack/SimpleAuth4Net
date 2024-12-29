import { Component, OnInit } from '@angular/core';
import { LoggerService } from '../core/_services/logger.service';

@Component({
  selector: 'app-about',
  templateUrl: './about.component.html',
  styleUrl: './about.component.scss',
})
export class AboutComponent implements OnInit {
  preferredMfaMethodExists: boolean = false;

  constructor(private logger: LoggerService) {}

  ngOnInit(): void {
    // Check if the "preferredMfaMethod" variable exists in local storage
    this.preferredMfaMethodExists =
      !!localStorage.getItem('preferredMfaMethod');
  }

  clearMfaPreference(): void {
    const confirmation = confirm(
      'Are you sure you want to clear your MFA preference?'
    );
    if (confirmation) {
      localStorage.removeItem('preferredMfaMethod');
      this.preferredMfaMethodExists = false;
      this.logger.success('MFA preference cleared successfully.');
    }
  }
}
