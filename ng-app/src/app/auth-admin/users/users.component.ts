import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AppUser } from '../../_api';
import { LoggerService } from '../../core/_services/logger.service';
import { RestService } from '../../core/_services/rest.service';
import { ApiSearchResults } from '../../core/api-search-results.class';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export class UsersComponent {
  results: ApiSearchResults<AppUser>;
  searchOptions: any = { sortField: 'Username', sortDirection: 'ASC' };
  loading = false;
  pageSize = 10;
  pageIndex = 0;

  constructor(
    private logger: LoggerService,
    private rest: RestService,
    private router: Router
  ) {}

  ngOnInit() {
    this.refresh();
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
      this.rest.deleteResource('User', user.id).subscribe(() => {
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
    this.searchOptions = {};
    this.refresh();
  }

  onSort(sortField: string) {
    this.searchOptions.sortField = sortField;
    this.searchOptions.sortDirection =
      this.searchOptions.sortDirection === 'DESC' ? 'ASC' : 'DESC';
    this.refresh();
  }
}
