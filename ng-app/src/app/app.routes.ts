import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { AboutComponent } from './about/about.component';

const routes: Routes = [
  {
    path: '',
    component: HomeComponent,
    data: { state: 'Home', allowAnonymous: true },
  },
  {
    path: 'account',
    loadChildren: () =>
      import('./account/account.module').then((m) => m.AccountModule),
  },
  {
    path: 'auth-admin',
    loadChildren: () =>
      import('./auth-admin/auth-admin.module').then((m) => m.AuthAdminModule),
  },
  {
    path: 'about',
    component: AboutComponent,
    data: { state: 'About', allowAnonymous: true },
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
