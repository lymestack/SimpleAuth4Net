import { Component, OnInit, Input } from '@angular/core';

@Component({
    selector: 'app-user-search-options',
    templateUrl: './user-search-options.component.html',
    styleUrls: ['./user-search-options.component.scss'],
    standalone: false
})
export class UserSearchOptionsComponent implements OnInit {
  @Input() searchOptions: any;

  constructor() {}

  ngOnInit() {}
}
