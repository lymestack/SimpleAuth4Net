import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { AppRole } from '../../../_api';

@Component({
    selector: 'app-role-list',
    templateUrl: './role-list.component.html',
    styleUrls: ['./role-list.component.scss'],
    standalone: false
})
export class RoleListComponent implements OnInit {
  @Input() roles: AppRole[];
  @Input() sortField: string;
  @Input() sortDirection: string;
  @Output() edit = new EventEmitter<AppRole>();
  @Output() remove = new EventEmitter<AppRole>();
  @Output() sort = new EventEmitter<string>();

  constructor() {}

  ngOnInit() {}
}
