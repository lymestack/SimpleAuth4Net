import { Component } from '@angular/core';
import { CardMenuItem } from '../shared/card-menu/card-menu.component';

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
}
