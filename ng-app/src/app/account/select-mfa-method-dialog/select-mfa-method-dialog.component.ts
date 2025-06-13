import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { MfaMethod } from '../../_api';

@Component({
    selector: 'app-select-mfa-method-dialog',
    templateUrl: './select-mfa-method-dialog.component.html',
    styleUrl: './select-mfa-method-dialog.component.scss',
    standalone: false
})
export class SelectMfaMethodDialogComponent {
  MfaMethod = MfaMethod;
  selectedMethod: MfaMethod = MfaMethod.Email;
  rememberChoice: boolean = true;

  constructor(public dialogRef: MatDialogRef<SelectMfaMethodDialogComponent>) {}

  onCancel(): void {
    this.dialogRef.close();
  }
}
