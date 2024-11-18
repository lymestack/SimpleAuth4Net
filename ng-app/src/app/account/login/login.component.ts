import { Component, Inject, OnInit } from '@angular/core';
import { AppConfig, LoginModel } from '../../_api';
import { AuthService } from '../../core/_services/auth.service';
import { APP_CONFIG } from '../../core/_services/config-injection';
import { LoggerService } from '../../core/_services/logger.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent implements OnInit {
  model: LoginModel = new LoginModel();
  useBootstrap = false;

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private auth: AuthService,
    private logger: LoggerService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.router.navigateByUrl('/');
    }
  }

  onSubmit() {
    this.auth.login(this.model).subscribe(
      (data: any) => {
        console.log('Logged in');
        this.router.navigateByUrl('/');
      },
      (err: any) => {
        this.logger.warning('Invalid login. Try again.');
      }
    );
  }
}
