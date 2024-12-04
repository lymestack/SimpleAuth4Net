import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { RolesComponent } from './roles.component';
import { RoleFormComponent } from './role-form/role-form.component';

const routes: Routes = [
  {
    path: '',
    component: RolesComponent,
    data: { state: 'roles' },
  },
  {
    path: 'add',
    component: RoleFormComponent,
    data: { state: 'addingRole' },
  },
  {
    path: 'edit/:id',
    component: RoleFormComponent,
    data: { state: 'editingRole' },
  },
];
@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class RolesRoutingModule {}
