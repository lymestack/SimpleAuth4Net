import { Component, OnInit, Input } from '@angular/core';

@Component({
    selector: 'app-role-search-options',
    templateUrl: './role-search-options.component.html',
    styleUrls: ['./role-search-options.component.scss'],
    standalone: false
})
export class RoleSearchOptionsComponent implements OnInit {
  @Input() searchOptions: any; // RoleSearchOptions;

  constructor() {}

  ngOnInit() {}
}
