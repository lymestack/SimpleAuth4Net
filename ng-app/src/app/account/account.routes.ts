import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AccountComponent } from './account.component';
import { LoginComponent } from './login/login.component';
import { LogoutComponent } from './logout/logout.component';

const routes: Routes = [
  {
    path: '',
    component: AccountComponent,
    data: { state: 'Accounts Home' },
  },
  {
    path: 'login',
    component: LoginComponent,
    data: { state: 'Account Login' },
  },
  {
    path: 'logout',
    component: LogoutComponent,
    data: { state: 'Account Logout' },
  },

  // otherwise redirect to home
  { path: '**', redirectTo: '' },
];
@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AccountsRoutingModule {}
