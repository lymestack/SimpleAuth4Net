import { Component, Inject } from '@angular/core';
import { CardMenuItem } from '../shared/card-menu/card-menu.component';
import { AppConfig } from '../_api';
import { APP_CONFIG } from '../core/_services/config-injection';
import { RestService } from '../core/_services/rest.service';
import { LoggerService } from '../core/_services/logger.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  menuItems: CardMenuItem[] = [
    {
      title: 'Home',
      description: 'You are here',
      icon: 'fa fa-home',
    },
  ];

  constructor(
    @Inject(APP_CONFIG) public config: AppConfig,
    private logger: LoggerService,
    private rest: RestService
  ) {}

  testSecureEndpoint() {
    this.rest.getResource('GetColorList').subscribe(
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
