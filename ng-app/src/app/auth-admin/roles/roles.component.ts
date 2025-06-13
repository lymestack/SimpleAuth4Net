import { Component } from '@angular/core';
import { AppRole } from '../../_api';
import { Router } from '@angular/router';
import { LoggerService } from '../../core/_services/logger.service';
import { RestService } from '../../core/_services/rest.service';
import { ApiSearchResults } from '../../core/api-search-results.class';

@Component({
    selector: 'app-roles',
    templateUrl: './roles.component.html',
    styleUrl: './roles.component.scss',
    standalone: false
})
export class RolesComponent {
  results: ApiSearchResults<AppRole>;
  searchOptions: any = { sortField: 'Name', sortDirection: 'ASC' }; // Api.RoleSearchOptions;
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
    const url = `AppRole/Search?pageIndex=${this.pageIndex}&pageSize=${this.pageSize}`;
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

  onEdit(role: AppRole) {
    this.router.navigateByUrl('/auth-admin/roles/edit/' + role.id);
  }

  onRemove(role: AppRole) {
    let userConfirmed = window.confirm(
      'Are you sure you want to delete this role?'
    );

    if (userConfirmed) {
      this.rest.deleteResource('AppRole', role.id).subscribe(() => {
        this.logger.info('The item was deleted.');
        this.refresh();
      });
    }
  }

  onAdd() {
    this.router.navigateByUrl('/auth-admin/roles/add');
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
