import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AccountComponent } from './account.component';
import { LoginComponent } from './login/login.component';
import { LogoutComponent } from './logout/logout.component';
import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { RegisterComponent } from './register/register.component';
import { RegisterConfirmationComponent } from './register-confirmation/register-confirmation.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';

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
    data: { state: 'Reset Passowrd' },
  },

  // otherwise redirect to home
  { path: '**', redirectTo: '' },
];
@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AccountRoutingModule {}
