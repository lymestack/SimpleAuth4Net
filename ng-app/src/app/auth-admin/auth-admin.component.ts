import { Component } from '@angular/core';
import { CardMenuItem } from '../shared/card-menu/card-menu.component';

@Component({
  selector: 'app-auth-admin',
  templateUrl: './auth-admin.component.html',
  styleUrl: './auth-admin.component.scss',
})
export class AuthAdminComponent {
  menuItems: CardMenuItem[] = [
    {
      title: 'Users',
      description: 'Manage users and their active sessions.',
      icon: 'fa fa-user',
      routerLink: '/auth-admin/users',
    },
    {
      title: 'Roles',
      description: 'Manage roles.',
      icon: 'fa fa-users',
      routerLink: '/auth-admin/roles',
    },
  ];
}
