import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppRole } from '../../../_api';
import { LoggerService } from '../../../core/_services/logger.service';
import { RestService } from '../../../core/_services/rest.service';

@Component({
  selector: 'app-role-form',
  templateUrl: './role-form.component.html',
  styleUrls: ['./role-form.component.scss'],
})
export class RoleFormComponent implements OnInit {
  model = new AppRole();
  saving = false;

  constructor(
    private logger: LoggerService,
    private rest: RestService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit() {
    let roleId = this.route.snapshot.params['id'];
    if (roleId) {
      this.rest
        .getResource('AppRole', roleId)
        .subscribe((data) => (this.model = data));
    }
  }

  onSave() {
    this.saving = true;
    this.rest.postResource('Role', this.model).subscribe((data) => {
      this.logger.success('Role saved.');
      this.router.navigateByUrl('/auth-admin/roles');
    });
  }

  onCancel() {
    this.router.navigateByUrl('/auth-admin/roles');
  }
}
