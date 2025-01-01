import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AccountComponent } from './account.component';
import { LoginComponent } from './login/login.component';
import { LogoutComponent } from './logout/logout.component';
import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { RegisterComponent } from './register/register.component';
import { RegisterConfirmationComponent } from './register-confirmation/register-confirmation.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { VerificationPendingComponent } from './verification-pending/verification-pending.component';
import { VerifyAccountComponent } from './verify-account/verify-account.component';
import { SetupAuthenticatorComponent } from './setup-authenticator/setup-authenticator.component';
import { UserSettingsComponent } from './user-settings/user-settings.component';
import { MicrosoftLoginCallbackComponent } from './microsoft-login-callback/microsoft-login-callback.component';

const routes: Routes = [
  {
    path: '',
    component: AccountComponent,
    data: { state: 'Accounts Home' },
  },
  {
    path: 'login',
    component: LoginComponent,
    data: { state: 'Login' },
  },
  {
    path: 'logout',
    component: LogoutComponent,
    data: { state: 'Logout' },
  },
  {
    path: 'register',
    component: RegisterComponent,
    data: { state: 'Register' },
  },
  {
    path: 'register-confirmation',
    component: RegisterConfirmationComponent,
    data: { state: 'Registration Confirmed' },
  },
  {
    path: 'forgot-password',
    component: ForgotPasswordComponent,
    data: { state: 'Forgot Passowrd' },
  },
  {
    path: 'reset-password',
    component: ResetPasswordComponent,
    data: { state: 'Reset Password' },
  },
  {
    path: 'verification-pending',
    component: VerificationPendingComponent,
    data: { state: 'Verification Pending' },
  },
  {
    path: 'verify',
    component: VerifyAccountComponent,
    data: { state: 'Verify Your Account' },
  },
  {
    path: 'verify-mfa-email',
    component: VerifyAccountComponent,
    data: { state: 'Verify Your Account (MFA via Email)' },
  },
  {
    path: 'verify-mfa-sms',
    component: VerifyAccountComponent,
    data: { state: 'Verify Your Account (MFA via SMS)' },
  },
  {
    path: 'verify-mfa-otp',
    component: VerifyAccountComponent,
    data: { state: 'Verify Your Account (MFA via OTP)' },
  },
  {
    path: 'setup-authenticator',
    component: SetupAuthenticatorComponent,
    data: { state: 'Setup Authenticator' },
  },
  {
    path: 'user-settings',
    component: UserSettingsComponent,
    data: { state: 'User Settings' },
  },
  {
    path: 'microsoft-login-callback',
    component: MicrosoftLoginCallbackComponent,
    data: { state: 'Microsoft Login Callback' },
  },

  // otherwise redirect to home
  { path: '**', redirectTo: '' },
];
@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AccountRoutingModule {}
