import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-role-search-options',
  templateUrl: './role-search-options.component.html',
  styleUrls: ['./role-search-options.component.scss'],
})
export class RoleSearchOptionsComponent implements OnInit {
  @Input() searchOptions: any; // RoleSearchOptions;

  constructor() {}

  ngOnInit() {}
}
