import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { AppUser } from '../../../_api';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss'],
})
export class UserListComponent implements OnInit {
  @Input() users: AppUser[];
  @Input() sortField: string;
  @Input() sortDirection: string;
  @Output() edit = new EventEmitter<AppUser>();
  @Output() remove = new EventEmitter<AppUser>();
  @Output() sort = new EventEmitter<string>();
  @Output() resetPassword = new EventEmitter<AppUser>();
  @Output() revokeSessions = new EventEmitter<AppUser>();

  constructor() {}

  ngOnInit() {}
}
