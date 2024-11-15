import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoginComponent } from './login/login.component';
import { LogoutComponent } from './logout/logout.component';
import { SharedModule } from '../shared/shared.module';
import { AccountComponent } from './account.component';

@NgModule({
  declarations: [LoginComponent, LogoutComponent, AccountComponent],
  imports: [CommonModule, SharedModule],
})
export class AccountModule {}
