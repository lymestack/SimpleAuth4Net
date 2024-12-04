import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedModule } from '../shared/shared.module';
import { AuthAdminRoutingModule } from './auth-admin.routes';

@NgModule({
  declarations: [],
  imports: [CommonModule, AuthAdminRoutingModule, SharedModule],
})
export class AuthAdminModule {}
