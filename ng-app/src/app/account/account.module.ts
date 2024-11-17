import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoginComponent } from './login/login.component';
import { LogoutComponent } from './logout/logout.component';
import { SharedModule } from '../shared/shared.module';
import { AccountComponent } from './account.component';
import { AccountRoutingModule } from './account.routes';
import { RegisterComponent } from './register/register.component';
import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { GoogleLoginButtonComponent } from './google-login-button/google-login-button.component';
import { UsernameEmailInputComponent } from './username-email-input/username-email-input.component';

@NgModule({
  declarations: [
    LoginComponent,
    LogoutComponent,
    AccountComponent,
    RegisterComponent,
    ForgotPasswordComponent,
    GoogleLoginButtonComponent,
    UsernameEmailInputComponent,
  ],
  imports: [CommonModule, SharedModule, AccountRoutingModule],
})
export class AccountModule {}
