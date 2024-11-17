import { Component, Inject } from '@angular/core';
import { AppConfig, LoginModel } from '../../_api';
import { AuthService } from '../../core/_services/auth.service';
import { APP_CONFIG } from '../../core/_services/config-injection';
@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  model: LoginModel = new LoginModel();
  useBootstrap = false;

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private auth: AuthService
  ) {}

  onSubmit() {
    // this.auth.login(this.model).subscribe((data: any) => {
    // })
  }
}
