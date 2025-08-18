import { Component, OnInit } from '@angular/core';
import { MatSelectionListChange } from '@angular/material/list';
import { ActivatedRoute, Router } from '@angular/router';
import { AppUser, AppRole } from '../../../_api';
import { Clipboard } from '@angular/cdk/clipboard';
import { LoggerService } from '../../../core/_services/logger.service';
import { RestService } from '../../../core/_services/rest.service';
import { PasswordService } from '../../../core/_services/password.service';

@Component({
    selector: 'app-user-form',
    templateUrl: './user-form.component.html',
    styleUrls: ['./user-form.component.scss'],
    standalone: false
})
export class UserFormComponent implements OnInit {
  rolesChanged = false;
  model = new AppUser();
  roles: AppRole[];
  saving = false;
  passwordHint: string = '';

  usernameValid: boolean;
  checkingUsername: boolean;
  userFormService: any;
  checkedUsername: boolean;
  usernameAvailable: boolean;

  constructor(
    private clipboard: Clipboard,
    private logger: LoggerService,
    private rest: RestService,
    private route: ActivatedRoute,
    private router: Router,
    private passwordService: PasswordService
  ) {}

  ngOnInit() {
    this.passwordHint = this.passwordService.getPasswordHint();
    this.model.newPassword = this.generatePassword();

    let AppUserId = this.route.snapshot.params['id'];
    if (AppUserId) {
      this.rest.getResource('AppUser', AppUserId).subscribe((data) => {
        this.model = data;
      });
    }

    this.rest.getResource('AppRole').subscribe((data) => (this.roles = data));
  }

  onSave() {
    this.saving = true;
    this.rest.postResource('AppUser', this.model).subscribe({
      next: (data) => {
        this.logger.success('User saved.');
        this.router.navigateByUrl('/auth-admin/users');
      },
      error: (error) => {
        this.saving = false;
        if (error.error?.error === 'USERNAME_EXISTS') {
          this.logger.error('Username already exists. Please choose a different username.');
        } else if (error.error?.error === 'EMAIL_EXISTS') {
          this.logger.error('Email address already exists. Please use a different email.');
        } else if (error.error?.message) {
          this.logger.error(error.error.message);
        } else {
          this.logger.error('Failed to save user. Please try again.');
        }
      }
    });
  }

  generatePassword(): string {
    return this.passwordService.generatePassword();
  }

  onGenerateNewPassword() {
    this.model.newPassword = this.generatePassword();
  }

  onCopyToClipboard() {
    this.clipboard.copy(this.model.newPassword);
    this.logger.info('Copied to clipboard...');
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

  rolesSelected(): boolean {
    return !!this.model.roles.length;
  }

  onNamePartChanged() {
    let retVal = this.model.firstName;
    if (this.model.lastName) retVal += ' ' + this.model.lastName;
  }

  onRoleToggle(roleName: string, checked: boolean) {
    const roles = this.model.roles;
    if (checked && !roles.includes(roleName)) {
      roles.push(roleName);
    } else if (!checked) {
      this.model.roles = roles.filter((r) => r !== roleName);
    }
  }
}
