import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UsersComponent } from './users.component';
import { SharedModule } from '../../shared/shared.module';
import { UserListComponent } from './user-list/user-list.component';
import { UserFormComponent } from './user-form/user-form.component';
import { UserSearchOptionsComponent } from './user-search-options/user-search-options.component';
import { AccountModule } from '../../account/account.module';
import { UsersRoutingModule } from './users.routes';
import { ResetPasswordModalComponent } from './reset-password-modal/reset-password-modal.component';

@NgModule({
  declarations: [
    UsersComponent,
    UserListComponent,
    UserFormComponent,
    UserSearchOptionsComponent,
    ResetPasswordModalComponent,
  ],
  imports: [CommonModule, SharedModule, UsersRoutingModule, AccountModule],
})
export class UsersModule {}
