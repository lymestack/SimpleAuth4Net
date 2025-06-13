import { Component, Inject, OnInit } from '@angular/core';
import { CardMenuItem } from '../shared/card-menu/card-menu.component';
import { AppConfig, AppUser } from '../_api';
import { APP_CONFIG } from '../core/_services/config-injection';
import { RestService } from '../core/_services/rest.service';
import { LoggerService } from '../core/_services/logger.service';
import { CurrentUserService } from '../core/_services/current-user.service';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrl: './home.component.scss',
    standalone: false
})
export class HomeComponent implements OnInit {
  appUser: AppUser;
  loggedIn: boolean | undefined = undefined;

  menuItems: CardMenuItem[] = [
    {
      title: 'Home',
      description: 'You are here',
      icon: 'fa fa-home',
    },
    {
      title: 'About',
      description: 'A brief explanation and link to docs',
      icon: 'fa fa-info-circle',
      routerLink: '/about',
    },
  ];

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private currentUser: CurrentUserService,
    private logger: LoggerService,
    private rest: RestService
  ) {}

  ngOnInit(): void {
    this.loggedIn = this.currentUser.isLoggedIn();

    if (this.currentUser.isInRole('Admin')) {
      this.menuItems.push({
        title: 'Admin',
        description: 'Manage Users and Roles',
        icon: 'fa fa-shield',
        routerLink: '/auth-admin',
      });
    }

    this.currentUser.getAppUser().subscribe((data: AppUser) => {
      this.appUser = data;
    });
  }

  testSecureEndpoint() {
    this.rest.getResource('Secure/GetColorList').subscribe(
      (data) => {
        console.log('Success', data);
        this.logger.success('That endpoint worked!');
      },
      (err) => {
        console.log('Error', err);
        this.logger.error('That endpoint threw an error.');
      }
    );
  }
}
