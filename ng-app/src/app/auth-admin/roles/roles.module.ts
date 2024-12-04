import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RolesComponent } from './roles.component';
import { SharedModule } from '../../shared/shared.module';
import { RoleSearchOptionsComponent } from './role-search-options/role-search-options.component';
import { RoleListComponent } from './role-list/role-list.component';
import { RoleFormComponent } from './role-form/role-form.component';
import { RolesRoutingModule } from './roles.routes';

@NgModule({
  declarations: [
    RolesComponent,
    RoleSearchOptionsComponent,
    RoleListComponent,
    RoleFormComponent,
  ],
  imports: [CommonModule, SharedModule, RolesRoutingModule],
})
export class RolesModule {}
