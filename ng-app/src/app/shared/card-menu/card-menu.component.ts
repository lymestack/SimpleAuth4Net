import { Component, Input, OnInit, Output, EventEmitter } from '@angular/core';

export class CardMenuItem {
  title: string = '';
  icon?: string;
  routerLink?: string;
  href?: string;
  description?: string;
}

@Component({
  selector: 'app-card-menu',
  templateUrl: './card-menu.component.html',
  styleUrls: ['./card-menu.component.scss'],
})
export class CardMenuComponent implements OnInit {
  @Input() menuItems: CardMenuItem[] = [];
  @Output() menuItemClicked = new EventEmitter<CardMenuItem>();
  columnClass = 'col-md-4';
  constructor() {}

  ngOnInit(): void {
    if (this.menuItems.length > 3 && this.menuItems.length % 3 !== 0)
      this.columnClass = 'col-md-3';
  }
}
