import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AppUser } from '../../_api';
import { LoggerService } from '../../core/_services/logger.service';
import { RestService } from '../../core/_services/rest.service';
import { ApiSearchResults } from '../../core/api-search-results.class';
import { ResetPasswordModalComponent } from './reset-password-modal/reset-password-modal.component';
import { MatDialog } from '@angular/material/dialog';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export class UsersComponent {
  results: ApiSearchResults<AppUser>;
  loading = false;
  pageSize = 10;
  pageIndex = 0;
  searchOptions: any;

  constructor(
    private dialog: MatDialog,
    private logger: LoggerService,
    private rest: RestService,
    private router: Router
  ) {}

  ngOnInit() {
    this.onReset();
  }

  private refresh() {
    this.loading = true;
    const url = `AppUser/Search?pageIndex=${this.pageIndex}&pageSize=${this.pageSize}`;
    this.rest.postResource(url, this.searchOptions).subscribe((results) => {
      this.results = results;
      this.loading = false;
    });
  }

  onSearch() {
    this.pageIndex = 0;
    this.refresh();
  }

  onPageEvent(pageEvent: any) {
    this.pageIndex = pageEvent.pageIndex;
    this.pageSize = pageEvent.pageSize;
    this.refresh();
  }

  onEdit(user: AppUser) {
    this.router.navigateByUrl('/auth-admin/users/edit/' + user.id);
  }

  onRemove(user: AppUser) {
    let confirm = window.confirm('Are you sure you want to delete this user?');
    if (confirm) {
      this.rest.deleteResource('AppUser', user.id).subscribe(() => {
        this.logger.info('The item was deleted.');
        this.refresh();
      });
    }
  }

  onAdd() {
    this.router.navigateByUrl('/auth-admin/users/add');
  }

  onReset() {
    this.pageIndex = 0;

    this.searchOptions = {
      active: true,
      sortField: 'Username',
      sortDirection: 'ASC',
    };

    this.refresh();
  }

  onSort(sortField: string) {
    this.searchOptions.sortField = sortField;
    this.searchOptions.sortDirection =
      this.searchOptions.sortDirection === 'DESC' ? 'ASC' : 'DESC';
    this.refresh();
  }

  onResetPassword(user: AppUser) {
    this.dialog.open(ResetPasswordModalComponent, {
      width: '500px',
      data: { username: user.username },
    });
  }

  onLogOutAll() {
    let confirm = window.confirm('Are you sure you want to log out all users?');

    if (confirm) {
      this.rest.postResource('Auth/RevokeAllSessions', {}).subscribe(() => {
        this.logger.info('All users have been logged out.');
      });
    }
  }

  onRevokeSessions(user: AppUser) {
    let confirm = window.confirm(
      `Are you sure you want to revoke all sessions for ${user.username}?`
    );

    if (confirm) {
      this.rest
        .postResource(
          'Auth/RevokeAllSessionsForUser?username=' +
            encodeURIComponent(user.username),
          {}
        )
        .subscribe(() => {
          this.logger.info(
            `All sessions for ${user.username} have been revoked.`
          );
        });
    }
  }
}
