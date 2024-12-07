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
import { RegisterConfirmationComponent } from './register-confirmation/register-confirmation.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { VerificationPendingComponent } from './verification-pending/verification-pending.component';
import { VerifyAccountComponent } from './verify-account/verify-account.component';

@NgModule({
  declarations: [
    LoginComponent,
    LogoutComponent,
    AccountComponent,
    RegisterComponent,
    ForgotPasswordComponent,
    GoogleLoginButtonComponent,
    UsernameEmailInputComponent,
    RegisterConfirmationComponent,
    ResetPasswordComponent,
    VerificationPendingComponent,
    VerifyAccountComponent,
  ],
  imports: [CommonModule, SharedModule, AccountRoutingModule],
  exports: [UsernameEmailInputComponent],
})
export class AccountModule {}
