import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { UsersComponent } from './users.component';
import { UserFormComponent } from './user-form/user-form.component';

const routes: Routes = [
  {
    path: '',
    component: UsersComponent,
    data: { state: 'users' },
  },
  {
    path: 'add',
    component: UserFormComponent,
    data: { state: 'addingUser' },
  },
  {
    path: 'edit/:id',
    component: UserFormComponent,
    data: { state: 'editingUser' },
  },
];
@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class UsersRoutingModule {}
