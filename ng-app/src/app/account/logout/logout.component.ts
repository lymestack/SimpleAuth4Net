import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../core/_services/auth.service';
import { Router } from '@angular/router';
import { LoggerService } from '../../core/_services/logger.service';
import { LocalStorageService } from '../../core/_services/local-storage.service';

@Component({
  selector: 'app-logout',
  templateUrl: './logout.component.html',
  styleUrl: './logout.component.scss',
})
export class LogoutComponent implements OnInit {
  constructor(
    private auth: AuthService,
    private localStorage: LocalStorageService,
    private logger: LoggerService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.auth.logout();
    this.localStorage.remove('AppUser');
    this.router.navigateByUrl('/account/login');
    this.logger.success('Successfully signed out...');
  }
}
