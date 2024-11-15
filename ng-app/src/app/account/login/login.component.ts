import { Component } from '@angular/core';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  model: LoginModel = new LoginModel();

  onSubmit() {
    console.log('Login submitted', this.model);
  }
}

export class LoginModel {
  userName: string = '';
  password: string = '';
}
