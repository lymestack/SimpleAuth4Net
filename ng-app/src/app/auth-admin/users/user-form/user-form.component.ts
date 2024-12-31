import { Component, OnInit } from '@angular/core';
import { MatSelectionListChange } from '@angular/material/list';
import { ActivatedRoute, Router } from '@angular/router';
import { AppUser, AppRole } from '../../../_api';
import { LoggerService } from '../../../core/_services/logger.service';
import { RestService } from '../../../core/_services/rest.service';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss'],
})
export class UserFormComponent implements OnInit {
  rolesChanged = false;
  model = new AppUser();
  roles: AppRole[];
  saving = false;

  usernameValid: boolean;
  checkingUsername: boolean;
  userFormService: any;
  checkedUsername: boolean;
  usernameAvailable: boolean;

  constructor(
    private logger: LoggerService,
    private rest: RestService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit() {
    let AppUserId = this.route.snapshot.params['id'];
    if (AppUserId) {
      this.rest
        .getResource('AppUser', AppUserId)
        .subscribe((data) => (this.model = data));
    }

    this.rest.getResource('AppRole').subscribe((data) => (this.roles = data));
  }

  onSave() {
    this.saving = true;
    this.rest.postResource('AppUser', this.model).subscribe((data) => {
      this.logger.success('User saved.');
      this.router.navigateByUrl('/auth-admin/users');
    });
  }

  onCancel() {
    this.router.navigateByUrl('/auth-admin/users');
  }

  isFormValid(): boolean {
    if (this.model.roles.length == 0) return false;
    if (this.usernameValid) return true;
    return true;
  }

  onRoleChanged(event: MatSelectionListChange) {
    this.rolesChanged = true;
    if (event.options[0].selected) {
      this.model.roles.push(event.options[0].value);
    } else {
      let i = this.model.roles.indexOf(event.options[0].value);
      this.model.roles.splice(i, 1);
    }
  }

  onEmailAddressChanged(newEmailAddress: string) {
    this.model.username = newEmailAddress;
    this.usernameValid = validateEmail(newEmailAddress);
    if (!this.usernameValid) return;

    this.checkingUsername = true;
    let url = 'Auth/UserExists?username=' + newEmailAddress;
    this.rest.getResource(url).subscribe((data: any) => {
      this.checkedUsername = true;
      this.checkingUsername = false;
      this.usernameAvailable = !data.exists;
    });

    function validateEmail(email: string) {
      // tslint:disable-next-line:max-line-length
      const re =
        /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
      return re.test(String(email).toLowerCase());
    }
  }
}
