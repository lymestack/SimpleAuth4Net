import { Component, Inject, OnInit } from '@angular/core';
import { AuthService } from '../../core/_services/auth.service';
import { LocalStorageService } from '../../core/_services/local-storage.service';
import { AppConfig } from '../../_api';
import { APP_CONFIG } from '../../core/_services/config-injection';

@Component({
    selector: 'app-logout',
    templateUrl: './logout.component.html',
    styleUrl: './logout.component.scss',
    standalone: false
})
export class LogoutComponent implements OnInit {
  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private auth: AuthService,
    private localStorage: LocalStorageService
  ) {}

  ngOnInit(): void {
    this.auth.logout().subscribe((data) => {
      window.location.href = this.config.environment.url;
    });
  }
}
