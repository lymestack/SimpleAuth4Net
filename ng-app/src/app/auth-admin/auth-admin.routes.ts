import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

const routes: Routes = [
  //   {
  //     path: '',
  //     component: AdminComponent,
  //     data: { state: 'AdminHome' },
  //   },
  {
    path: 'roles',
    loadChildren: () =>
      import('./roles/roles.module').then((m) => m.RolesModule),
  },

  {
    path: 'users',
    loadChildren: () =>
      import('./users/users.module').then((m) => m.UsersModule),
  },

  // otherwise redirect to home
  { path: '**', redirectTo: '' },
];
@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AuthAdminRoutingModule {}
